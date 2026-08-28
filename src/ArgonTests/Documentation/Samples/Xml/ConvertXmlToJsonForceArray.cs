// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class ConvertXmlToJsonForceArray : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region ConvertXmlToJsonForceArray

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

        Console.WriteLine(json);
        // {
        //   "person": {
        //     "@id": "1",
        //     "name": "Alan",
        //     "url": "http://www.google.com",
        //     "role": "Admin1"
        //   }
        // }

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

        Console.WriteLine(json);
        // {
        //   "person": {
        //     "@id": "1",
        //     "name": "Alan",
        //     "url": "http://www.google.com",
        //     "role": [
        //       "Admin"
        //     ]
        //   }
        // }

        #endregion

        Assert.Equal("""{"person":{"@id":"1","name":"Alan","url":"http://www.google.com","role":["Admin"]}}""", json);
    }
}