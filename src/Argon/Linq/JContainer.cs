// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Represents a token that can contain other tokens.
/// </summary>
public abstract class JContainer :
    JToken,
    IList<JToken>
{
    /// <summary>
    /// Gets the container's children tokens.
    /// </summary>
    protected abstract IList<JToken> ChildrenTokens { get; }

    internal JContainer()
    {
    }

    internal JContainer(JContainer other) :
        this()
    {
        // indexed loop: enumerating the container goes through IList<JToken> and
        // boxes an enumerator per cloned container
        var children = other.ChildrenTokens;
        for (var i = 0; i < children.Count; i++)
        {
            TryAddInternal(i, children[i], false);
        }

        SetLineInfo(this, null);
    }

    /// <summary>
    /// Gets a value indicating whether this token has child tokens.
    /// </summary>
    public override bool HasValues => ChildrenTokens.Count > 0;

    internal bool ContentsEqual(JContainer container)
    {
        if (container == this)
        {
            return true;
        }

        var t1 = ChildrenTokens;
        var t2 = container.ChildrenTokens;

        if (t1.Count != t2.Count)
        {
            return false;
        }

        for (var i = 0; i < t1.Count; i++)
        {
            if (!t1[i].DeepEquals(t2[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get the first child token of this token.
    /// </summary>
    public override JToken? First
    {
        get
        {
            var children = ChildrenTokens;
            return children.Count > 0 ? children[0] : null;
        }
    }

    /// <summary>
    /// Get the last child token of this token.
    /// </summary>
    public override JToken? Last
    {
        get
        {
            var children = ChildrenTokens;
            var count = children.Count;
            return count > 0 ? children[count - 1] : null;
        }
    }

    /// <summary>
    /// Returns a collection of the child tokens of this token, in document order.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> containing the child tokens of this <see cref="JToken" />, in document order.
    /// </returns>
    public override JEnumerable<JToken> Children() =>
        new(ChildrenTokens);

    /// <summary>
    /// Returns a collection of the child values of this token, in document order.
    /// </summary>
    /// <typeparam name="T">The type to convert the values to.</typeparam>
    /// <returns>
    /// A <see cref="IEnumerable{T}" /> containing the child values of this <see cref="JToken" />, in document order.
    /// </returns>
    public override IEnumerable<T?> Values<T>()
        where T : default =>
        ChildrenTokens.Convert<JToken, T>();

    /// <summary>
    /// Returns a collection of the descendant tokens for this token in document order.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> containing the descendant tokens of the <see cref="JToken" />.</returns>
    public IEnumerable<JToken> Descendants() =>
        GetDescendants(false);

    /// <summary>
    /// Returns a collection of the tokens that contain this token, and all descendant tokens of this token, in document order.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> containing this token, and all the descendant tokens of the <see cref="JToken" />.</returns>
    public IEnumerable<JToken> DescendantsAndSelf() =>
        GetDescendants(true);

    IEnumerable<JToken> GetDescendants(bool self)
    {
        if (self)
        {
            yield return this;
        }

        // pre-order pointer walk over the sibling/parent links: a single iterator for
        // the whole traversal instead of nested iterators per container depth
        var current = First;
        while (current != null)
        {
            yield return current;

            if (current is JContainer {HasValues: true} container)
            {
                current = container.First;
                continue;
            }

            // climb out of finished containers
            while (current != null && current != this && current == current.Parent!.Last)
            {
                current = current.Parent;
            }

            if (current == null || current == this)
            {
                yield break;
            }

            current = current.Next;
        }
    }

    internal static bool IsMultiContent([NotNullWhen(true)] object? content) =>
        content is
            IEnumerable and
            not string and
            not JToken and
            not byte[];

    JToken EnsureParentToken(JToken? item, bool skipParentCheck)
    {
        if (item == null)
        {
            return JValue.CreateNull();
        }

        if (skipParentCheck)
        {
            return item;
        }

        // to avoid a token having multiple parents or creating a recursive loop, create a copy if...
        // the item already has a parent
        // the item is being added to itself
        // the item is being added to the root parent of itself
        if (item.Parent != null || item == this || (item.HasValues && Root == item))
        {
            item = item.CloneToken();
        }

        return item;
    }

    internal abstract int IndexOfItem(JToken? item);

    internal virtual bool InsertItem(int index, JToken? item, bool skipParentCheck)
    {
        var children = ChildrenTokens;

        if (index > children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the List.");
        }

        item = EnsureParentToken(item, skipParentCheck);

        var previous = index == 0 ? null : children[index - 1];
        // haven't inserted new token yet so next token is still at the inserting index
        var next = index == children.Count ? null : children[index];

        ValidateToken(item, null);

        item.Parent = this;

        item.Previous = previous;
        previous?.Next = item;

        item.Next = next;
        next?.Previous = item;

        children.Insert(index, item);

        return true;
    }

    internal virtual void RemoveItemAt(int index)
    {
        var children = ChildrenTokens;

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is less than 0.");
        }

        if (index >= children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is equal to or greater than Count.");
        }

        var item = children[index];
        var previous = index == 0 ? null : children[index - 1];
        var next = index == children.Count - 1 ? null : children[index + 1];

        previous?.Next = next;

        next?.Previous = previous;

        item.Parent = null;
        item.Previous = null;
        item.Next = null;

        children.RemoveAt(index);
    }

    internal virtual bool RemoveItem(JToken? item)
    {
        if (item != null)
        {
            var index = IndexOfItem(item);
            if (index >= 0)
            {
                RemoveItemAt(index);
                return true;
            }
        }

        return false;
    }

    internal virtual JToken GetItem(int index) =>
        ChildrenTokens[index];

    internal virtual void SetItem(int index, JToken? item)
    {
        var children = ChildrenTokens;

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is less than 0.");
        }

        if (index >= children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is equal to or greater than Count.");
        }

        var existing = children[index];

        if (IsTokenUnchanged(existing, item))
        {
            return;
        }

        item = EnsureParentToken(item, false);

        ValidateToken(item, existing);

        var previous = index == 0 ? null : children[index - 1];
        var next = index == children.Count - 1 ? null : children[index + 1];

        item.Parent = this;

        item.Previous = previous;
        previous?.Next = item;

        item.Next = next;
        next?.Previous = item;

        children[index] = item;

        existing.Parent = null;
        existing.Previous = null;
        existing.Next = null;
    }

    internal virtual void ClearItems()
    {
        var children = ChildrenTokens;

        foreach (var item in children)
        {
            item.Parent = null;
            item.Previous = null;
            item.Next = null;
        }

        children.Clear();
    }

    internal virtual void ReplaceItem(JToken existing, JToken replacement)
    {
        if (existing.Parent != this)
        {
            return;
        }

        var index = IndexOfItem(existing);
        SetItem(index, replacement);
    }

    internal virtual bool ContainsItem(JToken? item) =>
        IndexOfItem(item) != -1;

    internal virtual void CopyItemsTo(Array array, int arrayIndex)
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
        foreach (var token in ChildrenTokens)
        {
            array.SetValue(token, arrayIndex + index);
            index++;
        }
    }

    internal static bool IsTokenUnchanged(JToken currentValue, JToken? newValue)
    {
        if (currentValue is JValue v1)
        {
            if (newValue == null)
            {
                // null will get turned into a JValue of type null
                return v1.Type == JTokenType.Null;
            }

            return v1.Equals(newValue);
        }

        return false;
    }

    internal virtual void ValidateToken(JToken o, JToken? existing)
    {
        if (o.Type == JTokenType.Property)
        {
            throw new ArgumentException($"Can not add {o.GetType()} to {GetType()}.");
        }
    }

    /// <summary>
    /// Adds the specified content as children of this <see cref="JToken" />.
    /// </summary>
    /// <param name="content">The content to be added.</param>
    public virtual void Add(object? content) =>
        TryAddInternal(ChildrenTokens.Count, content, false);

    internal bool TryAdd(object? content) =>
        TryAddInternal(ChildrenTokens.Count, content, false);

    internal void AddAndSkipParentCheck(JToken token) =>
        TryAddInternal(ChildrenTokens.Count, token, true);

    /// <summary>
    /// Adds the specified content as the first children of this <see cref="JToken" />.
    /// </summary>
    /// <param name="content">The content to be added.</param>
    public void AddFirst(object? content) =>
        TryAddInternal(0, content, false);

    internal bool TryAddInternal(int index, object? content, bool skipParentCheck)
    {
        if (IsMultiContent(content))
        {
            var enumerable = (IEnumerable) content;

            var multiIndex = index;
            foreach (var c in enumerable)
            {
                if (TryAddInternal(multiIndex, c, skipParentCheck))
                {
                    multiIndex++;
                }
            }

            return true;
        }

        var item = CreateFromContent(content);

        return InsertItem(index, item, skipParentCheck);
    }

    internal static JToken CreateFromContent(object? content)
    {
        if (content is JToken token)
        {
            return token;
        }

        return new JValue(content);
    }

    /// <summary>
    /// Creates a <see cref="JsonWriter" /> that can be used to add tokens to the <see cref="JToken" />.
    /// </summary>
    /// <returns>A <see cref="JsonWriter" /> that is ready to have content written to it.</returns>
    public JsonWriter CreateWriter() =>
        new JTokenWriter(this);

    /// <summary>
    /// Replaces the child nodes of this token with the specified content.
    /// </summary>
    public void ReplaceAll(object content)
    {
        ClearItems();
        Add(content);
    }

    /// <summary>
    /// Removes the child nodes from this token.
    /// </summary>
    public void RemoveAll() =>
        ClearItems();

    internal void ReadTokenFrom(JsonReader reader, JsonLoadSettings? options)
    {
        var startDepth = reader.Depth;

        if (!reader.Read())
        {
            throw JsonReaderException.Create(reader, $"Error reading {GetType().Name} from JsonReader.");
        }

        ReadContentFrom(reader, options);

        var endDepth = reader.Depth;

        if (endDepth > startDepth)
        {
            throw JsonReaderException.Create(reader, $"Unexpected end of content while loading {GetType().Name}.");
        }
    }

    void ReadContentFrom(JsonReader r, JsonLoadSettings? settings)
    {
        var lineInfo = r as IJsonLineInfo;

        var parent = this;

        do
        {
            if (parent is JProperty {Value: not null} parentProperty)
            {
                if (parentProperty == this)
                {
                    return;
                }

                parent = parentProperty.Parent;
            }

            MiscellaneousUtils.Assert(parent != null);

            switch (r.TokenType)
            {
                case JsonToken.None:
                    // new reader. move to actual content
                    break;
                case JsonToken.StartArray:
                    var a = new JArray();
                    a.SetLineInfo(lineInfo, settings);
                    parent.Add(a);
                    parent = a;
                    break;

                case JsonToken.EndArray:
                    if (parent == this)
                    {
                        return;
                    }

                    parent = parent.Parent;
                    break;
                case JsonToken.StartObject:
                    var o = new JObject();
                    o.SetLineInfo(lineInfo, settings);
                    parent.Add(o);
                    parent = o;
                    break;
                case JsonToken.EndObject:
                    if (parent == this)
                    {
                        return;
                    }

                    parent = parent.Parent;
                    break;
                case JsonToken.String:
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.Date:
                case JsonToken.Boolean:
                case JsonToken.Bytes:
                    var value = r.Value;
                    // for the typical value shapes the token type already determines the
                    // JTokenType, skipping the type-test ladder in the JValue(object) ctor;
                    // unusual shapes (custom readers) keep the general path
                    var knownType = r.TokenType switch
                    {
                        JsonToken.String when value is string => JTokenType.String,
                        JsonToken.Integer when value is long or int or ulong or BigInteger => JTokenType.Integer,
                        JsonToken.Float when value is double or decimal or float => JTokenType.Float,
                        JsonToken.Date when value is DateTime or DateTimeOffset => JTokenType.Date,
                        JsonToken.Boolean when value is bool => JTokenType.Boolean,
                        _ => JTokenType.None
                    };
                    var v = knownType == JTokenType.None
                        ? new JValue(value)
                        : new JValue(value, knownType);
                    v.SetLineInfo(lineInfo, settings);
                    parent.Add(v);
                    break;
                case JsonToken.Comment:
                    if (settings is {CommentHandling: CommentHandling.Load})
                    {
                        v = JValue.CreateComment((string?) r.GetValue());
                        v.SetLineInfo(lineInfo, settings);
                        parent.Add(v);
                    }

                    break;
                case JsonToken.Null:
                    v = JValue.CreateNull();
                    v.SetLineInfo(lineInfo, settings);
                    parent.Add(v);
                    break;
                case JsonToken.Undefined:
                    v = JValue.CreateUndefined();
                    v.SetLineInfo(lineInfo, settings);
                    parent.Add(v);
                    break;
                case JsonToken.PropertyName:
                    var property = ReadProperty(r, settings, lineInfo, parent);
                    parent = property;
                    break;
                default:
                    throw new InvalidOperationException($"The JsonReader should not be on a token of type {r.TokenType}.");
            }
        } while (r.Read());
    }

    static JProperty ReadProperty(JsonReader reader, JsonLoadSettings? settings, IJsonLineInfo? lineInfo, JContainer parent)
    {
        var parentObject = (JObject) parent;
        var propertyName = (string) reader.GetValue();
        var property = new JProperty(propertyName);
        property.SetLineInfo(lineInfo, settings);
        try
        {
            // JObject.ValidateToken rejects duplicate names, so a separate
            // pre-check would hash the name a second time on every property
            parentObject.Add(property);
        }
        catch (ArgumentException)
        {
            throw JsonReaderException.Create(reader, $"Property with the name '{propertyName}' already exists in the current JSON object.");
        }

        return property;
    }

    internal int ContentsHashCode()
    {
        var hashCode = 0;
        foreach (var item in ChildrenTokens)
        {
            hashCode ^= item.GetDeepHashCode();
        }

        return hashCode;
    }

    #region IList<JToken> Members

    int IList<JToken>.IndexOf(JToken item) =>
        IndexOfItem(item);

    void IList<JToken>.Insert(int index, JToken item) =>
        InsertItem(index, item, false);

    void IList<JToken>.RemoveAt(int index) =>
        RemoveItemAt(index);

    JToken IList<JToken>.this[int index]
    {
        get => GetItem(index);
        set => SetItem(index, value);
    }

    #endregion

    #region ICollection<JToken> Members

    void ICollection<JToken>.Add(JToken item) =>
        Add(item);

    void ICollection<JToken>.Clear() =>
        ClearItems();

    bool ICollection<JToken>.Contains(JToken item) =>
        ContainsItem(item);

    void ICollection<JToken>.CopyTo(JToken[] array, int arrayIndex) =>
        CopyItemsTo(array, arrayIndex);

    bool ICollection<JToken>.IsReadOnly => false;

    bool ICollection<JToken>.Remove(JToken item) =>
        RemoveItem(item);

    #endregion

    /// <summary>
    /// Gets the count of child JSON tokens.
    /// </summary>
    public int Count => ChildrenTokens.Count;
}