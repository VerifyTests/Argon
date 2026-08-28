# Serialize a Dictionary

This sample serializes a dictionary to JSON.

<!-- snippet: SerializeDictionary -->
<a id='snippet-SerializeDictionary'></a>
```cs
var points = new Dictionary<string, int>
{
    {"James", 9001},
    {"Jo", 3474},
    {"Jess", 11926}
};

var json = JsonConvert.SerializeObject(points, Formatting.Indented);

Console.WriteLine(json);
// {
//   "James": 9001,
//   "Jo": 3474,
//   "Jess": 11926
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/SerializeDictionary.cs#L12-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerializeDictionary' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
