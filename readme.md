# <img src='/src/icon.png' height='30px'> Argon

[![Build status](https://img.shields.io/appveyor/build/SimonCropp/Argon)](https://ci.appveyor.com/project/SimonCropp/Argon)
[![NuGet Status](https://img.shields.io/nuget/v/Argon.svg?label=Argon)](https://www.nuget.org/packages/Argon/)
[![NuGet Status](https://img.shields.io/nuget/v/Argon.DataSets.svg?label=Argon.DataSets)](https://www.nuget.org/packages/Argon.DataSets/)
[![NuGet Status](https://img.shields.io/nuget/v/Argon.Xml.svg?label=Argon.Xml)](https://www.nuget.org/packages/Argon.Xml/)
[![NuGet Status](https://img.shields.io/nuget/v/Argon.JsonPath.svg?label=Argon.JsonPath)](https://www.nuget.org/packages/Argon.JsonPath/)
[![NuGet Status](https://img.shields.io/nuget/v/Argon.FSharp.svg?label=Argon.FSharp)](https://www.nuget.org/packages/Argon.FSharp/)
[![NuGet Status](https://img.shields.io/nuget/v/Argon.InterfaceCallbacks.svg?label=Argon.InterfaceCallbacks)](https://www.nuget.org/packages/Argon.InterfaceCallbacks/)

Argon is a JSON framework for .NET. It is a hard fork of [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json).<!-- singleLineInclude: intro. path: /docs/intro.include.md -->

**See [Milestones](../../milestones?state=closed) for release notes.**


## Sponsors

### Entity Framework Extensions<!-- include: zzz. path: /docs/zzz.include.md -->

[Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=simoncropp&utm_medium=Argon) is a major sponsor and is proud to contribute to the development this project.

[![Entity Framework Extensions](https://raw.githubusercontent.com/VerifyTests/Argon/refs/heads/main/docs/zzz.png)](https://entityframework-extensions.net/?utm_source=simoncropp&utm_medium=Argon)<!-- endInclude -->


### JetBrains

[![JetBrains logo.](https://resources.jetbrains.com/storage/products/company/brand/logos/jetbrains.svg)](https://jb.gg/OpenSourceSupport)


## NuGet

 * https://www.nuget.org/packages/Argon
 * https://www.nuget.org/packages/Argon.DataSets
 * https://www.nuget.org/packages/Argon.Xml
 * https://www.nuget.org/packages/Argon.JsonPath
 * https://www.nuget.org/packages/Argon.FSharp
 * https://www.nuget.org/packages/Argon.InterfaceCallbacks


## Serialize JSON

<!-- snippet: SerializeJson -->
<a id='snippet-SerializeJson'></a>
```cs
var product = new Product
{
    Name = "Apple",
    Expiry = new(2008, 12, 28),
    Sizes = ["Small"]
};

var json = JsonConvert.SerializeObject(product);
// {
//   "Name": "Apple",
//   "Expiry": "2008-12-28T00:00:00",
//   "Sizes": [
//     "Small"
//   ]
// }
```
<sup><a href='/src/ArgonTests/Documentation/Snippets.cs#L38-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerializeJson' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Deserialize JSON

<!-- snippet: DeserializeJson -->
<a id='snippet-DeserializeJson'></a>
```cs
var json = """
    {
      'Name': 'Bad Boys',
      'ReleaseDate': '1995-4-7T00:00:00',
      'Genres': [
        'Action',
        'Comedy'
      ]
    }
    """;

var movie = JsonConvert.DeserializeObject<Movie>(json);

var name = movie.Name;
// Bad Boys
```
<sup><a href='/src/ArgonTests/Documentation/Snippets.cs#L69-L87' title='Snippet source file'>snippet source</a> | <a href='#snippet-DeserializeJson' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## LINQ to JSON
<!-- snippet: LinqToJson -->
<a id='snippet-LinqToJson'></a>
```cs
var jArray = new JArray
{
    "Manual text",
    new DateTime(2000, 5, 23)
};

var jObject = new JObject
{
    ["MyArray"] = jArray
};

var json = jObject.ToString();
// {
//   "MyArray": [
//     "Manual text",
//     "2000-05-23T00:00:00"
//   ]
// }
```
<sup><a href='/src/ArgonTests/Documentation/Snippets.cs#L11-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-LinqToJson' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

  * [Argon is JSON framework for .NET](/docs/readme.md)<!-- include: index. path: /docs/index.include.md -->
  * [Serialization using ContractResolver](/docs/ContractResolver.md)
  * [Converting between JSON and XML](/docs/ConvertingJSONandXML.md)
  * [Creating JSON](/docs/CreatingLINQtoJSON.md)
  * [Dates in JSON](/docs/DatesInJSON.md)
  * [Differences](/docs/differences.md)
  * [F#](/docs/FSharp.md)
  * [LINQ to JSON](/docs/LINQtoJSON.md)
  * [Parsing JSON](/docs/ParsingLINQtoJSON.md)
  * [Performance](/docs/Performance.md)
  * [Preserving Object References](/docs/PreserveObjectReferences.md)
  * [Querying JSON with LINQ](/docs/QueryingLINQtoJSON.md)
  * [Basic Reading and Writing JSON](/docs/ReadingWritingJSON.md)
  * [Reducing JSON size](/docs/ReducingSerializedJSONSize.md)
  * [Querying JSON with SelectToken](/docs/SelectToken.md)
  * [Attributes](/docs/SerializationAttributes.md)
  * [Serialization callbacks](/docs/SerializationCallbacks.md)
  * [Error handling during serialization and deserialization.](/docs/SerializationErrorHandling.md)
  * [Srialization Guide](/docs/SerializationGuide.md)
  * [Serialization Settings](/docs/SerializationSettings.md)
  * [ Serializing Collections](/docs/SerializingCollections.md)
  * [Serializing and Deserializing JSON](/docs/SerializingJSON.md)
  * [Deserializing Partial JSON Fragments](/docs/SerializingJSONFragments.md)
  * [Json](/docs/Json)
    * [Custom JsonReader](/docs/Json/CustomJsonReader.md)
    * [Custom JsonWriter](/docs/Json/CustomJsonWriter.md)
    * [Read JSON using JsonTextReader](/docs/Json/ReadJsonWithJsonTextReader.md)
    * [Read Multiple Fragments With JsonReader](/docs/Json/ReadMultipleContentWithJsonReader.md)
    * [Write JSON with JsonTextWriter](/docs/Json/WriteJsonWithJsonTextWriter.md)
  * [JsonPath](/docs/JsonPath)
    * [Querying JSON with complex JSON Path](/docs/JsonPath/ErrorWhenNoMatchQuery.md)
    * [Querying JSON with SelectToken](/docs/JsonPath/QueryJsonSelectToken.md)
    * [Querying JSON with JSON Path and escaped properties](/docs/JsonPath/QueryJsonSelectTokenEscaped.md)
    * [Querying JSON with complex JSON Path](/docs/JsonPath/QueryJsonSelectTokenJsonPath.md)
    * [Querying JSON with JSON Path and LINQ](/docs/JsonPath/QueryJsonSelectTokenWithLinq.md)
    * [Load and query JSON with JToken.SelectToken](/docs/JsonPath/RegexQuery.md)
    * [Querying JSON with complex JSON Path](/docs/JsonPath/StrictEqualsQuery.md)
  * [Linq](/docs/Linq)
    * [Cloning JSON with JToken.DeepClone](/docs/Linq/Clone.md)
    * [Create JSON from an Anonymous Type](/docs/Linq/CreateJsonAnonymousObject.md)
    * [Create JSON using Collection Initializers](/docs/Linq/CreateJsonCollectionInitializer.md)
    * [Create JSON declaratively with LINQ](/docs/Linq/CreateJsonDeclaratively.md)
    * [Create JSON with dynamic](/docs/Linq/CreateJsonDynamic.md)
    * [Create JSON with JTokenWriter](/docs/Linq/CreateJsonJTokenWriter.md)
    * [Create JObject and JArray programatically](/docs/Linq/CreateJsonManually.md)
    * [Create JTokenReader from JToken](/docs/Linq/CreateReader.md)
    * [Creates JTokenWriter JToken](/docs/Linq/CreateWriter.md)
    * [Comparing JSON with JToken.DeepEquals](/docs/Linq/DeepEquals.md)
    * [Deserializing from JSON with LINQ](/docs/Linq/DeserializeWithLinq.md)
    * [Create JSON from an Object](/docs/Linq/FromObject.md)
    * [Using JObject.Properties](/docs/Linq/JObjectProperties.md)
    * [Casting JValue](/docs/Linq/JValueCast.md)
    * [Using JValue.Value](/docs/Linq/JValueValue.md)
    * [Modifying JSON](/docs/Linq/ModifyJson.md)
    * [Parse JSON using JToken.Parse](/docs/Linq/ParseJsonAny.md)
    * [Parse JSON using JArray.Parse](/docs/Linq/ParseJsonArray.md)
    * [Parsing JSON Object using JObject.Parse](/docs/Linq/ParseJsonObject.md)
    * [Querying JSON with complex JSON Path](/docs/Linq/QueryJson.md)
    * [Querying JSON with dynamic](/docs/Linq/QueryJsonDynamic.md)
    * [Querying JSON with LINQ](/docs/Linq/QueryJsonLinq.md)
    * [Read JSON from a file using JObject](/docs/Linq/ReadJson.md)
    * [Serializing to JSON with LINQ](/docs/Linq/SerializeWithLinq.md)
    * [Convert JSON to a Type](/docs/Linq/ToObjectComplex.md)
    * [LINQ to JSON with JToken.ToObject](/docs/Linq/ToObjectGeneric.md)
    * [Convert JSON to a Type](/docs/Linq/ToObjectType.md)
    * [Write JSON text with JToken.ToString](/docs/Linq/ToString.md)
    * [Write JSON to a file](/docs/Linq/WriteToJsonFile.md)
  * [Serializer](/docs/Serializer)
    * [Custom IContractResolver](/docs/Serializer/CustomContractResolver.md)
    * [Custom JsonConverter](/docs/Serializer/CustomJsonConverter.md)
    * [Custom `JsonConverter<T>`](/docs/Serializer/CustomJsonConverterGeneric.md)
    * [DataContract and DataMember Attributes](/docs/Serializer/DataContractAndDataMember.md)
    * [Serialize with DefaultSettings](/docs/Serializer/DefaultSettings.md)
    * [DefaultValueAttribute](/docs/Serializer/DefaultValueAttributeIgnore.md)
    * [DefaultValueHandling setting](/docs/Serializer/DefaultValueHandlingIgnore.md)
    * [Deserialize an Anonymous Type](/docs/Serializer/DeserializeAnonymousType.md)
    * [Deserialize a Collection](/docs/Serializer/DeserializeCollection.md)
    * [ConstructorHandling setting](/docs/Serializer/DeserializeConstructorHandling.md)
    * [Deserialize a DataSet](/docs/Serializer/DeserializeDataSet.md)
    * [Deserialize a Dictionary](/docs/Serializer/DeserializeDictionary.md)
    * [Deserialize an immutable collection](/docs/Serializer/DeserializeImmutableCollections.md)
    * [MetadataPropertyHandling setting](/docs/Serializer/DeserializeMetadataPropertyHandling.md)
    * [MissingMemberHandling setting](/docs/Serializer/DeserializeMissingMemberHandling.md)
    * [Deserialize an Object](/docs/Serializer/DeserializeObject.md)
    * [ObjectCreationHandling setting](/docs/Serializer/DeserializeObjectCreationHandling.md)
    * [Deserialize JSON from a file](/docs/Serializer/DeserializeWithJsonSerializerFromFile.md)
    * [ErrorHandlingAttribute](/docs/Serializer/ErrorHandlingAttribute.md)
    * [ErrorHandling setting](/docs/Serializer/ErrorHandlingEvent.md)
    * [Float Precision](/docs/Serializer/FloatPrecision.md)
    * [JsonConstructorAttribute](/docs/Serializer/JsonConstructorAttribute.md)
    * [JsonConverterAttribute on a class](/docs/Serializer/JsonConverterAttributeClass.md)
    * [JsonConverterAttribute on a property](/docs/Serializer/JsonConverterAttributeProperty.md)
    * [ JsonObjectAttribute opt-in serialization](/docs/Serializer/JsonObjectAttributeOptIn.md)
    * [JsonObjectAttribute force object serialization](/docs/Serializer/JsonObjectAttributeOverrideIEnumerable.md)
    * [JsonPropertyAttribute items setting](/docs/Serializer/JsonPropertyItemLevelSetting.md)
    * [JsonPropertyAttribute name](/docs/Serializer/JsonPropertyName.md)
    * [JsonPropertyAttribute order](/docs/Serializer/JsonPropertyOrder.md)
    * [JsonPropertyAttribute property setting](/docs/Serializer/JsonPropertyPropertyLevelSetting.md)
    * [JsonPropertyAttribute required](/docs/Serializer/JsonPropertyRequired.md)
    * [MaxDepth setting](/docs/Serializer/MaxDepth.md)
    * [Camel case property names](/docs/Serializer/NamingStrategyCamelCase.md)
    * [Configure CamelCaseNamingStrategy](/docs/Serializer/NamingStrategySkipDictionaryKeys.md)
    * [Configure NamingStrategy property name serialization](/docs/Serializer/NamingStrategySkipSpecifiedNames.md)
    * [Snake case property names](/docs/Serializer/NamingStrategySnakeCase.md)
    * [NullValueHandling setting](/docs/Serializer/NullValueHandlingIgnore.md)
    * [PreserveReferencesHandling setting](/docs/Serializer/PreserveReferencesHandlingObject.md)
    * [JsonIgnoreAttribute](/docs/Serializer/PropertyJsonIgnore.md)
    * [ReferenceLoopHandling setting](/docs/Serializer/ReferenceLoopHandlingIgnore.md)
    * [Serialization Callback Interfaces](/docs/Serializer/SerializationCallbackInterfaces.md)
    * [Serializing Collections](/docs/Serializer/SerializeCollection.md)
    * [Custom IContractResolver](/docs/Serializer/SerializeContractResolver.md)
    * [Serialize a DataSet](/docs/Serializer/SerializeDataSet.md)
    * [DateTimeZoneHandling setting](/docs/Serializer/SerializeDateTimeZoneHandling.md)
    * [Serialize a Dictionary](/docs/Serializer/SerializeDictionary.md)
    * [Serialize an immutable collection](/docs/Serializer/SerializeImmutableCollections.md)
    * [Serialize an Object](/docs/Serializer/SerializeObject.md)
    * [Serialize Raw JSON value](/docs/Serializer/SerializeRawJson.md)
    * [Custom SerializationBinder](/docs/Serializer/SerializeSerializationBinder.md)
    * [TypeNameHandling setting](/docs/Serializer/SerializeTypeNameHandling.md)
    * [Serialize Unindented JSON](/docs/Serializer/SerializeUnindentedJson.md)
    * [Serialize with JsonConverters](/docs/Serializer/SerializeWithJsonConverters.md)
    * [Serialize JSON to a file](/docs/Serializer/SerializeWithJsonSerializerToFile.md)
  * [Xml](/docs/Xml)
    * [Convert JSON to XML](/docs/Xml/ConvertJsonToXml.md)
    * [Convert XML to JSON](/docs/Xml/ConvertXmlToJson.md)
    * [Convert XML to JSON and force array](/docs/Xml/ConvertXmlToJsonForceArray.md)<!-- endInclude -->

## Icon

[Helmet](https://thenounproject.com/term/helmet/1681772/) designed by [Juan Manuel Corredor](https://thenounproject.com/juan_corredor/) from [The Noun Project](https://thenounproject.com).
