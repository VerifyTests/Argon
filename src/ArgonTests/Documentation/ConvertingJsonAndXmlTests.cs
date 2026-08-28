// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.


// ReSharper disable UnusedVariable
// ReSharper disable NotAccessedVariable
// ReSharper disable RedundantAssignment

namespace Argon.Tests.Documentation;

public class ConvertingJsonAndXmlTests : TestFixtureBase
{
    [Fact]
    public void SerializeXmlNode()
    {
        #region SerializeXmlNode

        var xml = """
                  <?xml version='1.0' standalone='no'?>
                  <root>
                    <person id='1'>
                      <name>Alan</name>
                      <url>http://www.google.com</url>
                    </person>
                    <person id='2'>
                      <name>Louis</name>
                      <url>http://www.yahoo.com</url>
                    </person>
                  </root>
                  """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var jsonText = JsonXmlConvert.SerializeXmlNode(doc);
        //{
        //  "?xml": {
        //    "@version": "1.0",
        //    "@standalone": "no"
        //  },
        //  "root": {
        //    "person": [
        //      {
        //        "@id": "1",
        //        "name": "Alan",
        //        "url": "http://www.google.com"
        //      },
        //      {
        //        "@id": "2",
        //        "name": "Louis",
        //        "url": "http://www.yahoo.com"
        //      }
        //    ]
        //  }
        //}

        #endregion
    }

    [Fact]
    public void DeserializeXmlNode()
    {
        #region DeserializeXmlNode

        var json = """
            {
              '?xml': {
                '@version': '1.0',
                '@standalone': 'no'
              },
              'root': {
                'person': [
                  {
                    '@id': '1',
                    'name': 'Alan',
                    'url': 'http://www.google.com'
                  },
                  {
                    '@id': '2',
                    'name': 'Louis',
                    'url': 'http://www.yahoo.com'
                  }
                ]
              }
            }
            """;

        var doc = JsonXmlConvert.DeserializeXmlNode(json);
        // <?xml version="1.0" standalone="no"?>
        // <root>
        //   <person id="1">
        //     <name>Alan</name>
        //     <url>http://www.google.com</url>
        //   </person>
        //   <person id="2">
        //     <name>Louis</name>
        //     <url>http://www.yahoo.com</url>
        //   </person>
        // </root>

        #endregion
    }

    [Fact]
    public void ForceJsonArray()
    {
        #region ForceJsonArray

        var xml = """
                  <person id='1'>
                    <name>Alan</name>
                    <url>http://www.google.com</url>
                    <role>Admin1</role>
                  </person>
                  """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var json = JsonXmlConvert.SerializeXmlNode(doc);
        //{
        //  "person": {
        //    "@id": "1",
        //    "name": "Alan",
        //    "url": "http://www.google.com",
        //    "role": "Admin1"
        //  }
        //}

        xml = """
              <person xmlns:json='http://james.newtonking.com/projects/json' id='1'>
                <name>Alan</name>
                <url>http://www.google.com</url>
                <role json:Array='true'>Admin</role>
              </person>
              """;

        doc = new();
        doc.LoadXml(xml);

        json = JsonXmlConvert.SerializeXmlNode(doc);
        //{
        //  "person": {
        //    "@id": "1",
        //    "name": "Alan",
        //    "url": "http://www.google.com",
        //    "role": [
        //      "Admin"
        //    ]
        //  }
        //}

        #endregion
    }
}