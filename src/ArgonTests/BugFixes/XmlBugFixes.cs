// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Xml;

public class XmlBugFixes : TestFixtureBase
{
    // Bug: a single-element nested array lost its json:Array marker when the property name was
    // prefixed or XML-encoded, because the raw JSON name was compared against the decoded
    // element LocalName. Result: the inner array nesting was dropped on round-trip.

    [Fact]
    public void Nested_single_element_array_with_prefix_round_trips()
    {
        var json = """{"root":{"@xmlns:ns":"http://x","ns:items":[["a"]]}}""";

        var doc = JsonXmlConvert.DeserializeXmlNode(json, null, true);
        var roundTripped = JObject.Parse(JsonXmlConvert.SerializeXmlNode(doc));

        var items = roundTripped["root"]["ns:items"];
        Assert.Equal(JTokenType.Array, items.Type);
        Assert.Equal(JTokenType.Array, items[0].Type);
        Assert.Equal("a", (string) items[0][0]);
    }

    // Bug: two or more sibling comments serialized to "#comment": [] - all comment text dropped
    // plus a bogus property that cannot round-trip.

    [Fact]
    public void Multiple_sibling_comments_round_trip()
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

    // Bug: XmlDocumentWrapper.CreateXmlDocumentType discarded the internalSubset argument, so
    // JSON -> XmlDocument silently lost DTD internal subsets.

    [Fact]
    public void DocType_internal_subset_is_preserved()
    {
        var json = """{"!DOCTYPE":{"@name":"root","@internalSubset":"<!ENTITY foo \"bar\">"},"root":"x"}""";

        var doc = JsonXmlConvert.DeserializeXmlNode(json);

        Assert.NotNull(doc.DocumentType);
        Assert.Contains("ENTITY foo", doc.DocumentType.InternalSubset);
    }

    // Bug: #cdata-section / #text with a JSON null crashed with ArgumentNullException for
    // XDocument targets (while XmlDocument produced an empty node).

    [Fact]
    public void CData_null_does_not_crash_xdocument()
    {
        var xdoc = JsonXmlConvert.DeserializeXNode("""{"root":{"#cdata-section":null}}""");

        Assert.NotNull(xdoc);
        Assert.Contains("CDATA", xdoc.ToString());
    }

    [Fact]
    public void Text_null_does_not_crash_xdocument()
    {
        var xdoc = JsonXmlConvert.DeserializeXNode("""{"root":{"#text":null}}""");

        Assert.NotNull(xdoc);
    }
}
