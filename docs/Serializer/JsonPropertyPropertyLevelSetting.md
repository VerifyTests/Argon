# JsonPropertyAttribute property setting

This sample uses `Argon.JsonPropertyAttribute` to change how the property value is serialized.

<!-- snippet: JsonPropertyPropertyLevelSettingTypes -->
<a id='snippet-JsonPropertyPropertyLevelSettingTypes'></a>
```cs
public class Vessel
{
    public string Name { get; set; }
    public string Class { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? LaunchDate { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/JsonPropertyPropertyLevelSetting.cs#L9-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-JsonPropertyPropertyLevelSettingTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: JsonPropertyPropertyLevelSettingUsage -->
<a id='snippet-JsonPropertyPropertyLevelSettingUsage'></a>
```cs
var vessel = new Vessel
{
    Name = "Red October",
    Class = "Typhoon"
};

var json = JsonConvert.SerializeObject(vessel, Formatting.Indented);

Console.WriteLine(json);
// {
//   "Name": "Red October",
//   "Class": "Typhoon"
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/JsonPropertyPropertyLevelSetting.cs#L25-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-JsonPropertyPropertyLevelSettingUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
