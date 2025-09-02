// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Used by <see cref="JsonSerializer" /> to resolve a <see cref="JsonContract" /> for a given <see cref="System.Type" />.
/// </summary>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
public class DefaultContractResolver : IContractResolver
{
    // Json.NET Schema requires a property
    internal static IContractResolver Instance { get; } = new DefaultContractResolver();

    readonly DefaultJsonNameTable nameTable = new();

    readonly ThreadSafeStore<Type, JsonContract> contractCache;

    /// <summary>
    /// Gets or sets a value indicating whether compiler generated members should be serialized.
    /// </summary>
    public bool SerializeCompilerGeneratedMembers { get; set; }

    /// <summary>
    /// Gets or sets the naming strategy used to resolve how property names and dictionary keys are serialized.
    /// </summary>
    public NamingStrategy? NamingStrategy { get; set; }

    public static List<JsonConverter> Converters { get; } =
        [
        new StringBuilderConverter(),
        new ExpandoObjectConverter(),
        new KeyValuePairConverter(),
        new DriveInfoConverter(),
        new EncodingConverter(),
        new PathInfoConverter(),
        new RegexConverter(),
        new EncodingConverter(),
        new TimeZoneInfoConverter(),
        new VersionConverter(),
        new StringWriterConverter()
        ];

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultContractResolver" /> class.
    /// </summary>
    public DefaultContractResolver() =>
        contractCache = new(CreateContract);

    /// <summary>
    /// Resolves the contract for a given type.
    /// </summary>
    /// <param name="type">The type to resolve a contract for.</param>
    /// <returns>The contract for a given type.</returns>
    public virtual JsonContract ResolveContract(Type type) =>
        contractCache.Get(type);

    static bool FilterMembers(MemberInfo member)
    {
        if (member is PropertyInfo property)
        {
            if (property.IsIndexedProperty())
            {
                return false;
            }

            return !property.PropertyType.IsByRefLikeType();
        }

        if (member is FieldInfo field)
        {
            return !field.FieldType.IsByRefLikeType();
        }

        return true;
    }

    /// <summary>
    /// Gets the serializable members for the type.
    /// </summary>
    /// <param name="type">The type to get serializable members for.</param>
    /// <returns>The serializable members for the type.</returns>
    protected virtual IEnumerable<MemberInfo> GetSerializableMembers(Type type)
    {
        var memberSerialization = JsonTypeReflector.GetObjectMemberSerialization(type);

        if (memberSerialization == MemberSerialization.Fields)
        {
            // Do not filter ByRef types here because accessing FieldType/PropertyType can trigger additional assembly loads
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        var serializableMembers = new List<MemberInfo>();
        var dataContractAttribute = JsonTypeReflector.GetDataContractAttribute(type);

        // Exclude index properties and ByRef types
        var defaultMembers = type.GetFieldsAndProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(FilterMembers).ToList();

        // Do not filter ByRef types here because accessing FieldType/PropertyType can trigger additional assembly loads
        foreach (var member in type.GetFieldsAndProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!(member is not PropertyInfo property ||
                  !property.IsIndexedProperty()))
            {
                continue;
            }

            if (!ShouldSerialize(member, defaultMembers, dataContractAttribute))
            {
                continue;
            }

            serializableMembers.Add(member);
        }

        // don't include TargetSite on non-serializable exceptions
        // MemberBase is problematic to serialize. Large, self referencing instances, etc
        if (type.IsAssignableTo<Exception>())
        {
            return serializableMembers.Where(_ => !string.Equals(_.Name, "TargetSite", StringComparison.Ordinal));
        }

        return serializableMembers;
    }

    bool ShouldSerialize(MemberInfo member, List<MemberInfo> defaultMembers, DataContractAttribute? dataContractAttribute)
    {
        // exclude members that are compiler generated if set
        if (!SerializeCompilerGeneratedMembers &&
            member.IsDefined(typeof(CompilerGeneratedAttribute), true))
        {
            return false;
        }

        if (defaultMembers.Contains(member))
        {
            // add all members that are found by default member search
            return true;
        }

        // add members that are explicitly marked with JsonProperty/DataMember attribute
        // or are a field if serializing just fields
        if (member.GetAttribute<JsonPropertyAttribute>() != null)
        {
            return true;
        }

        if (member.GetAttribute<JsonRequiredAttribute>() != null)
        {
            return true;
        }

        if (dataContractAttribute != null &&
            member.GetAttribute<DataMemberAttribute>() != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a <see cref="JsonObjectContract" /> for the given type.
    /// </summary>
    /// <returns>A <see cref="JsonObjectContract" /> for the given type.</returns>
    protected virtual JsonObjectContract CreateObjectContract(Type type)
    {
        var contract = new JsonObjectContract(type);
        InitializeContract(contract);

        var nonNullableUnderlyingType = contract.NonNullableUnderlyingType;
        contract.MemberSerialization = JsonTypeReflector.GetObjectMemberSerialization(nonNullableUnderlyingType);
        contract.Properties.AddRange(CreateProperties(nonNullableUnderlyingType, contract.MemberSerialization));

        var attribute = AttributeCache<JsonObjectAttribute>.GetAttribute(nonNullableUnderlyingType);
        if (attribute != null)
        {
            contract.ItemRequired = attribute.itemRequired;
            contract.ItemNullValueHandling = attribute.itemNullValueHandling;
        }

        if (contract.IsInstantiable)
        {
            // check if a JsonConstructorAttribute has been defined and use that
            if (TryGetAttributeConstructor(nonNullableUnderlyingType, out var overrideConstructor))
            {
                contract.OverrideCreator = DelegateFactory.CreateParameterizedConstructor(overrideConstructor);
                contract.CreatorParameters.AddRange(CreateConstructorParameters(overrideConstructor, contract.Properties));
            }
            else if (contract.MemberSerialization == MemberSerialization.Fields)
            {
                // mimic DataContractSerializer behaviour when populating fields by overriding default creator to create an uninitialized object
                contract.DefaultCreator = contract.GetUninitializedObject;
            }
            else if (contract.DefaultCreator == null || contract.DefaultCreatorNonPublic)
            {
                var constructor = GetParameterizedConstructor(nonNullableUnderlyingType);
                if (constructor != null)
                {
                    contract.ParameterizedCreator = DelegateFactory.CreateParameterizedConstructor(constructor);
                    contract.CreatorParameters.AddRange(CreateConstructorParameters(constructor, contract.Properties));
                }
            }
            else if (nonNullableUnderlyingType.IsValueType)
            {
                // value types always have default constructor
                // check whether there is a constructor that matches with non-writable properties on value type
                if (TryGetImmutableConstructor(nonNullableUnderlyingType, contract.Properties, out var constructor))
                {
                    contract.OverrideCreator = DelegateFactory.CreateParameterizedConstructor(constructor);
                    contract.CreatorParameters.AddRange(CreateConstructorParameters(constructor, contract.Properties));
                }
            }
        }

        return contract;
    }

    static bool TryGetAttributeConstructor(Type type, [NotNullWhen(true)] out ConstructorInfo? constructor)
    {
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(_ => _.IsDefined(typeof(JsonConstructorAttribute), true))
            .ToList();
        if (constructors.Count == 0)
        {
            constructor = null;
            return false;
        }

        if (constructors.Count == 1)
        {
            constructor = constructors[0];
            return true;
        }

        throw new JsonException("Multiple constructors with the JsonConstructorAttribute.");
    }

    static bool TryGetImmutableConstructor(Type type, JsonPropertyCollection memberProperties, [NotNullWhen(true)] out ConstructorInfo? constructor)
    {
        foreach (var constructorItem in type.GetConstructors())
        {
            var parameters = constructorItem.GetParameters();
            if (parameters.Length <= 0)
            {
                continue;
            }

            foreach (var parameter in parameters)
            {
                var memberProperty = MatchProperty(memberProperties, parameter.Name, parameter.ParameterType);
                if (memberProperty == null || memberProperty.Writable)
                {
                    constructor = null;
                    return false;
                }
            }

            constructor = constructorItem;
            return true;
        }

        constructor = null;
        return false;
    }

    static ConstructorInfo? GetParameterizedConstructor(Type type)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 1)
        {
            return constructors[0];
        }

        return null;
    }

    /// <summary>
    /// Creates the constructor parameters.
    /// </summary>
    /// <param name="constructor">The constructor to create properties for.</param>
    /// <param name="memberProperties">The type's member properties.</param>
    /// <returns>Properties for the given <see cref="ConstructorInfo" />.</returns>
    protected virtual IList<JsonProperty> CreateConstructorParameters(ConstructorInfo constructor, JsonPropertyCollection memberProperties)
    {
        var constructorParameters = constructor.GetParameters();

        var parameterCollection = new JsonPropertyCollection(constructor.DeclaringType!);

        foreach (var parameterInfo in constructorParameters)
        {
            var matchingMemberProperty = MatchProperty(memberProperties, parameterInfo.Name, parameterInfo.ParameterType);

            // ensure that property will have a name from matching property or from parameterInfo
            // parameterInfo could have no name if generated by a proxy (I'm looking at you Castle)
            if (matchingMemberProperty == null &&
                parameterInfo.Name == null)
            {
                continue;
            }

            var property = CreatePropertyFromConstructorParameter(matchingMemberProperty, parameterInfo);
            parameterCollection.AddProperty(property);
        }

        return parameterCollection;
    }

    static JsonProperty? MatchProperty(JsonPropertyCollection properties, string? name, Type type)
    {
        // it is possible to generate a member with a null name using Reflection.Emit
        // protect against an ArgumentNullException from GetClosestMatchProperty by testing for null here
        if (name == null)
        {
            return null;
        }

        return properties.GetClosestMatchProperty(name, type);
    }

    /// <summary>
    /// Creates a <see cref="JsonProperty" /> for the given <see cref="ParameterInfo" />.
    /// </summary>
    /// <param name="matchingMemberProperty">The matching member property.</param>
    /// <param name="parameter">The constructor parameter.</param>
    /// <returns>A created <see cref="JsonProperty" /> for the given <see cref="ParameterInfo" />.</returns>
    protected virtual JsonProperty CreatePropertyFromConstructorParameter(JsonProperty? matchingMemberProperty, ParameterInfo parameter)
    {
        var declaringType = parameter.Member.DeclaringType!;
        var property = new JsonProperty(parameter.ParameterType, declaringType);

        SetPropertySettingsFromAttributes(property, parameter, parameter.Name!, declaringType, MemberSerialization.OptOut, out _);

        property.Readable = false;
        property.Writable = true;

        // "inherit" values from matching member property if unset on parameter
        if (matchingMemberProperty != null)
        {
            property.PropertyName = property.PropertyName != parameter.Name ? property.PropertyName : matchingMemberProperty.PropertyName;
            property.Converter ??= matchingMemberProperty.Converter;

            if (!property.hasExplicitDefaultValue && matchingMemberProperty.hasExplicitDefaultValue)
            {
                property.DefaultValue = matchingMemberProperty.DefaultValue;
            }

            property.required ??= matchingMemberProperty.required;
            property.IsReference ??= matchingMemberProperty.IsReference;
            property.NullValueHandling ??= matchingMemberProperty.NullValueHandling;
            property.DefaultValueHandling ??= matchingMemberProperty.DefaultValueHandling;
            property.ReferenceLoopHandling ??= matchingMemberProperty.ReferenceLoopHandling;
            property.ObjectCreationHandling ??= matchingMemberProperty.ObjectCreationHandling;
            property.TypeNameHandling ??= matchingMemberProperty.TypeNameHandling;
        }

        return property;
    }

    /// <summary>
    /// Resolves the default <see cref="JsonConverter" /> for the contract.
    /// </summary>
    /// <returns>The contract's default <see cref="JsonConverter" />.</returns>
    protected virtual JsonConverter? ResolveContractConverter(Type type) =>
        JsonTypeReflector.GetJsonConverter(type);

    static Func<object> GetDefaultCreator(Type createdType) =>
        DelegateFactory.CreateDefaultConstructor<object>(createdType);

    void InitializeContract(JsonContract contract)
    {
        var nonNullableUnderlyingType = contract.NonNullableUnderlyingType;
        var containerAttribute = AttributeCache<JsonContainerAttribute>.GetAttribute(nonNullableUnderlyingType);
        if (containerAttribute == null)
        {
            var dataContractAttribute = JsonTypeReflector.GetDataContractAttribute(nonNullableUnderlyingType);
            // doesn't have a null value
            if (dataContractAttribute is {IsReference: true})
            {
                contract.IsReference = true;
            }
        }
        else
        {
            contract.IsReference = containerAttribute.isReference;
        }

        contract.Converter = ResolveContractConverter(nonNullableUnderlyingType);

        // then see whether object is compatible with any of the built in converters
        contract.InternalConverter = JsonSerializer.GetMatchingConverter(Converters, nonNullableUnderlyingType);

        if (!contract.IsInstantiable)
        {
            return;
        }

        var createdType = contract.CreatedType;
        if (createdType.IsValueType)
        {
            contract.DefaultCreator = GetDefaultCreator(createdType);
            contract.DefaultCreatorNonPublic = false;
            return;
        }

        var constructor = createdType.GetDefaultConstructor(nonPublic: true);
        if (constructor != null)
        {
            contract.DefaultCreator = GetDefaultCreator(createdType);
            contract.DefaultCreatorNonPublic = !constructor.IsPublic;
        }
    }

    /// <summary>
    /// Creates a <see cref="JsonDictionaryContract" /> for the given type.
    /// </summary>
    /// <returns>A <see cref="JsonDictionaryContract" /> for the given type.</returns>
    protected virtual JsonDictionaryContract CreateDictionaryContract(Type type)
    {
        var contract = new JsonDictionaryContract(type);
        InitializeContract(contract);

        contract.DictionaryKeyResolver = ResolveDictionaryKey;

        if (TryGetAttributeConstructor(contract.NonNullableUnderlyingType, out var constructor))
        {
            contract.HasParameterizedCreator = GetHasParameterizedCreator(constructor, contract);
            contract.OverrideCreator = DelegateFactory.CreateParameterizedConstructor(constructor);
        }

        return contract;
    }

    static bool GetHasParameterizedCreator(ConstructorInfo constructor, JsonDictionaryContract contract)
    {
        var parameters = constructor.GetParameters();

        if (parameters.Length == 0)
        {
            return false;
        }

        var expectedType = contract is {DictionaryKeyType: not null, DictionaryValueType: not null}
            ? typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(contract.DictionaryKeyType, contract.DictionaryValueType))
            : typeof(IDictionary);

        if (parameters.Length == 1 &&
            expectedType.IsAssignableFrom(parameters[0].ParameterType))
        {
            return true;
        }

        throw new JsonException($"Constructor for '{contract.UnderlyingType}' must have no parameters or a single parameter that implements '{expectedType}'.");
    }

    /// <summary>
    /// Creates a <see cref="JsonArrayContract" /> for the given type.
    /// </summary>
    /// <returns>A <see cref="JsonArrayContract" /> for the given type.</returns>
    protected virtual JsonArrayContract CreateArrayContract(Type type)
    {
        var contract = new JsonArrayContract(type);
        InitializeContract(contract);

        if (TryGetAttributeConstructor(contract.NonNullableUnderlyingType, out var constructor))
        {
            contract.HasParameterizedCreator = HasParameterizedCreator(constructor, contract);
            contract.OverrideCreator = DelegateFactory.CreateParameterizedConstructor(constructor);
        }

        return contract;
    }

    static bool HasParameterizedCreator(ConstructorInfo constructor, JsonArrayContract contract)
    {
        var parameters = constructor.GetParameters();

        if (parameters.Length == 0)
        {
            return false;
        }

        var expectedParameterType = contract.CollectionItemType == null
            ? typeof(IEnumerable)
            : typeof(IEnumerable<>).MakeGenericType(contract.CollectionItemType);

        if (parameters.Length == 1 &&
            expectedParameterType.IsAssignableFrom(parameters[0].ParameterType))
        {
            return true;
        }

        throw new JsonException($"Constructor for '{contract.UnderlyingType}' must have no parameters or a single parameter that implements '{expectedParameterType}'.");
    }

    /// <summary>
    /// Creates a <see cref="JsonPrimitiveContract" /> for the given type.
    /// </summary>
    /// <returns>A <see cref="JsonPrimitiveContract" /> for the given type.</returns>
    protected virtual JsonPrimitiveContract CreatePrimitiveContract(Type type)
    {
        var contract = new JsonPrimitiveContract(type);
        InitializeContract(contract);

        return contract;
    }

    /// <summary>
    /// Creates a <see cref="JsonLinqContract" /> for the given type.
    /// </summary>
    /// <returns>A <see cref="JsonLinqContract" /> for the given type.</returns>
    protected virtual JsonLinqContract CreateLinqContract(Type type)
    {
        var contract = new JsonLinqContract(type);
        InitializeContract(contract);

        return contract;
    }

    /// <summary>
    /// Creates a <see cref="JsonDynamicContract" /> for the given type.
    /// </summary>
    /// <returns>A <see cref="JsonDynamicContract" /> for the given type.</returns>
    protected virtual JsonDynamicContract CreateDynamicContract(Type type)
    {
        var contract = new JsonDynamicContract(type);
        InitializeContract(contract);

        contract.PropertyNameResolver = (writer, name) => ResolveDictionaryKey(writer, name, name);

        contract.Properties.AddRange(CreateProperties(type, MemberSerialization.OptOut));

        return contract;
    }

    /// <summary>
    /// Creates a <see cref="JsonStringContract" /> for the given type.
    /// </summary>
    /// <returns>A <see cref="JsonStringContract" /> for the given type.</returns>
    protected virtual JsonStringContract CreateStringContract(Type type)
    {
        var contract = new JsonStringContract(type);
        InitializeContract(contract);

        return contract;
    }

    /// <summary>
    /// Determines which contract type is created for the given type.
    /// </summary>
    /// <returns>A <see cref="JsonContract" /> for the given type.</returns>
    protected virtual JsonContract CreateContract(Type type)
    {
        var t = type.EnsureNotByRefType();

        if (IsJsonPrimitiveType(t))
        {
            return CreatePrimitiveContract(type);
        }

        t = t.EnsureNotNullableType();
        var containerAttribute = AttributeCache<JsonContainerAttribute>.GetAttribute(t);

        if (containerAttribute is JsonObjectAttribute)
        {
            return CreateObjectContract(type);
        }

        if (containerAttribute is JsonArrayAttribute)
        {
            return CreateArrayContract(type);
        }

        if (containerAttribute is JsonDictionaryAttribute)
        {
            return CreateDictionaryContract(type);
        }

        if (t == typeof(JToken) || t.IsSubclassOf(typeof(JToken)))
        {
            return CreateLinqContract(type);
        }

        if (t.IsDictionary())
        {
            return CreateDictionaryContract(type);
        }

        if (t.IsAssignableTo<IEnumerable>())
        {
            return CreateArrayContract(type);
        }

        if (CanConvertToString(t))
        {
            return CreateStringContract(type);
        }

        if (t.IsAssignableTo<IDynamicMetaObjectProvider>())
        {
            return CreateDynamicContract(type);
        }

        // tested last because it is not possible to automatically deserialize custom IConvertible types
        if (IsIConvertible(t))
        {
            return CreatePrimitiveContract(t);
        }

        return CreateObjectContract(type);
    }

    static bool IsJsonPrimitiveType(Type type)
    {
        var typeCode = ConvertUtils.GetTypeCode(type);

        return typeCode != PrimitiveTypeCode.Empty &&
               typeCode != PrimitiveTypeCode.Object;
    }

    static bool IsIConvertible(Type t)
    {
        if (t.IsAssignableTo<IConvertible>()
            || (t.IsNullableType() && Nullable.GetUnderlyingType(t).IsAssignableTo<IConvertible>()))
        {
            return !t.IsAssignableTo<JToken>();
        }

        return false;
    }

    static bool CanConvertToString(Type type)
    {
        if (JsonTypeReflector.TryGetStringConverter(type, out _))
        {
            return true;
        }
#if NET6_0_OR_GREATER
        if (type == typeof(Date) ||
            type == typeof(Time))
        {
            return true;
        }
#endif
        return type == typeof(Type) ||
               type.IsSubclassOf(typeof(Type));
    }

    /// <summary>
    /// Creates properties for the given <see cref="JsonContract" />.
    /// </summary>
    /// <param name="type">The type to create properties for.</param>
    /// <param name="memberSerialization">The member serialization mode for the type.</param>
    /// <returns>Properties for the given <see cref="JsonContract" />.</returns>
    protected virtual IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var members = GetSerializableMembers(type);
        if (members == null)
        {
            throw new JsonSerializationException("Null collection of serializable members returned.");
        }

        var nameTable = GetNameTable();

        var properties = new JsonPropertyCollection(type);

        foreach (var member in members)
        {
            var property = CreateProperty(member, memberSerialization);

            property.PropertyName = nameTable.Add(property.PropertyName!);

            properties.AddProperty(property);
        }

        return properties.OrderBy(_ => _.Order ?? -1).ToList();
    }

    public virtual JsonNameTable GetNameTable() => nameTable;

    /// <summary>
    /// Creates the <see cref="IValueProvider" /> used by the serializer to get and set values from a member.
    /// </summary>
    /// <returns>The <see cref="IValueProvider" /> used by the serializer to get and set values from a member.</returns>
    protected virtual IValueProvider CreateMemberValueProvider(MemberInfo member) =>
        new DynamicValueProvider(member);

    /// <summary>
    /// Creates a <see cref="JsonProperty" /> for the given <see cref="MemberInfo" />.
    /// </summary>
    /// <param name="memberSerialization">The member's parent <see cref="MemberSerialization" />.</param>
    /// <param name="member">The member to create a <see cref="JsonProperty" /> for.</param>
    /// <returns>A created <see cref="JsonProperty" /> for the given <see cref="MemberInfo" />.</returns>
    protected virtual JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var declaringType = member.DeclaringType!;
        var property = new JsonProperty(member.GetMemberUnderlyingType(), declaringType)
        {
            ValueProvider = CreateMemberValueProvider(member)
        };

        SetPropertySettingsFromAttributes(property, member, member.Name, declaringType, memberSerialization, out var allowNonPublicAccess);

        if (memberSerialization == MemberSerialization.Fields)
        {
            // write to readonly fields
            property.Readable = true;
            property.Writable = true;
        }
        else
        {
            property.Readable = member.CanReadMemberValue(allowNonPublicAccess);
            property.Writable = member.CanSetMemberValue(allowNonPublicAccess, property.HasMemberAttribute);
        }

        return property;
    }

    void SetPropertySettingsFromAttributes(JsonProperty property, ICustomAttributeProvider attributeProvider, string name, Type declaringType, MemberSerialization memberSerialization, out bool allowNonPublicAccess)
    {
        var dataContractAttribute = JsonTypeReflector.GetDataContractAttribute(declaringType);

        var member = attributeProvider as MemberInfo;

        DataMemberAttribute? dataMemberAttribute;
        if (dataContractAttribute == null || member == null)
        {
            dataMemberAttribute = null;
        }
        else
        {
            dataMemberAttribute = JsonTypeReflector.GetDataMemberAttribute(member);
        }

        var propertyAttribute = JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(attributeProvider);

        property.PropertyName = GetPropertyName(name, propertyAttribute, dataMemberAttribute);

        property.UnderlyingName = name;

        var hasMemberAttribute = false;
        if (propertyAttribute == null)
        {
            property.NullValueHandling = null;
            property.ReferenceLoopHandling = null;
            property.ObjectCreationHandling = null;
            property.TypeNameHandling = null;
            property.IsReference = null;
            property.ItemIsReference = null;
            property.ItemConverter = null;
            property.ItemReferenceLoopHandling = null;
            property.ItemTypeNameHandling = null;
            if (dataMemberAttribute != null)
            {
                property.required = dataMemberAttribute.IsRequired ? Required.AllowNull : Required.Default;
                property.Order = dataMemberAttribute.Order == -1 ? null : dataMemberAttribute.Order;
                property.DefaultValueHandling = dataMemberAttribute.EmitDefaultValue ? null : DefaultValueHandling.Ignore;

                hasMemberAttribute = true;
            }
        }
        else
        {
            property.required = propertyAttribute.required;
            property.Order = propertyAttribute.order;
            property.DefaultValueHandling = propertyAttribute.defaultValueHandling;
            hasMemberAttribute = true;
            property.NullValueHandling = propertyAttribute.nullValueHandling;
            property.ReferenceLoopHandling = propertyAttribute.referenceLoopHandling;
            property.ObjectCreationHandling = propertyAttribute.objectCreationHandling;
            property.TypeNameHandling = propertyAttribute.typeNameHandling;
            property.IsReference = propertyAttribute.isReference;

            property.ItemIsReference = propertyAttribute.itemIsReference;
            property.ItemConverter = propertyAttribute.ItemConverterType == null ? null : JsonTypeReflector.CreateJsonConverterInstance(propertyAttribute.ItemConverterType);
            property.ItemReferenceLoopHandling = propertyAttribute.itemReferenceLoopHandling;
            property.ItemTypeNameHandling = propertyAttribute.itemTypeNameHandling;
        }

        var requiredAttribute = JsonTypeReflector.GetAttribute<JsonRequiredAttribute>(attributeProvider);
        if (requiredAttribute != null)
        {
            property.required = Required.Always;
            hasMemberAttribute = true;
        }

        property.HasMemberAttribute = hasMemberAttribute;

        var ignored = GetPropertyIgnored(attributeProvider, memberSerialization, hasMemberAttribute);

        property.Ignored = ignored;

        // resolve converter for property
        // the class type might have a converter but the property converter takes precedence
        property.Converter = JsonTypeReflector.GetJsonConverter(attributeProvider);

        var defaultValueAttribute = JsonTypeReflector.GetAttribute<DefaultValueAttribute>(attributeProvider);
        if (defaultValueAttribute != null)
        {
            property.DefaultValue = defaultValueAttribute.Value;
        }

        allowNonPublicAccess = hasMemberAttribute ||
                               memberSerialization == MemberSerialization.Fields;
    }

    static bool GetPropertyIgnored(ICustomAttributeProvider attributeProvider, MemberSerialization memberSerialization, bool hasMemberAttribute)
    {
        var hasJsonIgnoreAttribute = JsonTypeReflector.GetAttribute<JsonIgnoreAttribute>(attributeProvider) != null;

        if (memberSerialization == MemberSerialization.OptIn)
        {
            // ignored if it has JsonIgnore/NonSerialized or does not have DataMember or JsonProperty attributes
            return hasJsonIgnoreAttribute || !hasMemberAttribute;
        }

        var hasIgnoreDataMemberAttribute = JsonTypeReflector.GetAttribute<IgnoreDataMemberAttribute>(attributeProvider) != null;

        // ignored if it has JsonIgnore or NonSerialized or IgnoreDataMember attributes
        return hasJsonIgnoreAttribute || hasIgnoreDataMemberAttribute;
    }

    string GetPropertyName(string name, JsonPropertyAttribute? propertyAttribute, DataMemberAttribute? dataMemberAttribute)
    {
        string mappedName;
        bool hasSpecifiedName;
        if (propertyAttribute?.PropertyName != null)
        {
            mappedName = propertyAttribute.PropertyName;
            hasSpecifiedName = true;
        }
        else if (dataMemberAttribute?.Name != null)
        {
            mappedName = dataMemberAttribute.Name;
            hasSpecifiedName = true;
        }
        else
        {
            mappedName = name;
            hasSpecifiedName = false;
        }

        if (NamingStrategy == null)
        {
            return ResolvePropertyName(mappedName);
        }

        return NamingStrategy.GetPropertyName(mappedName, hasSpecifiedName);
    }

    /// <summary>
    /// Resolves the name of the property.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>Resolved name of the property.</returns>
    protected virtual string ResolvePropertyName(string propertyName)
    {
        if (NamingStrategy == null)
        {
            return propertyName;
        }

        return NamingStrategy.GetPropertyName(propertyName, false);
    }

    /// <summary>
    /// Resolves the key of the dictionary. By default <see cref="ResolvePropertyName" /> is used to resolve dictionary keys.
    /// </summary>
    /// <param name="name">Key of the dictionary.</param>
    /// <returns>Resolved key of the dictionary.</returns>
    protected virtual string ResolveDictionaryKey(JsonWriter writer, string name, object original)
    {
        if (NamingStrategy == null)
        {
            return ResolvePropertyName(name);
        }

        return NamingStrategy.GetDictionaryKey(name, original);
    }

    /// <summary>
    /// Gets the resolved name of the property.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>Name of the property.</returns>
    public string GetResolvedPropertyName(string propertyName) =>
        // this is a new method rather than changing the visibility of ResolvePropertyName to avoid
        // a breaking change for anyone who has overridden the method
        ResolvePropertyName(propertyName);
}