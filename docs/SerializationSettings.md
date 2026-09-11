# Serialization Settings

JsonSerializer has a number of properties on it to customize how it serializes JSON. These can also be used with the methods on JsonConvert via the `Argon.JsonSerializerSettings` overloads.


## DateFormatHandling

`Argon.DateFormatHandling` controls how dates are serialized.

<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>IsoDateFormat</para></entry>
    <entry><para>By default Json.NET writes dates in the ISO 8601 format, e.g. `"2012-03-21T05:40Z"`.</entry>
  </row>
  <row>
    <entry><para>MicrosoftDateFormat</para></entry>
    <entry><para>Dates are written in the Microsoft JSON format, e.g. `"\/Date(1198908717056)\/"`.</entry>
  </row>
</table>


## MissingMemberHandling

`Argon.MissingMemberHandling` controls how missing members, e.g. JSON contains a property that isn't a member on the object, are handled during deserialization.

<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>Ignore</para></entry>
    <entry><para>By default Json.NET ignores JSON if there is no field or property for its value to be set to during deserialization.</entry>
  </row>
  <row>
    <entry><para>Error</para></entry>
    <entry><para>Json.NET errors when there is a missing member during deserialization.</entry>
  </row>
</table>


## ReferenceLoopHandling

`Argon.ReferenceLoopHandling` controls how circular referencing objects, e.g. a Person object referencing itself via a Manager property, are serialized.

The `System.Object.Equals(System.Object)` method is used to test whether an object is in a circular reference. By default `Object.Equals(Object)` will test whether the references are equal for reference types and private and public values are equal for value types. Classes and structs can override this method.

<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>Error</para></entry>
    <entry><para>By default Json.NET will error if a reference loop is encountered (otherwise the serializer will get into an infinite loop).</entry>
  </row>
  <row>
    <entry><para>Ignore</para></entry>
    <entry><para>Json.NET will ignore objects in reference loops and not serialize them. The first time an object is encountered it will be serialized as usual but if the object is encountered as a child object of itself the serializer will skip serializing it.</entry>
  </row>
  <row>
    <entry><para>Serialize</para></entry>
    <entry><para>This option forces Json.NET to serialize objects in reference loops. This is useful if objects are nested but not indefinitely.</entry>
  </row>
</table>

ReferenceLoopHandling can be used as an argument when calling the serializer, it can be set on an object's properties or a collection's items using `Argon.JsonContainerAttribute.ItemReferenceLoopHandling`, customized on a property with `Argon.JsonPropertyAttribute.ReferenceLoopHandling` or a property's object properties or collection items using `Argon.JsonPropertyAttribute.ItemReferenceLoopHandling`.


## NullValueHandling

`Argon.NullValueHandling` controls how null values on .NET objects are handled during serialization and how null values in JSON are handled during deserialization.

<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>Include</para></entry>
    <entry><para>By default Json.NET writes null values to JSON when serializing and sets null values to fields/properties when deserializing.</entry>
  </row>
  <row>
    <entry><para>Ignore</para></entry>
    <entry><para>Json.NET will skip writing JSON properties if the .NET value is null when serializing and will skip setting fields/properties if the JSON property is null when deserializing.</entry>
  </row>
</table>

NullValueHandling can also be customized on individual properties with JsonPropertyAttribute.


## DefaultValueHandling

`Argon.DefaultValueHandling` controls how Json.NET uses default values set using the .NET `System.ComponentModel.DefaultValueAttribute` when serializing and deserializing.

<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>Include</para></entry>
    <entry><para>By default Json.NET will write a field/property value to JSON when serializing if the value is the same as the field/property's default value.
    The Json.NET deserializer will continue setting a field/property if the JSON value is the same as the default value.</entry>
  </row>
  <row>
    <entry><para>Ignore</para></entry>
    <entry><para>Json.NET will skip writing a field/property value to JSON if the value is the same as the field/property's default value, or the custom
    value specified in `System.ComponentModel.DefaultValueAttribute` if the attribute is present. The Json.NET deserializer
    will skip setting a .NET object's field/property if the JSON value is the same as the default value.</entry>
  </row>
</table>

DefaultValueHandling can also be customized on individual properties with JsonPropertyAttribute.


## ObjectCreationHandling

`Argon.ObjectCreationHandling` controls how objects are created and deserialized to during deserialization.

<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>Auto</para></entry>
    <entry><para>By default Json.NET will attempt to set JSON values onto existing objects and add JSON values to existing collections during deserialization. </para></entry>
  </row>
  <row>
    <entry><para>Reuse</para></entry>
    <entry><para>Same behaviour as auto.</entry>
  </row>
  <row>
    <entry><para>Replace</para></entry>
    <entry><para>Json.NET will always recreate objects and collections before setting values to them during deserialization.</entry>
  </row>
</table>
<para>ObjectCreationHandling can also be customized on individual properties with JsonPropertyAttribute.


## TypeNameHandling


`Argon.TypeNameHandling` should be used with caution when your application deserializes JSON from an external source.

Incoming types should be validated with a custom `Argon.ISerializationBinder` when deserializing with a value other than `TypeNameHandling.None`.

`Argon.TypeNameHandling` controls whether Json.NET includes .NET type names during serialization with a `$type` property and reads .NET type names from that property to determine what type to create during deserialization.

Metadata properties like `$type` must be located at the beginning of a JSON object to be successfully detected during deserialization. If you can't control the order of properties in your JSON object then `Argon.MetadataPropertyHandling` can be used to remove this restriction.

The value of the `$type` property can be customized and validated by creating your own `Argon.ISerializationBinder`.

<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>None</para></entry>
    <entry><para>By default Json.NET does not read or write type names during deserialization.</entry>
  </row>
  <row>
    <entry><para>Objects</para></entry>
    <entry><para>Json.NET will write and use type names for objects but not collections.</entry>
  </row>
  <row>
    <entry><para>Arrays</para></entry>
    <entry><para>Json.NET will write and use type names for collections but not objects.</entry>
  </row>
  <row>
    <entry><para>Auto</para></entry>
    <entry><para>Json.NET will check whether an object/collection matches its declared property and writes the type name if they do not match, e.g. a property with a type of Mammal has a derived instance of Dog assigned. Auto will ensure that type information isn't lost when serializing/deserializing automatically without having to write type names for every object.</entry>
  </row>
  <row>
    <entry><para>All</para></entry>
    <entry><para>Json.NET will write and use type names for objects and collections.</entry>
  </row>
</table>

TypeNameHandling can be used as an argument when calling the serializer, it can be set on an object's properties or a collection's items using `Argon.JsonContainerAttribute.ItemTypeNameHandling`, customized on a property with `Argon.JsonPropertyAttribute.TypeNameHandling` or a property's object properties or collection items using `Argon.JsonPropertyAttribute.ItemTypeNameHandling`.


## InferClosedTypePolymorphism

A C# 15 `closed` type records its permitted descendants at compile time. `InferClosedTypePolymorphism`
uses that recorded list to write and read a type discriminator, so a closed hierarchy round trips
without `TypeNameHandling` and without an `Argon.ISerializationBinder`. It is off by default.

```cs
public closed record class PaymentEvent(string PaymentId);

public sealed record class PaymentAuthorized(string PaymentId, decimal Amount) : PaymentEvent(PaymentId);

var settings = new JsonSerializerSettings
{
    InferClosedTypePolymorphism = true
};
```

A value whose declared type is the closed base is written with a `$type` property holding the simple
name of its runtime type, which is the same shape System.Text.Json produces for its own
`InferClosedTypePolymorphism` option:

```json
{"$type":"PaymentAuthorized","Amount":42.5,"PaymentId":"p-123"}
```

Unlike `TypeNameHandling`, the set of types an incoming discriminator can name is fixed by the
compiler. A payload can only ever select one of the hierarchy's declared descendants, so there is no
need for an `Argon.ISerializationBinder` to validate type names, and the warning above about
deserializing JSON from an external source does not apply.

Points worth knowing:

 * Only terminal descendants carry a discriminator. A closed type nested inside a closed hierarchy is
   expanded through, and a branch that is not itself closed is where inference stops. Serializing a
   type below that boundary throws rather than emitting JSON that cannot be read back.
 * A discriminator is only written where there is a declared type to compare against: a property, a
   collection item, a dictionary value, or a root serialized with an explicit type. As with
   `TypeNameHandling.Auto`, a root passed without a type carries no discriminator, so use
   `JsonConvert.SerializeObject(value, typeof(PaymentEvent), settings)`.
 * Discriminators are simple type names, so two descendants of the same hierarchy cannot share a name
   even when they are in different namespaces. Attempting it throws.
 * Where both this setting and `TypeNameHandling` apply to the same value, the inferred simple name
   wins. Types that are not part of a closed hierarchy are unaffected and keep using
   `TypeNameHandling` and the binder.
 * The `$type` property has to be the first property of the object, which is where Argon writes it.
   `Argon.MetadataPropertyHandling.ReadAhead` lifts that restriction for payloads from other
   producers, and `Argon.MetadataPropertyHandling.Ignore` disables the feature on read.

See [Unions and closed type hierarchies](Unions.md) for union support, which is unrelated and needs
no configuration.


## TypeNameAssemblyFormat

`System.Runtime.Serialization.Formatters.FormatterAssemblyStyle` controls how type names are written during serialization.

<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>Simple</para></entry>
    <entry><para>By default Json.NET writes the partial assembly name with the type, e.g. System.Data.DataSet, System.Data. Note that Silverlight and Windows Phone are not able to use this format.</entry>
  </row>
  <row>
    <entry><para>Full</para></entry>
    <entry><para>Json.NET will write the full assembly name, including version number, culture and public key token.</entry>
  </row>
</table>
<para>Read more about the valid values at `System.Runtime.Serialization.Formatters.FormatterAssemblyStyle`.


## SerializationBinder

The `Argon.ISerializationBinder` is used to resolve .NET types to type names during serialization and type names to .NET types during deserialization.

If TypeNameHandling is enabled then it is strongly recommended that a custom `Argon.ISerializationBinder` is used to validate incoming type names for security reasons.


## MetadataPropertyHandling

`Argon.MetadataPropertyHandling` controls how metadata properties like `$type` and `$id` are read during deserialization.

For performance reasons by default the JsonSerializer assumes that any metadata properties are located at the beginning of a JSON object. If you are unable to guarantee the order of properties in JSON you are deserializing then `MetadataPropertyHandling.ReadAhead` removes this restriction at the cost of some performance.


<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>Default</para></entry>
    <entry><para>By default Json.NET will only read metadata properties that are at the beginning of a JSON object.</entry>
  </row>
  <row>
    <entry><para>ReadAhead</para></entry>
    <entry><para>Json.NET will look for metadata properties located anywhere in a JSON object.</entry>
  </row>
  <row>
    <entry><para>Ignore</para></entry>
    <entry><para>Json.NET will ignore metadata properties.</entry>
  </row>
</table>


## ConstructorHandling

`Argon.ConstructorHandling` controls how constructors are used when initializing objects during deserialization.

<table>
  <tableHeader>
    <row>
      <entry><para>Member</para></entry>
      <entry><para>Description</para></entry>
    </row>
  </tableHeader>
  <row>
    <entry><para>Default</para></entry>
    <entry><para>By default Json.NET will first look for a constructor marked with the JsonConstructorAttribute, then look for a public default constructor
    (a constructor that doesn't take any arguments), then check if the class has a single public constructor with arguments and finally check for a
    non-public default constructor. If the class has multiple public constructors with arguments an error will be thrown. This can be fixed by
    marking one of the constructors with the JsonConstructorAttribute.</entry>
  </row>
  <row>
    <entry><para>AllowNonPublicDefaultConstructor</para></entry>
    <entry><para>Json.NET will use a classes private default constructor before constructors with arguments if available. </para></entry>
  </row>
</table>


## Converters

This is the collection of JsonConverters that will be used during serialization and deserialization.

A `Argon.JsonConverter` allows JSON to be manually written during serialization and read during deserialization. This is useful for particularly complex JSON structures or for when you want to change how a type is serialized.

When a JsonConverter has been added to a JsonSerializer it will be checked for every value that is being serialized/deserialized using its CanConvert to see if it should be used. If CanConvert returns true then the JsonConverter will be used to read or write the JSON for that value. Note that while a JsonConverter gives you complete control over that values JSON, many Json.NET serialization features are no longer available like type name and reference handling.

JsonConverters can be used as an argument when calling the serializer, it can be set on an object or property using `Argon.JsonConverterAttribute`, it be set on an object's properties or a collection's items using `Argon.JsonContainerAttribute.ItemConverterType`, or a property's object properties or collection items using`Argon.JsonPropertyAttribute.ItemConverterType`.

To create your own custom converter inherit from the JsonConverter class. Read more about the built-in JsonConverters below:

 * DatesInJSON
 * ConvertingJSONandXML
 * CustomCreationConverter
 * `Argon.StringEnumConverter`


## ContractResolver

Internally for every .NET type the JsonSerializer will create a contract of how the type should be serialized and deserialized, based on type metadata and attributes applied to the class. Specifying a custom `Argon.IContractResolver` allows the creation of contracts to be customized.

See also: [ContractResolver]


## TraceWriter

The Json.NET serializer supports logging and debugging using the `Argon.ITraceWriter` interface. By assigning a trace writer you can debug what happens inside the Json.NET serializer when serializing and deserializing JSON.

Read more about TraceWriters here: [SerializationTracing]


## Error

The `Argon.JsonSerializer.Error` event can catch errors during serialization and either handle the event and continue with serialization or let the error bubble up and be thrown to the application.

Read more about error handling here: [SerializationErrorHandling]


## Related Topics

 * [SerializationGuide]
 * [SerializationAttributes]
 * [DatesInJSON]
 * `Argon.JsonSerializer`
 * `Argon.JsonSerializerSettings`
 * `Argon.JsonConverter`
 * `System.Runtime.Serialization.SerializationBinder`
