# Write JSON text with JToken.ToString

This sample converts LINQ to JSON objects to JSON.

<!-- snippet: ToString -->
<a id='snippet-ToString'></a>
```cs
var o = JObject.Parse("{'string1':'value','integer2':99,'datetime3':'2000-05-23T00:00:00'}");

Console.WriteLine(o.ToString());
// {
//   "string1": "value",
//   "integer2": 99,
//   "datetime3": "2000-05-23T00:00:00"
// }

Console.WriteLine(o.ToString(Formatting.None));
// {"string1":"value","integer2":99,"datetime3":"2000-05-23T00:00:00"}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Linq/ToString.cs#L12-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-ToString' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
