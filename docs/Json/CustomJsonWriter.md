# Custom JsonWriter

This sample creates a custom `Argon.JsonWriter`.

<!-- snippet: CustomJsonWriterTypes -->
<a id='snippet-CustomJsonWriterTypes'></a>
```cs
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
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Json/CustomJsonWriter.cs#L7-L281' title='Snippet source file'>snippet source</a> | <a href='#snippet-CustomJsonWriterTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: CustomJsonWriterUsage -->
<a id='snippet-CustomJsonWriterUsage'></a>
```cs
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
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Json/CustomJsonWriter.cs#L288-L324' title='Snippet source file'>snippet source</a> | <a href='#snippet-CustomJsonWriterUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
