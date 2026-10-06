# Unions and closed type hierarchies

C# 15 adds `union` types and `closed` type hierarchies. Argon supports both, and produces the same
JSON that System.Text.Json does.

Both are language features rather than runtime features, so they can be used on any target
framework given a C# 15 compiler and the marker types the compiler references. On .NET 11 those
types ship in the framework; below it they come from a polyfill such as
[Polyfill](https://github.com/SimonCropp/Polyfill).


## Unions

A union value is written as its active case, with no wrapper and no type discriminator:

```cs
public union IntOrString(int, string);

JsonConvert.SerializeObject(new IntOrString(42));   // 42
JsonConvert.SerializeObject(new IntOrString("hi")); // "hi"
```

No converter or configuration is required. A union holding no value writes `null`.

On deserialization the case is chosen from the shape of the JSON value: a number picks the numeric
case, a string the string case, an array the collection case, and so on.

When several cases share a shape, which in practice means several cases that serialize as JSON
objects, the case is chosen by comparing the payload's property names against each candidate's
properties:

```cs
public union UnionPet(Cat, Dog);

public record Cat(string Name, bool Indoor);
public record Dog(string Name, bool GoodBoy);

// matches Cat, because Indoor is a Cat property
JsonConvert.DeserializeObject<UnionPet>("""{"Name":"Tibbles","Indoor":true}""");
```

Property naming strategies are respected, so the comparison uses the resolved names. If a payload
matches no case, or matches two equally well, a `JsonSerializationException` is thrown naming the
union and the candidate cases.

A case whose type is read by a `JsonConverter` is treated as matching any shape, since the converter
decides its own JSON. The same applies to an `object` or `JToken` case. An enum case read by
`StringEnumConverter` matches both strings and numbers. If that leaves more than one case matching
a payload, a `JsonSerializationException` is thrown.

`NaN`, `Infinity` and `-Infinity` are written as strings, so those three strings select a `float`
or `double` case. No other string is read as a number.

A case can hold `null` only if it is declared nullable. Given `union IntOrString(int, string)` in a
nullable enabled context, neither case accepts `null`, so JSON `null` reads back as the default
union. Declaring the case as `string?` makes it the case that takes a null payload.


### Serializer settings

The global `TypeNameHandling` setting can tell apart cases that share a shape, using the existing
`$type` and `$values` metadata. `TypeNameHandling.Auto` writes the type of the case, since the
declared type is the union. As with any use of `TypeNameHandling`, validate incoming type names
with a custom `ISerializationBinder` when reading external JSON.

`PreserveReferencesHandling` works with union cases: the `$id` and `$ref` metadata is not counted
when comparing property names, and a `$ref` selects the case from the type of the object it refers
to.

The payload is buffered before a case is chosen, and by default floating point numbers are read as
`double`. That loses precision for a `decimal` case, or for `decimal` members inside a case. Set
`FloatParseHandling.Decimal` when deserializing those unions.


## Closed type hierarchies

A `closed` type records its permitted descendants at compile time, and is always implicitly
abstract. Argon can use that recorded list to write and read a type discriminator, without
`TypeNameHandling` and without an `Argon.ISerializationBinder`:

```cs
public closed record class PaymentEvent(string PaymentId);

public sealed record class PaymentAuthorized(string PaymentId, decimal Amount) : PaymentEvent(PaymentId);

var settings = new JsonSerializerSettings
{
    InferClosedTypePolymorphism = true
};
```

A value whose declared type is the closed base is written with a `$type` property holding the simple
name of its runtime type:

```json
{"$type":"PaymentAuthorized","Amount":42.5,"PaymentId":"p-123"}
```

This is off by default. See
[InferClosedTypePolymorphism](SerializationSettings.md#inferclosedtypepolymorphism) for the details,
including how it differs from `TypeNameHandling` and what it does not cover.
