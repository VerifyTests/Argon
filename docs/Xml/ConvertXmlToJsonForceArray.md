# Convert XML to JSON and force array

This sample reads the `json:Array='true'` attribute in the XML and places its value in an array when converting the XML to JSON.

<!-- snippet: ConvertXmlToJsonForceArray -->
<a id='snippet-ConvertXmlToJsonForceArray'></a>
```cs
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
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Xml/ConvertXmlToJsonForceArray.cs#L10-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConvertXmlToJsonForceArray' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
