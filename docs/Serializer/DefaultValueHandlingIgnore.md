# DefaultValueHandling setting

This sample uses the `Argon.DefaultValueHandling` setting to not serialize properties with a default value.

<!-- snippet: DefaultValueHandlingIgnoreTypes -->
<a id='snippet-DefaultValueHandlingIgnoreTypes'></a>
```cs
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Person Partner { get; set; }
    public decimal? Salary { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/DefaultValueHandlingIgnore.cs#L9-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-DefaultValueHandlingIgnoreTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: DefaultValueHandlingIgnoreUsage -->
<a id='snippet-DefaultValueHandlingIgnoreUsage'></a>
```cs
var person = new Person();

var jsonIncludeDefaultValues = JsonConvert.SerializeObject(person, Formatting.Indented);

Console.WriteLine(jsonIncludeDefaultValues);
// {
//   "Name": null,
//   "Age": 0,
//   "Partner": null,
//   "Salary": null
// }

var jsonIgnoreDefaultValues = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
{
    DefaultValueHandling = DefaultValueHandling.Ignore
});

Console.WriteLine(jsonIgnoreDefaultValues);
// {}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/DefaultValueHandlingIgnore.cs#L24-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-DefaultValueHandlingIgnoreUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
