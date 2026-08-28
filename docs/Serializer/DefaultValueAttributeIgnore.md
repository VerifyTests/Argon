# DefaultValueAttribute

This sample uses the `System.ComponentModel.DefaultValueAttribute` to override the default value for a property and exclude it from serialization using `Argon.DefaultValueHandling`.

<!-- snippet: DefaultValueAttributeIgnoreTypes -->
<a id='snippet-DefaultValueAttributeIgnoreTypes'></a>
```cs
public class Customer
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [DefaultValue(" ")] public string FullName => $"{FirstName} {LastName}";
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/DefaultValueAttributeIgnore.cs#L10-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-DefaultValueAttributeIgnoreTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: DefaultValueAttributeIgnoreUsage -->
<a id='snippet-DefaultValueAttributeIgnoreUsage'></a>
```cs
var customer = new Customer();

var jsonIncludeDefaultValues = JsonConvert.SerializeObject(customer, Formatting.Indented);

Console.WriteLine(jsonIncludeDefaultValues);
// {
//   "FirstName": null,
//   "LastName": null,
//   "FullName": " "
// }

var jsonIgnoreDefaultValues = JsonConvert.SerializeObject(customer, Formatting.Indented, new JsonSerializerSettings
{
    DefaultValueHandling = DefaultValueHandling.Ignore
});

Console.WriteLine(jsonIgnoreDefaultValues);
// {}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/DefaultValueAttributeIgnore.cs#L25-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-DefaultValueAttributeIgnoreUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
