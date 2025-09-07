# Serialize an Object

This sample serializes an object to JSON.

<!-- snippet: SerializeObjectTypes -->
<a id='snippet-SerializeObjectTypes'></a>
```cs
public class Account
{
    public string Email { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedDate { get; set; }
    public IList<string> Roles { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/SerializeObject.cs#L7-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerializeObjectTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: SerializeObjectUsage -->
<a id='snippet-SerializeObjectUsage'></a>
```cs
var account = new Account
{
    Email = "james@example.com",
    Active = true,
    CreatedDate = new(2013, 1, 20, 0, 0, 0, DateTimeKind.Utc),
    Roles =
    [
        "User",
        "Admin"
    ]
};

var json = JsonConvert.SerializeObject(account, Formatting.Indented);
// {
//   "Email": "james@example.com",
//   "Active": true,
//   "CreatedDate": "2013-01-20T00:00:00Z",
//   "Roles": [
//     "User",
//     "Admin"
//   ]
// }

Console.WriteLine(json);
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/SerializeObject.cs#L22-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerializeObjectUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
