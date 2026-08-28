// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class Issue1327 : TestFixtureBase
{
    public class PersonWithXmlNode
    {
        public XmlNode TestXml { get; set; }

        public string Name { get; set; }

        public int IdNumber { get; set; }
    }

    public class PersonWithXObject
    {
        public XObject TestXml1 { get; set; }
        public XNode TestXml2 { get; set; }
        public XContainer TestXml3 { get; set; }

        public string Name { get; set; }

        public int IdNumber { get; set; }
    }

    [Fact]
    public void Test_XmlNode()
    {
        var json = """
            {
              "TestXml": {
                "orders": {
                  "order": {
                    "id": "550268",
                    "name": "vinoth"
                  }
                }
              },
              "Name": "Kumar",
              "IdNumber": 990268
            }
            """;
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var p = JsonConvert.DeserializeObject<PersonWithXmlNode>(json, settings);

        Assert.Equal("Kumar", p.Name);
        Assert.Equal("vinoth", p.TestXml.SelectSingleNode("//name").InnerText);
    }

    [Fact]
    public void Test_XObject()
    {
        var json = """
            {
              "TestXml1": {
                "orders": {
                  "order": {
                    "id": "550268",
                    "name": "vinoth"
                  }
                }
              },
              "TestXml2": {
                "orders": {
                  "order": {
                    "id": "550268",
                    "name": "vinoth"
                  }
                }
              },
              "TestXml3": {
                "orders": {
                  "order": {
                    "id": "550268",
                    "name": "vinoth"
                  }
                }
              },
              "Name": "Kumar",
              "IdNumber": 990268
            }
            """;

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new XmlNodeConverter());
        var p = JsonConvert.DeserializeObject<PersonWithXObject>(json, settings);

        Assert.Equal("Kumar", p.Name);
        Assert.Equal("vinoth", (string) ((XDocument) p.TestXml1).Root.Element("order").Element("name"));
        Assert.Equal("vinoth", (string) ((XDocument) p.TestXml2).Root.Element("order").Element("name"));
        Assert.Equal("vinoth", (string) ((XDocument) p.TestXml3).Root.Element("order").Element("name"));
    }
}