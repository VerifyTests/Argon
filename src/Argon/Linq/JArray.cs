// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Represents a JSON array.
/// </summary>
/// <example>
/// <code lang="cs" source="..\src\Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParseArray" title="Parsing a JSON Array from Text" />
/// </example>
public class JArray :
    JContainer,
    IList<JToken>
{
    readonly List<JToken> values = [];

    /// <summary>
    /// Gets the container's children tokens.
    /// </summary>
    protected override IList<JToken> ChildrenTokens => values;

    /// <summary>
    /// Gets the node type for this <see cref="JToken" />.
    /// </summary>
    public override JTokenType Type => JTokenType.Array;

    /// <summary>
    /// Initializes a new instance of the <see cref="JArray" /> class.
    /// </summary>
    public JArray()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JArray" /> class from another <see cref="JArray" /> object.
    /// </summary>
    /// <param name="other">A <see cref="JArray" /> object to copy from.</param>
    public JArray(JArray other)
        : base(other)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JArray" /> class with the specified content.
    /// </summary>
    public JArray(params object[] content) :
        this((object) content)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JArray" /> class with the specified content.
    /// </summary>
    public JArray(object content) =>
        Add(content);

    internal override bool DeepEquals(JToken node) =>
        node is JArray t && ContentsEqual(t);

    internal override JToken CloneToken() =>
        new JArray(this);

    /// <summary>
    /// Loads an <see cref="JArray" /> from a <see cref="JsonReader" />.
    /// </summary>
    /// <param name="reader">A <see cref="JsonReader" /> that will be read for the content of the <see cref="JArray" />.</param>
    /// <returns>A <see cref="JArray" /> that contains the JSON that was read from the specified <see cref="JsonReader" />.</returns>
    public new static JArray Load(JsonReader reader) =>
        Load(reader, null);

    /// <summary>
    /// Loads an <see cref="JArray" /> from a <see cref="JsonReader" />.
    /// </summary>
    /// <param name="reader">A <see cref="JsonReader" /> that will be read for the content of the <see cref="JArray" />.</param>
    /// <param name="settings">
    /// The <see cref="JsonLoadSettings" /> used to load the JSON.
    /// If this is <c>null</c>, default load settings will be used.
    /// </param>
    /// <returns>A <see cref="JArray" /> that contains the JSON that was read from the specified <see cref="JsonReader" />.</returns>
    public new static JArray Load(JsonReader reader, JsonLoadSettings? settings)
    {
        if (reader.TokenType == JsonToken.None)
        {
            if (!reader.Read())
            {
                throw JsonReaderException.Create(reader, "Error reading JArray from JsonReader.");
            }
        }

        reader.MoveToContent();

        if (reader.TokenType != JsonToken.StartArray)
        {
            throw JsonReaderException.Create(reader, $"Error reading JArray from JsonReader. Current JsonReader item is not an array: {reader.TokenType}");
        }

        var array = new JArray();
        array.SetLineInfo(reader as IJsonLineInfo, settings);

        array.ReadTokenFrom(reader, settings);

        return array;
    }

    /// <summary>
    /// Load a <see cref="JArray" /> from a string that contains JSON.
    /// </summary>
    /// <param name="json">A <see cref="String" /> that contains JSON.</param>
    /// <returns>A <see cref="JArray" /> populated from the string that contains JSON.</returns>
    /// <example>
    /// <code lang="cs" source="..\src\Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParseArray" title="Parsing a JSON Array from Text" />
    /// </example>
    public new static JArray Parse(string json) =>
        Parse(json, null);

    /// <summary>
    /// Load a <see cref="JArray" /> from a string that contains JSON.
    /// </summary>
    /// <param name="json">A <see cref="String" /> that contains JSON.</param>
    /// <param name="settings">
    /// The <see cref="JsonLoadSettings" /> used to load the JSON.
    /// If this is <c>null</c>, default load settings will be used.
    /// </param>
    /// <returns>A <see cref="JArray" /> populated from the string that contains JSON.</returns>
    /// <example>
    /// <code lang="cs" source="..\src\Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParseArray" title="Parsing a JSON Array from Text" />
    /// </example>
    public new static JArray Parse(string json, JsonLoadSettings? settings)
    {
        using var reader = new JsonTextReader(new StringReader(json));
        var array = Load(reader, settings);

        while (reader.Read())
        {
            // Any content encountered here other than a comment will throw in the reader.
        }

        return array;
    }

    /// <summary>
    /// Creates a <see cref="JArray" /> from an object.
    /// </summary>
    /// <param name="o">The object that will be used to create <see cref="JArray" />.</param>
    /// <returns>A <see cref="JArray" /> with the values of the specified object.</returns>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public new static JArray FromObject(object o) =>
        FromObject(o, JsonSerializer.CreateDefault());

    /// <summary>
    /// Creates a <see cref="JArray" /> from an object.
    /// </summary>
    /// <param name="o">The object that will be used to create <see cref="JArray" />.</param>
    /// <param name="serializer">The <see cref="JsonSerializer" /> that will be used to read the object.</param>
    /// <returns>A <see cref="JArray" /> with the values of the specified object.</returns>
    public new static JArray FromObject(object o, JsonSerializer serializer)
    {
        var token = FromObjectInternal(o, serializer);

        if (token is JArray array)
        {
            return array;
        }

        throw new($"Object serialized to {token.Type}. JArray instance expected.");

    }

    /// <summary>
    /// Writes this token to a <see cref="JsonWriter" />.
    /// </summary>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
    {
        writer.WriteStartArray();

        foreach (var value in values)
        {
            value.WriteTo(writer, converters);
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Gets the <see cref="JToken" /> with the specified key.
    /// </summary>
    public override JToken? this[object key]
    {
        get
        {
            if (key is int i)
            {
                return GetItem(i);
            }

            throw new($"Accessed JArray values with invalid key value: {MiscellaneousUtils.ToString(key)}. Int32 array index expected.");

        }
        set
        {
            if (key is not int i)
            {
                throw new($"Set JArray values with invalid key value: {MiscellaneousUtils.ToString(key)}. Int32 array index expected.");
            }

            SetItem(i, value);
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="Argon.JToken" /> at the specified index.
    /// </summary>
    public JToken this[int index]
    {
        get => GetItem(index);
        set => SetItem(index, value);
    }

    internal override int IndexOfItem(JToken? item)
    {
        if (item == null)
        {
            return -1;
        }

        return values.IndexOfReference(item);
    }

    #region IList<JToken> Members

    /// <summary>
    /// Determines the index of a specific item in the <see cref="JArray" />.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="JArray" />.</param>
    /// <returns>
    /// The index of <paramref name="item" /> if found in the list; otherwise, -1.
    /// </returns>
    public int IndexOf(JToken item) =>
        IndexOfItem(item);

    /// <summary>
    /// Inserts an item to the <see cref="JArray" /> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
    /// <param name="item">The object to insert into the <see cref="JArray" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is not a valid index in the <see cref="JArray" />.
    /// </exception>
    public void Insert(int index, JToken item) =>
        InsertItem(index, item, false);

    /// <summary>
    /// Removes the <see cref="JArray" /> item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is not a valid index in the <see cref="JArray" />.
    /// </exception>
    public void RemoveAt(int index) =>
        RemoveItemAt(index);

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="IEnumerator{T}" /> of <see cref="JToken" /> that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<JToken> GetEnumerator() =>
        Children().GetEnumerator();

    #endregion

    #region ICollection<JToken> Members

    /// <summary>
    /// Adds an item to the <see cref="JArray" />.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="JArray" />.</param>
    public void Add(JToken item) =>
        Add((object) item);

    /// <summary>
    /// Removes all items from the <see cref="JArray" />.
    /// </summary>
    public void Clear() =>
        ClearItems();

    /// <summary>
    /// Determines whether the <see cref="JArray" /> contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="JArray" />.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="item" /> is found in the <see cref="JArray" />; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(JToken item) =>
        ContainsItem(item);

    /// <summary>
    /// Copies the elements of the <see cref="JArray" /> to an array, starting at a particular array index.
    /// </summary>
    public void CopyTo(JToken[] array, int arrayIndex) =>
        CopyItemsTo(array, arrayIndex);

    /// <summary>
    /// Gets a value indicating whether the <see cref="JArray" /> is read-only.
    /// </summary>
    /// <returns><c>true</c> if the <see cref="JArray" /> is read-only; otherwise, <c>false</c>.</returns>
    public bool IsReadOnly => false;

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="JArray" />.
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="JArray" />.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="item" /> was successfully removed from the <see cref="JArray" />; otherwise, <c>false</c>. This method also returns <c>false</c> if <paramref name="item" /> is not found in the original <see cref="JArray" />.
    /// </returns>
    public bool Remove(JToken item) =>
        RemoveItem(item);

    #endregion

    internal override int GetDeepHashCode() =>
        ContentsHashCode();
}