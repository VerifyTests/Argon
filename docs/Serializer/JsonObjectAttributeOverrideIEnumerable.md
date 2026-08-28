# JsonObjectAttribute force object serialization

This sample uses `Argon.JsonObjectAttribute` to serialize a class that implements `System.Collections.Generic.IEnumerable<T>` as a JSON object instead of a JSON array.

<!-- snippet: JsonObjectAttributeOverrideIEnumerableTypes -->
<a id='snippet-JsonObjectAttributeOverrideIEnumerableTypes'></a>
```cs
[JsonObject]
public class Directory : IEnumerable<string>
{
    public string Name { get; set; }
    public IList<string> Files { get; set; } = [];

    public IEnumerator<string> GetEnumerator() =>
        Files.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/JsonObjectAttributeOverrideIEnumerable.cs#L9-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-JsonObjectAttributeOverrideIEnumerableTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: JsonObjectAttributeOverrideIEnumerableUsage -->
<a id='snippet-JsonObjectAttributeOverrideIEnumerableUsage'></a>
```cs
var directory = new Directory
{
    Name = "My Documents",
    Files =
    {
        "ImportantLegalDocuments.docx",
        "WiseFinancalAdvice.xlsx"
    }
};

var json = JsonConvert.SerializeObject(directory, Formatting.Indented);

Console.WriteLine(json);
// {
//   "Name": "My Documents",
//   "Files": [
//     "ImportantLegalDocuments.docx",
//     "WiseFinancalAdvice.xlsx"
//   ]
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/JsonObjectAttributeOverrideIEnumerable.cs#L29-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-JsonObjectAttributeOverrideIEnumerableUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
