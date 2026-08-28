# Configure NamingStrategy property name serialization

This sample configures a `Argon.CamelCaseNamingStrategy` to not camel case properties that already have a name specified with an attribute.

<!-- snippet: NamingStrategySkipSpecifiedNamesTypes -->
<a id='snippet-NamingStrategySkipSpecifiedNamesTypes'></a>
```cs
public class User
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    [JsonProperty(PropertyName = "UPN")] public string Upn { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/NamingStrategySkipSpecifiedNames.cs#L9-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-NamingStrategySkipSpecifiedNamesTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: NamingStrategySkipSpecifiedNamesUsage -->
<a id='snippet-NamingStrategySkipSpecifiedNamesUsage'></a>
```cs
var user = new User
{
    FirstName = "John",
    LastName = "Smith",
    Upn = "john.smith@acme.com"
};

var contractResolver = new DefaultContractResolver
{
    NamingStrategy = new CamelCaseNamingStrategy
    {
        OverrideSpecifiedNames = false
    }
};

var json = JsonConvert.SerializeObject(user, new JsonSerializerSettings
{
    ContractResolver = contractResolver,
    Formatting = Formatting.Indented
});

Console.WriteLine(json);
// {
//   "firstName": "John",
//   "lastName": "Smith",
//   "UPN": "john.smith@acme.com"
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/NamingStrategySkipSpecifiedNames.cs#L23-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-NamingStrategySkipSpecifiedNamesUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
