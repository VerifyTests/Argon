// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Represents a writer that provides a fast, non-cached, forward-only way of generating JSON data.
/// </summary>
public class JTokenWriter :
    JsonWriter
{
    JContainer? token;

    JContainer? parent;

    // used when writer is writing single value and the value has no containing parent
    JValue? value;

    /// <summary>
    /// Gets the <see cref="JToken" /> at the writer's current position.
    /// </summary>
    public JToken? CurrentToken { get; private set; }

    /// <summary>
    /// Gets the token being written.
    /// </summary>
    public JToken? Token
    {
        get
        {
            if (token != null)
            {
                return token;
            }

            return value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JTokenWriter" /> class writing to the given <see cref="JContainer" />.
    /// </summary>
    /// <param name="container">The container being written to.</param>
    public JTokenWriter(JContainer container)
    {
        token = container;
        parent = container;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JTokenWriter" /> class.
    /// </summary>
    public JTokenWriter()
    {
    }

    /// <summary>
    /// Flushes whatever is in the buffer to the underlying <see cref="JContainer" />.
    /// </summary>
    public override void Flush()
    {
    }

    /// <summary>
    /// Writes the beginning of a JSON object.
    /// </summary>
    public override void WriteStartObject()
    {
        base.WriteStartObject();

        AddParent(new JObject());
    }

    void AddParent(JContainer container)
    {
        if (parent == null)
        {
            token = container;
        }
        else
        {
            parent.AddAndSkipParentCheck(container);
        }

        parent = container;
        CurrentToken = container;
    }

    void RemoveParent()
    {
        CurrentToken = parent;
        parent = parent!.Parent;

        if (parent is {Type: JTokenType.Property})
        {
            parent = parent.Parent;
        }
    }

    /// <summary>
    /// Writes the beginning of a JSON array.
    /// </summary>
    public override void WriteStartArray()
    {
        base.WriteStartArray();

        AddParent(new JArray());
    }

    /// <summary>
    /// Writes the end.
    /// </summary>
    protected override void WriteEnd(JsonToken token) =>
        RemoveParent();

    /// <summary>
    /// Writes the property name of a name/value pair on a JSON object.
    /// </summary>
    public override void WritePropertyName(string name)
    {
        // avoid duplicate property name exception
        // last property name wins
        (parent as JObject)?.Remove(name);

        AddParent(new JProperty(name));

        // don't set state until after in case of an error
        // incorrect state will cause issues if writer is disposed when closing open properties
        base.WritePropertyName(name);
    }

    /// <summary>
    /// Writes the property name of a name/value pair on a JSON object.
    /// </summary>
    public override void WritePropertyName(CharSpan name)
    {
        var nameString = name.ToString();

        // avoid duplicate property name exception
        // last property name wins
        (parent as JObject)?.Remove(nameString);

        AddParent(new JProperty(nameString));

        // don't set state until after in case of an error
        // incorrect state will cause issues if writer is disposed when closing open properties
        base.WritePropertyName(name);
    }

    void AddRawValue(object? value, JTokenType type) =>
        AddJValue(new(value, type));

    internal void AddJValue(JValue? value)
    {
        if (parent == null)
        {
            this.value = value ?? JValue.CreateNull();
            CurrentToken = this.value;
            return;
        }

        // TryAdd will return false if an invalid JToken type is added.
        // For example, a JComment can't be added to a JObject.
        // If there is an invalid JToken type then skip it.
        if (parent.TryAdd(value))
        {
            CurrentToken = parent.Last;

            if (parent.Type == JTokenType.Property)
            {
                parent = parent.Parent;
            }
        }
    }

    #region WriteValue methods

    /// <summary>
    /// Writes a <see cref="Object" /> value.
    /// An error will be raised if the value cannot be written as a single JSON token.
    /// </summary>
    public override void WriteValue(object? value)
    {
        if (value is BigInteger)
        {
            InternalWriteValue(JsonToken.Integer);
            AddRawValue(value, JTokenType.Integer);
        }
        else
        {
            base.WriteValue(value);
        }
    }

    /// <summary>
    /// Writes a null value.
    /// </summary>
    public override void WriteNull()
    {
        base.WriteNull();
        AddJValue(JValue.CreateNull());
    }

    /// <summary>
    /// Writes an undefined value.
    /// </summary>
    public override void WriteUndefined()
    {
        base.WriteUndefined();
        AddJValue(JValue.CreateUndefined());
    }

    /// <summary>
    /// Writes raw JSON.
    /// </summary>
    public override void WriteRaw(char? json) =>
        AddJValue(new JRaw(json));

    /// <summary>
    /// Writes raw JSON.
    /// </summary>
    public override void WriteRaw(string? json) =>
        AddJValue(new JRaw(json));

    /// <summary>
    /// Writes raw JSON.
    /// </summary>
    public override void WriteRaw(StringBuilder? json) =>
        WriteRaw(json?.ToString());

    /// <summary>
    /// Writes raw JSON.
    /// </summary>
    public override void WriteRaw(CharSpan json) =>
        WriteRaw(json.ToString());

    /// <summary>
    /// Writes a comment <c>/*...*/</c> containing the specified text.
    /// </summary>
    public override void WriteComment(string? text)
    {
        base.WriteComment(text);
        AddJValue(JValue.CreateComment(text));
    }

    /// <summary>
    /// Writes a comment <c>/*...*/</c> containing the specified text.
    /// </summary>
    public override void WriteComment(CharSpan text)
    {
        base.WriteComment(text);
        AddJValue(JValue.CreateComment(text.ToString()));
    }

    /// <summary>
    /// Writes a <see cref="String" /> value.
    /// </summary>
    public override void WriteValue(string? value)
    {
        if (value == null)
        {
            WriteNull();
            return;
        }

        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="String" /> value.
    /// </summary>
    public override void WriteValue(CharSpan value)
    {
        base.WriteValue(value);
        AddJValue(new(value.ToString()));
    }

    /// <summary>
    /// Writes a <see cref="Int32" /> value.
    /// </summary>
    public override void WriteValue(int value)
    {
        base.WriteValue(value);
        AddRawValue(value, JTokenType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="UInt32" /> value.
    /// </summary>
    public override void WriteValue(uint value)
    {
        base.WriteValue(value);
        AddRawValue(value, JTokenType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="Int64" /> value.
    /// </summary>
    public override void WriteValue(long value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="UInt64" /> value.
    /// </summary>
    public override void WriteValue(ulong value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="Single" /> value.
    /// </summary>
    public override void WriteValue(float value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="Double" /> value.
    /// </summary>
    public override void WriteValue(double value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="Boolean" /> value.
    /// </summary>
    public override void WriteValue(bool value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="Int16" /> value.
    /// </summary>
    public override void WriteValue(short value)
    {
        base.WriteValue(value);
        AddRawValue(value, JTokenType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="UInt16" /> value.
    /// </summary>
    public override void WriteValue(ushort value)
    {
        base.WriteValue(value);
        AddRawValue(value, JTokenType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="Char" /> value.
    /// </summary>
    public override void WriteValue(char value)
    {
        base.WriteValue(value);
        var s = value.ToString(InvariantCulture);
        AddJValue(new(s));
    }

    /// <summary>
    /// Writes a <see cref="Byte" /> value.
    /// </summary>
    public override void WriteValue(byte value)
    {
        base.WriteValue(value);
        AddRawValue(value, JTokenType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="SByte" /> value.
    /// </summary>
    public override void WriteValue(sbyte value)
    {
        base.WriteValue(value);
        AddRawValue(value, JTokenType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="Decimal" /> value.
    /// </summary>
    public override void WriteValue(decimal value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="DateTime" /> value.
    /// </summary>
    public override void WriteValue(DateTime value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="DateTimeOffset" /> value.
    /// </summary>
    public override void WriteValue(DateTimeOffset value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="Byte" />[] value.
    /// </summary>
    public override void WriteValue(byte[]? value)
    {
        if (value == null)
        {
            WriteNull();
            return;
        }

        base.WriteValue(value);
        AddJValue(new(value, JTokenType.Bytes));
    }

    /// <summary>
    /// Writes a <see cref="TimeSpan" /> value.
    /// </summary>
    public override void WriteValue(TimeSpan value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="Guid" /> value.
    /// </summary>
    public override void WriteValue(Guid value)
    {
        base.WriteValue(value);
        AddJValue(new(value));
    }

    /// <summary>
    /// Writes a <see cref="Uri" /> value.
    /// </summary>
    public override void WriteValue(Uri? value)
    {
        if (value == null)
        {
            WriteNull();
            return;
        }

        base.WriteValue(value);
        AddJValue(new(value));
    }

    #endregion

    internal override void WriteToken(JsonReader reader, bool writeChildren, bool writeComments)
    {
        // cloning the token rather than reading then writing it doesn't lose some type information, e.g. Guid, byte[], etc
        if (reader is JTokenReader tokenReader && writeChildren && writeComments)
        {
            if (tokenReader.TokenType == JsonToken.None)
            {
                if (!tokenReader.Read())
                {
                    return;
                }
            }

            var value = tokenReader.CurrentToken!.CloneToken();

            if (parent == null)
            {
                CurrentToken = value;

                if (token == null && this.value == null)
                {
                    token = value as JContainer;
                    this.value = value as JValue;
                }
            }
            else
            {
                parent.Add(value);
                CurrentToken = parent.Last;

                // if the writer was in a property then move out of it and up to its parent object
                if (parent.Type == JTokenType.Property)
                {
                    parent = parent.Parent;
                    InternalWriteValue(JsonToken.Null);
                }
            }

            tokenReader.Skip();
        }
        else
        {
            base.WriteToken(reader, writeChildren, writeComments);
        }
    }
}