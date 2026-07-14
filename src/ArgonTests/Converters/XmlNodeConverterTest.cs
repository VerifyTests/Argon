// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Xml;
using System.Xml.Linq;
using TestObjects;
using Formatting = Argon.Formatting;

// ReSharper disable UseObjectOrCollectionInitializer

public class XmlNodeConverterTest : TestFixtureBase
{
    // a single-element nested array lost its json:Array marker when the property name was
    // prefixed or XML-encoded, because the raw JSON name was compared against the decoded
    // element LocalName. Result: the inner array nesting was dropped on round-trip.
    [Fact]
    public void NestedSingleElementArrayWithPrefixRoundTrips()
    {
        var json = """{"root":{"@xmlns:ns":"http://x","ns:items":[["a"]]}}""";

        var doc = JsonXmlConvert.DeserializeXmlNode(json, null, true);
        var roundTripped = JObject.Parse(JsonXmlConvert.SerializeXmlNode(doc));

        var items = roundTripped["root"]["ns:items"];
        Assert.Equal(JTokenType.Array, items.Type);
        Assert.Equal(JTokenType.Array, items[0].Type);
        Assert.Equal("a", (string) items[0][0]);
    }

    // two or more sibling comments serialized to "#comment": [] - all comment text dropped
    // plus a bogus property that cannot round-trip.
    [Fact]
    public void MultipleSiblingCommentsRoundTrip()
    {
        var doc = new XmlDocument();
        doc.LoadXml("<root><!--a--><!--b--></root>");

        var json = JsonXmlConvert.SerializeXmlNode(doc);
        Assert.DoesNotContain("#comment", json);

        var roundTripped = JsonXmlConvert.DeserializeXmlNode(json);
        var comments = roundTripped.DocumentElement
            .ChildNodes
            .Cast<XmlNode>()
            .Where(_ => _.NodeType == XmlNodeType.Comment)
            .Select(_ => _.Value)
            .ToList();

        Assert.Equal(new[] {"a", "b"}, comments);
    }

    // XmlDocumentWrapper.CreateXmlDocumentType discarded the internalSubset argument, so
    // JSON -> XmlDocument silently lost DTD internal subsets.
    [Fact]
    public void DocTypeInternalSubsetIsPreserved()
    {
        var json = """{"!DOCTYPE":{"@name":"root","@internalSubset":"<!ENTITY foo \"bar\">"},"root":"x"}""";

        var doc = JsonXmlConvert.DeserializeXmlNode(json);

        Assert.NotNull(doc.DocumentType);
        Assert.Contains("ENTITY foo", doc.DocumentType.InternalSubset);
    }

    // #cdata-section / #text with a JSON null crashed with ArgumentNullException for
    // XDocument targets (while XmlDocument produced an empty node).
    [Fact]
    public void CDataNullDoesNotCrashXDocument()
    {
        var xdoc = JsonXmlConvert.DeserializeXNode("""{"root":{"#cdata-section":null}}""");

        Assert.NotNull(xdoc);
        Assert.Contains("CDATA", xdoc.ToString());
    }

    [Fact]
    public void TextNullDoesNotCrashXDocument()
    {
        var xdoc = JsonXmlConvert.DeserializeXNode("""{"root":{"#text":null}}""");

        Assert.NotNull(xdoc);
    }

    static string SerializeXmlNode(XmlNode node)
    {
        var json = JsonXmlConvert.SerializeXmlNode(node, Formatting.Indented);

        var reader = new XmlNodeReader(node);
        XObject xNode;
        if (node is XmlDocument)
        {
            xNode = XDocument.Load(reader);
        }
        else if (node is XmlAttribute attribute)
        {
            xNode = new XAttribute(XName.Get(attribute.LocalName, attribute.NamespaceURI), attribute.Value);
        }
        else
        {
            reader.MoveToContent();
            xNode = XNode.ReadFrom(reader);
        }

        var linqJson = JsonXmlConvert.SerializeXNode(xNode, Formatting.Indented);

        Assert.Equal(json, linqJson);

        return json;
    }

    static XmlNode DeserializeXmlNode(string json) =>
        DeserializeXmlNode(json, null);

    static XmlNode DeserializeXmlNode(string json, string deserializeRootElementName)
    {
        var reader = new JsonTextReader(new StringReader(json));
        reader.Read();
        var converter = new XmlNodeConverter();
        if (deserializeRootElementName != null)
        {
            converter.DeserializeRootElementName = deserializeRootElementName;
        }

        var node = (XmlNode) converter.ReadJson(reader, typeof(XmlDocument), null, new());

        var xmlText = node.OuterXml;

        reader = new(new StringReader(json));
        reader.Read();
        var d = (XDocument) converter.ReadJson(reader, typeof(XDocument), null, new());

        var linqXmlText = d.ToString(SaveOptions.DisableFormatting);
        if (d.Declaration != null)
        {
            linqXmlText = d.Declaration + linqXmlText;
        }

        Assert.Equal(xmlText, linqXmlText);

        return node;
    }

    static string IndentXml(string xml)
    {
        var reader = XmlReader.Create(new StringReader(xml));

        var stringWriter = new StringWriter();
        var writer = XmlWriter.Create(stringWriter, new() {Indent = true, OmitXmlDeclaration = true});

        while (reader.Read())
        {
            writer.WriteNode(reader, false);
        }

        writer.Flush();

        return stringWriter.ToString();
    }

    [Fact]
    public void DeserializeXmlNode_DefaultDate()
    {
        var xmlNode = JsonXmlConvert.DeserializeXmlNode("{Time: \"0001-01-01T00:00:00\"}");

        Assert.Equal("<Time>0001-01-01T00:00:00</Time>", xmlNode.OuterXml);
    }

    [Fact]
    public void XmlNode_Null()
    {
        var json = JsonXmlConvert.SerializeXmlNode(null);

        Assert.Equal("null", json);
    }

    [Fact]
    public void XmlNode_Roundtrip_PropertyNameWithColon()
    {
        const string initialJson = """{"Be:fore:After!":"Value!"}""";

        var xmlNode = JsonXmlConvert.DeserializeXmlNode(initialJson, null, false, true);

        Assert.Equal("<Be_x003A_fore_x003A_After_x0021_>Value!</Be_x003A_fore_x003A_After_x0021_>", xmlNode.OuterXml);

        var json = JsonXmlConvert.SerializeXmlNode(xmlNode);

        Assert.Equal(initialJson, json);
    }

    [Fact]
    public void XmlNode_Roundtrip_PropertyNameWithEscapedValue()
    {
        const string initialJson = """{"BeforeAfter!":"Value!"}""";

        var xmlNode = JsonXmlConvert.DeserializeXmlNode(initialJson);

        Assert.Equal("<BeforeAfter_x0021_>Value!</BeforeAfter_x0021_>", xmlNode.OuterXml);

        var json = JsonXmlConvert.SerializeXmlNode(xmlNode);

        Assert.Equal(initialJson, json);
    }

    /**
     * [Fact]
     * public void XmlNode_EncodeSpecialCharacters()
     * {
     * string initialJson = @"{
     * ""?xml"": {
     * ""@version"": ""1.0"",
     * ""@standalone"": ""no""
     * },
     * ""?xml-stylesheet"": ""href=\""classic.xsl\"" type=\""text/xml\"""",
     * ""span"": {
     * ""@class"": ""vevent"",
     * ""a"": {
     * ""@class"": ""url"",
     * ""@href"": ""http://www.web2con.com/"",
     * ""span"": [
     * {
     * ""@class"": ""summary"",
     * ""#text"": ""Web 2.0 Conference"",
     * ""#cdata-section"": ""my escaped text""
     * },
     * {
     * ""@class"": ""location"",
     * ""#text"": ""Argent Hotel, San Francisco, CA""
     * }
     * ],
     * ""abbr"": [
     * {
     * ""@class"": ""dtstart"",
     * ""@title"": ""2005-10-05"",
     * ""#text"": ""October 5""
     * },
     * {
     * ""@class"": ""dtend"",
     * ""@title"": ""2005-10-08"",
     * ""#text"": ""7""
     * }
     * ]
     * }
     * }
     * }";
     *
     * XmlDocument xmlNode = JsonXmlConvert.DeserializeXmlNode(initialJson, "root", false, true);
     *
     * StringAssert.AreEqual(@"
     * <root>
     * <_x003F_xml>
     * <_x0040_version>1.0</_x0040_version>
     * <_x0040_standalone>no</_x0040_standalone>
     * </_x003F_xml>
     * <_x003F_xml-stylesheet>href=""classic.xsl"" type=""text/xml""</_x003F_xml-stylesheet>
     * <span>
     * <_x0040_class>vevent</_x0040_class>
     * <a>
     * <_x0040_class>url</_x0040_class>
     * <_x0040_href>http://www.web2con.com/</_x0040_href>
     * <span>
     * <_x0040_class>summary</_x0040_class>
     * <_x0023_text>Web 2.0 Conference</_x0023_text>
     * <_x0023_cdata-section>my escaped text</_x0023_cdata-section>
     * </span>
     * <span>
     * <_x0040_class>location</_x0040_class>
     * <_x0023_text>Argent Hotel, San Francisco, CA</_x0023_text>
     * </span>
     * <abbr>
     * <_x0040_class>dtstart</_x0040_class>
     * <_x0040_title>2005-10-05</_x0040_title>
     * <_x0023_text>October 5</_x0023_text>
     * </abbr>
     * <abbr>
     * <_x0040_class>dtend</_x0040_class>
     * <_x0040_title>2005-10-08</_x0040_title>
     * <_x0023_text>7</_x0023_text>
     * </abbr>
     * </a>
     * </span>
     * </root>
     * ", IndentXml(xmlNode.OuterXml));
     *
     * string json = JsonXmlConvert.SerializeXmlNode(xmlNode, Formatting.Indented, true);
     *
     * Xunit.Assert.Equal(initialJson, json);
     * }
     * *
     */
    [Fact]
    public void XmlNode_UnescapeTextContent()
    {
        var xmlNode = new XmlDocument();
        xmlNode.LoadXml("<root>A &gt; B</root>");

        var json = JsonXmlConvert.SerializeXmlNode(xmlNode);

        Assert.Equal("""{"root":"A > B"}""", json);
    }

    [Fact]
    public void DeserializeXNode_DefaultDate()
    {
        var xmlNode = JsonXmlConvert.DeserializeXNode("{Time: \"0001-01-01T00:00:00\"}");

        Assert.Equal("<Time>0001-01-01T00:00:00</Time>", xmlNode.ToString());
    }

    [Fact]
    public void XNode_Null()
    {
        var json = JsonXmlConvert.SerializeXNode(null);

        Assert.Equal("null", json);
    }

    [Fact]
    public void XNode_UnescapeTextContent()
    {
        var xmlNode = XElement.Parse("<root>A &gt; B</root>");

        var json = JsonXmlConvert.SerializeXNode(xmlNode);

        Assert.Equal("""{"root":"A > B"}""", json);
    }

    [Fact]
    public void XNode_Roundtrip_PropertyNameWithColon()
    {
        const string initialJson = """{"Be:fore:After!":"Value!"}""";

        var xmlNode = JsonXmlConvert.DeserializeXNode(initialJson, null, false, true);

        Assert.Equal("<Be_x003A_fore_x003A_After_x0021_>Value!</Be_x003A_fore_x003A_After_x0021_>", xmlNode.ToString());

        var json = JsonXmlConvert.SerializeXNode(xmlNode);

        Assert.Equal(initialJson, json);
    }

    [Fact]
    public void XNode_Roundtrip_PropertyNameWithEscapedValue()
    {
        const string initialJson = """{"BeforeAfter!":"Value!"}""";

        var xmlNode = JsonXmlConvert.DeserializeXNode(initialJson);

        Assert.Equal("<BeforeAfter_x0021_>Value!</BeforeAfter_x0021_>", xmlNode.ToString());

        var json = JsonXmlConvert.SerializeXNode(xmlNode);

        Assert.Equal(initialJson, json);
    }

    //TODO: re enable
    /**
     * [Fact]
     * public void XNode_EncodeSpecialCharacters()
     * {
     * string initialJson = @"{
     * ""?xml"": {
     * ""@version"": ""1.0"",
     * ""@standalone"": ""no""
     * },
     * ""?xml-stylesheet"": ""href=\""classic.xsl\"" type=\""text/xml\"""",
     * ""span"": {
     * ""@class"": ""vevent"",
     * ""a"": {
     * ""@class"": ""url"",
     * ""@href"": ""http://www.web2con.com/"",
     * ""span"": [
     * {
     * ""@class"": ""summary"",
     * ""#text"": ""Web 2.0 Conference"",
     * ""#cdata-section"": ""my escaped text""
     * },
     * {
     * ""@class"": ""location"",
     * ""#text"": ""Argent Hotel, San Francisco, CA""
     * }
     * ],
     * ""abbr"": [
     * {
     * ""@class"": ""dtstart"",
     * ""@title"": ""2005-10-05"",
     * ""#text"": ""October 5""
     * },
     * {
     * ""@class"": ""dtend"",
     * ""@title"": ""2005-10-08"",
     * ""#text"": ""7""
     * }
     * ]
     * }
     * }
     * }";
     *
     * XDocument xmlNode = JsonXmlConvert.DeserializeXNode(initialJson, "root", false, true);
     *
     * StringAssert.AreEqual(@"
     * <root>
     * <_x003F_xml>
     * <_x0040_version>1.0</_x0040_version>
     * <_x0040_standalone>no</_x0040_standalone>
     * </_x003F_xml>
     * <_x003F_xml-stylesheet>href=""classic.xsl"" type=""text/xml""</_x003F_xml-stylesheet>
     * <span>
     * <_x0040_class>vevent</_x0040_class>
     * <a>
     * <_x0040_class>url</_x0040_class>
     * <_x0040_href>http://www.web2con.com/</_x0040_href>
     * <span>
     * <_x0040_class>summary</_x0040_class>
     * <_x0023_text>Web 2.0 Conference</_x0023_text>
     * <_x0023_cdata-section>my escaped text</_x0023_cdata-section>
     * </span>
     * <span>
     * <_x0040_class>location</_x0040_class>
     * <_x0023_text>Argent Hotel, San Francisco, CA</_x0023_text>
     * </span>
     * <abbr>
     * <_x0040_class>dtstart</_x0040_class>
     * <_x0040_title>2005-10-05</_x0040_title>
     * <_x0023_text>October 5</_x0023_text>
     * </abbr>
     * <abbr>
     * <_x0040_class>dtend</_x0040_class>
     * <_x0040_title>2005-10-08</_x0040_title>
     * <_x0023_text>7</_x0023_text>
     * </abbr>
     * </a>
     * </span>
     * </root>
     * ", xmlNode.ToString());
     *
     * string json = JsonXmlConvert.SerializeXNode(xmlNode, Formatting.Indented, true);
     *
     * Xunit.Assert.Equal(initialJson, json);
     * }
     *
     * [Fact]
     * public void XNode_MetadataArray_EncodeSpecialCharacters()
     * {
     * string initialJson = @"{
     * ""$id"": ""1"",
     * ""$values"": [
     * ""1"",
     * ""2"",
     * ""3"",
     * ""4"",
     * ""5""
     * ]
     * }";
     *
     * XDocument xmlNode = JsonXmlConvert.DeserializeXNode(initialJson, "root", false, true);
     *
     * StringAssert.AreEqual(@"
     * <root>
     * <_x0024_id>1</_x0024_id>
     * <_x0024_values>1</_x0024_values>
     * <_x0024_values>2</_x0024_values>
     * <_x0024_values>3</_x0024_values>
     * <_x0024_values>4</_x0024_values>
     * <_x0024_values>5</_x0024_values>
     * </root>
     * ", xmlNode.ToString());
     *
     * string json = JsonXmlConvert.SerializeXNode(xmlNode, Formatting.Indented, true);
     *
     * Xunit.Assert.Equal(initialJson, json);
     * }
     *
     * *
     */
    [Fact]
    public void SerializeDollarProperty()
    {
        var json1 = """{"$":"test"}""";

        var doc = JsonXmlConvert.DeserializeXNode(json1);

        Assert.Equal("<_x0024_>test</_x0024_>", doc.ToString());

        var json2 = JsonXmlConvert.SerializeXNode(doc);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void SerializeNonKnownDollarProperty()
    {
        var json1 = """{"$JELLY":"test"}""";

        var doc = JsonXmlConvert.DeserializeXNode(json1);

        Assert.Equal("<_x0024_JELLY>test</_x0024_JELLY>", doc.ToString());

        var json2 = JsonXmlConvert.SerializeXNode(doc);

        Assert.Equal(json1, json2);
    }

    public class MyModel
    {
        public string MyProperty { get; set; }
    }

    [Fact]
    public void ConvertNullString()
    {
        var json = new JObject
        {
            ["Prop1"] = (string) null,
            ["Prop2"] = new MyModel().MyProperty
        };

        var xmlNodeConverter = new XmlNodeConverter {DeserializeRootElementName = "object"};
        var settings = new JsonSerializerSettings
        {
            Converters = [xmlNodeConverter]
        };
        var serializer = JsonSerializer.CreateDefault(settings);
        var d = json.ToObject<XDocument>(serializer);

        XUnitAssert.AreEqualNormalized(
            """
            <object>
              <Prop1 />
              <Prop2 />
            </object>
            """,
            d.ToString());
    }

    public class Foo
    {
        public XElement Bar { get; set; }
    }

    [Fact]
    public void SerializeAndDeserializeXElement()
    {
        var foo = new Foo {Bar = null};
        var json = JsonConvert.SerializeObject(foo);

        Assert.Equal("""{"Bar":null}""", json);
        var foo2 = JsonConvert.DeserializeObject<Foo>(json);

        Assert.Null(foo2.Bar);
    }

    [Fact]
    public void MultipleNamespacesXDocument()
    {
        var xml = """<result xp_0:end="2014-08-15 13:12:11.9184" xp_0:start="2014-08-15 13:11:49.3140" xp_0:time_diff="22604.3836" xmlns:xp_0="Test1" p2:end="2014-08-15 13:13:49.5522" p2:start="2014-08-15 13:13:49.0268" p2:time_diff="525.4646" xmlns:p2="Test2" />""";

        var d = XDocument.Parse(xml);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(d, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "result": {
                "@xp_0:end": "2014-08-15 13:12:11.9184",
                "@xp_0:start": "2014-08-15 13:11:49.3140",
                "@xp_0:time_diff": "22604.3836",
                "@xmlns:xp_0": "Test1",
                "@p2:end": "2014-08-15 13:13:49.5522",
                "@p2:start": "2014-08-15 13:13:49.0268",
                "@p2:time_diff": "525.4646",
                "@xmlns:p2": "Test2"
              }
            }
            """,
            json);

        var doc = JsonConvert.DeserializeObject<XDocument>(json, settings);

        XUnitAssert.AreEqualNormalized(xml, doc.ToString());
    }

    [Fact]
    public void MultipleNamespacesXmlDocument()
    {
        var xml = """<result xp_0:end="2014-08-15 13:12:11.9184" xp_0:start="2014-08-15 13:11:49.3140" xp_0:time_diff="22604.3836" xmlns:xp_0="Test1" p2:end="2014-08-15 13:13:49.5522" p2:start="2014-08-15 13:13:49.0268" p2:time_diff="525.4646" xmlns:p2="Test2" />""";

        var d = new XmlDocument();
        d.LoadXml(xml);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(d, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "result": {
                "@xp_0:end": "2014-08-15 13:12:11.9184",
                "@xp_0:start": "2014-08-15 13:11:49.3140",
                "@xp_0:time_diff": "22604.3836",
                "@xmlns:xp_0": "Test1",
                "@p2:end": "2014-08-15 13:13:49.5522",
                "@p2:start": "2014-08-15 13:13:49.0268",
                "@p2:time_diff": "525.4646",
                "@xmlns:p2": "Test2"
              }
            }
            """,
            json);

        var doc = JsonConvert.DeserializeObject<XmlDocument>(json, settings);

        XUnitAssert.AreEqualNormalized(xml, doc.OuterXml);
    }

    [Fact]
    public void SerializeXmlElement()
    {
        var xml = """
                  <payload>
                      <Country>6</Country>
                      <FinancialTransactionApprovalRequestUID>79</FinancialTransactionApprovalRequestUID>
                      <TransactionStatus>Approved</TransactionStatus>
                      <StatusChangeComment></StatusChangeComment>
                      <RequestedBy>Someone</RequestedBy>
                  </payload>
                  """;

        var xmlDocument = new XmlDocument();

        xmlDocument.LoadXml(xml);

        var result = xmlDocument.FirstChild.ChildNodes.Cast<XmlNode>().ToArray();

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(result, settings); // <--- fails here with the cast message

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "Country": "6"
              },
              {
                "FinancialTransactionApprovalRequestUID": "79"
              },
              {
                "TransactionStatus": "Approved"
              },
              {
                "StatusChangeComment": ""
              },
              {
                "RequestedBy": "Someone"
              }
            ]
            """,
            json);
    }

    [Fact]
    public void SerializeXElement()
    {
        var xml = """
                  <payload>
                      <Country>6</Country>
                      <FinancialTransactionApprovalRequestUID>79</FinancialTransactionApprovalRequestUID>
                      <TransactionStatus>Approved</TransactionStatus>
                      <StatusChangeComment></StatusChangeComment>
                      <RequestedBy>Someone</RequestedBy>
                  </payload>
                  """;

        var xmlDocument = XDocument.Parse(xml);

        var result = xmlDocument.Root.Nodes().ToArray();

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(result, settings); // <--- fails here with the cast message

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "Country": "6"
              },
              {
                "FinancialTransactionApprovalRequestUID": "79"
              },
              {
                "TransactionStatus": "Approved"
              },
              {
                "StatusChangeComment": ""
              },
              {
                "RequestedBy": "Someone"
              }
            ]
            """,
            json);
    }

    public class DecimalContainer
    {
        public decimal Number { get; set; }
    }

    [Fact]
    public void FloatParseHandlingDecimal()
    {
        var d = (decimal) Math.PI + 1000000000m;
        var x = new DecimalContainer {Number = d};

        var json = JsonConvert.SerializeObject(x, Formatting.Indented);

        var doc1 = JsonConvert.DeserializeObject<XDocument>(json, new JsonSerializerSettings
        {
            Converters = {new XmlNodeConverter()},
            FloatParseHandling = FloatParseHandling.Decimal
        });

        var xml = doc1.ToString();
        Assert.Equal("<Number>1000000003.14159265358979</Number>", xml);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new XmlNodeConverter());
        var json2 = JsonConvert.SerializeObject(doc1, settings);

        var x2 = JsonConvert.DeserializeObject<DecimalContainer>(json2);

        Assert.Equal(x.Number, x2.Number);
    }

    public class DateTimeOffsetContainer
    {
        public DateTimeOffset Date { get; set; }
    }

    [Fact]
    public void GroupElementsOfTheSameName()
    {
        var xml = "<root><p>Text1<span>Span1</span> <span>Span2</span> Text2</p></root>";

        var json = JsonXmlConvert.SerializeXNode(XElement.Parse(xml));

        Assert.Equal("""{"root":{"p":{"#text":["Text1"," Text2"],"span":["Span1","Span2"]}}}""", json);

        var doc = JsonXmlConvert.DeserializeXNode(json);

        XUnitAssert.AreEqualNormalized(
            """
            <root>
              <p>Text1 Text2<span>Span1</span><span>Span2</span></p>
            </root>
            """,
            doc.ToString());
    }

    [Fact]
    public void SerializeEmptyDocument()
    {
        var doc = new XmlDocument();
        doc.LoadXml("<root />");

        var json = JsonXmlConvert.SerializeXmlNode(doc, Formatting.Indented, true);
        Assert.Equal("null", json);

        doc = new();
        doc.LoadXml("<root></root>");

        json = JsonXmlConvert.SerializeXmlNode(doc, Formatting.Indented, true);
        Assert.Equal(
            """
            ""
            """,
            json);

        var doc1 = XDocument.Parse("<root />");

        json = JsonXmlConvert.SerializeXNode(doc1, Formatting.Indented, true);
        Assert.Equal("null", json);

        doc1 = XDocument.Parse("<root></root>");

        json = JsonXmlConvert.SerializeXNode(doc1, Formatting.Indented, true);
        Assert.Equal(
            """
            ""
            """,
            json);
    }

    [Fact]
    public void SerializeAndDeserializeXmlWithNamespaceInChildrenAndNoValueInChildren()
    {
        var xmlString = """
                        <root>
                        <b xmlns='http://www.example.com/ns'/>
                        <c>AAA</c>
                        <test>adad</test>
                        </root>
                        """;

        var xml = XElement.Parse(xmlString);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonXmlConvert.SerializeXNode(xml);
        var xmlBack = JsonConvert.DeserializeObject<XElement>(json, settings);

        var equals = XNode.DeepEquals(xmlBack, xml);
        Assert.True(equals);
    }

    [Fact]
    public void DeserializeUndeclaredNamespacePrefix()
    {
        var doc = JsonXmlConvert.DeserializeXmlNode("{ A: { '@xsi:nil': true } }");

        Assert.Equal("""<A nil="true" />""", doc.OuterXml);

        var xdoc = JsonXmlConvert.DeserializeXNode("{ A: { '@xsi:nil': true } }");

        Assert.Equal(doc.OuterXml, xdoc.ToString());
    }

    [Fact]
    public void DeserializeMultipleRootElements()
    {
        var json = """
            {
                "Id": 1,
                 "Email": "james@example.com",
                 "Active": true,
                 "CreatedDate": "2013-01-20T00:00:00Z",
                 "Roles": [
                   "User",
                   "Admin"
                 ],
                "Team": {
                    "Id": 2,
                    "Name": "Software Developers",
                    "Description": "Creators of fine software products and services."
                }
            }
            """;
        var exception = Assert.Throws<JsonSerializationException>(() => JsonXmlConvert.DeserializeXmlNode(json));
        Assert.Equal("JSON root object has multiple properties. The root object must have a single property in order to create a valid XML document. Consider specifying a DeserializeRootElementName. Path 'Email', line 3, position 13.", exception.Message);
    }

    [Fact]
    public void DocumentSerializeIndented()
    {
        var xml = """
            <?xml version="1.0" standalone="no"?>
            <?xml-stylesheet href="classic.xsl" type="text/xml"?>
            <span class="vevent">
              <a class="url" href="http://www.web2con.com/">
                <span class="summary">Web 2.0 Conference<![CDATA[my escaped text]]></span>
                <abbr class="dtstart" title="2005-10-05">October 5</abbr>
                <abbr class="dtend" title="2005-10-08">7</abbr>
                <span class="location">Argent Hotel, San Francisco, CA</span>
              </a>
            </span>
            """;
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var jsonText = SerializeXmlNode(doc);
        var expected = """
            {
              "?xml": {
                "@version": "1.0",
                "@standalone": "no"
              },
              "?xml-stylesheet": "href=\"classic.xsl\" type=\"text/xml\"",
              "span": {
                "@class": "vevent",
                "a": {
                  "@class": "url",
                  "@href": "http://www.web2con.com/",
                  "span": [
                    {
                      "@class": "summary",
                      "#text": "Web 2.0 Conference",
                      "#cdata-section": "my escaped text"
                    },
                    {
                      "@class": "location",
                      "#text": "Argent Hotel, San Francisco, CA"
                    }
                  ],
                  "abbr": [
                    {
                      "@class": "dtstart",
                      "@title": "2005-10-05",
                      "#text": "October 5"
                    },
                    {
                      "@class": "dtend",
                      "@title": "2005-10-08",
                      "#text": "7"
                    }
                  ]
                }
              }
            }
            """;

        XUnitAssert.AreEqualNormalized(expected, jsonText);
    }

    [Fact]
    public void SerializeNodeTypes()
    {
        var doc = new XmlDocument();

        var xml = """
                  <?xml version="1.0" encoding="utf-8" ?>
                  <xs:schema xs:id="SomeID"
                    xmlns=""
                    xmlns:xs="http://www.w3.org/2001/XMLSchema"
                    xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
                    <xs:element name="MyDataSet" msdata:IsDataSet="true">
                    </xs:element>
                  </xs:schema>
                  """;

        var document = new XmlDocument();
        document.LoadXml(xml);

        // XmlAttribute
        var attribute = document.DocumentElement.ChildNodes[0].Attributes["IsDataSet", "urn:schemas-microsoft-com:xml-msdata"];
        attribute.Value = "true";

        var jsonText = JsonXmlConvert.SerializeXmlNode(attribute);

        Assert.Equal("""{"@msdata:IsDataSet":"true"}""", jsonText);

        var d = XDocument.Parse(xml);
        var a = d.Root.Element("{http://www.w3.org/2001/XMLSchema}element").Attribute("{urn:schemas-microsoft-com:xml-msdata}IsDataSet");

        jsonText = JsonXmlConvert.SerializeXNode(a);

        Assert.Equal("""{"@msdata:IsDataSet":"true"}""", jsonText);

        // XmlProcessingInstruction
        var instruction = doc.CreateProcessingInstruction(
            "xml-stylesheet",
            """
            href="classic.xsl" type="text/xml"
            """);

        jsonText = JsonXmlConvert.SerializeXmlNode(instruction);

        Assert.Equal("""{"?xml-stylesheet":"href=\"classic.xsl\" type=\"text/xml\""}""", jsonText);

        // XmlProcessingInstruction
        var cDataSection = doc.CreateCDataSection("<Kiwi>true</Kiwi>");

        jsonText = JsonXmlConvert.SerializeXmlNode(cDataSection);

        Assert.Equal("""{"#cdata-section":"<Kiwi>true</Kiwi>"}""", jsonText);

        // XmlElement
        var element = doc.CreateElement("xs", "Choice", "http://www.w3.org/2001/XMLSchema");
        element.SetAttributeNode(doc.CreateAttribute("msdata", "IsDataSet", "urn:schemas-microsoft-com:xml-msdata"));

        var aa = doc.CreateAttribute("xmlns", "xs", "http://www.w3.org/2000/xmlns/");
        aa.Value = "http://www.w3.org/2001/XMLSchema";
        element.SetAttributeNode(aa);

        aa = doc.CreateAttribute("xmlns", "msdata", "http://www.w3.org/2000/xmlns/");
        aa.Value = "urn:schemas-microsoft-com:xml-msdata";
        element.SetAttributeNode(aa);

        element.AppendChild(instruction);
        element.AppendChild(cDataSection);

        doc.AppendChild(element);

        jsonText = JsonXmlConvert.SerializeXmlNode(element, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "xs:Choice": {
                "@msdata:IsDataSet": "",
                "@xmlns:xs": "http://www.w3.org/2001/XMLSchema",
                "@xmlns:msdata": "urn:schemas-microsoft-com:xml-msdata",
                "?xml-stylesheet": "href=\"classic.xsl\" type=\"text/xml\"",
                "#cdata-section": "<Kiwi>true</Kiwi>"
              }
            }
            """,
            jsonText);
    }

    [Fact]
    public void SerializeNodeTypes_Encoding()
    {
        var node = DeserializeXmlNode(
            """
            {
              "xs!:Choice!": {
                "@msdata:IsDataSet!": "",
                "@xmlns:xs!": "http://www.w3.org/2001/XMLSchema",
                "@xmlns:msdata": "urn:schemas-microsoft-com:xml-msdata",
                "?xml-stylesheet": "href=\"classic.xsl\" type=\"text/xml\"",
                "#cdata-section": "<Kiwi>true</Kiwi>"
              }
            }
            """);

        Assert.Equal("""<xs_x0021_:Choice_x0021_ msdata:IsDataSet_x0021_="" xmlns:xs_x0021_="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata"><?xml-stylesheet href="classic.xsl" type="text/xml"?><![CDATA[<Kiwi>true</Kiwi>]]></xs_x0021_:Choice_x0021_>""", node.InnerXml);

        var json = SerializeXmlNode(node);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "xs!:Choice!": {
                "@msdata:IsDataSet!": "",
                "@xmlns:xs!": "http://www.w3.org/2001/XMLSchema",
                "@xmlns:msdata": "urn:schemas-microsoft-com:xml-msdata",
                "?xml-stylesheet": "href=\"classic.xsl\" type=\"text/xml\"",
                "#cdata-section": "<Kiwi>true</Kiwi>"
              }
            }
            """,
            json);
    }

    [Fact]
    public void DocumentFragmentSerialize()
    {
        var doc = new XmlDocument();

        var fragment = doc.CreateDocumentFragment();

        fragment.InnerXml = "<Item>widget</Item><Item>widget</Item>";

        var jsonText = JsonXmlConvert.SerializeXmlNode(fragment);

        var expected = """{"Item":["widget","widget"]}""";

        Assert.Equal(expected, jsonText);
    }

    [Fact]
    public void XmlDocumentTypeSerialize()
    {
        var xml = """<?xml version="1.0" encoding="utf-8"?><!DOCTYPE STOCKQUOTE PUBLIC "-//W3C//DTD StockQuote 1.5//EN" "http://www.idontexistnopenopewhatnope123.org/dtd/stockquote_1.5.dtd"><STOCKQUOTE ROWCOUNT="2"><RESULT><ROW><ASK>0</ASK><BID>0</BID><CHANGE>-16.310</CHANGE><COMPANYNAME>Dow Jones</COMPANYNAME><DATETIME>2014-04-17 15:50:37</DATETIME><DIVIDEND>0</DIVIDEND><EPS>0</EPS><EXCHANGE></EXCHANGE><HIGH>16460.490</HIGH><LASTDATETIME>2014-04-17 15:50:37</LASTDATETIME><LASTPRICE>16408.540</LASTPRICE><LOW>16368.140</LOW><OPEN>16424.140</OPEN><PCHANGE>-0.099</PCHANGE><PE>0</PE><PREVIOUSCLOSE>16424.850</PREVIOUSCLOSE><SHARES>0</SHARES><TICKER>DJII</TICKER><TRADES>0</TRADES><VOLUME>136188700</VOLUME><YEARHIGH>11309.000</YEARHIGH><YEARLOW>9302.280</YEARLOW><YIELD>0</YIELD></ROW><ROW><ASK>0</ASK><BID>0</BID><CHANGE>9.290</CHANGE><COMPANYNAME>NASDAQ</COMPANYNAME><DATETIME>2014-04-17 15:40:01</DATETIME><DIVIDEND>0</DIVIDEND><EPS>0</EPS><EXCHANGE></EXCHANGE><HIGH>4110.460</HIGH><LASTDATETIME>2014-04-17 15:40:01</LASTDATETIME><LASTPRICE>4095.520</LASTPRICE><LOW>4064.700</LOW><OPEN>4080.300</OPEN><PCHANGE>0.227</PCHANGE><PE>0</PE><PREVIOUSCLOSE>4086.230</PREVIOUSCLOSE><SHARES>0</SHARES><TICKER>COMP</TICKER><TRADES>0</TRADES><VOLUME>1784210100</VOLUME><YEARHIGH>4371.710</YEARHIGH><YEARLOW>3154.960</YEARLOW><YIELD>0</YIELD></ROW></RESULT><STATUS>Couldn't find ticker: SPIC?</STATUS><STATUSCODE>2</STATUSCODE></STOCKQUOTE>""";

        var expected = """
            {
              "?xml": {
                "@version": "1.0",
                "@encoding": "utf-8"
              },
              "!DOCTYPE": {
                "@name": "STOCKQUOTE",
                "@public": "-//W3C//DTD StockQuote 1.5//EN",
                "@system": "http://www.idontexistnopenopewhatnope123.org/dtd/stockquote_1.5.dtd"
              },
              "STOCKQUOTE": {
                "@ROWCOUNT": "2",
                "RESULT": {
                  "ROW": [
                    {
                      "ASK": "0",
                      "BID": "0",
                      "CHANGE": "-16.310",
                      "COMPANYNAME": "Dow Jones",
                      "DATETIME": "2014-04-17 15:50:37",
                      "DIVIDEND": "0",
                      "EPS": "0",
                      "EXCHANGE": "",
                      "HIGH": "16460.490",
                      "LASTDATETIME": "2014-04-17 15:50:37",
                      "LASTPRICE": "16408.540",
                      "LOW": "16368.140",
                      "OPEN": "16424.140",
                      "PCHANGE": "-0.099",
                      "PE": "0",
                      "PREVIOUSCLOSE": "16424.850",
                      "SHARES": "0",
                      "TICKER": "DJII",
                      "TRADES": "0",
                      "VOLUME": "136188700",
                      "YEARHIGH": "11309.000",
                      "YEARLOW": "9302.280",
                      "YIELD": "0"
                    },
                    {
                      "ASK": "0",
                      "BID": "0",
                      "CHANGE": "9.290",
                      "COMPANYNAME": "NASDAQ",
                      "DATETIME": "2014-04-17 15:40:01",
                      "DIVIDEND": "0",
                      "EPS": "0",
                      "EXCHANGE": "",
                      "HIGH": "4110.460",
                      "LASTDATETIME": "2014-04-17 15:40:01",
                      "LASTPRICE": "4095.520",
                      "LOW": "4064.700",
                      "OPEN": "4080.300",
                      "PCHANGE": "0.227",
                      "PE": "0",
                      "PREVIOUSCLOSE": "4086.230",
                      "SHARES": "0",
                      "TICKER": "COMP",
                      "TRADES": "0",
                      "VOLUME": "1784210100",
                      "YEARHIGH": "4371.710",
                      "YEARLOW": "3154.960",
                      "YIELD": "0"
                    }
                  ]
                },
                "STATUS": "Couldn't find ticker: SPIC?",
                "STATUSCODE": "2"
              }
            }
            """;

        var doc1 = new XmlDocument
        {
            XmlResolver = null
        };
        doc1.LoadXml(xml);

        var json1 = JsonXmlConvert.SerializeXmlNode(doc1, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(expected, json1);

        var doc11 = JsonXmlConvert.DeserializeXmlNode(json1);

        XUnitAssert.AreEqualNormalized(xml, ToStringWithDeclaration(doc11));

        var doc2 = XDocument.Parse(xml);

        var json2 = JsonXmlConvert.SerializeXNode(doc2, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(expected, json2);

        var doc22 = JsonXmlConvert.DeserializeXNode(json2);

        XUnitAssert.AreEqualNormalized(xml, ToStringWithDeclaration(doc22));
    }

    public class Utf8StringWriter(StringBuilder sb) : StringWriter(sb)
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    public static string ToStringWithDeclaration(XDocument doc, bool indent = false)
    {
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(new Utf8StringWriter(builder), new() {Indent = indent}))
        {
            doc.Save(writer);
        }

        return builder.ToString();
    }

    public static string ToStringWithDeclaration(XmlDocument doc, bool indent = false)
    {
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(new Utf8StringWriter(builder), new() {Indent = indent}))
        {
            doc.Save(writer);
        }

        return builder.ToString();
    }

    [Fact]
    public void NamespaceSerializeDeserialize()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8" ?>
            <xs:schema xs:id="SomeID"
                xmlns=""
                xmlns:xs="http://www.w3.org/2001/XMLSchema"
                xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
                <xs:element name="MyDataSet" msdata:IsDataSet="true">
                    <xs:complexType>
                        <xs:choice maxOccurs="unbounded">
                            <xs:element name="customers" >
                                <xs:complexType >
                                    <xs:sequence>
                                        <xs:element name="CustomerID" type="xs:integer"
                                                    minOccurs="0" />
                                        <xs:element name="CompanyName" type="xs:string"
                                                    minOccurs="0" />
                                        <xs:element name="Phone" type="xs:string" />
                                    </xs:sequence>
                                </xs:complexType>
                            </xs:element>
                        </xs:choice>
                    </xs:complexType>
                </xs:element>
            </xs:schema>
            """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var jsonText = SerializeXmlNode(doc);

        var expected = """
            {
              "?xml": {
                "@version": "1.0",
                "@encoding": "utf-8"
              },
              "xs:schema": {
                "@xs:id": "SomeID",
                "@xmlns": "",
                "@xmlns:xs": "http://www.w3.org/2001/XMLSchema",
                "@xmlns:msdata": "urn:schemas-microsoft-com:xml-msdata",
                "xs:element": {
                  "@name": "MyDataSet",
                  "@msdata:IsDataSet": "true",
                  "xs:complexType": {
                    "xs:choice": {
                      "@maxOccurs": "unbounded",
                      "xs:element": {
                        "@name": "customers",
                        "xs:complexType": {
                          "xs:sequence": {
                            "xs:element": [
                              {
                                "@name": "CustomerID",
                                "@type": "xs:integer",
                                "@minOccurs": "0"
                              },
                              {
                                "@name": "CompanyName",
                                "@type": "xs:string",
                                "@minOccurs": "0"
                              },
                              {
                                "@name": "Phone",
                                "@type": "xs:string"
                              }
                            ]
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        XUnitAssert.AreEqualNormalized(expected, jsonText);

        var deserializedDoc = (XmlDocument) DeserializeXmlNode(jsonText);

        Assert.Equal(doc.InnerXml, deserializedDoc.InnerXml);
    }

    [Fact]
    public void FailOnIncomplete()
    {
        var json = "{'Row' : ";

        var exception = Assert.Throws<JsonSerializationException>(() => JsonXmlConvert.DeserializeXmlNode(json, "ROOT"));
        Assert.Equal("Unexpected end when reading JSON. Path 'Row', line 1, position 9.", exception.Message);
    }

    [Fact]
    public void DocumentDeserialize()
    {
        var jsonText = """
            {
              "?xml": {
                "@version": "1.0",
                "@standalone": "no"
              },
              "span": {
                "@class": "vevent",
                "a": {
                  "@class": "url",
                  "span": {
                    "@class": "summary",
                    "#text": "Web 2.0 Conference",
                    "#cdata-section": "my escaped text"
                  },
                  "@href": "http://www.web2con.com/"
                }
              }
            }
            """;

        var doc = (XmlDocument) DeserializeXmlNode(jsonText);

        var expected = """
                       <?xml version="1.0" standalone="no"?>
                       <span class="vevent">
                         <a class="url" href="http://www.web2con.com/">
                           <span class="summary">Web 2.0 Conference<![CDATA[my escaped text]]></span>
                         </a>
                       </span>
                       """;

        var formattedXml = GetIndentedInnerXml(doc);

        XUnitAssert.AreEqualNormalized(expected, formattedXml);
    }

    static string GetIndentedInnerXml(XmlNode node)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true
        };

        var stringWriter = new StringWriter();

        using (var writer = XmlWriter.Create(stringWriter, settings))
        {
            node.WriteTo(writer);
        }

        return stringWriter.ToString();
    }

    public class Foo2
    {
        public XmlElement Bar { get; set; }
    }

    [Fact]
    public void SerializeAndDeserializeXmlElement()
    {
        var foo = new Foo2 {Bar = null};
        var json = JsonConvert.SerializeObject(foo);

        Assert.Equal("""{"Bar":null}""", json);
        var foo2 = JsonConvert.DeserializeObject<Foo2>(json);

        Assert.Null(foo2.Bar);
    }

    [Fact]
    public void SingleTextNode()
    {
        var xml = """
                  <?xml version="1.0" standalone="no"?>
                  <root>
                    <person id="1">
                      <name>Alan</name>
                      <url>http://www.google.com</url>
                    </person>
                    <person id="2">
                      <name>Louis</name>
                      <url>http://www.yahoo.com</url>
                    </person>
                  </root>
                  """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var jsonText = SerializeXmlNode(doc);

        var newDoc = (XmlDocument) DeserializeXmlNode(jsonText);

        Assert.Equal(doc.InnerXml, newDoc.InnerXml);
    }

    [Fact]
    public void EmptyNode()
    {
        var xml = """
                  <?xml version="1.0" standalone="no"?>
                  <root>
                    <person id="1">
                      <name>Alan</name>
                      <url />
                    </person>
                    <person id="2">
                      <name>Louis</name>
                      <url>http://www.yahoo.com</url>
                    </person>
                  </root>
                  """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var jsonText = SerializeXmlNode(doc);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "?xml": {
                "@version": "1.0",
                "@standalone": "no"
              },
              "root": {
                "person": [
                  {
                    "@id": "1",
                    "name": "Alan",
                    "url": null
                  },
                  {
                    "@id": "2",
                    "name": "Louis",
                    "url": "http://www.yahoo.com"
                  }
                ]
              }
            }
            """,
            jsonText);

        var newDoc = (XmlDocument) DeserializeXmlNode(jsonText);

        Assert.Equal(doc.InnerXml, newDoc.InnerXml);
    }

    [Fact]
    public void OtherElementDataTypes()
    {
        var jsonText = """{"?xml":{"@version":"1.0","@standalone":"no"},"root":{"person":[{"@id":"1","Float":2.5,"Integer":99},{"Boolean":true,"@id":"2","date":"2000-03-30T00:00:00Z"}]}}""";

        var newDoc = (XmlDocument) DeserializeXmlNode(jsonText);

        var expected = """<?xml version="1.0" standalone="no"?><root><person id="1"><Float>2.5</Float><Integer>99</Integer></person><person id="2"><Boolean>true</Boolean><date>2000-03-30T00:00:00Z</date></person></root>""";

        Assert.Equal(expected, newDoc.InnerXml);
    }

    [Fact]
    public void NoRootObject()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonXmlConvert.DeserializeXmlNode("[1]"));
        Assert.Equal("XmlNodeConverter can only convert JSON that begins with an object. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void RootObjectMultipleProperties()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonXmlConvert.DeserializeXmlNode("{Prop1:1,Prop2:2}"));
        Assert.Equal("JSON root object has multiple properties. The root object must have a single property in order to create a valid XML document. Consider specifying a DeserializeRootElementName. Path 'Prop2', line 1, position 15.", exception.Message);
    }


    [Fact]
    public void ForceJsonArray()
    {
        var arrayXml = """
            <root xmlns:json="http://james.newtonking.com/projects/json">
                <person id="1">
                    <name>Alan</name>
                    <url>http://www.google.com</url>
                    <role json:Array="true">Admin</role>
                </person>
            </root>
            """;

        var arrayDoc = new XmlDocument();
        arrayDoc.LoadXml(arrayXml);

        var arrayJsonText = SerializeXmlNode(arrayDoc);
        var expected = """
            {
              "root": {
                "person": {
                  "@id": "1",
                  "name": "Alan",
                  "url": "http://www.google.com",
                  "role": [
                    "Admin"
                  ]
                }
              }
            }
            """;
        XUnitAssert.AreEqualNormalized(expected, arrayJsonText);

        arrayXml = """
            <root xmlns:json="http://james.newtonking.com/projects/json">
                <person id="1">
                    <name>Alan</name>
                    <url>http://www.google.com</url>
                    <role json:Array="true">Admin1</role>
                    <role json:Array="true">Admin2</role>
                </person>
            </root>
            """;

        arrayDoc = new();
        arrayDoc.LoadXml(arrayXml);

        arrayJsonText = SerializeXmlNode(arrayDoc);
        expected = """
            {
              "root": {
                "person": {
                  "@id": "1",
                  "name": "Alan",
                  "url": "http://www.google.com",
                  "role": [
                    "Admin1",
                    "Admin2"
                  ]
                }
              }
            }
            """;
        XUnitAssert.AreEqualNormalized(expected, arrayJsonText);

        arrayXml = """
            <root xmlns:json="http://james.newtonking.com/projects/json">
                <person id="1">
                  <name>Alan</name>
                  <url>http://www.google.com</url>
                  <role json:Array="false">Admin1</role>
                </person>
            </root>
            """;

        arrayDoc = new();
        arrayDoc.LoadXml(arrayXml);

        arrayJsonText = SerializeXmlNode(arrayDoc);
        expected = """
            {
              "root": {
                "person": {
                  "@id": "1",
                  "name": "Alan",
                  "url": "http://www.google.com",
                  "role": "Admin1"
                }
              }
            }
            """;
        XUnitAssert.AreEqualNormalized(expected, arrayJsonText);

        arrayXml = """
            <root>
                <person id="1">
                    <name>Alan</name>
                    <url>http://www.google.com</url>
                    <role json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">Admin</role>
                </person>
            </root>
            """;

        arrayDoc = new();
        arrayDoc.LoadXml(arrayXml);

        arrayJsonText = SerializeXmlNode(arrayDoc);
        expected = """
            {
              "root": {
                "person": {
                  "@id": "1",
                  "name": "Alan",
                  "url": "http://www.google.com",
                  "role": [
                    "Admin"
                  ]
                }
              }
            }
            """;
        XUnitAssert.AreEqualNormalized(expected, arrayJsonText);
    }

    [Fact]
    public void MultipleRootPropertiesXmlDocument()
    {
        var json = """{"count": 773840,"photos": null}""";

        var exception = Assert.Throws<JsonSerializationException>(() => JsonXmlConvert.DeserializeXmlNode(json));
        Assert.Equal("JSON root object has multiple properties. The root object must have a single property in order to create a valid XML document. Consider specifying a DeserializeRootElementName. Path 'photos', line 1, position 26.", exception.Message);
    }

    [Fact]
    public void MultipleRootPropertiesXDocument()
    {
        var json = """{"count": 773840,"photos": null}""";

        var exception = Assert.Throws<JsonSerializationException>(() => JsonXmlConvert.DeserializeXNode(json));
        Assert.Equal("JSON root object has multiple properties. The root object must have a single property in order to create a valid XML document. Consider specifying a DeserializeRootElementName. Path 'photos', line 1, position 26.", exception.Message);
    }

    [Fact]
    public void MultipleRootPropertiesAddRootElement()
    {
        var json = """{"count": 773840,"photos": 773840}""";

        var newDoc = JsonXmlConvert.DeserializeXmlNode(json, "myRoot");

        Assert.Equal("<myRoot><count>773840</count><photos>773840</photos></myRoot>", newDoc.InnerXml);

        var newXDoc = JsonXmlConvert.DeserializeXNode(json, "myRoot");

        Assert.Equal("<myRoot><count>773840</count><photos>773840</photos></myRoot>", newXDoc.ToString(SaveOptions.DisableFormatting));
    }

    [Fact]
    public void NestedArrays()
    {
        var json = """
            {
              "available_sizes": [
                [
                  "assets/images/resized/0001/1070/11070v1-max-150x150.jpg",
                  "assets/images/resized/0001/1070/11070v1-max-150x150.jpg"
                ],
                [
                  "assets/images/resized/0001/1070/11070v1-max-250x250.jpg",
                  "assets/images/resized/0001/1070/11070v1-max-250x250.jpg"
                ],
                [
                  "assets/images/resized/0001/1070/11070v1-max-250x250.jpg"
                ]
              ]
            }
            """;

        var newDoc = JsonXmlConvert.DeserializeXmlNode(json, "myRoot");

        IndentXml(newDoc.InnerXml);

        XUnitAssert.AreEqualNormalized(
            """
            <myRoot>
              <available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes>
              </available_sizes>
              <available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
              </available_sizes>
              <available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
              </available_sizes>
            </myRoot>
            """,
            IndentXml(newDoc.InnerXml));

        var newXDoc = JsonXmlConvert.DeserializeXNode(json, "myRoot");

        XUnitAssert.AreEqualNormalized(
            """
            <myRoot>
              <available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes>
              </available_sizes>
              <available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
              </available_sizes>
              <available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
              </available_sizes>
            </myRoot>
            """,
            IndentXml(newXDoc.ToString(SaveOptions.DisableFormatting)));

        var newJson = JsonXmlConvert.SerializeXmlNode(newDoc, Formatting.Indented);
        Console.WriteLine(newJson);
    }

    [Fact]
    public void RoundTripNestedArrays()
    {
        var json = """
            {
              "available_sizes": [
                [
                  "assets/images/resized/0001/1070/11070v1-max-150x150.jpg",
                  "assets/images/resized/0001/1070/11070v1-max-150x150.jpg"
                ],
                [
                  "assets/images/resized/0001/1070/11070v1-max-250x250.jpg",
                  "assets/images/resized/0001/1070/11070v1-max-250x250.jpg"
                ],
                [
                  "assets/images/resized/0001/1070/11070v1-max-250x250.jpg"
                ]
              ]
            }
            """;

        var newDoc = JsonXmlConvert.DeserializeXmlNode(json, "myRoot", true);

        XUnitAssert.AreEqualNormalized(
            """
            <myRoot>
              <available_sizes json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes>
              </available_sizes>
              <available_sizes json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
              </available_sizes>
              <available_sizes json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <available_sizes json:Array="true">assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
              </available_sizes>
            </myRoot>
            """,
            IndentXml(newDoc.InnerXml));

        var newXDoc = JsonXmlConvert.DeserializeXNode(json, "myRoot", true);

        XUnitAssert.AreEqualNormalized(
            """
            <myRoot>
              <available_sizes json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes>
              </available_sizes>
              <available_sizes json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
                <available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
              </available_sizes>
              <available_sizes json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <available_sizes json:Array="true">assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes>
              </available_sizes>
            </myRoot>
            """,
            IndentXml(newXDoc.ToString(SaveOptions.DisableFormatting)));

        var newJson = JsonXmlConvert.SerializeXmlNode(newDoc, Formatting.Indented, true);
        XUnitAssert.AreEqualNormalized(json, newJson);
    }

    [Fact]
    public void MultipleNestedArraysToXml()
    {
        var json = """
            {
              "available_sizes": [
                [
                  [113, 150],
                  "assets/images/resized/0001/1070/11070v1-max-150x150.jpg"
                ],
                [
                  [189, 250],
                  "assets/images/resized/0001/1070/11070v1-max-250x250.jpg"
                ],
                [
                  [341, 450],
                  "assets/images/resized/0001/1070/11070v1-max-450x450.jpg"
                ]
              ]
            }
            """;

        var newDoc = JsonXmlConvert.DeserializeXmlNode(json, "myRoot");

        Assert.Equal("<myRoot><available_sizes><available_sizes><available_sizes>113</available_sizes><available_sizes>150</available_sizes></available_sizes><available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes></available_sizes><available_sizes><available_sizes><available_sizes>189</available_sizes><available_sizes>250</available_sizes></available_sizes><available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes></available_sizes><available_sizes><available_sizes><available_sizes>341</available_sizes><available_sizes>450</available_sizes></available_sizes><available_sizes>assets/images/resized/0001/1070/11070v1-max-450x450.jpg</available_sizes></available_sizes></myRoot>", newDoc.InnerXml);

        var newXDoc = JsonXmlConvert.DeserializeXNode(json, "myRoot");

        Assert.Equal("<myRoot><available_sizes><available_sizes><available_sizes>113</available_sizes><available_sizes>150</available_sizes></available_sizes><available_sizes>assets/images/resized/0001/1070/11070v1-max-150x150.jpg</available_sizes></available_sizes><available_sizes><available_sizes><available_sizes>189</available_sizes><available_sizes>250</available_sizes></available_sizes><available_sizes>assets/images/resized/0001/1070/11070v1-max-250x250.jpg</available_sizes></available_sizes><available_sizes><available_sizes><available_sizes>341</available_sizes><available_sizes>450</available_sizes></available_sizes><available_sizes>assets/images/resized/0001/1070/11070v1-max-450x450.jpg</available_sizes></available_sizes></myRoot>", newXDoc.ToString(SaveOptions.DisableFormatting));
    }

    [Fact]
    public void Encoding()
    {
        var doc = new XmlDocument();

        doc.LoadXml(@"<name>O""Connor</name>"); // i use "" so it will be easier to see the  problem

        var json = SerializeXmlNode(doc);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "name": "O\"Connor"
            }
            """,
            json);
    }

    [Fact]
    public void SerializeComment()
    {
        var xml = """
            <span class="vevent">
              <a class="url" href="http://www.web2con.com/"><!-- Hi --><span>Text</span></a><!-- Hi! -->
            </span>
            """;
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var jsonText = SerializeXmlNode(doc);

        var expected = """
            {
              "span": {
                "@class": "vevent",
                "a": {
                  "@class": "url",
                  "@href": "http://www.web2con.com/"/* Hi */,
                  "span": "Text"
                }/* Hi! */
              }
            }
            """;

        XUnitAssert.AreEqualNormalized(expected, jsonText);

        var newDoc = (XmlDocument) DeserializeXmlNode(jsonText);
        Assert.Equal("""<span class="vevent"><a class="url" href="http://www.web2con.com/"><!-- Hi --><span>Text</span></a><!-- Hi! --></span>""", newDoc.InnerXml);
    }

    [Fact]
    public void SerializeExample()
    {
        var xml = """
                  <?xml version="1.0" standalone="no"?>
                  <root>
                    <person id="1">
                    <name>Alan</name>
                    <url>http://www.google.com</url>
                    </person>
                    <person id="2">
                    <name>Louis</name>
                    <url>http://www.yahoo.com</url>
                    </person>
                  </root>
                  """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var jsonText = SerializeXmlNode(doc);
        // {
        //   "?xml": {
        //     "@version": "1.0",
        //     "@standalone": "no"
        //   },
        //   "root": {
        //     "person": [
        //       {
        //         "@id": "1",
        //         "name": "Alan",
        //         "url": "http://www.google.com"
        //       },
        //       {
        //         "@id": "2",
        //         "name": "Louis",
        //         "url": "http://www.yahoo.com"
        //       }
        //     ]
        //   }
        // }

        // format
        jsonText = JObject.Parse(jsonText).ToString();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "?xml": {
                "@version": "1.0",
                "@standalone": "no"
              },
              "root": {
                "person": [
                  {
                    "@id": "1",
                    "name": "Alan",
                    "url": "http://www.google.com"
                  },
                  {
                    "@id": "2",
                    "name": "Louis",
                    "url": "http://www.yahoo.com"
                  }
                ]
              }
            }
            """,
            jsonText);

        var newDoc = (XmlDocument) DeserializeXmlNode(jsonText);

        Assert.Equal(doc.InnerXml, newDoc.InnerXml);
    }

    [Fact]
    public void DeserializeExample()
    {
        var json = """
            {
            "?xml": {
              "@version": "1.0",
              "@standalone": "no"
            },
            "root": {
              "person": [
                {
                  "@id": "1",
                  "name": "Alan",
                  "url": "http://www.google.com"
                },
                {
                  "@id": "2",
                  "name": "Louis",
                  "url": "http://www.yahoo.com"
                }
              ]
            }
            }
            """;

        var doc = (XmlDocument) DeserializeXmlNode(json);
        // <?xml version="1.0" standalone="no"?>
        // <root>
        //   <person id="1">
        //   <name>Alan</name>
        //   <url>http://www.google.com</url>
        //   </person>
        //   <person id="2">
        //   <name>Louis</name>
        //   <url>http://www.yahoo.com</url>
        //   </person>
        // </root>

        XUnitAssert.AreEqualNormalized(
            """<?xml version="1.0" standalone="no"?><root><person id="1"><name>Alan</name><url>http://www.google.com</url></person><person id="2"><name>Louis</name><url>http://www.yahoo.com</url></person></root>""",
            doc.InnerXml);
    }

    [Fact]
    public void EscapingNames()
    {
        var json = """
            {
              "root!": {
                "person!": [
                  {
                    "@id!": "1",
                    "name!": "Alan",
                    "url!": "http://www.google.com"
                  },
                  {
                    "@id!": "2",
                    "name!": "Louis",
                    "url!": "http://www.yahoo.com"
                  }
                ]
              }
            }
            """;

        var doc = (XmlDocument) DeserializeXmlNode(json);

        Assert.Equal("""<root_x0021_><person_x0021_ id_x0021_="1"><name_x0021_>Alan</name_x0021_><url_x0021_>http://www.google.com</url_x0021_></person_x0021_><person_x0021_ id_x0021_="2"><name_x0021_>Louis</name_x0021_><url_x0021_>http://www.yahoo.com</url_x0021_></person_x0021_></root_x0021_>""", doc.InnerXml);

        var json2 = SerializeXmlNode(doc);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "root!": {
                "person!": [
                  {
                    "@id!": "1",
                    "name!": "Alan",
                    "url!": "http://www.google.com"
                  },
                  {
                    "@id!": "2",
                    "name!": "Louis",
                    "url!": "http://www.yahoo.com"
                  }
                ]
              }
            }
            """,
            json2);
    }

    [Fact]
    public void SerializeDeserializeMetadataProperties()
    {
        var circularDictionary = new PreserveReferencesHandlingTests.CircularDictionary
        {
            { "other", new() { { "blah", null } } }
        };
        circularDictionary.Add("self", circularDictionary);

        var json = JsonConvert.SerializeObject(circularDictionary, Formatting.Indented,
            new JsonSerializerSettings {PreserveReferencesHandling = PreserveReferencesHandling.All});

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "other": {
                "$id": "2",
                "blah": null
              },
              "self": {
                "$ref": "1"
              }
            }
            """,
            json);

        var node = DeserializeXmlNode(json, "root");
        var xml = GetIndentedInnerXml(node);
        var expected = """
            <?xml version="1.0" encoding="utf-16"?>
            <root xmlns:json="http://james.newtonking.com/projects/json" json:id="1">
              <other json:id="2">
                <blah />
              </other>
              <self json:ref="1" />
            </root>
            """;

        XUnitAssert.AreEqualNormalized(expected, xml);

        var xmlJson = SerializeXmlNode(node);
        var expectedXmlJson = """
            {
              "root": {
                "$id": "1",
                "other": {
                  "$id": "2",
                  "blah": null
                },
                "self": {
                  "$ref": "1"
                }
              }
            }
            """;

        XUnitAssert.AreEqualNormalized(expectedXmlJson, xmlJson);
    }

    [Fact]
    public void SerializeDeserializeMetadataArray()
    {
        var json = """
            {
              "$id": "1",
              "$values": [
                "1",
                "2",
                "3",
                "4",
                "5"
              ]
            }
            """;

        XmlNode node = JsonXmlConvert.DeserializeXmlNode(json, "root");
        var xml = GetIndentedInnerXml(node);

        XUnitAssert.AreEqualNormalized(
            """
            <?xml version="1.0" encoding="utf-16"?>
            <root xmlns:json="http://james.newtonking.com/projects/json" json:id="1">
              <values xmlns="http://james.newtonking.com/projects/json">1</values>
              <values xmlns="http://james.newtonking.com/projects/json">2</values>
              <values xmlns="http://james.newtonking.com/projects/json">3</values>
              <values xmlns="http://james.newtonking.com/projects/json">4</values>
              <values xmlns="http://james.newtonking.com/projects/json">5</values>
            </root>
            """, xml);

        var newJson = JsonXmlConvert.SerializeXmlNode(node, Formatting.Indented, true);

        XUnitAssert.AreEqualNormalized(json, newJson);
    }

    [Fact]
    public void SerializeDeserializeMetadataArrayNoId()
    {
        var json = """
            {
              "$values": [
                "1",
                "2",
                "3",
                "4",
                "5"
              ]
            }
            """;

        XmlNode node = JsonXmlConvert.DeserializeXmlNode(json, "root");
        var xml = GetIndentedInnerXml(node);

        XUnitAssert.AreEqualNormalized(
            """
            <?xml version="1.0" encoding="utf-16"?>
            <root xmlns:json="http://james.newtonking.com/projects/json">
              <values xmlns="http://james.newtonking.com/projects/json">1</values>
              <values xmlns="http://james.newtonking.com/projects/json">2</values>
              <values xmlns="http://james.newtonking.com/projects/json">3</values>
              <values xmlns="http://james.newtonking.com/projects/json">4</values>
              <values xmlns="http://james.newtonking.com/projects/json">5</values>
            </root>
            """
            , xml);

        var newJson = JsonXmlConvert.SerializeXmlNode(node, Formatting.Indented, true);

        XUnitAssert.AreEqualNormalized(json, newJson);
    }

    [Fact]
    public void SerializeDeserializeMetadataArrayWithIdLast()
    {
        var json = """
            {
              "$values": [
                "1",
                "2",
                "3",
                "4",
                "5"
              ],
              "$id": "1"
            }
            """;

        XmlNode node = JsonXmlConvert.DeserializeXmlNode(json, "root");
        var xml = GetIndentedInnerXml(node);

        XUnitAssert.AreEqualNormalized(
            """
            <?xml version="1.0" encoding="utf-16"?>
            <root xmlns:json="http://james.newtonking.com/projects/json" json:id="1">
              <values xmlns="http://james.newtonking.com/projects/json">1</values>
              <values xmlns="http://james.newtonking.com/projects/json">2</values>
              <values xmlns="http://james.newtonking.com/projects/json">3</values>
              <values xmlns="http://james.newtonking.com/projects/json">4</values>
              <values xmlns="http://james.newtonking.com/projects/json">5</values>
            </root>
            """,
            xml);

        var newJson = JsonXmlConvert.SerializeXmlNode(node, Formatting.Indented, true);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "$values": [
                "1",
                "2",
                "3",
                "4",
                "5"
              ]
            }
            """,
            newJson);
    }

    [Fact]
    public void SerializeMetadataPropertyWithBadValue()
    {
        var json = """
            {
              "$id": []
            }
            """;

        var exception = Assert.Throws<JsonSerializationException>(() => JsonXmlConvert.DeserializeXmlNode(json, "root"));
        Assert.Equal("Unexpected JsonToken: StartArray. Path '$id', line 2, position 10.", exception.Message);
    }

    [Fact]
    public void SerializeDeserializeMetadataWithNullValue()
    {
        var json = """
            {
              "$id": null
            }
            """;

        XmlNode node = JsonXmlConvert.DeserializeXmlNode(json, "root");
        var xml = GetIndentedInnerXml(node);

        XUnitAssert.AreEqualNormalized(
            """
            <?xml version="1.0" encoding="utf-16"?>
            <root xmlns:json="http://james.newtonking.com/projects/json" json:id="" />
            """,
            xml);

        var newJson = JsonXmlConvert.SerializeXmlNode(node, Formatting.Indented, true);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": ""
            }
            """,
            newJson);
    }

    [Fact]
    public void SerializeDeserializeMetadataArrayNull()
    {
        var json = """
            {
              "$id": "1",
              "$values": null
            }
            """;

        XmlNode node = JsonXmlConvert.DeserializeXmlNode(json, "root");
        var xml = GetIndentedInnerXml(node);

        XUnitAssert.AreEqualNormalized(
            """
            <?xml version="1.0" encoding="utf-16"?>
            <root xmlns:json="http://james.newtonking.com/projects/json" json:id="1">
              <values xmlns="http://james.newtonking.com/projects/json" />
            </root>
            """,
            xml);

        var newJson = JsonXmlConvert.SerializeXmlNode(node, Formatting.Indented, true);

        XUnitAssert.AreEqualNormalized(json, newJson);
    }

    [Fact]
    public void EmptyPropertyName()
    {
        var json = """
            {
              "8452309520V2": {
                "": {
                  "CLIENT": {
                    "ID_EXPIRATION_1": {
                      "VALUE": "12/12/2000",
                      "DATATYPE": "D",
                      "MSG": "Missing Identification Exp. Date 1"
                    },
                    "ID_ISSUEDATE_1": {
                      "VALUE": "",
                      "DATATYPE": "D",
                      "MSG": "Missing Identification Issue Date 1"
                    }
                  }
                },
                "457463534534": {
                  "ACCOUNT": {
                    "FUNDING_SOURCE": {
                      "VALUE": "FS0",
                      "DATATYPE": "L",
                      "MSG": "Missing Source of Funds"
                    }
                  }
                }
              }
            }{
              "34534634535345": {
                "": {
                  "CLIENT": {
                    "ID_NUMBER_1": {
                      "VALUE": "",
                      "DATATYPE": "S",
                      "MSG": "Missing Picture ID"
                    },
                    "ID_EXPIRATION_1": {
                      "VALUE": "12/12/2000",
                      "DATATYPE": "D",
                      "MSG": "Missing Picture ID"
                    },
                    "WALK_IN": {
                      "VALUE": "",
                      "DATATYPE": "L",
                      "MSG": "Missing Walk in"
                    },
                    "PERSONAL_MEETING": {
                      "VALUE": "PM1",
                      "DATATYPE": "L",
                      "MSG": "Missing Met Client in Person"
                    },
                    "ID_ISSUEDATE_1": {
                      "VALUE": "",
                      "DATATYPE": "D",
                      "MSG": "Missing Picture ID"
                    },
                    "PHOTO_ID": {
                      "VALUE": "",
                      "DATATYPE": "L",
                      "MSG": "Missing Picture ID"
                    },
                    "ID_TYPE_1": {
                      "VALUE": "",
                      "DATATYPE": "L",
                      "MSG": "Missing Picture ID"
                    }
                  }
                },
                "45635624523": {
                  "ACCOUNT": {
                    "FUNDING_SOURCE": {
                      "VALUE": "FS1",
                      "DATATYPE": "L",
                      "MSG": "Missing Source of Funds"
                    }
                  }
                }
              }
            }
            """;

        var exception = Assert.Throws<JsonSerializationException>(() => DeserializeXmlNode(json));
        Assert.Equal("XmlNodeConverter cannot convert JSON with an empty property name to XML. Path '8452309520V2.', line 3, position 9.", exception.Message);
    }

    [Fact]
    public void SingleItemArrayPropertySerialization()
    {
        var product = new Product
        {
            Name = "Apple",
            ExpiryDate = new(2008, 12, 28, 0, 0, 0, DateTimeKind.Utc),
            Price = 3.99M,
            Sizes = ["Small"]
        };

        var output = JsonConvert.SerializeObject(product, new IsoDateTimeConverter());

        var xmlProduct = JsonXmlConvert.DeserializeXmlNode(output, "product", true);

        XUnitAssert.AreEqualNormalized(
            """
            <product>
              <Name>Apple</Name>
              <ExpiryDate>2008-12-28T00:00:00Z</ExpiryDate>
              <Price>3.99</Price>
              <Sizes json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">Small</Sizes>
            </product>
            """,
            IndentXml(xmlProduct.InnerXml));

        var output2 = JsonXmlConvert.SerializeXmlNode(xmlProduct.DocumentElement, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "product": {
                "Name": "Apple",
                "ExpiryDate": "2008-12-28T00:00:00Z",
                "Price": "3.99",
                "Sizes": [
                  "Small"
                ]
              }
            }
            """,
            output2);
    }

    public class TestComplexArrayClass
    {
        public string Name { get; set; }
        public IList<Product> Products { get; set; }
    }

    [Fact]
    public void ComplexSingleItemArrayPropertySerialization()
    {
        var o = new TestComplexArrayClass
        {
            Name = "Hi",
            Products =
            [
                new() {Name = "First"}
            ]
        };

        var output = JsonConvert.SerializeObject(o, new IsoDateTimeConverter());

        var xmlProduct = JsonXmlConvert.DeserializeXmlNode(output, "test", true);

        XUnitAssert.AreEqualNormalized(
            """
                <test>
                  <Name>Hi</Name>
                  <Products json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                    <Name>First</Name>
                    <ExpiryDate>2000-01-01T00:00:00Z</ExpiryDate>
                    <Price>0</Price>
                    <Sizes />
                  </Products>
                </test>
                """,
            IndentXml(xmlProduct.InnerXml));

        var output2 = JsonXmlConvert.SerializeXmlNode(xmlProduct.DocumentElement, Formatting.Indented, true);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "Hi",
              "Products": [
                {
                  "Name": "First",
                  "ExpiryDate": "2000-01-01T00:00:00Z",
                  "Price": "0",
                  "Sizes": null
                }
              ]
            }
            """,
            output2);
    }

    [Fact]
    public void OmitRootObject()
    {
        var xml = """
            <test>
              <Name>Hi</Name>
              <Name>Hi</Name>
              <Products json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <Name>First</Name>
                <ExpiryDate>2000-01-01T00:00:00Z</ExpiryDate>
                <Price>0</Price>
                <Sizes />
              </Products>
            </test>
            """;

        var d = new XmlDocument();
        d.LoadXml(xml);

        var output = JsonXmlConvert.SerializeXmlNode(d, Formatting.Indented, true);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": [
                "Hi",
                "Hi"
              ],
              "Products": [
                {
                  "Name": "First",
                  "ExpiryDate": "2000-01-01T00:00:00Z",
                  "Price": "0",
                  "Sizes": null
                }
              ]
            }
            """,
            output);
    }

    [Fact]
    public void EmptyElementWithArrayAttributeShouldWriteAttributes()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8" ?>
            <root xmlns:json="http://james.newtonking.com/projects/json">
                <A>
                    <B name="sample" json:Array="true"/>
                    <C></C>
                    <C></C>
                </A>
            </root>
            """;

        var d = new XmlDocument();
        d.LoadXml(xml);

        var json = JsonXmlConvert.SerializeXmlNode(d, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "?xml": {
                "@version": "1.0",
                "@encoding": "utf-8"
              },
              "root": {
                "A": {
                  "B": [
                    {
                      "@name": "sample"
                    }
                  ],
                  "C": [
                    "",
                    ""
                  ]
                }
              }
            }
            """,
            json);

        var d2 = JsonXmlConvert.DeserializeXmlNode(json);

        XUnitAssert.AreEqualNormalized(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <A>
                <B name="sample" />
                <C></C>
                <C></C>
              </A>
            </root>
            """,
            ToStringWithDeclaration(d2, true));
    }

    [Fact]
    public void EmptyElementWithArrayAttributeShouldWriteElement()
    {
        var xml = """
            <root>
                <Reports d1p1:Array="true" xmlns:d1p1="http://james.newtonking.com/projects/json" />
            </root>
            """;

        var d = new XmlDocument();
        d.LoadXml(xml);

        var json = JsonXmlConvert.SerializeXmlNode(d, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "root": {
                "Reports": [
                  {}
                ]
              }
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeNonInt64IntegerValues()
    {
        var dict = new Dictionary<string, object> {{"Int16", (short) 1}, {"Float", 2f}, {"Int32", 3}};
        var obj = JObject.FromObject(dict);
        var serializer = JsonSerializer.Create(
            new()
            {
                Converters =
                {
                    new XmlNodeConverter
                    {
                        DeserializeRootElementName = "root"
                    }
                }
            });
        using var reader = obj.CreateReader();
        var value = serializer.Deserialize<XmlDocument>(reader);

        Assert.Equal("<root><Int16>1</Int16><Float>2</Float><Int32>3</Int32></root>", value.InnerXml);
    }

    [Fact]
    public void DeserializingBooleanValues()
    {
        var ms = new MemoryStream("""{root:{"@booleanType":true}}"""u8.ToArray());
        var xml = new MemoryStream();

        JsonBodyToSoapXml(ms, xml);

        var xmlString = System.Text.Encoding.UTF8.GetString(xml.ToArray());

        Assert.Equal("""<?xml version="1.0" encoding="utf-8"?><root booleanType="true" />""", xmlString);
    }

    [Fact]
    public void NullAttributeValue()
    {
        var node = JsonXmlConvert.DeserializeXmlNode(
            """
            {
                "metrics": {
                    "type": "CPULOAD",
                    "@value": null
                }
            }
            """);

        XUnitAssert.AreEqualNormalized(
            """<metrics value=""><type>CPULOAD</type></metrics>""",
            node.OuterXml);
    }

    [Fact]
    public void NonStandardAttributeValues()
    {
        var o = new JObject
        {
            new JProperty("root", new JObject
            {
                new JProperty("@uri", new JValue(new Uri("http://localhost/"))),
                new JProperty("@time_span", new JValue(TimeSpan.FromMinutes(1))),
                new JProperty("@bytes", new JValue("Hello world"u8.ToArray()))
            })
        };

        using var jsonReader = o.CreateReader();
        var serializer = JsonSerializer.Create(new()
        {
            Converters = {new XmlNodeConverter()}
        });

        var document = serializer.Deserialize<XmlDocument>(jsonReader);

        XUnitAssert.AreEqualNormalized(
            """<root uri="http://localhost/" time_span="00:01:00" bytes="SGVsbG8gd29ybGQ=" />""",
            document.OuterXml);
    }

    [Fact]
    public void NonStandardElementsValues()
    {
        var o = new JObject
        {
            new JProperty("root", new JObject
            {
                new JProperty("uri", new JValue(new Uri("http://localhost/"))),
                new JProperty("time_span", new JValue(TimeSpan.FromMinutes(1))),
                new JProperty("bytes", new JValue("Hello world"u8.ToArray()))
            })
        };

        using var jsonReader = o.CreateReader();
        var serializer = JsonSerializer.Create(new()
        {
            Converters = {new XmlNodeConverter()}
        });

        var document = serializer.Deserialize<XmlDocument>(jsonReader);

        XUnitAssert.AreEqualNormalized("<root><uri>http://localhost/</uri><time_span>00:01:00</time_span><bytes>SGVsbG8gd29ybGQ=</bytes></root>", document.OuterXml);
    }

    static void JsonBodyToSoapXml(Stream json, Stream xml)
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var serializer = JsonSerializer.Create(settings);
        using var reader = new JsonTextReader(new StreamReader(json));
        var doc = serializer.Deserialize<XmlDocument>(reader);
        if (reader.Read() && reader.TokenType != JsonToken.Comment)
        {
            throw new JsonSerializationException("Additional text found in JSON string after finishing deserializing object.");
        }

        var writerSettings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false)
        };
        using var writer = XmlWriter.Create(xml, writerSettings);
        doc.Save(writer);
    }

    [Fact]
    public void DeserializeXNodeDefaultNamespace()
    {
        var xaml = """
                   <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:toolkit="clr-namespace:Microsoft.Phone.Controls;assembly=Microsoft.Phone.Controls.Toolkit" Style="{StaticResource trimFormGrid}" x:Name="TrimObjectForm">
                     <Grid.ColumnDefinitions>
                       <ColumnDefinition Width="63*" />
                       <ColumnDefinition Width="320*" />
                     </Grid.ColumnDefinitions>
                     <Grid.RowDefinitions xmlns="">
                       <RowDefinition />
                       <RowDefinition />
                       <RowDefinition />
                       <RowDefinition />
                       <RowDefinition />
                       <RowDefinition />
                       <RowDefinition />
                       <RowDefinition />
                     </Grid.RowDefinitions>
                     <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding TypedTitle, Converter={StaticResource trimPropertyConverter}}" Name="RecordTypedTitle" Grid.Column="1" Grid.Row="0" xmlns="" />
                     <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding ExternalReference, Converter={StaticResource trimPropertyConverter}}" Name="RecordExternalReference" Grid.Column="1" Grid.Row="1" xmlns="" />
                     <toolkit:DatePicker Style="{StaticResource trimFormGrid_DP}" Value="{Binding DateCreated, Converter={StaticResource trimPropertyConverter}}" Name="RecordDateCreated" Grid.Column="1" Grid.Row="2" />
                     <toolkit:DatePicker Style="{StaticResource trimFormGrid_DP}" Value="{Binding DateDue, Converter={StaticResource trimPropertyConverter}}" Name="RecordDateDue" Grid.Column="1" Grid.Row="3" />
                     <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Author, Converter={StaticResource trimPropertyConverter}}" Name="RecordAuthor" Grid.Column="1" Grid.Row="4" xmlns="" />
                     <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Container, Converter={StaticResource trimPropertyConverter}}" Name="RecordContainer" Grid.Column="1" Grid.Row="5" xmlns="" />
                     <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding IsEnclosed, Converter={StaticResource trimPropertyConverter}}" Name="RecordIsEnclosed" Grid.Column="1" Grid.Row="6" xmlns="" />
                     <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Assignee, Converter={StaticResource trimPropertyConverter}}" Name="RecordAssignee" Grid.Column="1" Grid.Row="7" xmlns="" />
                     <TextBlock Grid.Column="0" Text="Title (Free Text Part)" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="0" xmlns="" />
                     <TextBlock Grid.Column="0" Text="External ID" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="1" xmlns="" />
                     <TextBlock Grid.Column="0" Text="Date Created" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="2" xmlns="" />
                     <TextBlock Grid.Column="0" Text="Date Due" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="3" xmlns="" />
                     <TextBlock Grid.Column="0" Text="Author" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="4" xmlns="" />
                     <TextBlock Grid.Column="0" Text="Container" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="5" xmlns="" />
                     <TextBlock Grid.Column="0" Text="Enclosed?" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="6" xmlns="" />
                     <TextBlock Grid.Column="0" Text="Assignee" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="7" xmlns="" />
                   </Grid>
                   """;

        var json = JsonXmlConvert.SerializeXNode(XDocument.Parse(xaml), Formatting.Indented);

        var expectedJson = """
            {
              "Grid": {
                "@xmlns": "http://schemas.microsoft.com/winfx/2006/xaml/presentation",
                "@xmlns:x": "http://schemas.microsoft.com/winfx/2006/xaml",
                "@xmlns:toolkit": "clr-namespace:Microsoft.Phone.Controls;assembly=Microsoft.Phone.Controls.Toolkit",
                "@Style": "{StaticResource trimFormGrid}",
                "@x:Name": "TrimObjectForm",
                "Grid.ColumnDefinitions": {
                  "ColumnDefinition": [
                    {
                      "@Width": "63*"
                    },
                    {
                      "@Width": "320*"
                    }
                  ]
                },
                "Grid.RowDefinitions": {
                  "@xmlns": "",
                  "RowDefinition": [
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                  ]
                },
                "TextBox": [
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding TypedTitle, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordTypedTitle",
                    "@Grid.Column": "1",
                    "@Grid.Row": "0",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding ExternalReference, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordExternalReference",
                    "@Grid.Column": "1",
                    "@Grid.Row": "1",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding Author, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordAuthor",
                    "@Grid.Column": "1",
                    "@Grid.Row": "4",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding Container, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordContainer",
                    "@Grid.Column": "1",
                    "@Grid.Row": "5",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding IsEnclosed, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordIsEnclosed",
                    "@Grid.Column": "1",
                    "@Grid.Row": "6",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding Assignee, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordAssignee",
                    "@Grid.Column": "1",
                    "@Grid.Row": "7",
                    "@xmlns": ""
                  }
                ],
                "toolkit:DatePicker": [
                  {
                    "@Style": "{StaticResource trimFormGrid_DP}",
                    "@Value": "{Binding DateCreated, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordDateCreated",
                    "@Grid.Column": "1",
                    "@Grid.Row": "2"
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_DP}",
                    "@Value": "{Binding DateDue, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordDateDue",
                    "@Grid.Column": "1",
                    "@Grid.Row": "3"
                  }
                ],
                "TextBlock": [
                  {
                    "@Grid.Column": "0",
                    "@Text": "Title (Free Text Part)",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "0",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "External ID",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "1",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Date Created",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "2",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Date Due",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "3",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Author",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "4",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Container",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "5",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Enclosed?",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "6",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Assignee",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "7",
                    "@xmlns": ""
                  }
                ]
              }
            }
            """;

        XUnitAssert.AreEqualNormalized(expectedJson, json);

        XNode node = JsonXmlConvert.DeserializeXNode(json);

        var xaml2 = node.ToString();

        var expectedXaml = """
                           <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:toolkit="clr-namespace:Microsoft.Phone.Controls;assembly=Microsoft.Phone.Controls.Toolkit" Style="{StaticResource trimFormGrid}" x:Name="TrimObjectForm">
                             <Grid.ColumnDefinitions>
                               <ColumnDefinition Width="63*" />
                               <ColumnDefinition Width="320*" />
                             </Grid.ColumnDefinitions>
                             <Grid.RowDefinitions xmlns="">
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                             </Grid.RowDefinitions>
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding TypedTitle, Converter={StaticResource trimPropertyConverter}}" Name="RecordTypedTitle" Grid.Column="1" Grid.Row="0" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding ExternalReference, Converter={StaticResource trimPropertyConverter}}" Name="RecordExternalReference" Grid.Column="1" Grid.Row="1" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Author, Converter={StaticResource trimPropertyConverter}}" Name="RecordAuthor" Grid.Column="1" Grid.Row="4" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Container, Converter={StaticResource trimPropertyConverter}}" Name="RecordContainer" Grid.Column="1" Grid.Row="5" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding IsEnclosed, Converter={StaticResource trimPropertyConverter}}" Name="RecordIsEnclosed" Grid.Column="1" Grid.Row="6" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Assignee, Converter={StaticResource trimPropertyConverter}}" Name="RecordAssignee" Grid.Column="1" Grid.Row="7" xmlns="" />
                             <toolkit:DatePicker Style="{StaticResource trimFormGrid_DP}" Value="{Binding DateCreated, Converter={StaticResource trimPropertyConverter}}" Name="RecordDateCreated" Grid.Column="1" Grid.Row="2" />
                             <toolkit:DatePicker Style="{StaticResource trimFormGrid_DP}" Value="{Binding DateDue, Converter={StaticResource trimPropertyConverter}}" Name="RecordDateDue" Grid.Column="1" Grid.Row="3" />
                             <TextBlock Grid.Column="0" Text="Title (Free Text Part)" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="0" xmlns="" />
                             <TextBlock Grid.Column="0" Text="External ID" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="1" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Date Created" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="2" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Date Due" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="3" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Author" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="4" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Container" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="5" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Enclosed?" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="6" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Assignee" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="7" xmlns="" />
                           </Grid>
                           """;

        XUnitAssert.AreEqualNormalized(expectedXaml, xaml2);
    }

    [Fact]
    public void DeserializeXmlNodeDefaultNamespace()
    {
        var xaml = """
            <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:toolkit="clr-namespace:Microsoft.Phone.Controls;assembly=Microsoft.Phone.Controls.Toolkit" Style="{StaticResource trimFormGrid}" x:Name="TrimObjectForm">
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="63*" />
                <ColumnDefinition Width="320*" />
              </Grid.ColumnDefinitions>
              <Grid.RowDefinitions xmlns="">
                <RowDefinition />
                <RowDefinition />
                <RowDefinition />
                <RowDefinition />
                <RowDefinition />
                <RowDefinition />
                <RowDefinition />
                <RowDefinition />
              </Grid.RowDefinitions>
              <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding TypedTitle, Converter={StaticResource trimPropertyConverter}}" Name="RecordTypedTitle" Grid.Column="1" Grid.Row="0" xmlns="" />
              <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding ExternalReference, Converter={StaticResource trimPropertyConverter}}" Name="RecordExternalReference" Grid.Column="1" Grid.Row="1" xmlns="" />
              <toolkit:DatePicker Style="{StaticResource trimFormGrid_DP}" Value="{Binding DateCreated, Converter={StaticResource trimPropertyConverter}}" Name="RecordDateCreated" Grid.Column="1" Grid.Row="2" />
              <toolkit:DatePicker Style="{StaticResource trimFormGrid_DP}" Value="{Binding DateDue, Converter={StaticResource trimPropertyConverter}}" Name="RecordDateDue" Grid.Column="1" Grid.Row="3" />
              <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Author, Converter={StaticResource trimPropertyConverter}}" Name="RecordAuthor" Grid.Column="1" Grid.Row="4" xmlns="" />
              <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Container, Converter={StaticResource trimPropertyConverter}}" Name="RecordContainer" Grid.Column="1" Grid.Row="5" xmlns="" />
              <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding IsEnclosed, Converter={StaticResource trimPropertyConverter}}" Name="RecordIsEnclosed" Grid.Column="1" Grid.Row="6" xmlns="" />
              <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Assignee, Converter={StaticResource trimPropertyConverter}}" Name="RecordAssignee" Grid.Column="1" Grid.Row="7" xmlns="" />
              <TextBlock Grid.Column="0" Text="Title (Free Text Part)" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="0" xmlns="" />
              <TextBlock Grid.Column="0" Text="External ID" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="1" xmlns="" />
              <TextBlock Grid.Column="0" Text="Date Created" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="2" xmlns="" />
              <TextBlock Grid.Column="0" Text="Date Due" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="3" xmlns="" />
              <TextBlock Grid.Column="0" Text="Author" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="4" xmlns="" />
              <TextBlock Grid.Column="0" Text="Container" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="5" xmlns="" />
              <TextBlock Grid.Column="0" Text="Enclosed?" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="6" xmlns="" />
              <TextBlock Grid.Column="0" Text="Assignee" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="7" xmlns="" />
            </Grid>
            """;

        var document = new XmlDocument();
        document.LoadXml(xaml);

        var json = JsonXmlConvert.SerializeXmlNode(document, Formatting.Indented);

        var expectedJson = """
            {
              "Grid": {
                "@xmlns": "http://schemas.microsoft.com/winfx/2006/xaml/presentation",
                "@xmlns:x": "http://schemas.microsoft.com/winfx/2006/xaml",
                "@xmlns:toolkit": "clr-namespace:Microsoft.Phone.Controls;assembly=Microsoft.Phone.Controls.Toolkit",
                "@Style": "{StaticResource trimFormGrid}",
                "@x:Name": "TrimObjectForm",
                "Grid.ColumnDefinitions": {
                  "ColumnDefinition": [
                    {
                      "@Width": "63*"
                    },
                    {
                      "@Width": "320*"
                    }
                  ]
                },
                "Grid.RowDefinitions": {
                  "@xmlns": "",
                  "RowDefinition": [
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                  ]
                },
                "TextBox": [
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding TypedTitle, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordTypedTitle",
                    "@Grid.Column": "1",
                    "@Grid.Row": "0",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding ExternalReference, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordExternalReference",
                    "@Grid.Column": "1",
                    "@Grid.Row": "1",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding Author, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordAuthor",
                    "@Grid.Column": "1",
                    "@Grid.Row": "4",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding Container, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordContainer",
                    "@Grid.Column": "1",
                    "@Grid.Row": "5",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding IsEnclosed, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordIsEnclosed",
                    "@Grid.Column": "1",
                    "@Grid.Row": "6",
                    "@xmlns": ""
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_TB}",
                    "@Text": "{Binding Assignee, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordAssignee",
                    "@Grid.Column": "1",
                    "@Grid.Row": "7",
                    "@xmlns": ""
                  }
                ],
                "toolkit:DatePicker": [
                  {
                    "@Style": "{StaticResource trimFormGrid_DP}",
                    "@Value": "{Binding DateCreated, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordDateCreated",
                    "@Grid.Column": "1",
                    "@Grid.Row": "2"
                  },
                  {
                    "@Style": "{StaticResource trimFormGrid_DP}",
                    "@Value": "{Binding DateDue, Converter={StaticResource trimPropertyConverter}}",
                    "@Name": "RecordDateDue",
                    "@Grid.Column": "1",
                    "@Grid.Row": "3"
                  }
                ],
                "TextBlock": [
                  {
                    "@Grid.Column": "0",
                    "@Text": "Title (Free Text Part)",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "0",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "External ID",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "1",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Date Created",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "2",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Date Due",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "3",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Author",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "4",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Container",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "5",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Enclosed?",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "6",
                    "@xmlns": ""
                  },
                  {
                    "@Grid.Column": "0",
                    "@Text": "Assignee",
                    "@Style": "{StaticResource trimFormGrid_LBL}",
                    "@Grid.Row": "7",
                    "@xmlns": ""
                  }
                ]
              }
            }
            """;

        XUnitAssert.AreEqualNormalized(expectedJson, json);

        XmlNode node = JsonXmlConvert.DeserializeXmlNode(json);

        var stringWriter = new StringWriter();
        var writer = XmlWriter.Create(stringWriter, new()
        {
            Indent = true,
            OmitXmlDeclaration = true
        });
        node.WriteTo(writer);
        writer.Flush();

        var xaml2 = stringWriter.ToString();

        var expectedXaml = """
                           <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:toolkit="clr-namespace:Microsoft.Phone.Controls;assembly=Microsoft.Phone.Controls.Toolkit" Style="{StaticResource trimFormGrid}" x:Name="TrimObjectForm">
                             <Grid.ColumnDefinitions>
                               <ColumnDefinition Width="63*" />
                               <ColumnDefinition Width="320*" />
                             </Grid.ColumnDefinitions>
                             <Grid.RowDefinitions xmlns="">
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                               <RowDefinition />
                             </Grid.RowDefinitions>
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding TypedTitle, Converter={StaticResource trimPropertyConverter}}" Name="RecordTypedTitle" Grid.Column="1" Grid.Row="0" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding ExternalReference, Converter={StaticResource trimPropertyConverter}}" Name="RecordExternalReference" Grid.Column="1" Grid.Row="1" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Author, Converter={StaticResource trimPropertyConverter}}" Name="RecordAuthor" Grid.Column="1" Grid.Row="4" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Container, Converter={StaticResource trimPropertyConverter}}" Name="RecordContainer" Grid.Column="1" Grid.Row="5" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding IsEnclosed, Converter={StaticResource trimPropertyConverter}}" Name="RecordIsEnclosed" Grid.Column="1" Grid.Row="6" xmlns="" />
                             <TextBox Style="{StaticResource trimFormGrid_TB}" Text="{Binding Assignee, Converter={StaticResource trimPropertyConverter}}" Name="RecordAssignee" Grid.Column="1" Grid.Row="7" xmlns="" />
                             <toolkit:DatePicker Style="{StaticResource trimFormGrid_DP}" Value="{Binding DateCreated, Converter={StaticResource trimPropertyConverter}}" Name="RecordDateCreated" Grid.Column="1" Grid.Row="2" />
                             <toolkit:DatePicker Style="{StaticResource trimFormGrid_DP}" Value="{Binding DateDue, Converter={StaticResource trimPropertyConverter}}" Name="RecordDateDue" Grid.Column="1" Grid.Row="3" />
                             <TextBlock Grid.Column="0" Text="Title (Free Text Part)" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="0" xmlns="" />
                             <TextBlock Grid.Column="0" Text="External ID" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="1" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Date Created" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="2" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Date Due" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="3" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Author" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="4" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Container" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="5" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Enclosed?" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="6" xmlns="" />
                             <TextBlock Grid.Column="0" Text="Assignee" Style="{StaticResource trimFormGrid_LBL}" Grid.Row="7" xmlns="" />
                           </Grid>
                           """;

        XUnitAssert.AreEqualNormalized(expectedXaml, xaml2);
    }

    [Fact]
    public void DeserializeAttributePropertyNotAtStart()
    {
        var json = """{"item": {"@action": "update", "@itemid": "1", "elements": [{"@action": "none", "@id": "2"},{"@action": "none", "@id": "3"}],"@description": "temp"}}""";

        var xmldoc = JsonXmlConvert.DeserializeXmlNode(json);

        Assert.Equal("""<item action="update" itemid="1" description="temp"><elements action="none" id="2" /><elements action="none" id="3" /></item>""", xmldoc.InnerXml);
    }

    [Fact]
    public void SerializingXmlNamespaceScope()
    {
        var xmlString = """
                        <root xmlns="http://www.example.com/ns">
                          <a/>
                          <bns:b xmlns:bns="http://www.example.com/ns"/>
                          <c/>
                        </root>
                        """;

        var xml = XElement.Parse(xmlString);

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var json1 = JsonConvert.SerializeObject(xml, settings);

        Assert.Equal("""{"root":{"@xmlns":"http://www.example.com/ns","a":null,"bns:b":{"@xmlns:bns":"http://www.example.com/ns"},"c":null}}""", json1);
        var xml1 = new XmlDocument();
        xml1.LoadXml(xmlString);

        var json2 = JsonConvert.SerializeObject(xml1, settings);

        Assert.Equal("""{"root":{"@xmlns":"http://www.example.com/ns","a":null,"bns:b":{"@xmlns:bns":"http://www.example.com/ns"},"c":null}}""", json2);
    }

    public class NullableXml
    {
        public string Name;
        public XElement notNull;
        public XElement isNull;
    }

    [Fact]
    public void SerializeAndDeserializeNullableXml()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var xml = new NullableXml {Name = "test", notNull = XElement.Parse("<root>test</root>")};
        var json = JsonConvert.SerializeObject(xml, settings);

        var w2 = JsonConvert.DeserializeObject<NullableXml>(json, settings);
        Assert.Equal(xml.Name, w2.Name);
        Assert.Equal(xml.isNull, w2.isNull);
        Assert.Equal(xml.notNull.ToString(), w2.notNull.ToString());
    }

    [Fact]
    public void SerializeAndDeserializeXElementWithNamespaceInChildrenRootDontHaveNameSpace()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var xmlString = """
                        <root>
                        <b xmlns='http://www.example.com/ns'>Asd</b>
                        <c>AAA</c>
                        <test>adad</test>
                        </root>
                        """;

        var xml = XElement.Parse(xmlString);

        var json = JsonXmlConvert.SerializeXNode(xml);
        var xmlBack = JsonConvert.DeserializeObject<XElement>(json, settings);

        var equals = XNode.DeepEquals(xmlBack, xml);
        Assert.True(equals);
    }

    [Fact]
    public void SerializeAndDeserializeXmlElementWithNamespaceInChildrenRootDontHaveNameSpace()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new XmlNodeConverter());
        var xmlString = """
                        <root>
                        <b xmlns='http://www.example.com/ns'>Asd</b>
                        <c>AAA</c>
                        <test>adad</test>
                        </root>
                        """;

        var xml = new XmlDocument();
        xml.LoadXml(xmlString);

        var json = JsonXmlConvert.SerializeXmlNode(xml);
        var xmlBack = JsonConvert.DeserializeObject<XmlDocument>(json, settings);

        Assert.Equal("""<root><b xmlns="http://www.example.com/ns">Asd</b><c>AAA</c><test>adad</test></root>""", xmlBack.OuterXml);
    }

    [Fact]
    public void DeserializeBigInteger()
    {
        var json = "{\"DocumentId\":13779965364495889899 }";

        var node = JsonXmlConvert.DeserializeXmlNode(json);

        Assert.Equal("<DocumentId>13779965364495889899</DocumentId>", node.OuterXml);

        var json2 = JsonXmlConvert.SerializeXmlNode(node);

        Assert.Equal("""{"DocumentId":"13779965364495889899"}""", json2);
    }

    [Fact]
    public void DeserializeXmlIncompatibleCharsInPropertyName()
    {
        var json = "{\"%name\":\"value\"}";

        var node = JsonXmlConvert.DeserializeXmlNode(json);

        Assert.Equal("<_x0025_name>value</_x0025_name>", node.OuterXml);

        var json2 = JsonXmlConvert.SerializeXmlNode(node);

        Assert.Equal(json, json2);
    }

    [Fact]
    public void RootPropertyError()
    {
        var json = """
            {
              "$id": "1",
              "AOSLocaleName": "en-US",
              "AXLanguage": "EN-AU",
              "Company": "AURE",
              "CompanyTimeZone": 8,
              "CurrencyInfo": {
                "$id": "2",
                "CurrencyCode": "AUD",
                "Description": "Australian Dollar",
                "ExchangeRate": 100.0,
                "ISOCurrencyCode": "AUD",
                "Prefix": "",
                "Suffix": ""
              },
              "IsSysAdmin": true,
              "UserId": "lamar.miller",
              "UserPreferredCalendar": 0,
              "UserPreferredTimeZone": 8
            }
            """;

        var exception = Assert.Throws<JsonSerializationException>(() => JsonXmlConvert.DeserializeXmlNode(json));
        Assert.Equal("JSON root object has property '$id' that will be converted to an attribute. A root object cannot have any attribute properties. Consider specifying a DeserializeRootElementName. Path '$id', line 2, position 12.", exception.Message);
    }

    [Fact]
    public void SerializeEmptyNodeAndOmitRoot()
    {
        var xmlString = "<myemptynode />";

        var xml = new XmlDocument();
        xml.LoadXml(xmlString);

        var json = JsonXmlConvert.SerializeXmlNode(xml, Formatting.Indented, true);

        Assert.Equal("null", json);
    }

    [Fact]
    public void Serialize_XDocument_NoRoot()
    {
        var d = new XDocument();

        var json = JsonXmlConvert.SerializeXNode(d);

        Assert.Equal("{}", json);
    }

    [Fact]
    public void Deserialize_XDocument_NoRoot()
    {
        var d = JsonXmlConvert.DeserializeXNode("{}");

        Assert.Null(d.Root);
        Assert.Null(d.Declaration);
    }

    [Fact]
    public void Serialize_XDocument_NoRootWithDeclaration()
    {
        var d = new XDocument
        {
            Declaration = new("Version!", "Encoding!", "Standalone!")
        };

        var json = JsonXmlConvert.SerializeXNode(d);

        Assert.Equal("""{"?xml":{"@version":"Version!","@encoding":"Encoding!","@standalone":"Standalone!"}}""", json);
    }

    [Fact]
    public void Deserialize_XDocument_NoRootWithDeclaration()
    {
        var d = JsonXmlConvert.DeserializeXNode("""{"?xml":{"@version":"Version!","@encoding":"Encoding!","@standalone":"Standalone!"}}""");

        Assert.Null(d.Root);
        Assert.Equal("Version!", d.Declaration.Version);
        Assert.Equal("Encoding!", d.Declaration.Encoding);
        Assert.Equal("Standalone!", d.Declaration.Standalone);
    }

    [Fact]
    public void SerializeEmptyNodeAndOmitRoot_XElement()
    {
        var xmlString = "<myemptynode />";

        var xml = XElement.Parse(xmlString);

        var json = JsonXmlConvert.SerializeXNode(xml, Formatting.Indented, true);

        Assert.Equal("null", json);
    }

    [Fact]
    public void SerializeElementExplicitAttributeNamespace()
    {
        var original = XElement.Parse("<MyElement xmlns=\"http://example.com\" />");
        Assert.Equal("""<MyElement xmlns="http://example.com" />""", original.ToString());

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(original, settings);
        Assert.Equal("""{"MyElement":{"@xmlns":"http://example.com"}}""", json);

        var deserialized = JsonConvert.DeserializeObject<XElement>(json, settings);
        Assert.Equal("""<MyElement xmlns="http://example.com" />""", deserialized.ToString());
    }

    [Fact]
    public void SerializeElementImplicitAttributeNamespace()
    {
        var original = new XElement("{http://example.com}MyElement");
        Assert.Equal("""<MyElement xmlns="http://example.com" />""", original.ToString());

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(original, settings);
        Assert.Equal("""{"MyElement":{"@xmlns":"http://example.com"}}""", json);

        var deserialized = JsonConvert.DeserializeObject<XElement>(json, settings);
        Assert.Equal("""<MyElement xmlns="http://example.com" />""", deserialized.ToString());
    }

    [Fact]
    public void SerializeDocumentExplicitAttributeNamespace()
    {
        var original = XDocument.Parse("<MyElement xmlns=\"http://example.com\" />");
        Assert.Equal("""<MyElement xmlns="http://example.com" />""", original.ToString());

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(original, settings);
        Assert.Equal("""{"MyElement":{"@xmlns":"http://example.com"}}""", json);

        var deserialized = JsonConvert.DeserializeObject<XDocument>(json, settings);
        Assert.Equal("""<MyElement xmlns="http://example.com" />""", deserialized.ToString());
    }

    [Fact]
    public void SerializeDocumentImplicitAttributeNamespace()
    {
        var original = new XDocument(new XElement("{http://example.com}MyElement"));
        Assert.Equal("""<MyElement xmlns="http://example.com" />""", original.ToString());

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(original, settings);
        Assert.Equal("""{"MyElement":{"@xmlns":"http://example.com"}}""", json);

        var deserialized = JsonConvert.DeserializeObject<XDocument>(json, settings);
        Assert.Equal("""<MyElement xmlns="http://example.com" />""", deserialized.ToString());
    }

    public class Model
    {
        public XElement Document { get; set; }
    }

    [Fact]
    public void DeserializeDateInElementText()
    {
        var model = new Model
        {
            Document = new("Value", new XAttribute("foo", "bar"))
            {
                Value = "2001-01-01T11:11:11"
            }
        };

        var serializer = JsonSerializer.Create(new()
        {
            Converters =
            [
                ..new[]
                {
                    new XmlNodeConverter()
                }
            ]
        });

        var json = new StringBuilder(1024);

        using (var stringWriter = new StringWriter(json, InvariantCulture))
        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.None
               })
        {
            serializer.Serialize(jsonWriter, model);

            Assert.Equal("""{"Document":{"Value":{"@foo":"bar","#text":"2001-01-01T11:11:11"}}}""", json.ToString());
        }

        using (var stringReader = new StringReader(json.ToString()))
        using (var jsonReader = new JsonTextReader(stringReader))
        {
            var document = serializer.Deserialize<XDocument>(jsonReader);

            XUnitAssert.AreEqualNormalized(
                """
                <Document>
                  <Value foo="bar">2001-01-01T11:11:11</Value>
                </Document>
                """,
                document.ToString());
        }
    }
}