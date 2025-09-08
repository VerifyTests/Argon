# Create JSON from an Object

This sample converts .NET values to LINQ to JSON using `Argon.JToken.FromObject(System.Object)`.

<!-- snippet: FromObjectTypes -->
<a id='snippet-FromObjectTypes'></a>
```cs
public class Computer
{
    public string Cpu { get; set; }
    public int Memory { get; set; }
    public IList<string> Drives { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Linq/FromObject.cs#L7-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-FromObjectTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: FromObjectUsage -->
<a id='snippet-FromObjectUsage'></a>
```cs
var i = (JValue) JToken.FromObject(12345);

Console.WriteLine(i.Type);
// Integer
Console.WriteLine(i.ToString());
// 12345

var s = (JValue) JToken.FromObject("A string");

Console.WriteLine(s.Type);
// String
Console.WriteLine(s.ToString());
// A string

var computer = new Computer
{
    Cpu = "Intel",
    Memory = 32,
    Drives =
    [
        "DVD",
        "SSD"
    ]
};

var o = (JObject) JToken.FromObject(computer);

Console.WriteLine(o.ToString());
// {
//   "Cpu": "Intel",
//   "Memory": 32,
//   "Drives": [
//     "DVD",
//     "SSD"
//   ]
// }

var a = (JArray) JToken.FromObject(computer.Drives);

Console.WriteLine(a.ToString());
// [
//   "DVD",
//   "SSD"
// ]
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Linq/FromObject.cs#L21-L68' title='Snippet source file'>snippet source</a> | <a href='#snippet-FromObjectUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
