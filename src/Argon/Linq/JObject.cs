// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Represents a JSON object.
/// </summary>
/// <example>
/// <code lang="cs" source="..\src\Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParse" title="Parsing a JSON Object from Text" />
/// </example>
public class JObject :
    JContainer,
    IDictionary<string, JToken?>
{
    readonly JPropertyKeyedCollection properties = [];

    /// <summary>
    /// Gets the container's children tokens.
    /// </summary>
    protected override IList<JToken> ChildrenTokens => properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="JObject" /> class.
    /// </summary>
    public JObject()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JObject" /> class from another <see cref="JObject" /> object.
    /// </summary>
    /// <param name="other">A <see cref="JObject" /> object to copy from.</param>
    public JObject(JObject other) :
        base(other)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JObject" /> class with the specified content.
    /// </summary>
    public JObject(params object[] content) :
        this((object) content)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JObject" /> class with the specified content.
    /// </summary>
    public JObject(object content) =>
        Add(content);

    internal override bool DeepEquals(JToken node)
    {
        if (node is not JObject t)
        {
            return false;
        }

        return properties.Compare(t.properties);
    }

    internal override int IndexOfItem(JToken? item)
    {
        if (item == null)
        {
            return -1;
        }

        return properties.IndexOfReference(item);
    }

    internal override bool InsertItem(int index, JToken? item, bool skipParentCheck)
    {
        // don't add comments to JObject, no name to reference comment by
        if (item is {Type: JTokenType.Comment})
        {
            return false;
        }

        return base.InsertItem(index, item, skipParentCheck);
    }

    internal override void ValidateToken(JToken o, JToken? existing)
    {
        if (o.Type != JTokenType.Property)
        {
            throw new ArgumentException($"Can not add {o.GetType()} to {GetType()}.");
        }

        var newProperty = (JProperty) o;

        if (existing != null)
        {
            var existingProperty = (JProperty) existing;

            if (newProperty.Name == existingProperty.Name)
            {
                return;
            }
        }

        if (properties.Contains(newProperty.Name))
        {
            throw new ArgumentException($"Can not add property {newProperty.Name} to {GetType()}. Property with the same name already exists on object.");
        }
    }

    internal override JToken CloneToken() =>
        new JObject(this);

    /// <summary>
    /// Gets the node type for this <see cref="JToken" />.
    /// </summary>
    public override JTokenType Type => JTokenType.Object;

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}" /> of <see cref="JProperty" /> of this object's properties.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JProperty" /> of this object's properties.</returns>
    public IEnumerable<JProperty> Properties() =>
        properties.Cast<JProperty>();

    /// <summary>
    /// Gets the <see cref="JProperty" /> with the specified name.
    /// The exact name will be searched for first and if no matching property is found then
    /// the <see cref="StringComparison" /> will be used to match a property.
    /// </summary>
    /// <param name="comparison">One of the enumeration values that specifies how the strings will be compared.</param>
    /// <returns>A <see cref="JProperty" /> matched with the specified name.</returns>
    public JProperty Property(string name, StringComparison comparison = StringComparison.Ordinal)
    {
        var property = PropertyOrNull(name, comparison);
        if (property == null)
        {
            throw new($"Property `{name}` not found.");
        }

        return property;
    }

    /// <summary>
    /// Gets the <see cref="JProperty" /> with the specified name.
    /// The exact name will be searched for first and if no matching property is found then
    /// the <see cref="StringComparison" /> will be used to match a property.
    /// </summary>
    /// <param name="comparison">One of the enumeration values that specifies how the strings will be compared.</param>
    /// <returns>A <see cref="JProperty" /> matched with the specified name or <c>null</c>.</returns>
    public JProperty? PropertyOrNull(string name, StringComparison comparison = StringComparison.Ordinal)
    {
        if (properties.TryGetValue(name, out var propertyByName))
        {
            return (JProperty) propertyByName;
        }

        // test above already uses this comparison so no need to repeat
        if (comparison != StringComparison.Ordinal)
        {
            foreach (JProperty property in properties)
            {
                if (string.Equals(property.Name, name, comparison))
                {
                    return property;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a <see cref="JEnumerable{T}" /> of <see cref="JToken" /> of this object's property values.
    /// </summary>
    /// <returns>A <see cref="JEnumerable{T}" /> of <see cref="JToken" /> of this object's property values.</returns>
    public JEnumerable<JToken> PropertyValues() =>
        new(Properties().Select(_ => _.Value));

    /// <summary>
    /// Gets the <see cref="JToken" /> with the specified key.
    /// </summary>
    public override JToken? this[object key]
    {
        get
        {
            if (key is not string propertyName)
            {
                throw new ArgumentException($"Accessed JObject values with invalid key value: {MiscellaneousUtils.ToString(key)}. Object property name expected.");
            }

            return this[propertyName];
        }
        set
        {
            if (key is not string propertyName)
            {
                throw new ArgumentException($"Set JObject values with invalid key value: {MiscellaneousUtils.ToString(key)}. Object property name expected.");
            }

            this[propertyName] = value;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="JToken" /> with the specified property name.
    /// </summary>
    public JToken? this[string propertyName]
    {
        get
        {
            var property = PropertyOrNull(propertyName);

            return property?.Value;
        }
        set
        {
            var property = PropertyOrNull(propertyName);
            if (property == null)
            {
                Add(propertyName, value);
            }
            else
            {
                property.Value = value!;
            }
        }
    }

    /// <summary>
    /// Loads a <see cref="JObject" /> from a <see cref="JsonReader" />.
    /// </summary>
    /// <param name="reader">A <see cref="JsonReader" /> that will be read for the content of the <see cref="JObject" />.</param>
    /// <returns>A <see cref="JObject" /> that contains the JSON that was read from the specified <see cref="JsonReader" />.</returns>
    /// <exception cref="JsonReaderException">
    /// <paramref name="reader" /> is not valid JSON.
    /// </exception>
    public new static JObject Load(JsonReader reader) =>
        Load(reader, null);

    /// <summary>
    /// Loads a <see cref="JObject" /> from a <see cref="JsonReader" />.
    /// </summary>
    /// <param name="reader">A <see cref="JsonReader" /> that will be read for the content of the <see cref="JObject" />.</param>
    /// <param name="settings">
    /// The <see cref="JsonLoadSettings" /> used to load the JSON.
    /// If this is <c>null</c>, default load settings will be used.
    /// </param>
    /// <returns>A <see cref="JObject" /> that contains the JSON that was read from the specified <see cref="JsonReader" />.</returns>
    /// <exception cref="JsonReaderException">
    /// <paramref name="reader" /> is not valid JSON.
    /// </exception>
    public new static JObject Load(JsonReader reader, JsonLoadSettings? settings)
    {
        if (reader.TokenType == JsonToken.None)
        {
            if (!reader.Read())
            {
                throw JsonReaderException.Create(reader, "Error reading JObject from JsonReader.");
            }
        }

        reader.MoveToContent();

        if (reader.TokenType != JsonToken.StartObject)
        {
            throw JsonReaderException.Create(reader, $"Error reading JObject from JsonReader. Current JsonReader item is not an object: {reader.TokenType}");
        }

        var o = new JObject();
        o.SetLineInfo(reader as IJsonLineInfo, settings);

        o.ReadTokenFrom(reader, settings);

        return o;
    }

    /// <summary>
    /// Load a <see cref="JObject" /> from a string that contains JSON.
    /// </summary>
    /// <param name="json">A <see cref="String" /> that contains JSON.</param>
    /// <returns>A <see cref="JObject" /> populated from the string that contains JSON.</returns>
    /// <exception cref="JsonReaderException">
    /// <paramref name="json" /> is not valid JSON.
    /// </exception>
    /// <example>
    /// <code lang="cs" source="..\src\Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParse" title="Parsing a JSON Object from Text" />
    /// </example>
    public new static JObject Parse(string json) =>
        Parse(json, null);

    /// <summary>
    /// Load a <see cref="JObject" /> from a string that contains JSON.
    /// </summary>
    /// <param name="json">A <see cref="String" /> that contains JSON.</param>
    /// <param name="settings">
    /// The <see cref="JsonLoadSettings" /> used to load the JSON.
    /// If this is <c>null</c>, default load settings will be used.
    /// </param>
    /// <returns>A <see cref="JObject" /> populated from the string that contains JSON.</returns>
    /// <exception cref="JsonReaderException">
    /// <paramref name="json" /> is not valid JSON.
    /// </exception>
    /// <example>
    /// <code lang="cs" source="..\src\Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParse" title="Parsing a JSON Object from Text" />
    /// </example>
    public new static JObject Parse(string json, JsonLoadSettings? settings)
    {
        using var reader = new JsonTextReader(new StringReader(json));
        var o = Load(reader, settings);

        while (reader.Read())
        {
            // Any content encountered here other than a comment will throw in the reader.
        }

        return o;
    }

    /// <summary>
    /// Creates a <see cref="JObject" /> from an object.
    /// </summary>
    /// <param name="o">The object that will be used to create <see cref="JObject" />.</param>
    /// <returns>A <see cref="JObject" /> with the values of the specified object.</returns>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public new static JObject FromObject(object o) =>
        FromObject(o, JsonSerializer.CreateDefault());

    /// <summary>
    /// Creates a <see cref="JObject" /> from an object.
    /// </summary>
    /// <param name="o">The object that will be used to create <see cref="JObject" />.</param>
    /// <param name="serializer">The <see cref="JsonSerializer" /> that will be used to read the object.</param>
    /// <returns>A <see cref="JObject" /> with the values of the specified object.</returns>
    public new static JObject FromObject(object o, JsonSerializer serializer)
    {
        var token = FromObjectInternal(o, serializer);

        if (token.Type != JTokenType.Object)
        {
            throw new ArgumentException($"Object serialized to {token.Type}. JObject instance expected.");
        }

        return (JObject) token;
    }

    /// <summary>
    /// Writes this token to a <see cref="JsonWriter" />.
    /// </summary>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
    {
        writer.WriteStartObject();

        foreach (var property in properties)
        {
            property.WriteTo(writer, converters);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Gets the <see cref="Argon.JToken" /> with the specified property name.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>The <see cref="Argon.JToken" /> with the specified property name.</returns>
    public JToken? GetValue(string? propertyName) =>
        GetValue(propertyName, StringComparison.Ordinal);

    /// <summary>
    /// Gets the <see cref="Argon.JToken" /> with the specified property name.
    /// The exact property name will be searched for first and if no matching property is found then
    /// the <see cref="StringComparison" /> will be used to match a property.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="comparison">One of the enumeration values that specifies how the strings will be compared.</param>
    /// <returns>The <see cref="Argon.JToken" /> with the specified property name.</returns>
    public JToken? GetValue(string? propertyName, StringComparison comparison)
    {
        if (propertyName == null)
        {
            return null;
        }

        // attempt to get value via dictionary first for performance
        var property = PropertyOrNull(propertyName, comparison);

        return property?.Value;
    }

    /// <summary>
    /// Tries to get the <see cref="Argon.JToken" /> with the specified property name.
    /// The exact property name will be searched for first and if no matching property is found then
    /// the <see cref="StringComparison" /> will be used to match a property.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="comparison">One of the enumeration values that specifies how the strings will be compared.</param>
    /// <returns><c>true</c> if a value was successfully retrieved; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(string propertyName, StringComparison comparison, [NotNullWhen(true)] out JToken? value)
    {
        value = GetValue(propertyName, comparison);
        return value != null;
    }

    #region IDictionary<string,JToken> Members

    /// <summary>
    /// Adds the specified property name.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    public void Add(string propertyName, JToken? value) =>
        Add(new JProperty(propertyName, value));

    /// <summary>
    /// Determines whether the JSON object has the specified property name.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns><c>true</c> if the JSON object has the specified property name; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(string propertyName) =>
        properties.Contains(propertyName);

    ICollection<string> IDictionary<string, JToken?>.Keys => properties.Keys;

    /// <summary>
    /// Removes the property with the specified name.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns><c>true</c> if item was successfully removed; otherwise, <c>false</c>.</returns>
    public bool Remove(string propertyName)
    {
        var property = PropertyOrNull(propertyName);
        if (property == null)
        {
            return false;
        }

        property.Remove();
        return true;
    }

    /// <summary>
    /// Tries to get the <see cref="Argon.JToken" /> with the specified property name.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns><c>true</c> if a value was successfully retrieved; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(string propertyName, [NotNullWhen(true)] out JToken? value)
    {
        var property = PropertyOrNull(propertyName);
        if (property == null)
        {
            value = null;
            return false;
        }

        value = property.Value;
        return true;
    }

    ICollection<JToken?> IDictionary<string, JToken?>.Values => throw new NotImplementedException();

    #endregion

    #region ICollection<KeyValuePair<string,JToken>> Members

    void ICollection<KeyValuePair<string, JToken?>>.Add(KeyValuePair<string, JToken?> item) =>
        Add(new JProperty(item.Key, item.Value));

    void ICollection<KeyValuePair<string, JToken?>>.Clear() =>
        RemoveAll();

    bool ICollection<KeyValuePair<string, JToken?>>.Contains(KeyValuePair<string, JToken?> item)
    {
        var property = PropertyOrNull(item.Key);
        if (property == null)
        {
            return false;
        }

        return property.Value == item.Value;
    }

    void ICollection<KeyValuePair<string, JToken?>>.CopyTo(KeyValuePair<string, JToken?>[] array, int arrayIndex)
    {
        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "arrayIndex is less than 0.");
        }

        if (arrayIndex >= array.Length && arrayIndex != 0)
        {
            throw new ArgumentException("arrayIndex is equal to or greater than the length of array.");
        }

        if (Count > array.Length - arrayIndex)
        {
            throw new ArgumentException("The number of elements in the source JObject is greater than the available space from arrayIndex to the end of the destination array.");
        }

        var index = 0;
        foreach (JProperty property in properties)
        {
            array[arrayIndex + index] = new(property.Name, property.Value);
            index++;
        }
    }

    bool ICollection<KeyValuePair<string, JToken?>>.IsReadOnly => false;

    bool ICollection<KeyValuePair<string, JToken?>>.Remove(KeyValuePair<string, JToken?> item)
    {
        if (!((ICollection<KeyValuePair<string, JToken?>>) this).Contains(item))
        {
            return false;
        }

        ((IDictionary<string, JToken>) this).Remove(item.Key);
        return true;
    }

    #endregion

    internal override int GetDeepHashCode() =>
        ContentsHashCode();

    /// <summary>
    /// Returns an enumerator that can be used to iterate through the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="IEnumerator{T}" /> that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<KeyValuePair<string, JToken?>> GetEnumerator()
    {
        foreach (JProperty property in properties)
        {
            yield return new(property.Name, property.Value);
        }
    }

    /// <summary>
    /// Returns the <see cref="DynamicMetaObject" /> responsible for binding operations performed on this object.
    /// </summary>
    /// <param name="parameter">The expression tree representation of the runtime value.</param>
    /// <returns>
    /// The <see cref="DynamicMetaObject" /> to bind this object.
    /// </returns>
    protected override DynamicMetaObject GetMetaObject(Expression parameter)
    {
#if HAVE_COMPONENT_MODEL
        if (!DynamicIsSupported)
        {
            throw new NotSupportedException(DynamicNotSupportedMessage);
        }
#endif
        // Can be disabled because we throw when dynamic is not supported before
#pragma warning disable IL2026, IL3050
        return new DynamicProxyMetaObject<JObject>(parameter, this, new JObjectDynamicProxy());
#pragma warning restore IL2026, IL3050
    }

    class JObjectDynamicProxy :
        DynamicProxy<JObject>
    {
        public override bool TryGetMember(JObject instance, GetMemberBinder binder, out object? result)
        {
            // result can be null
            result = instance[binder.Name];
            return true;
        }

        public override bool TrySetMember(JObject instance, SetMemberBinder binder, object value)
        {
            // this can throw an error if value isn't a valid for a JValue
            if (value is not JToken v)
            {
                v = new JValue(value);
            }

            instance[binder.Name] = v;
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames(JObject instance)
        {
            foreach (var property in instance.Properties())
            {
                yield return property.Name;
            }
        }
    }
}