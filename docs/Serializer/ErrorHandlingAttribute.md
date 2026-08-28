# ErrorHandlingAttribute

This sample uses `Argon.OnErrorAttribute` to ignore the exception thrown setting the Roles property.


<!-- snippet: ErrorHandlingAttributeTypes -->
<a id='snippet-ErrorHandlingAttributeTypes'></a>
```cs
public class Employee :
    IJsonOnSerializeError
{
    public string Name { get; set; }
    public int Age { get; set; }

    public List<string> Roles
    {
        get
        {
            if (field == null)
            {
                throw new("Roles not loaded!");
            }

            return field;
        }
        set;
    }

    public string Title { get; set; }

    public void OnSerializeError(object originalObject, string path, object member, Exception exception, Action markAsHandled) =>
        markAsHandled();
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/ErrorHandlingAttribute.cs#L9-L37' title='Snippet source file'>snippet source</a> | <a href='#snippet-ErrorHandlingAttributeTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: ErrorHandlingAttributeUsage -->
<a id='snippet-ErrorHandlingAttributeUsage'></a>
```cs
var person = new Employee
{
    Name = "George Michael Bluth",
    Age = 16,
    Roles = null,
    Title = "Mister Manager"
};

var settings = new JsonSerializerSettings();
settings.AddInterfaceCallbacks();
var json = JsonConvert.SerializeObject(person, Formatting.Indented, settings);

Console.WriteLine(json);
// {
//   "Name": "George Michael Bluth",
//   "Age": 16,
//   "Title": "Mister Manager"
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/ErrorHandlingAttribute.cs#L42-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-ErrorHandlingAttributeUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
