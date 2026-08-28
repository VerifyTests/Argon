# JsonConverterAttribute on a property

This sample uses the `Argon.JsonConverterAttribute` to specify that a `Argon.JsonConverter` should be used when serializing and deserializing a property.

<!-- snippet: JsonConverterAttributePropertyTypes -->
<a id='snippet-JsonConverterAttributePropertyTypes'></a>
```cs
public enum UserStatus
{
    NotConfirmed,
    Active,
    Deleted
}

public class User
{
    public string UserName { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public UserStatus Status { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/JsonConverterAttributeProperty.cs#L9-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-JsonConverterAttributePropertyTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: JsonConverterAttributePropertyUsage -->
<a id='snippet-JsonConverterAttributePropertyUsage'></a>
```cs
var user = new User
{
    UserName = @"domain\username",
    Status = UserStatus.Deleted
};

var json = JsonConvert.SerializeObject(user, Formatting.Indented);

Console.WriteLine(json);
// {
//   "UserName": "domain\\username",
//   "Status": "Deleted"
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/JsonConverterAttributeProperty.cs#L31-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-JsonConverterAttributePropertyUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
