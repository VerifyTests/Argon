// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

class DefaultReferenceResolver : IReferenceResolver
{
    int referenceCount;

    static BidirectionalDictionary<string, object> GetMappings(object context)
    {
        if (context is not JsonSerializerInternalBase internalSerializer)
        {
            if (context is JsonSerializerProxy proxy)
            {
                internalSerializer = proxy.GetInternalSerializer();
            }
            else
            {
                throw new JsonException("The DefaultReferenceResolver can only be used internally.");
            }
        }

        return internalSerializer.DefaultReferenceMappings;
    }

    public object ResolveReference(object context, string reference)
    {
        GetMappings(context).TryGetByFirst(reference, out var value);
        return value!;
    }

    public string GetReference(object context, object value)
    {
        var mappings = GetMappings(context);

        if (!mappings.TryGetBySecond(value, out var reference))
        {
            referenceCount++;
            reference = referenceCount.ToString(InvariantCulture);
            mappings.Set(reference, value);
        }

        return reference;
    }

    public void AddReference(object context, string reference, object value) =>
        GetMappings(context).Set(reference, value);

    public bool IsReferenced(object context, object value) =>
        GetMappings(context).TryGetBySecond(value, out _);
}