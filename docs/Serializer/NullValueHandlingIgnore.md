# NullValueHandling setting

This sample serializes an object to JSON with `Argon.NullValueHandling` set to Ignore so that properties with a default value of null aren't included in the JSON result.

<!-- snippet: NullValueHandlingIgnoreTypes -->
<a id='snippet-NullValueHandlingIgnoreTypes'></a>
```cs
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Person Partner { get; set; }
    public decimal? Salary { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/NullValueHandlingIgnore.cs#L9-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-NullValueHandlingIgnoreTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: NullValueHandlingIgnoreUsage -->
<a id='snippet-NullValueHandlingIgnoreUsage'></a>
```cs
var person = new Person
{
    Name = "Nigal Newborn",
    Age = 1
};

var jsonIncludeNullValues = JsonConvert.SerializeObject(person, Formatting.Indented);

Console.WriteLine(jsonIncludeNullValues);
// {
//   "Name": "Nigal Newborn",
//   "Age": 1,
//   "Partner": null,
//   "Salary": null
// }

var jsonIgnoreNullValues = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore
});

Console.WriteLine(jsonIgnoreNullValues);
// {
//   "Name": "Nigal Newborn",
//   "Age": 1
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/NullValueHandlingIgnore.cs#L24-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-NullValueHandlingIgnoreUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
