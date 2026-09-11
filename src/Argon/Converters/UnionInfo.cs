// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

/// <summary>
/// Reflected metadata for a C# union type.
/// </summary>
/// <remarks>
/// The C# compiler applies <c>System.Runtime.CompilerServices.UnionAttribute</c> to a union
/// declaration, but the attribute definition comes from either the target framework or a polyfill
/// compiled into the declaring assembly. Every assembly can therefore hold its own distinct copy of
/// the attribute, so the marker is matched by full name rather than through a compile time type
/// reference. For the same reason the active case is read through the conventional public
/// <c>object Value</c> property instead of through <c>IUnion</c>, which the compiler documents as
/// optional.
/// </remarks>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
class UnionInfo
{
    const string unionAttributeName = "UnionAttribute";
    const string unionAttributeFullName = "System.Runtime.CompilerServices.UnionAttribute";
    const string valuePropertyName = "Value";

    static readonly ThreadSafeStore<Type, bool> isUnionCache = new(DetectUnion);
    static readonly ThreadSafeStore<Type, UnionInfo> infoCache = new(Create);

    /// <summary>
    /// The JSON shape a union case serializes to, used to match an incoming payload to a case.
    /// </summary>
    public enum Shape
    {
        Object,
        Array,
        String,
        Number,
        Boolean
    }

    public record Case(Type CaseType, bool AcceptsNull, ObjectConstructor Constructor);

    public static bool IsUnion(Type type) =>
        isUnionCache.Get(type);

    public static UnionInfo Get(Type type) =>
        infoCache.Get(type);

    static bool DetectUnion(Type type)
    {
        foreach (var data in type.GetCustomAttributesData())
        {
            var attributeType = data.AttributeType;
            // Name is compared before FullName because FullName allocates for constructed and nested types
            if (attributeType.Name == unionAttributeName &&
                attributeType.FullName == unionAttributeFullName)
            {
                return true;
            }
        }

        return false;
    }

    static UnionInfo Create(Type unionType)
    {
        var cases = new List<Case>();
        var nullability = new NullabilityInfoContext();

        foreach (var constructor in unionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != 1)
            {
                continue;
            }

            var parameter = parameters[0];
            var caseType = parameter.ParameterType;
            if (caseType.IsByRef ||
                caseType.GetCustomAttribute<CompilerGeneratedAttribute>() != null ||
                cases.Any(_ => _.CaseType == caseType))
            {
                continue;
            }

            cases.Add(
                new(
                    caseType,
                    AcceptsNull(parameter, caseType, nullability),
                    DelegateFactory.CreateParameterizedConstructor(constructor)));
        }

        SortMostDerivedFirst(cases);

        return new(unionType, cases, FindValueAccessor(unionType));
    }

    static bool AcceptsNull(ParameterInfo parameter, Type caseType, NullabilityInfoContext nullability)
    {
        if (Nullable.GetUnderlyingType(caseType) != null)
        {
            return true;
        }

        if (caseType.IsValueType)
        {
            return false;
        }

        return nullability.Create(parameter).WriteState != NullabilityState.NotNull;
    }

    // a case whose type derives from another case's type has to be considered first, so that the
    // nearest declared case wins when a value satisfies more than one of them
    static void SortMostDerivedFirst(List<Case> cases)
    {
        for (var index = 0; index < cases.Count; index++)
        {
            for (var candidate = index + 1; candidate < cases.Count; candidate++)
            {
                var current = cases[index].CaseType;
                var other = cases[candidate].CaseType;
                if (current != other &&
                    current.IsAssignableFrom(other))
                {
                    (cases[index], cases[candidate]) = (cases[candidate], cases[index]);
                }
            }
        }
    }

    static Func<object, object?>? FindValueAccessor(Type unionType)
    {
        var property = unionType.GetProperty(valuePropertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null ||
            property.PropertyType != typeof(object) ||
            property.GetMethod is not {IsPublic: true} ||
            property.GetIndexParameters().Length > 0)
        {
            return null;
        }

        return DelegateFactory.CreateGet<object>(property);
    }

    readonly Type unionType;
    readonly Func<object, object?>? valueAccessor;

    UnionInfo(Type unionType, IReadOnlyList<Case> cases, Func<object, object?>? valueAccessor)
    {
        this.unionType = unionType;
        this.valueAccessor = valueAccessor;
        Cases = cases;
        NullCase = cases.FirstOrDefault(_ => _.AcceptsNull);
    }

    public IReadOnlyList<Case> Cases { get; }

    /// <summary>
    /// The first case that can hold a null payload, or <c>null</c> when no case accepts one.
    /// </summary>
    public Case? NullCase { get; }

    public object? GetValue(object union)
    {
        if (valueAccessor == null)
        {
            throw new JsonSerializationException($"Union type '{unionType}' does not expose a public 'object Value' property so its value cannot be read.");
        }

        return valueAccessor(union);
    }

    public Case ResolveCase(JToken token, JsonSerializer serializer)
    {
        var shape = GetShape(token.Type);

        var candidates = new List<Case>();
        foreach (var unionCase in Cases)
        {
            if (GetShape(unionCase.CaseType, serializer) == shape)
            {
                candidates.Add(unionCase);
            }
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        var shapeName = shape.ToString().ToLowerInvariant();

        if (candidates.Count == 0)
        {
            throw new JsonSerializationException($"No case of union type '{unionType}' is serialized as a JSON {shapeName}. Cases: {CaseNames(Cases)}.");
        }

        if (token is JObject payload)
        {
            return ResolveStructurally(payload, candidates, serializer);
        }

        throw new JsonSerializationException($"Multiple cases of union type '{unionType}' are serialized as a JSON {shapeName} so the payload is ambiguous. Candidates: {CaseNames(candidates)}.");
    }

    // several cases share the object shape, so they are told apart by comparing the payload's
    // property names against each candidate's resolved properties
    Case ResolveStructurally(JObject payload, List<Case> candidates, JsonSerializer serializer)
    {
        Case? best = null;
        var bestMatched = -1;
        var tied = false;

        foreach (var unionCase in candidates)
        {
            if (serializer.ResolveContract(unionCase.CaseType) is not JsonObjectContract contract)
            {
                continue;
            }

            var matched = 0;
            var unknown = false;
            foreach (var property in payload.Properties())
            {
                if (contract.Properties.GetClosestMatchProperty(property.Name) == null)
                {
                    unknown = true;
                    break;
                }

                matched++;
            }

            if (unknown)
            {
                continue;
            }

            if (matched > bestMatched)
            {
                best = unionCase;
                bestMatched = matched;
                tied = false;
            }
            else if (matched == bestMatched)
            {
                tied = true;
            }
        }

        if (best == null)
        {
            throw new JsonSerializationException($"No case of union type '{unionType}' has properties matching the JSON object. Candidates: {CaseNames(candidates)}.");
        }

        if (tied)
        {
            throw new JsonSerializationException($"The JSON object matches multiple cases of union type '{unionType}' equally well. Candidates: {CaseNames(candidates)}.");
        }

        return best;
    }

    static string CaseNames(IReadOnlyList<Case> cases) =>
        string.Join(", ", cases.Select(_ => $"'{_.CaseType}'"));

    static Shape GetShape(JTokenType tokenType) =>
        tokenType switch
        {
            JTokenType.Array => Shape.Array,
            JTokenType.Integer or JTokenType.Float => Shape.Number,
            JTokenType.Boolean => Shape.Boolean,
            JTokenType.String or JTokenType.Date or JTokenType.Guid or JTokenType.Uri or JTokenType.TimeSpan or JTokenType.Bytes => Shape.String,
            _ => Shape.Object
        };

    static Shape GetShape(Type caseType, JsonSerializer serializer)
    {
        var contract = serializer.ResolveContract(caseType);
        return contract.ContractType switch
        {
            JsonContractType.Array => Shape.Array,
            JsonContractType.Object or JsonContractType.Dictionary or JsonContractType.Dynamic => Shape.Object,
            JsonContractType.String => Shape.String,
            _ => GetPrimitiveShape(contract.NonNullableUnderlyingType)
        };
    }

    static Shape GetPrimitiveShape(Type type)
    {
        if (type.IsEnum)
        {
            return Shape.Number;
        }

        return ConvertUtils.GetTypeCode(type) switch
        {
            PrimitiveTypeCode.Boolean or PrimitiveTypeCode.BooleanNullable => Shape.Boolean,
            PrimitiveTypeCode.Char or PrimitiveTypeCode.CharNullable or
                PrimitiveTypeCode.DateTime or PrimitiveTypeCode.DateTimeNullable or
                PrimitiveTypeCode.DateTimeOffset or PrimitiveTypeCode.DateTimeOffsetNullable or
                PrimitiveTypeCode.Guid or PrimitiveTypeCode.GuidNullable or
                PrimitiveTypeCode.TimeSpan or PrimitiveTypeCode.TimeSpanNullable or
                PrimitiveTypeCode.Uri or PrimitiveTypeCode.String or
                PrimitiveTypeCode.Bytes => Shape.String,
            _ => Shape.Number
        };
    }
}
