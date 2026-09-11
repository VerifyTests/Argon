// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

/// <summary>
/// Resolves the derived types the C# compiler records for a <c>closed</c> type hierarchy, and the
/// type discriminator inferred for each of them.
/// </summary>
/// <remarks>
/// The compiler applies <c>System.Runtime.CompilerServices.IsClosedTypeAttribute</c> to a closed
/// type, but the attribute definition comes from either the target framework or a polyfill compiled
/// into the declaring assembly. Every assembly can therefore hold its own distinct copy, so the
/// marker is matched by full name rather than through a compile time type reference. Reading it
/// through <see cref="CustomAttributeData" /> also avoids instantiating the attribute, which cannot
/// be cast to a single type for the same reason.
/// </remarks>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
partial class ClosedTypeInfo
{
    const string attributeName = "IsClosedTypeAttribute";
    const string attributeFullName = "System.Runtime.CompilerServices.IsClosedTypeAttribute";
    const string derivedTypesName = "DerivedTypes";
    const int maxDepth = 32;

    static readonly ThreadSafeStore<Type, ClosedTypeInfo?> cache = new(Build);

    /// <summary>
    /// The metadata for <paramref name="type" />, or <c>null</c> when it is not a closed type
    /// hierarchy that anything can be inferred from. The negative result is cached, because the
    /// writer asks this question for every object it writes.
    /// </summary>
    public static ClosedTypeInfo? Find(Type type) =>
        cache.Get(type);

    static ClosedTypeInfo? Build(Type type)
    {
        if (!TryGetDerivedTypes(type, out var derivedTypes))
        {
            return null;
        }

        var toDiscriminator = new Dictionary<Type, string>();
        var toType = new Dictionary<string, Type>(StringComparer.Ordinal);
        string? error = null;

        Add(type, derivedTypes, toDiscriminator, toType, ref error, 0);

        if (toType.Count == 0 &&
            error == null)
        {
            // a closed type with no descendants is a valid declaration, but nothing can be inferred
            // from it, so it stays non polymorphic
            return null;
        }

        return new(toDiscriminator, toType, error);
    }

    static bool TryGetDerivedTypes(Type type, [NotNullWhen(true)] out IList<CustomAttributeTypedArgument>? derivedTypes)
    {
        foreach (var data in type.GetCustomAttributesData())
        {
            var attributeType = data.AttributeType;
            // Name is compared before FullName because FullName allocates for constructed and nested types
            if (attributeType.Name != attributeName ||
                attributeType.FullName != attributeFullName)
            {
                continue;
            }

            foreach (var argument in data.NamedArguments)
            {
                if (argument.MemberName == derivedTypesName &&
                    argument.TypedValue.Value is IList<CustomAttributeTypedArgument> values)
                {
                    derivedTypes = values;
                    return true;
                }
            }

            break;
        }

        derivedTypes = null;
        return false;
    }

    static void Add(Type declaring, IList<CustomAttributeTypedArgument> derivedTypes, Dictionary<Type, string> toDiscriminator, Dictionary<string, Type> toType, ref string? error, int depth)
    {
        if (depth > maxDepth)
        {
            error ??= $"Closed type hierarchy for '{declaring}' nests deeper than {maxDepth} levels.";
            return;
        }

        foreach (var argument in derivedTypes)
        {
            if (argument.Value is not Type derived)
            {
                continue;
            }

            var closed = Close(declaring, derived);
            if (closed == null ||
                !declaring.IsAssignableFrom(closed))
            {
                // an open generic that cannot be closed, or a hand written attribute naming an
                // unrelated type. skipping it means the writer reports the runtime type as not
                // being a known derived type, rather than silently emitting an unusable name
                continue;
            }

            // only terminal (non closed) leaves carry a discriminator, matching System.Text.Json.
            // a closed type nested inside a closed type is expanded through
            if (TryGetDerivedTypes(closed, out var nested))
            {
                Add(closed, nested, toDiscriminator, toType, ref error, depth + 1);
                continue;
            }

            var discriminator = Discriminator(closed);

            if (toType.TryGetValue(discriminator, out var existing))
            {
                if (existing != closed)
                {
                    error ??= $"Closed type hierarchy has two derived types with the type discriminator '{discriminator}': '{existing}' and '{closed}'. Type discriminators are inferred from the simple type name and must be unique.";
                }

                continue;
            }

            toType.Add(discriminator, closed);
            toDiscriminator[closed] = discriminator;
        }
    }

    // `closed record Shape<T>` records typeof(Circle<>), because an attribute argument cannot
    // reference T, so the declaring type's arguments are re-applied
    static Type? Close(Type declaring, Type derived)
    {
        if (!derived.IsGenericTypeDefinition)
        {
            return derived;
        }

        if (!declaring.IsGenericType)
        {
            return null;
        }

        var arguments = declaring.GetGenericArguments();
        if (arguments.Length != derived.GetGenericArguments().Length)
        {
            return null;
        }

        try
        {
            return derived.MakeGenericType(arguments);
        }
        catch (ArgumentException)
        {
            // a generic constraint the declaring type's arguments do not satisfy
            return null;
        }
    }

    static string Discriminator(Type type)
    {
        var name = type.Name;
        var index = name.IndexOf('`');
        return index == -1 ? name : name[..index];
    }
}

/// <summary>
/// The terminal derived types of a closed type hierarchy, and the discriminator inferred for each.
/// </summary>
partial class ClosedTypeInfo(
    Dictionary<Type, string> toDiscriminator,
    Dictionary<string, Type> toType,
    string? error)
{
    /// <summary>
    /// Set when the hierarchy cannot be used, so that the failure is cached rather than recomputed.
    /// The reader and writer throw it on first use, which lets the exception carry the JSON path.
    /// </summary>
    public string? Error { get; } = error;

    public bool TryGetDiscriminator(Type type, [NotNullWhen(true)] out string? discriminator) =>
        toDiscriminator.TryGetValue(type, out discriminator);

    public bool TryGetType(string discriminator, [NotNullWhen(true)] out Type? type) =>
        toType.TryGetValue(discriminator, out type);
}
