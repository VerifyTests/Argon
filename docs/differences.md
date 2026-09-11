# Differences

WIP

## Additions

### JsonDictionaryContract.OrderByKey

### JsonDictionaryContract.ShouldSerializeItem

### JsonArrayContract.ShouldSerializeItem

### C# union support

Unions are serialized as their active case with no wrapper and no discriminator, matching
System.Text.Json. No converter or configuration is required. See
[Unions and closed type hierarchies](Unions.md).

### JsonSerializerSettings.InferClosedTypePolymorphism

Writes and reads a short `$type` discriminator for C# `closed` type hierarchies, using the
descendants the compiler recorded, without needing `TypeNameHandling` or an `ISerializationBinder`.
See [Unions and closed type hierarchies](Unions.md).

### DateOnly and TimeOnly support

Serialized and deserialized as ISO strings with no converter required, on net6.0 and above. See [Dates in JSON](DatesInJSON.md).


## Migrating from Json.net


### Nuget

 * Remove [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json)
 * Add [Argon](https://www.nuget.org/packages/Argon)


### Namespace

 * Remove `using Newtonsoft.Json*`
 * Add `using Argon`


### XML

If using the Xml serialization features of Json.net:

 * Add [Argon.Xml](https://www.nuget.org/packages/Argon.Xml) nuget.
 * Add `using Argon.Xml`
 * Add `XmlNodeConverter` to the `JsonSerializerSettings.Converters`.


### Argon.DataSets

If using the DataSet serialization features of Json.net:

 * Add the [Argon.DataSets](https://www.nuget.org/packages/Argon.DataSets) nuget.
 * Call `JsonSerializerSettings.AddDataSetConverters()`.


### Argon.JsonPath

If using the JsonPath serialization features of Json.net:

 * Add the [Argon.JsonPath](https://www.nuget.org/packages/Argon.JsonPath) nuget.
