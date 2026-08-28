# Serialize Raw JSON value

This sample uses `Argon.JRaw` properties to serialize JSON with raw content.

<!-- snippet: SerializeRawJsonTypes -->
<a id='snippet-SerializeRawJsonTypes'></a>
```cs
public class JavaScriptSettings
{
    public JRaw OnLoadFunction { get; set; }
    public JRaw OnUnloadFunction { get; set; }
}
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/SerializeRawJson.cs#L9-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerializeRawJsonTypes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: SerializeRawJsonUsage -->
<a id='snippet-SerializeRawJsonUsage'></a>
```cs
var settings = new JavaScriptSettings
{
    OnLoadFunction = [with("OnLoad")],
    OnUnloadFunction = [with("function(e) { alert(e); }")]
};

var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

Console.WriteLine(json);
// {
//   "OnLoadFunction": OnLoad,
//   "OnUnloadFunction": function(e) { alert(e); }
// }
```
<sup><a href='/src/ArgonTests/Documentation/Samples/Serializer/SerializeRawJson.cs#L22-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerializeRawJsonUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
