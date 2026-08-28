# F#

[![NuGet Status](https://img.shields.io/nuget/v/Argon.FSharp.svg)](https://www.nuget.org/packages/Argon.FSharp/)

F# support is shipped in a separate nuget [Argon.FSharp](https://www.nuget.org/packages/Argon.FSharp/).


## Converters

 * `FSharpListConverter`
 * `FSharpMapConverter`
 * `DiscriminatedUnionConverter`


## FSharpConverters.Instances

<!-- snippet: FSharpConvertersInstances -->
<a id='snippet-FSharpConvertersInstances'></a>
```cs
var json = JsonConvert.SerializeObject(
    target,
    Formatting.Indented,
    FSharpConverters.Instances);

var result = JsonConvert.DeserializeObject<Target>(
                 json,
                 FSharpConverters.Instances) ??
             throw new ArgumentNullException("JsonConvert.DeserializeObject<Target>(json, FSharpConverters.Instances)");
```
<sup><a href='/src/ArgonTests/Serialization/FSharpTests.cs#L97-L109' title='Snippet source file'>snippet source</a> | <a href='#snippet-FSharpConvertersInstances' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Add F# Converters to a `JsonSerializerSettings`

<!-- snippet: AddFSharpConverters -->
<a id='snippet-AddFSharpConverters'></a>
```cs
var settings = new JsonSerializerSettings();
settings.AddFSharpConverters();
```
<sup><a href='/src/ArgonTests/Serialization/FSharpTests.cs#L117-L122' title='Snippet source file'>snippet source</a> | <a href='#snippet-AddFSharpConverters' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
