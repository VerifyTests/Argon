# JsonPropertyAttribute name

This sample uses `Argon.JsonPropertyAttribute` to change the names of properties when they are serialized to JSON.

<!-- snippet: JsonPropertyNameTypes -->
<a id='snippet-JsonPropertyNameTypes'></a>
```cs
public class Videogame
{
    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("release_date")] public DateTime ReleaseDate { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/JsonPropertyName.cs#L9-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-JsonPropertyNameTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: JsonPropertyNameUsage -->
<a id='snippet-JsonPropertyNameUsage'></a>
```cs
var starcraft = new Videogame
{
    Name = "Starcraft",
    ReleaseDate = new(1998, 1, 1)
};

var json = JsonConvert.SerializeObject(starcraft, Formatting.Indented);

Console.WriteLine(json);
// {
//   "name": "Starcraft",
//   "release_date": "1998-01-01T00:00:00"
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/JsonPropertyName.cs#L23-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-JsonPropertyNameUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
