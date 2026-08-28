// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

abstract class JsonSerializerInternalBase(JsonSerializer serializer)
{
    class ReferenceEqualsEqualityComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object? x, object? y) =>
            ReferenceEquals(x, y);

        int IEqualityComparer<object>.GetHashCode(object obj) =>
            // put objects in a bucket based on their reference
            RuntimeHelpers.GetHashCode(obj);
    }

    internal readonly JsonSerializer Serializer = serializer;
    protected JsonSerializerProxy? InternalSerializer;

    protected static bool HasFlag(DefaultValueHandling? value, DefaultValueHandling flag)
    {
        if (value == null)
        {
            return false;
        }

        return (value & flag) == flag;
    }

    [field: AllowNull, MaybeNull]
    //since compiler thinks the new field keyword is not instance
#pragma warning disable CA1822
    internal BidirectionalDictionary<string, object> DefaultReferenceMappings =>
#pragma warning restore CA1822
        // override equality comparer for object key dictionary
        // object will be modified as it deserializes and might have mutable hashcode
        field ??= new(
            EqualityComparer<string>.Default,
            new ReferenceEqualsEqualityComparer(),
            "A different value already has the Id '{0}'.",
            "A different Id has already been assigned for value '{0}'. This error may be caused by an object being reused multiple times during deserialization and can be fixed with the setting ObjectCreationHandling.Replace.");

    Dictionary<Type, JsonConverter?>? matchingConverters;

    /// <summary>
    /// Resolves the converter for a type from the converters registered on the serializer.
    /// </summary>
    /// <remarks>
    /// Memoizes the scan. The underlying lookup calls virtual <see cref="JsonConverter.CanConvert" />
    /// on every registered converter, and it runs for every value serialized and every property,
    /// collection item and dictionary entry deserialized. The result cannot be cached on the
    /// contract because contracts are shared between serializers with different converter lists,
    /// so it is cached here instead - a fresh instance is created for each serialize/deserialize
    /// call, so converters registered between calls are always picked up.
    /// </remarks>
    protected JsonConverter? GetMatchingConverter(Type type)
    {
        var converters = Serializer.Converters;
        if (converters.Count == 0)
        {
            return null;
        }

        matchingConverters ??= [];

        if (matchingConverters.TryGetValue(type, out var converter))
        {
            return converter;
        }

        converter = Serializer.GetMatchingConverter(type);
        matchingConverters[type] = converter;
        return converter;
    }

    protected NullValueHandling ResolvedNullValueHandling(JsonObjectContract? containerContract, JsonProperty property) =>
        property.NullValueHandling ??
        containerContract?.ItemNullValueHandling ??
        Serializer.NullValueHandling ??
        default;
}