# ObjectCreationHandling setting

This sample deserializes JSON with `Argon.ObjectCreationHandling` set to Replace so that collection values aren't duplicated.

<!-- snippet: DeserializeObjectCreationHandlingTypes -->
<a id='snippet-DeserializeObjectCreationHandlingTypes'></a>
```cs
public class UserViewModel
{
    public string Name { get; set; }
    public IList<string> Offices { get; } =
    [
        "Auckland",
        "Wellington",
        "Christchurch"
    ];
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/DeserializeObjectCreationHandling.cs#L7-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-DeserializeObjectCreationHandlingTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: DeserializeObjectCreationHandlingUsage -->
<a id='snippet-DeserializeObjectCreationHandlingUsage'></a>
```cs
var json = """
    {
      'Name': 'James',
      'Offices': [
        'Auckland',
        'Wellington',
        'Christchurch'
      ]
    }
    """;

var model1 = JsonConvert.DeserializeObject<UserViewModel>(json);

foreach (var office in model1.Offices)
{
    Console.WriteLine(office);
}
// Auckland
// Wellington
// Christchurch
// Auckland
// Wellington
// Christchurch

var model2 = JsonConvert.DeserializeObject<UserViewModel>(json, new JsonSerializerSettings
{
    ObjectCreationHandling = ObjectCreationHandling.Replace
});

foreach (var office in model2.Offices)
{
    Console.WriteLine(office);
}

// Auckland
// Wellington
// Christchurch
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/DeserializeObjectCreationHandling.cs#L25-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-DeserializeObjectCreationHandlingUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
