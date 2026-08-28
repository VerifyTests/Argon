// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.


// ReSharper disable UnusedVariable

#region CustomJsonReaderTypes

public class XmlJsonReader(XmlReader reader) : JsonReader
{
    readonly Stack<JTokenType> stateStack = new();

    JTokenType? valueType;

    JTokenType PeekState()
    {
        var current = stateStack.Count > 0 ? stateStack.Peek() : JTokenType.None;
        return current;
    }

    public override bool Read()
    {
        if (HandleValueType())
        {
            return true;
        }

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    var typeName = reader.GetAttribute("type");
                    if (typeName == null)
                    {
                        throw new("No type specified.");
                    }

                    valueType = (JTokenType) Enum.Parse(typeof(JTokenType), typeName, true);

                    switch (PeekState())
                    {
                        case JTokenType.None:
                            HandleValueType();
                            return true;
                        case JTokenType.Object:
                            SetToken(JsonToken.PropertyName, reader.LocalName);
                            stateStack.Push(JTokenType.Property);
                            return true;
                        case JTokenType.Array:
                            continue;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case XmlNodeType.EndElement:
                    switch (stateStack.Peek())
                    {
                        case JTokenType.Object:
                            SetToken(JsonToken.EndObject);
                            stateStack.Pop();
                            if (PeekState() == JTokenType.Property)
                            {
                                stateStack.Pop();
                            }

                            return true;
                        case JTokenType.Array:
                            SetToken(JsonToken.EndArray);
                            stateStack.Pop();
                            if (PeekState() == JTokenType.Property)
                            {
                                stateStack.Pop();
                            }

                            return true;
                    }

                    stateStack.Pop();
                    if (PeekState() == JTokenType.Property)
                    {
                        stateStack.Pop();
                    }

                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    switch (valueType)
                    {
                        case JTokenType.Integer:
                            SetToken(JsonToken.Integer, Convert.ToInt64(reader.Value, InvariantCulture));
                            break;
                        case JTokenType.Float:
                            SetToken(JsonToken.Float, Convert.ToDouble(reader.Value, InvariantCulture));
                            break;
                        case JTokenType.String:
                        case JTokenType.Uri:
                        case JTokenType.TimeSpan:
                        case JTokenType.Guid:
                            SetToken(JsonToken.String, reader.Value);
                            break;
                        case JTokenType.Boolean:
                            SetToken(JsonToken.Boolean, Convert.ToBoolean(reader.Value, InvariantCulture));
                            break;
                        case JTokenType.Date:
                            SetToken(JsonToken.Date, Convert.ToDateTime(reader.Value, InvariantCulture));
                            break;
                        case JTokenType.Bytes:
                            SetToken(JsonToken.Bytes, Convert.FromBase64String(reader.Value));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    stateStack.Push(valueType.Value);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return false;
    }

    bool HandleValueType()
    {
        switch (valueType)
        {
            case JTokenType.Null:
                SetToken(JsonToken.Null);
                valueType = null;

                if (PeekState() == JTokenType.Property)
                {
                    stateStack.Pop();
                }

                return true;
            case JTokenType.Object:
                SetToken(JsonToken.StartObject);
                stateStack.Push(JTokenType.Object);
                valueType = null;
                return true;
            case JTokenType.Array:
                SetToken(JsonToken.StartArray);
                stateStack.Push(JTokenType.Array);
                valueType = null;
                return true;
        }

        return false;
    }

    public override int? ReadAsInt32()
    {
        if (Read())
        {
            return Value == null ? null : Convert.ToInt32(Value);
        }

        return null;
    }

    public override string ReadAsString()
    {
        if (Read())
        {
            return (string) Value;
        }

        return null;
    }

    public override byte[] ReadAsBytes()
    {
        if (Read())
        {
            return (byte[]) Value;
        }

        return null;
    }

    public override decimal? ReadAsDecimal()
    {
        if (Read())
        {
            return Value == null ? null : Convert.ToDecimal(Value);
        }

        return null;
    }

    public override DateTime? ReadAsDateTime()
    {
        if (Read())
        {
            return Value == null ? null : Convert.ToDateTime(Value);
        }

        return null;
    }

    public override DateTimeOffset? ReadAsDateTimeOffset()
    {
        if (Read())
        {
            return Value == null ? null : Convert.ToDateTime(Value);
        }

        return null;
    }
}

#endregion

public class CustomJsonReader : TestFixtureBase
{
    [Fact]
    public async Task Example()
    {
        #region CustomJsonReaderUsage

        var xml = """
                  <Root type="Object">
                    <Null type="Null" />
                    <String type="String">This is a string!</String>
                    <Char type="String">!</Char>
                    <Integer type="Integer">123</Integer>
                    <DateTime type="Date">2001-02-22T20:59:59Z</DateTime>
                    <DateTimeOffset type="Date">2001-02-22T20:59:59+12:00</DateTimeOffset>
                    <Float type="Float">1.1</Float>
                    <Double type="Float">3.14</Double>
                    <Decimal type="Float">19.95</Decimal>
                    <Guid type="Guid">d66eab59-3715-4b35-9e06-fa61c1216eaa</Guid>
                    <Uri type="Uri">http://james.newtonking.com</Uri>
                    <Array type="Array">
                      <Item type="Integer">1</Item>
                      <Item type="Bytes">SGVsbG8gd29ybGQh</Item>
                      <Item type="Boolean">True</Item>
                    </Array>
                    <Object type="Object">
                      <String type="String">This is a string!</String>
                      <Null type="Null" />
                    </Object>
                  </Root>
                  """;

        var sr = new StringReader(xml);

        using (var xmlReader = XmlReader.Create(sr, new() {IgnoreWhitespace = true}))
        using (var reader = new XmlJsonReader(xmlReader))
        {
            var o = JObject.Load(reader);
            //{
            //  "Null": null,
            //  "String": "This is a string!",
            //  "Char": "!",
            //  "Integer": 123,
            //  "DateTime": "2001-02-23T09:59:59+13:00",
            //  "DateTimeOffset": "2001-02-22T21:59:59+13:00",
            //  "Float": 1.1,
            //  "Double": 3.14,
            //  "Decimal": 19.95,
            //  "Guid": "d66eab59-3715-4b35-9e06-fa61c1216eaa",
            //  "Uri": "http://james.newtonking.com",
            //  "Array": [
            //    1,
            //    "SGVsbG8gd29ybGQh",
            //    true
            //  ],
            //  "Object": {
            //    "String": "This is a string!",
            //    "Null": null
            //  }
            //}
        }

        #endregion

        using (var xmlReader = XmlReader.Create(new StringReader(xml), new() {IgnoreWhitespace = true}))
        using (var reader = new XmlJsonReader(xmlReader))
        {
            await reader.VerifyReaderState();
        }
    }
}