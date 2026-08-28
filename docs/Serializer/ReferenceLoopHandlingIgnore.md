# ReferenceLoopHandling setting

This sample sets `Argon.ReferenceLoopHandling` to Ignore so that looping values are excluded from serialization instead of throwing an exception.

<!-- snippet: ReferenceLoopHandlingIgnoreTypes -->
<a id='snippet-ReferenceLoopHandlingIgnoreTypes'></a>
```cs
public class Employee
{
    public string Name { get; set; }
    public Employee Manager { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/ReferenceLoopHandlingIgnore.cs#L9-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-ReferenceLoopHandlingIgnoreTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: ReferenceLoopHandlingIgnoreUsage -->
<a id='snippet-ReferenceLoopHandlingIgnoreUsage'></a>
```cs
var joe = new Employee {Name = "Joe User"};
var mike = new Employee {Name = "Mike Manager"};
joe.Manager = mike;
mike.Manager = mike;

var json = JsonConvert.SerializeObject(joe, Formatting.Indented, new JsonSerializerSettings
{
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
});

Console.WriteLine(json);
// {
//   "Name": "Joe User",
//   "Manager": {
//     "Name": "Mike Manager"
//   }
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/ReferenceLoopHandlingIgnore.cs#L22-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-ReferenceLoopHandlingIgnoreUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
