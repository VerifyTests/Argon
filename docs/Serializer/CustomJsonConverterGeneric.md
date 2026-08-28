# Custom `JsonConverter<T>`

This sample creates a custom converter from `<c>JsonConverter<T>` that overrides serialization for the `Version` class with a custom display string.

<!-- snippet: CustomJsonConverterGenericTypes -->
<a id='snippet-CustomJsonConverterGenericTypes'></a>
```cs
public class VersionConverter : JsonConverter<Version>
{
    public override void WriteJson(JsonWriter writer, Version value, JsonSerializer serializer) =>
        writer.WriteValue(value.ToString());

    public override Version ReadJson(
        JsonReader reader,
        Type type,
        Version existing,
        bool hasExisting,
        JsonSerializer serializer)
    {
        var s = (string) reader.Value;

        return new(s);
    }
}

public class NuGetPackage
{
    public string PackageId { get; set; }
    public Version Version { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/CustomJsonConverterGeneric.cs#L9-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-CustomJsonConverterGenericTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: CustomJsonConverterGenericUsage -->
<a id='snippet-CustomJsonConverterGenericUsage'></a>
```cs
var p1 = new NuGetPackage
{
    PackageId = "Argon",
    Version = new(10, 0, 4)
};

var json = JsonConvert.SerializeObject(p1, Formatting.Indented, new VersionConverter());

Console.WriteLine(json);
// {
//   "PackageId": "Argon",
//   "Version": "10.0.4"
// }

var p2 = JsonConvert.DeserializeObject<NuGetPackage>(json, new VersionConverter());

Console.WriteLine(p2.Version.ToString());
// 10.0.4
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/CustomJsonConverterGeneric.cs#L40-L61' title='Snippet source file'>snippet source</a> | <a href='#snippet-CustomJsonConverterGenericUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
