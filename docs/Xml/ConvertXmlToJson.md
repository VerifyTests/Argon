# Convert XML to JSON

This sample converts XML to JSON.

<!-- snippet: ConvertXmlToJson -->
<a id='snippet-ConvertXmlToJson'></a>
```cs
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
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Xml/ConvertXmlToJson.cs#L10-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConvertXmlToJson' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
