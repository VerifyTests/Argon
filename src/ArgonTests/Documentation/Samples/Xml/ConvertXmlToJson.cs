// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class ConvertXmlToJson : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region ConvertXmlToJson

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

        var json = JsonXmlConvert.SerializeXmlNode(doc);

        Console.WriteLine(json);
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

        #endregion

        Assert.Equal(
            """{"?xml":{"@version":"1.0","@standalone":"no"},"root":{"person":[{"@id":"1","name":"Alan","url":"http://www.google.com"},{"@id":"2","name":"Louis","url":"http://www.yahoo.com"}]}}""",
            json);
    }
}