// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// A collection of <see cref="JsonProperty" /> objects.
/// </summary>
public class JsonPropertyCollection : KeyedCollection<string, JsonProperty>
{
    readonly Type type;
    readonly List<JsonProperty> list;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPropertyCollection" /> class.
    /// </summary>
    /// <param name="type">The type.</param>
    public JsonPropertyCollection(Type type)
        : base(StringComparer.Ordinal)
    {
        this.type = type;

        // foreach over List<T> to avoid boxing the Enumerator
        list = (List<JsonProperty>) Items;
    }

    // Same backing list, exposed so hot serializer loops can foreach without
    // boxing the enumerator through Collection<T>'s interface dispatch.
    internal List<JsonProperty> List => list;

    internal void AddRange(IList<JsonProperty> collection)
    {
        foreach (var value in collection)
        {
            Add(value);
        }
    }

    /// <summary>
    /// When implemented in a derived class, extracts the key from the specified element.
    /// </summary>
    /// <param name="item">The element from which to extract the key.</param>
    /// <returns>The key for the specified element.</returns>
    protected override string GetKeyForItem(JsonProperty item) =>
        item.PropertyName!;

    /// <summary>
    /// Adds a <see cref="JsonProperty" /> object.
    /// </summary>
    /// <param name="property">The property to add to the collection.</param>
    public void AddProperty(JsonProperty property)
    {
        MiscellaneousUtils.Assert(property.PropertyName != null);

        if (Contains(property.PropertyName))
        {
            // don't overwrite existing property with ignored property
            if (property.Ignored)
            {
                return;
            }

            var existingProperty = this[property.PropertyName];
            var duplicateProperty = true;

            if (existingProperty.Ignored)
            {
                // remove ignored property so it can be replaced in collection
                Remove(existingProperty);
                duplicateProperty = false;
            }
            else
            {
                if (property.DeclaringType.IsSubclassOf(existingProperty.DeclaringType) ||
                    (existingProperty.DeclaringType.IsInterface &&
                     property.DeclaringType.ImplementInterface(existingProperty.DeclaringType)))
                {
                    // current property is on a derived class and hides the existing
                    Remove(existingProperty);
                    duplicateProperty = false;
                }

                if (existingProperty.DeclaringType.IsSubclassOf(property.DeclaringType)
                    || (property.DeclaringType.IsInterface && existingProperty.DeclaringType.ImplementInterface(property.DeclaringType)))
                {
                    // current property is hidden by the existing so don't add it
                    return;
                }

                if (type.ImplementInterface(existingProperty.DeclaringType) && type.ImplementInterface(property.DeclaringType))
                {
                    // current property was already defined on another interface
                    return;
                }
            }

            if (duplicateProperty)
            {
                throw new JsonSerializationException($"A member with the name '{property.PropertyName}' already exists on '{type}'. Use the JsonPropertyAttribute to specify another name.");
            }
        }

        Add(property);
    }

    /// <summary>
    /// Gets the closest matching <see cref="JsonProperty" /> object.
    /// First attempts to get an exact case match of <paramref name="name" /> and then
    /// a case insensitive match.
    /// </summary>
    /// <param name="name">Name of the property.</param>
    /// <returns>A matching property if found.</returns>
    public JsonProperty? GetClosestMatchProperty(string name)
    {
        if (TryGetProperty(name, out var propertyByName))
        {
            return propertyByName;
        }

        foreach (var property in list)
        {
            if (string.Equals(name, property.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the closest matching <see cref="JsonProperty" /> object.
    /// First attempts to get an exact case match of <paramref name="name" /> and then
    /// a case insensitive match.
    /// </summary>
    /// <param name="name">Name of the property.</param>
    /// <param name="type">Type of the property.</param>
    /// <returns>A matching property if found.</returns>
    public JsonProperty? GetClosestMatchProperty(string name, Type type)
    {
        if (TryGetProperty(name, out var propertyByName))
        {
            if (type == propertyByName.PropertyType)
            {
                return propertyByName;
            }
        }

        foreach (var property in list)
        {
            if (type == property.PropertyType &&
                string.Equals(name, property.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property;
            }
        }

        return null;
    }

    bool TryGetProperty(string key, [NotNullWhen(true)] out JsonProperty? item)
    {
        if (Dictionary == null)
        {
            item = default;
            return false;
        }

        return Dictionary.TryGetValue(key, out item);
    }
}