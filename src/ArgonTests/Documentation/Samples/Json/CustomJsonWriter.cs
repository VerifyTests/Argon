// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

#region CustomJsonWriterTypes

public class XmlJsonWriter(XmlWriter writer) :
    JsonWriter
{
    string propertyName;

    public override void WriteComment(CharSpan text)
    {
        base.WriteComment(text);
        writer.WriteComment(text.ToString());
    }

    public override void WriteComment(string text)
    {
        base.WriteComment(text);
        writer.WriteComment(text);
    }

    public override void WritePropertyName(string name)
    {
        base.WritePropertyName(name);
        propertyName = name;
    }

    public override void WriteNull()
    {
        base.WriteNull();

        WriteValueElement(JTokenType.Null);
        writer.WriteEndElement();
    }

    public override void WriteRaw(string json) =>
        throw new NotImplementedException();

    public override void WriteRaw(char? json) =>
        throw new NotImplementedException();

    public override void WriteRaw(StringBuilder json) =>
        throw new NotImplementedException();

    public override void WriteRaw(CharSpan json) =>
        throw new NotImplementedException();

    public override void WriteValue(DateTime value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Date);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(DateTimeOffset value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Date);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(Guid value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Guid);
        writer.WriteValue(value.ToString());
        writer.WriteEndElement();
    }

    public override void WriteValue(TimeSpan value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.TimeSpan);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(Uri value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Uri);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(string value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.String);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(CharSpan value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.String);
        writer.WriteValue(value.ToString());
        writer.WriteEndElement();
    }

    public override void WriteValue(int value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Integer);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(long value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Integer);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(short value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Integer);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(byte value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Integer);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(bool value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Boolean);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(char value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.String);
        writer.WriteValue(value.ToString(InvariantCulture));
        writer.WriteEndElement();
    }

    public override void WriteValue(decimal value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Float);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(double value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Float);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    public override void WriteValue(float value)
    {
        base.WriteValue(value);

        WriteValueElement(JTokenType.Float);
        writer.WriteValue(value);
        writer.WriteEndElement();
    }

    void WriteValueElement(JTokenType type)
    {
        if (propertyName == null)
        {
            WriteValueElement("Item", type);
        }
        else
        {
            WriteValueElement(propertyName, type);
            propertyName = null;
        }
    }

    void WriteValueElement(string elementName, JTokenType type)
    {
        writer.WriteStartElement(elementName);
        writer.WriteAttributeString("type", type.ToString());
    }

    public override void WriteStartArray()
    {
        var isStart = WriteState == WriteState.Start;

        base.WriteStartArray();

        if (isStart)
        {
            WriteValueElement("Root", JTokenType.Array);
        }
        else
        {
            WriteValueElement(JTokenType.Array);
        }
    }

    public override void WriteStartObject()
    {
        var isStart = WriteState == WriteState.Start;

        base.WriteStartObject();

        if (isStart)
        {
            WriteValueElement("Root", JTokenType.Object);
        }
        else
        {
            WriteValueElement(JTokenType.Object);
        }
    }

    public override void WriteEndArray()
    {
        base.WriteEndArray();
        writer.WriteEndElement();
    }

    public override void WriteEndObject()
    {
        base.WriteEndObject();
        writer.WriteEndElement();
    }

    public override void Flush() =>
        writer.Flush();

    protected override void WriteIndent()
    {
        writer.WriteWhitespace(Environment.NewLine);

        // levels of indentation multiplied by the indent count
        var currentIndentCount = Top * 2;

        while (currentIndentCount > 0)
        {
            // write up to a max of 10 characters at once to avoid creating too many new strings
            var writeCount = Math.Min(currentIndentCount, 10);

            writer.WriteWhitespace(new(' ', writeCount));

            currentIndentCount -= writeCount;
        }
    }
}

#endregion

public class CustomJsonWriter : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region CustomJsonWriterUsage

        var user = new
        {
            Name = "James",
            Age = 30,
            Enabled = true,
            Roles = new[]
            {
                "Publisher",
                "Administrator"
            }
        };

        var stringWriter = new StringWriter();

        using (var xmlWriter = XmlWriter.Create(stringWriter, new() {OmitXmlDeclaration = true}))
        using (var writer = new XmlJsonWriter(xmlWriter))
        {
            writer.Formatting = Formatting.Indented;

            var serializer = new JsonSerializer();
            serializer.Serialize(writer, user);
        }

        Console.WriteLine(stringWriter.ToString());
        //<Root type="Object">
        //  <Name type="String">James</Name>
        //  <Age type="Integer">30</Age>
        //  <Enabled type="Boolean">true</Enabled>
        //  <Roles type="Array">
        //    <Item type="String">Publisher</Item>
        //    <Item type="String">Administrator</Item>
        //  </Roles>
        //</Root>

        #endregion

        stringWriter = new();

        using (var xmlWriter = XmlWriter.Create(stringWriter, new() {OmitXmlDeclaration = true}))
        using (var writer = new XmlJsonWriter(xmlWriter))
        {
            writer.Formatting = Formatting.Indented;

            writer.WriteStartObject();

            writer.WritePropertyName("Null");
            writer.WriteNull();

            writer.WritePropertyName("String");
            writer.WriteValue("This is a string!");

            writer.WritePropertyName("Char");
            writer.WriteValue('!');

            writer.WritePropertyName("Integer");
            writer.WriteValue(123);

            writer.WritePropertyName("DateTime");
            writer.WriteValue(new DateTime(2001, 2, 22, 20, 59, 59, DateTimeKind.Utc));

            writer.WritePropertyName("DateTimeOffset");
            writer.WriteValue(new DateTimeOffset(2001, 2, 22, 20, 59, 59, TimeSpan.FromHours(12)));

            writer.WritePropertyName("Float");
            writer.WriteValue(1.1f);

            writer.WritePropertyName("Double");
            writer.WriteValue(3.14d);

            writer.WritePropertyName("Decimal");
            writer.WriteValue(19.95m);

            writer.WritePropertyName("Guid");
            writer.WriteValue(Guid.NewGuid());

            writer.WritePropertyName("Uri");
            writer.WriteValue(new Uri("http://james.newtonking.com"));

            writer.WritePropertyName("Array");
            writer.WriteStartArray();
            writer.WriteValue(1);
            writer.WriteValue(2);
            writer.WriteValue(3);
            writer.WriteEndArray();

            writer.WritePropertyName("Object");
            writer.WriteStartObject();
            writer.WritePropertyName("String");
            writer.WriteValue("This is a string!");
            writer.WritePropertyName("Null");
            writer.WriteNull();
            writer.WriteEndObject();

            writer.WriteEndObject();

            writer.Flush();
        }

        Console.WriteLine(stringWriter.ToString());

        //<Root type="Object">
        //  <Null type="Null" />
        //  <String type="String">This is a string!</String>
        //  <Char type="String">!</Char>
        //  <Integer type="Integer">123</Integer>
        //  <DateTime type="Date">2001-02-22T20:59:59Z</DateTime>
        //  <DateTimeOffset type="Date">2001-02-22T20:59:59+12:00</DateTimeOffset>
        //  <Float type="Float">1.1</Float>
        //  <Double type="Float">3.14</Double>
        //  <Decimal type="Float">19.95</Decimal>
        //  <Guid type="Guid">d66eab59-3715-4b35-9e06-fa61c1216eaa</Guid>
        //  <Uri type="Uri">http://james.newtonking.com</Uri>
        //  <Array type="Array">
        //    <Item type="Integer">1</Item>
        //    <Item type="Integer">2</Item>
        //    <Item type="Integer">3</Item>
        //  </Array>
        //  <Object type="Object">
        //    <String type="String">This is a string!</String>
        //    <Null type="Null" />
        //  </Object>
        //  <Constructor type="Constructor" name="Date">
        //    <Item type="Integer">2000</Item>
        //    <Item type="Integer">12</Item>
        //    <Item type="Integer">30</Item>
        //  </Constructor>
        //</Root>
    }
}