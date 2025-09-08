// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#if !NET6_0_OR_GREATER
using System.Web.Script.Serialization;
using System.Drawing;
#endif
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Net.Mail;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using TestObjects;
using Formatting = Argon.Formatting;
using JsonConstructor = Argon.JsonConstructorAttribute;
// ReSharper disable UnusedVariable
// ReSharper disable RedundantAssignment

public class JsonSerializerTest : TestFixtureBase
{
    [Fact]
    public void ListSourceSerialize()
    {
        var c = new ListSourceTest
        {
            strprop = "test"
        };
        var json = JsonConvert.SerializeObject(c);

        Assert.Equal("""{"strprop":"test"}""", json);

        var c2 = JsonConvert.DeserializeObject<ListSourceTest>(json);

        Assert.Equal("test", c2.strprop);
    }

    public struct ImmutableStruct(string value)
    {
        public string Value { get; } = value;
        public int Value2 { get; set; } = 0;
    }

    [Fact]
    public void DeserializeImmutableStruct()
    {
        var result = JsonConvert.DeserializeObject<ImmutableStruct>("{ \"Value\": \"working\", \"Value2\": 2 }");

        Assert.Equal("working", result.Value);
        Assert.Equal(2, result.Value2);
    }

    public struct AlmostImmutableStruct(string value, int value2)
    {
        public string Value { get; } = value;
        public int Value2 { get; set; } = value2;
    }

    [Fact]
    public void DeserializeAlmostImmutableStruct()
    {
        var result = JsonConvert.DeserializeObject<AlmostImmutableStruct>("{ \"Value\": \"working\", \"Value2\": 2 }");

        Assert.Null(result.Value);
        Assert.Equal(2, result.Value2);
    }

    public class ErroringClass
    {
        public DateTime Tags { get; set; }
    }

    [Fact]
    public void DontCloseInputOnDeserializeError()
    {
        using var stream = File.OpenRead("large.json");
        try
        {
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                reader.SupportMultipleContent = true;
                reader.CloseInput = false;

                // read into array
                reader.Read();

                var ser = new JsonSerializer
                {
                    CheckAdditionalContent = false
                };

                ser.Deserialize<IList<ErroringClass>>(reader);
            }

            Assert.Fail();
        }
        catch (Exception)
        {
            Assert.True(stream.Position > 0);

            stream.Seek(0, SeekOrigin.Begin);

            Assert.Equal(0, stream.Position);
        }
    }

    [Fact]
    public void SerializeInterfaceWithHiddenProperties()
    {
        var mySubclass = MyFactory.InstantiateSubclass();
        var myMainClass = MyFactory.InstantiateManiClass();

        //Class implementing interface with hidden members - flat object.
        var strJsonSubclass = JsonConvert.SerializeObject(mySubclass, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "ID": 123,
              "Name": "ABC",
              "P1": true,
              "P2": 44
            }
            """,
            strJsonSubclass);

        //Class implementing interface with hidden members - member of another class.
        var strJsonMainClass = JsonConvert.SerializeObject(myMainClass, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "ID": 567,
              "Name": "XYZ",
              "Subclass": {
                "ID": 123,
                "Name": "ABC",
                "P1": true,
                "P2": 44
              }
            }
            """,
            strJsonMainClass);
    }

    public class GenericIEnumerableWithImplicitConversion
    {
        public IEnumerable<ClassWithImplicitOperator> Enumerable { get; set; }
    }

    [Fact]
    public void DeserializeGenericIEnumerableWithImplicitConversion()
    {
        var deserialized = """
                           {
                             "Enumerable": [ "abc", "def" ]
                           }
                           """;
        var enumerableClass = JsonConvert.DeserializeObject<GenericIEnumerableWithImplicitConversion>(deserialized);
        var enumerableObject = enumerableClass.Enumerable.ToArray();
        Assert.Equal(2, enumerableObject.Length);
        Assert.Equal("abc", enumerableObject[0].Value);
        Assert.Equal("def", enumerableObject[1].Value);
    }

    public class Foo64
    {
        public string Blah { get; set; }
    }

    [Fact]
    public void LargeIntegerAsString()
    {
        var largeBrokenNumber = JsonConvert.DeserializeObject<Foo64>("{\"Blah\": 43443333222211111117 }");
        Assert.Equal("43443333222211111117", largeBrokenNumber.Blah);

        var largeOddWorkingNumber = JsonConvert.DeserializeObject<Foo64>("{\"Blah\": 53443333222211111117 }");
        Assert.Equal("53443333222211111117", largeOddWorkingNumber.Blah);
    }

    [Fact]
    public void DeserializeBoolean_Null()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IList<bool>>("[null]"));
        Assert.Equal("Error converting value {null} to type 'System.Boolean'. Path '[0]', line 1, position 5.", exception.Message);
    }

    [Fact]
    public void DeserializeBoolean_DateTime()
    {
        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<IList<bool>>("['2000-12-20T10:55:55Z']"));
        Assert.Equal("Could not convert string to boolean: 2000-12-20T10:55:55Z. Path '[0]', line 1, position 23.", exception.Message);
    }

    [Fact]
    public void DeserializeBoolean_BadString()
    {
        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<IList<bool>>("['pie']"));
        Assert.Equal("Could not convert string to boolean: pie. Path '[0]', line 1, position 6.", exception.Message);
    }

    [Fact]
    public void DeserializeBoolean_EmptyString()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IList<bool>>("['']"));
        Assert.Equal("Error converting value {null} to type 'System.Boolean'. Path '[0]', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void DeserializeBooleans()
    {
        var l = JsonConvert.DeserializeObject<IList<bool>>(
            """
            [
              1,
              0,
              1.1,
              0.0,
              0.000000000001,
              9999999999,
              -9999999999,
              9999999999999999999999999999999999999999999999999999999999999999999999,
              -9999999999999999999999999999999999999999999999999999999999999999999999,
              'true',
              'TRUE',
              'false',
              'FALSE'
            ]
            """);

        var i = 0;
        Assert.True(l[i++]);
        Assert.False( l[i++]);
        Assert.True(l[i++]);
        Assert.False( l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.False( l[i++]);
        Assert.False( l[i++]);
    }

    [Fact]
    public void DeserializeNullableBooleans()
    {
        var l = JsonConvert.DeserializeObject<IList<bool?>>(
            """
            [
              1,
              0,
              1.1,
              0.0,
              0.000000000001,
              9999999999,
              -9999999999,
              9999999999999999999999999999999999999999999999999999999999999999999999,
              -9999999999999999999999999999999999999999999999999999999999999999999999,
              'true',
              'TRUE',
              'false',
              'FALSE',
              '',
              null
            ]
            """);

        var i = 0;
        Assert.True(l[i++]);
        Assert.False(l[i++]);
        Assert.True(l[i++]);
        Assert.False(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.True(l[i++]);
        Assert.False(l[i++]);
        Assert.False(l[i++]);
        Assert.Null(l[i++]);
        Assert.Null(l[i++]);
    }

    [Fact]
    public void CaseInsensitiveRequiredPropertyConstructorCreation()
    {
        var foo1 = new FooRequired(["A", "B", "C"]);
        var json = JsonConvert.SerializeObject(foo1);

        XUnitAssert.AreEqualNormalized("""{"Bars":["A","B","C"]}""", json);

        var foo2 = JsonConvert.DeserializeObject<FooRequired>(json);
        Assert.Equal(foo1.Bars.Count, foo2.Bars.Count);
        Assert.Equal(foo1.Bars[0], foo2.Bars[0]);
        Assert.Equal(foo1.Bars[1], foo2.Bars[1]);
        Assert.Equal(foo1.Bars[2], foo2.Bars[2]);
    }

    [Fact]
    public void CoercedEmptyStringWithRequired()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Binding>("{requiredProperty:''}"));
        Assert.Equal("Required property 'RequiredProperty' expects a value but got null. Path '', line 1, position 21.", exception.Message);
    }

    [Fact]
    public void CoercedEmptyStringWithRequired_DisallowNull()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Binding_DisallowNull>("{requiredProperty:''}"));
        Assert.Equal("Required property 'RequiredProperty' expects a non-null value. Path '', line 1, position 21.", exception.Message);
    }

    [Fact]
    public void DisallowNull_NoValue()
    {
        var o = JsonConvert.DeserializeObject<Binding_DisallowNull>("{}");
        Assert.Null(o.RequiredProperty);
    }

    [Fact]
    public void CoercedEmptyStringWithRequiredConstructor()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<FooRequired>("{Bars:''}"));
        Assert.Equal("Required property 'Bars' expects a value but got null. Path '', line 1, position 9.", exception.Message);
    }

    [Fact]
    public void NoErrorWhenValueDoesNotMatchIgnoredProperty()
    {
        var p = JsonConvert.DeserializeObject<IgnoredProperty>("{'StringProp1':[1,2,3],'StringProp2':{}}");
        Assert.Null(p.StringProp1);
        Assert.Null(p.StringProp2);
    }

    [Fact]
    public void Serialize_Required_DisallowedNull()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(new Binding_DisallowNull()));
        Assert.Equal("Cannot write a null value for property 'RequiredProperty'. Property requires a non-null value. Path ''.", exception.Message);
    }

    [Fact]
    public void Serialize_Required_DisallowedNull_NullValueHandlingIgnore()
    {
        var json = JsonConvert.SerializeObject(
            new Binding_DisallowNull(),
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        Assert.Equal("{}", json);
    }

    [Fact]
    public void Serialize_ItemRequired_DisallowedNull()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(new DictionaryWithNoNull()));
        Assert.Equal("Cannot write a null value for property 'Name'. Property requires a non-null value. Path ''.", exception.Message);
    }

    [Fact]
    public void DictionaryKeyContractResolverTest()
    {
        var person = new
        {
            Name = "James",
            Age = 1,
            RoleNames = new Dictionary<string, bool>
            {
                {
                    "IsAdmin", true
                },
                {
                    "IsModerator", false
                }
            }
        };

        var json = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
        {
            ContractResolver = new DictionaryKeyContractResolver()
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "NAME": "James",
              "AGE": 1,
              "ROLENAMES": {
                "IsAdmin": true,
                "IsModerator": false
              }
            }
            """,
            json);
    }

    [Fact]
    public void IncompleteContainers()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IList<object>>("[1,"));
        Assert.Equal("Unexpected end when deserializing array. Path '[0]', line 1, position 3.", exception.Message);

        var exception1 = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IList<int>>("[1,"));
        Assert.Equal("Unexpected end when deserializing array. Path '[0]', line 1, position 3.", exception1.Message);

        var exception2 = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IList<int>>("[1"));
        Assert.Equal("Unexpected end when deserializing array. Path '[0]', line 1, position 2.", exception2.Message);

        var exception3 = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IDictionary<string, int>>("{'key':1,"));
        Assert.Equal("Unexpected end when deserializing object. Path 'key', line 1, position 9.", exception3.Message);

        var exception4 = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IDictionary<string, int>>("{'key':1"));
        Assert.Equal("Unexpected end when deserializing object. Path 'key', line 1, position 8.", exception4.Message);

        var exception5 = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IncompleteTestClass>("{'key':1,"));
        Assert.Equal("Unexpected end when deserializing object. Path 'key', line 1, position 9.", exception5.Message);

        var exception6 = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IncompleteTestClass>("{'key':1"));
        Assert.Equal("Unexpected end when deserializing object. Path 'key', line 1, position 8.", exception6.Message);
    }

    [Fact]
    public void DeserializeEnumsByName()
    {
        var e1 = JsonConvert.DeserializeObject<EnumA>("'ValueA'");
        Assert.Equal(EnumA.ValueA, e1);

        var e2 = JsonConvert.DeserializeObject<EnumA>("'value_a'", new StringEnumConverter());
        Assert.Equal(EnumA.ValueA, e2);
    }

    [Fact]
    public void RequiredPropertyTest()
    {
        var c1 = new RequiredPropertyTestClass();

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(c1));
        Assert.Equal("Cannot write a null value for property 'Name'. Property requires a value. Path ''.", exception.Message);

        var c2 = new RequiredPropertyTestClass
        {
            Name = "Name!"
        };

        var json = JsonConvert.SerializeObject(c2);

        Assert.Equal("""{"Name":"Name!"}""", json);

        var exception1 = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<RequiredPropertyTestClass>("{}"));
        Assert.Equal("Required property 'Name' not found in JSON. Path '', line 1, position 2.", exception1.Message);

        var exception2 = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<RequiredPropertyTestClass>("""{"Name":null}"""));
        Assert.Equal("Required property 'Name' expects a value but got null. Path '', line 1, position 13.", exception2.Message);

        var c3 = JsonConvert.DeserializeObject<RequiredPropertyTestClass>("""{"Name":"Name!"}""");

        Assert.Equal("Name!", c3.Name);
    }

    [Fact]
    public void RequiredPropertyConstructorTest()
    {
        var c1 = new RequiredPropertyConstructorTestClass(null);

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(c1));
        Assert.Equal("Cannot write a null value for property 'Name'. Property requires a value. Path ''.", exception.Message);

        var c2 = new RequiredPropertyConstructorTestClass("Name!");

        var json = JsonConvert.SerializeObject(c2);

        Assert.Equal("""{"Name":"Name!"}""", json);

        var exception1 = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<RequiredPropertyConstructorTestClass>("{}"));
        Assert.Equal("Required property 'Name' not found in JSON. Path '', line 1, position 2.", exception1.Message);

        var c3 = JsonConvert.DeserializeObject<RequiredPropertyConstructorTestClass>("""{"Name":"Name!"}""");

        Assert.Equal("Name!", c3.Name);
    }

    [Fact]
    public void NeverResolveIgnoredPropertyTypes()
    {
        var v = new Version(1, 2, 3, 4);

        var c1 = new IgnoredPropertiesTestClass
        {
            IgnoredProperty = v,
            IgnoredList = [v],
            IgnoredDictionary = new()
            {
                {
                    "Value", v
                }
            },
            Name = "Name!"
        };

        var json = JsonConvert.SerializeObject(
            c1,
            Formatting.Indented,
            new JsonSerializerSettings
            {
                ContractResolver = new IgnoredPropertiesContractResolver()
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "Name!"
            }
            """,
            json);

        var deserializeJson =
            """
            {
              "IgnoredList": [
                {
                  "Major": 1,
                  "Minor": 2,
                  "Build": 3,
                  "Revision": 4,
                  "MajorRevision": 0,
                  "MinorRevision": 4
                }
              ],
              "IgnoredDictionary": {
                "Value": {
                  "Major": 1,
                  "Minor": 2,
                  "Build": 3,
                  "Revision": 4,
                  "MajorRevision": 0,
                  "MinorRevision": 4
                }
              },
              "Name": "Name!"
            }
            """;

        var c2 = JsonConvert.DeserializeObject<IgnoredPropertiesTestClass>(
            deserializeJson,
            new JsonSerializerSettings
            {
                ContractResolver = new IgnoredPropertiesContractResolver()
            });

        Assert.Equal("Name!", c2.Name);
    }

    [Fact]
    public void SerializeValueTuple()
    {
        var tuple = ValueTuple.Create(1, 2, "string");

        var json = JsonConvert.SerializeObject(tuple, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Item1": 1,
              "Item2": 2,
              "Item3": "string"
            }
            """,
            json);

        var t2 = JsonConvert.DeserializeObject<ValueTuple<int, int, string>>(json);

        Assert.Equal(1, t2.Item1);
        Assert.Equal(2, t2.Item2);
        Assert.Equal("string", t2.Item3);
    }

    [Fact]
    public void DeserializeStructWithConstructorAttribute()
    {
        var result = JsonConvert.DeserializeObject<ImmutableStructWithConstructorAttribute>("{ \"Value\": \"working\" }");

        Assert.Equal("working", result.Value);
    }

    [method: JsonConstructor]
    public struct ImmutableStructWithConstructorAttribute(string value)
    {
        public string Value { get; } = value;
    }

    [Fact]
    public void DeserializeNullToJTokenProperty()
    {
        var otc = JsonConvert.DeserializeObject<NullTestClass>(
            """
            {
                "Value1": null,
                "Value2": null,
                "Value3": null,
                "Value4": null,
                "Value5": null
            }
            """);
        Assert.Null(otc.Value1);
        Assert.Equal(JTokenType.Null, otc.Value2.Type);
        Assert.Equal(JTokenType.Raw, otc.Value3.Type);
        Assert.Equal(JTokenType.Null, otc.Value4.Type);
        Assert.Null(otc.Value5);
    }

    [Fact]
    public void ReadIntegerWithError()
    {
        var json = """
                   {
                       ParentId: 1,
                       ChildId: 333333333333333333333333333333333333333
                   }
                   """;

        var l = JsonConvert.DeserializeObject<Link>(
            json,
            new JsonSerializerSettings
            {
                DeserializeError = (_, _, _, _, _, markAsHandled) => markAsHandled()
            });

        Assert.Equal(0, l.ChildId);
    }

    [Fact]
    public void DeserializeObservableCollection()
    {
        var s = JsonConvert.DeserializeObject<ObservableCollection<string>>("['1','2']");
        Assert.Equal(2, s.Count);
        Assert.Equal("1", s[0]);
        Assert.Equal("2", s[1]);
    }

    [Fact]
    public void SerializeObservableCollection()
    {
        var c1 = new ObservableCollection<string>
        {
            "1",
            "2"
        };

        var output = JsonConvert.SerializeObject(c1);
        Assert.Equal("[\"1\",\"2\"]", output);

        var c2 = JsonConvert.DeserializeObject<ObservableCollection<string>>(output);
        Assert.Equal(2, c2.Count);
        Assert.Equal("1", c2[0]);
        Assert.Equal("2", c2[1]);
    }

    [Fact]
    public void DeserializeBoolAsStringInDictionary()
    {
        var d = JsonConvert.DeserializeObject<Dictionary<string, string>>("{\"Test1\":false}");
        Assert.Single(d);
        Assert.Equal("false", d["Test1"]);
    }

    [Fact]
    public void NewProperty()
    {
        Assert.Equal(
            """{"IsTransient":true}""",
            JsonConvert.SerializeObject(
                new ChildClass
            {
                IsTransient = true
            }));

        var childClass = JsonConvert.DeserializeObject<ChildClass>("""{"IsTransient":true}""");
        Assert.True(childClass.IsTransient);
    }

    [Fact]
    public void NewPropertyVirtual()
    {
        Assert.Equal(
            """{"IsTransient":true}""",
            JsonConvert.SerializeObject(
                new ChildClassVirtual
            {
                IsTransient = true
            }));

        var childClass = JsonConvert.DeserializeObject<ChildClassVirtual>("""{"IsTransient":true}""");
        Assert.True(childClass.IsTransient);
    }

    [Fact]
    public void CanSerializeWithBuiltInTypeAsGenericArgument()
    {
        var input = new ResponseWithNewGenericProperty<int>
        {
            Message = "Trying out integer as type parameter",
            Data = 25,
            Result = "This should be fine"
        };

        var json = JsonConvert.SerializeObject(input);
        var deserialized = JsonConvert.DeserializeObject<ResponseWithNewGenericProperty<int>>(json);

        Assert.Equal(input.Data, deserialized.Data);
        Assert.Equal(input.Message, deserialized.Message);
        Assert.Equal(input.Result, deserialized.Result);
    }

    [Fact]
    public void CanSerializeWithBuiltInTypeAsGenericArgumentVirtual()
    {
        var input = new ResponseWithNewGenericPropertyVirtual<int>
        {
            Message = "Trying out integer as type parameter",
            Data = 25,
            Result = "This should be fine"
        };

        var json = JsonConvert.SerializeObject(input);
        var deserialized = JsonConvert.DeserializeObject<ResponseWithNewGenericPropertyVirtual<int>>(json);

        Assert.Equal(input.Data, deserialized.Data);
        Assert.Equal(input.Message, deserialized.Message);
        Assert.Equal(input.Result, deserialized.Result);
    }

    [Fact]
    public void CanSerializeWithBuiltInTypeAsGenericArgumentOverride()
    {
        var input = new ResponseWithNewGenericPropertyOverride<int>
        {
            Message = "Trying out integer as type parameter",
            Data = 25,
            Result = "This should be fine"
        };

        var json = JsonConvert.SerializeObject(input);
        var deserialized = JsonConvert.DeserializeObject<ResponseWithNewGenericPropertyOverride<int>>(json);

        Assert.Equal(input.Data, deserialized.Data);
        Assert.Equal(input.Message, deserialized.Message);
        Assert.Equal(input.Result, deserialized.Result);
    }

    [Fact]
    public void CanSerializedWithGenericClosedTypeAsArgument()
    {
        var input = new ResponseWithNewGenericProperty<List<int>>
        {
            Message = "More complex case - generic list of int",
            Data = Enumerable.Range(50, 70).ToList(),
            Result = "This should be fine too"
        };

        var json = JsonConvert.SerializeObject(input);
        var deserialized = JsonConvert.DeserializeObject<ResponseWithNewGenericProperty<List<int>>>(json);

        Assert.Equal(input.Data, deserialized.Data);
        Assert.Equal(input.Message, deserialized.Message);
        Assert.Equal(input.Result, deserialized.Result);
    }

    [Fact]
    public void DeserializeVersionString()
    {
        var json = "['1.2.3.4']";
        var deserialized = JsonConvert.DeserializeObject<List<Version>>(json);

        Assert.Equal(1, deserialized[0].Major);
        Assert.Equal(2, deserialized[0].Minor);
        Assert.Equal(3, deserialized[0].Build);
        Assert.Equal(4, deserialized[0].Revision);
    }

    [Fact]
    public void DeserializeJObjectWithComments()
    {
        var json = """
                   /* Test */
                   {
                       /*Test*/"A":/* Test */true/* Test */,
                       /* Test */"B":/* Test */false/* Test */,
                       /* Test */"C":/* Test */[
                           /* Test */
                           1/* Test */
                       ]/* Test */
                   }
                   /* Test */
                   """;
        var o = (JObject) JsonConvert.DeserializeObject(json);
        Assert.Equal(3, o.Count);
        Assert.True((bool) o["A"]);
        Assert.False( (bool) o["B"]);
        Assert.Single(o["C"]);
        Assert.Equal(1, (int) o["C"][0]);

        Assert.True(JToken.DeepEquals(o, JObject.Parse(json)));

        json = "{/* Test */}";
        o = (JObject) JsonConvert.DeserializeObject(json);
        Assert.Empty(o);
        Assert.True(JToken.DeepEquals(o, JObject.Parse(json)));

        json = """{"A": true/* Test */}""";
        o = (JObject) JsonConvert.DeserializeObject(json);
        Assert.Single(o);
        Assert.True((bool) o["A"]);
        Assert.True(JToken.DeepEquals(o, JObject.Parse(json)));
    }

    [Fact]
    public void DeserializeCommentTestObjectWithComments()
    {
        var o = JsonConvert.DeserializeObject<CommentTestObject>("{/* Test */}");
        Assert.Null(o.A);

        o = JsonConvert.DeserializeObject<CommentTestObject>("""{"A": true/* Test */}""");
        Assert.True(o.A);
    }

    [Fact]
    public void JsonSerializerProperties()
    {
        var serializer = new JsonSerializer();

        Assert.Null(serializer.SerializationBinder);

        var customBinder = new DefaultSerializationBinder();
        serializer.SerializationBinder = customBinder;
        Assert.Equal(customBinder, serializer.SerializationBinder);

        serializer.CheckAdditionalContent = true;
        Assert.True(serializer.CheckAdditionalContent);

        serializer.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        Assert.Equal(ConstructorHandling.AllowNonPublicDefaultConstructor, serializer.ConstructorHandling);

        var resolver = new CamelCasePropertyNamesContractResolver();
        serializer.ContractResolver = resolver;
        Assert.Equal(resolver, serializer.ContractResolver);

        serializer.Converters.Add(new StringEnumConverter());
        Assert.Single(serializer.Converters);

        serializer.EqualityComparer = EqualityComparer<object>.Default;
        Assert.Equal(EqualityComparer<object>.Default, serializer.EqualityComparer);

        serializer.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
        Assert.Equal(DefaultValueHandling.IgnoreAndPopulate, serializer.DefaultValueHandling);

        serializer.FloatFormatHandling = FloatFormatHandling.Symbol;
        Assert.Equal(FloatFormatHandling.Symbol, serializer.FloatFormatHandling);

        serializer.FloatParseHandling = FloatParseHandling.Decimal;
        Assert.Equal(FloatParseHandling.Decimal, serializer.FloatParseHandling);

        serializer.Formatting = Formatting.Indented;
        Assert.Equal(Formatting.Indented, serializer.Formatting);

        serializer.MaxDepth = 9001;
        Assert.Equal(9001, serializer.MaxDepth);

        serializer.MissingMemberHandling = MissingMemberHandling.Error;
        Assert.Equal(MissingMemberHandling.Error, serializer.MissingMemberHandling);

        serializer.NullValueHandling = NullValueHandling.Ignore;
        Assert.Equal(NullValueHandling.Ignore, serializer.NullValueHandling);

        serializer.ObjectCreationHandling = ObjectCreationHandling.Replace;
        Assert.Equal(ObjectCreationHandling.Replace, serializer.ObjectCreationHandling);

        serializer.PreserveReferencesHandling = PreserveReferencesHandling.All;
        Assert.Equal(PreserveReferencesHandling.All, serializer.PreserveReferencesHandling);

        serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        Assert.Equal(ReferenceLoopHandling.Ignore, serializer.ReferenceLoopHandling);

        var referenceResolver = new IdReferenceResolver();
        serializer.ReferenceResolver = referenceResolver;
        Assert.Equal(referenceResolver, serializer.ReferenceResolver);

        serializer.EscapeHandling = EscapeHandling.EscapeNonAscii;
        Assert.Equal(EscapeHandling.EscapeNonAscii, serializer.EscapeHandling);

        serializer.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
        Assert.Equal(TypeNameAssemblyFormatHandling.Simple, serializer.TypeNameAssemblyFormatHandling);

        serializer.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full;
        Assert.Equal(TypeNameAssemblyFormatHandling.Full, serializer.TypeNameAssemblyFormatHandling);

        serializer.TypeNameHandling = TypeNameHandling.All;
        Assert.Equal(TypeNameHandling.All, serializer.TypeNameHandling);
    }

    [Fact]
    public void JsonSerializerSettingsProperties()
    {
        var settings = new JsonSerializerSettings();

        Assert.Null(settings.SerializationBinder);

        var customBinder = new DefaultSerializationBinder();
        settings.SerializationBinder = customBinder;
        Assert.Equal(customBinder, settings.SerializationBinder);

        settings.CheckAdditionalContent = true;
        Assert.True(settings.CheckAdditionalContent);

        settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        Assert.Equal(ConstructorHandling.AllowNonPublicDefaultConstructor, settings.ConstructorHandling);

        var resolver = new CamelCasePropertyNamesContractResolver();
        settings.ContractResolver = resolver;
        Assert.Equal(resolver, settings.ContractResolver);

        settings.Converters.Add(new StringEnumConverter());
        Assert.Single(settings.Converters);

        settings.EqualityComparer = EqualityComparer<object>.Default;
        Assert.Equal(EqualityComparer<object>.Default, settings.EqualityComparer);

        settings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
        Assert.Equal(DefaultValueHandling.IgnoreAndPopulate, settings.DefaultValueHandling);

        settings.FloatFormatHandling = FloatFormatHandling.Symbol;
        Assert.Equal(FloatFormatHandling.Symbol, settings.FloatFormatHandling);

        settings.FloatParseHandling = FloatParseHandling.Decimal;
        Assert.Equal(FloatParseHandling.Decimal, settings.FloatParseHandling);

        settings.Formatting = Formatting.Indented;
        Assert.Equal(Formatting.Indented, settings.Formatting);

        settings.MaxDepth = 9001;
        Assert.Equal(9001, settings.MaxDepth);

        settings.MissingMemberHandling = MissingMemberHandling.Error;
        Assert.Equal(MissingMemberHandling.Error, settings.MissingMemberHandling);

        settings.NullValueHandling = NullValueHandling.Ignore;
        Assert.Equal(NullValueHandling.Ignore, settings.NullValueHandling);

        settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
        Assert.Equal(ObjectCreationHandling.Replace, settings.ObjectCreationHandling);

        settings.PreserveReferencesHandling = PreserveReferencesHandling.All;
        Assert.Equal(PreserveReferencesHandling.All, settings.PreserveReferencesHandling);

        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        Assert.Equal(ReferenceLoopHandling.Ignore, settings.ReferenceLoopHandling);

        var referenceResolver = new IdReferenceResolver();

        settings.ReferenceResolverProvider = () => referenceResolver;
        Assert.Equal(referenceResolver, settings.ReferenceResolverProvider());

        settings.EscapeHandling = EscapeHandling.EscapeNonAscii;
        Assert.Equal(EscapeHandling.EscapeNonAscii, settings.EscapeHandling);

        settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
        Assert.Equal(TypeNameAssemblyFormatHandling.Simple, settings.TypeNameAssemblyFormatHandling);

        settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full;
        Assert.Equal(TypeNameAssemblyFormatHandling.Full, settings.TypeNameAssemblyFormatHandling);

        settings.TypeNameHandling = TypeNameHandling.All;
        Assert.Equal(TypeNameHandling.All, settings.TypeNameHandling);
    }

    [Fact]
    public void JsonSerializerProxyProperties()
    {
        var proxy = new JsonSerializerProxy(new JsonSerializerInternalReader(new()));

        Assert.Null(proxy.SerializationBinder);

        var customBinder = new DefaultSerializationBinder();

        proxy.SerializationBinder = customBinder;
        Assert.Equal(customBinder, proxy.SerializationBinder);

        proxy.CheckAdditionalContent = true;
        Assert.True(proxy.CheckAdditionalContent);

        proxy.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        Assert.Equal(ConstructorHandling.AllowNonPublicDefaultConstructor, proxy.ConstructorHandling);

        var resolver = new CamelCasePropertyNamesContractResolver();
        proxy.ContractResolver = resolver;
        Assert.Equal(resolver, proxy.ContractResolver);

        proxy.Converters.Add(new StringEnumConverter());
        Assert.Single(proxy.Converters);

        proxy.EqualityComparer = EqualityComparer<object>.Default;
        Assert.Equal(EqualityComparer<object>.Default, proxy.EqualityComparer);

        proxy.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
        Assert.Equal(DefaultValueHandling.IgnoreAndPopulate, proxy.DefaultValueHandling);

        proxy.FloatFormatHandling = FloatFormatHandling.Symbol;
        Assert.Equal(FloatFormatHandling.Symbol, proxy.FloatFormatHandling);

        proxy.FloatParseHandling = FloatParseHandling.Decimal;
        Assert.Equal(FloatParseHandling.Decimal, proxy.FloatParseHandling);

        proxy.Formatting = Formatting.Indented;
        Assert.Equal(Formatting.Indented, proxy.Formatting);

        proxy.MaxDepth = 9001;
        Assert.Equal(9001, proxy.MaxDepth);

        proxy.MissingMemberHandling = MissingMemberHandling.Error;
        Assert.Equal(MissingMemberHandling.Error, proxy.MissingMemberHandling);

        proxy.NullValueHandling = NullValueHandling.Ignore;
        Assert.Equal(NullValueHandling.Ignore, proxy.NullValueHandling);

        proxy.ObjectCreationHandling = ObjectCreationHandling.Replace;
        Assert.Equal(ObjectCreationHandling.Replace, proxy.ObjectCreationHandling);

        proxy.PreserveReferencesHandling = PreserveReferencesHandling.All;
        Assert.Equal(PreserveReferencesHandling.All, proxy.PreserveReferencesHandling);

        proxy.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        Assert.Equal(ReferenceLoopHandling.Ignore, proxy.ReferenceLoopHandling);

        var referenceResolver = new IdReferenceResolver();
        proxy.ReferenceResolver = referenceResolver;
        Assert.Equal(referenceResolver, proxy.ReferenceResolver);

        proxy.EscapeHandling = EscapeHandling.EscapeNonAscii;
        Assert.Equal(EscapeHandling.EscapeNonAscii, proxy.EscapeHandling);

        proxy.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
        Assert.Equal(TypeNameAssemblyFormatHandling.Simple, proxy.TypeNameAssemblyFormatHandling);

        proxy.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full;
        Assert.Equal(TypeNameAssemblyFormatHandling.Full, proxy.TypeNameAssemblyFormatHandling);

        proxy.TypeNameHandling = TypeNameHandling.All;
        Assert.Equal(TypeNameHandling.All, proxy.TypeNameHandling);
    }

    [Fact]
    public void DeserializeLargeFloat()
    {
        var o = JsonConvert.DeserializeObject("100000000000000000000000000000000000000.0");

        Assert.IsType<double>(o);

        Assert.True(MathUtils.ApproxEquals(1E+38, (double) o));
    }

    [Fact]
    public void SerializeDeserializeRegex()
    {
        var regex = new Regex("(hi)", RegexOptions.CultureInvariant);

        var json = JsonConvert.SerializeObject(regex, Formatting.Indented);

        var r2 = JsonConvert.DeserializeObject<Regex>(json);

        Assert.Equal("(hi)", r2.ToString());
        Assert.Equal(RegexOptions.CultureInvariant, r2.Options);
    }

    [Fact]
    public void SerializeDeserializeTimeZoneInfo()
    {
        var info = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        var json = JsonConvert.SerializeObject(info, Formatting.Indented);

        var info2 = JsonConvert.DeserializeObject<TimeZoneInfo>(json);

        Assert.Equal(info.Id, info2.Id);
    }

    [Fact]
    public void SerializeDeserializeEncoding()
    {
        var encoding = Encoding.UTF8;
        var json = JsonConvert.SerializeObject(encoding, Formatting.Indented);
        var encoding2 = JsonConvert.DeserializeObject<Encoding>(json);
        Assert.Equal(encoding.EncodingName, encoding2.EncodingName);
    }

    [Fact]
    public void EmbedJValueStringInNewJObject()
    {
        var v = new JValue((string) null);
        var o = JObject.FromObject(new
        {
            title = v
        });

        var oo = new JObject
        {
            {
                "title", v
            }
        };

        var output = o.ToString();

        Assert.Null(v.Value);
        Assert.Equal(JTokenType.String, v.Type);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "title": null
            }
            """,
            output);
    }

    // bug: the generic member (T) that hides the base member will not
    // be used when serializing and deserializing the object,
    // resulting in unexpected behavior during serialization and deserialization.

    [Fact]
    public void BaseClassSerializesAsExpected()
    {
        var original = new Foo1
        {
            foo = "value"
        };
        var json = JsonConvert.SerializeObject(original);
        var expectedJson = """{"foo":"value"}""";
        Assert.Equal(expectedJson, json); // passes
    }

    [Fact]
    public void BaseClassDeserializesAsExpected()
    {
        var json = """{"foo":"value"}""";
        var deserialized = JsonConvert.DeserializeObject<Foo1>(json);
        Assert.Equal("value", deserialized.foo); // passes
    }

    [Fact]
    public void DerivedClassHidingBasePropertySerializesAsExpected()
    {
        var original = new FooBar1
        {
            foo = new()
            {
                bar = "value"
            }
        };
        var json = JsonConvert.SerializeObject(original);
        var expectedJson = """{"foo":{"bar":"value"}}""";
        Assert.Equal(expectedJson, json); // passes
    }

    [Fact]
    public void DerivedClassHidingBasePropertyDeserializesAsExpected()
    {
        var json = """{"foo":{"bar":"value"}}""";
        var deserialized = JsonConvert.DeserializeObject<FooBar1>(json);
        Assert.NotNull(deserialized.foo); // passes
        Assert.Equal("value", deserialized.foo.bar); // passes
    }

    [Fact]
    public void DerivedGenericClassHidingBasePropertySerializesAsExpected()
    {
        var original = new Foo1<Bar1>
        {
            foo = new()
            {
                bar = "value"
            },
            foo2 = new()
            {
                bar = "value2"
            }
        };
        var json = JsonConvert.SerializeObject(original);
        var expectedJson = """{"foo":{"bar":"value"},"foo2":{"bar":"value2"}}""";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void DerivedGenericClassHidingBasePropertyDeserializesAsExpected()
    {
        var json = """{"foo":{"bar":"value"},"foo2":{"bar":"value2"}}""";
        var deserialized = JsonConvert.DeserializeObject<Foo1<Bar1>>(json);
        // passes (bug only occurs for generics that /hide/ another property)
        Assert.NotNull(deserialized.foo2);
        // also passes, with no issue
        Assert.Equal("value2", deserialized.foo2.bar);
        Assert.NotNull(deserialized.foo);
        Assert.Equal("value", deserialized.foo.bar);
    }

    [Fact]
    public void ConversionOperator()
    {
        // Creating a simple dictionary that has a non-string key
        var dictStore = new Dictionary<DictionaryKeyCast, int>();
        for (var i = 0; i < 800; i++)
        {
            dictStore.Add(new(i.ToString(InvariantCulture), i), i);
        }

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        var serializer = JsonSerializer.Create(settings);
        var ms = new MemoryStream();

        var streamWriter = new StreamWriter(ms);
        serializer.Serialize(streamWriter, dictStore);
        streamWriter.Flush();

        ms.Seek(0, SeekOrigin.Begin);

        var stopWatch = Stopwatch.StartNew();
        serializer.Deserialize(new StreamReader(ms), typeof(Dictionary<DictionaryKeyCast, int>));
        stopWatch.Stop();
    }

    [Fact]
    public void PersonTypedObjectDeserialization()
    {
        var store = new Store();

        var jsonText = JsonConvert.SerializeObject(store);

        var deserializedStore = (Store) JsonConvert.DeserializeObject(jsonText, typeof(Store));

        Assert.Equal(store.Established, deserializedStore.Established);
        Assert.Equal(store.product.Count, deserializedStore.product.Count);

        Console.WriteLine(jsonText);
    }

    [Fact]
    public void TypedObjectDeserialization()
    {
        var product = new Product
        {
            Name = "Apple",
            ExpiryDate = new(2008, 12, 28),
            Price = 3.99M,
            Sizes =
            [
                "Small",
                "Medium",
                "Large"
            ]
        };

        var output = JsonConvert.SerializeObject(product);
        //{
        //  "Name": "Apple",
        //  "ExpiryDate": "\/Date(1230375600000+1300)\/",
        //  "Price": 3.99,
        //  "Sizes": [
        //    "Small",
        //    "Medium",
        //    "Large"
        //  ]
        //}

        var deserializedProduct = (Product) JsonConvert.DeserializeObject(output, typeof(Product));

        Assert.Equal("Apple", deserializedProduct.Name);
        Assert.Equal(new(2008, 12, 28), deserializedProduct.ExpiryDate);
        Assert.Equal(3.99m, deserializedProduct.Price);
        Assert.Equal("Small", deserializedProduct.Sizes[0]);
        Assert.Equal("Medium", deserializedProduct.Sizes[1]);
        Assert.Equal("Large", deserializedProduct.Sizes[2]);
    }

    //[Fact]
    //public void Advanced()
    //{
    //  Product product = new Product();
    //  product.ExpiryDate = new DateTime(2008, 12, 28);

    //  JsonSerializer serializer = new JsonSerializer();
    //  serializer.Converters.Add(new JavaScriptDateTimeConverter());
    //  serializer.NullValueHandling = NullValueHandling.Ignore;

    //  using (StreamWriter sw = new StreamWriter(@"c:\json.txt"))
    //  using (JsonWriter writer = new JsonTextWriter(sw))
    //  {
    //    serializer.Serialize(writer, product);
    //    // {"ExpiryDate":new Date(1230375600000),"Price":0}
    //  }
    //}

    [Fact]
    public void JsonConvertSerializer()
    {
        var value = """{"Name":"Orange", "Price":3.99, "ExpiryDate":"01/24/2010 12:00:00"}""";

        var product = (Product)JsonConvert.DeserializeObject(value, typeof(Product));

        Assert.Equal("Orange", product.Name);
        Assert.Equal(new(2010, 1, 24, 12, 0, 0), product.ExpiryDate);
        Assert.Equal(3.99m, product.Price);
    }

    [Fact]
    public void TestMethodExecutorObject()
    {
        var executorObject = new MethodExecutorObject
        {
            serverClassName = "BanSubs",
            serverMethodParams =
                [
                "21321546",
                "101",
                "1236",
                "D:\\1.txt"
                ],
            clientGetResultFunction = "ClientBanSubsCB"
        };

        var output = JsonConvert.SerializeObject(executorObject);

        var executorObject2 = (MethodExecutorObject)JsonConvert.DeserializeObject(output, typeof(MethodExecutorObject));

        Assert.NotSame(executorObject, executorObject2);
        Assert.Equal("BanSubs", executorObject2.serverClassName);
        Assert.Equal(4, executorObject2.serverMethodParams.Length);
        Assert.Contains("101", executorObject2.serverMethodParams);
        Assert.Equal("ClientBanSubsCB", executorObject2.clientGetResultFunction);
    }

    [Fact]
    public void HashtableDeserialization()
    {
        var value = """{"Name":"Orange", "Price":3.99, "ExpiryDate":"01/24/2010 12:00:00"}""";

        var p = (Hashtable)JsonConvert.DeserializeObject(value, typeof(Hashtable));

        Assert.Equal("Orange", p["Name"].ToString());
    }

    [Fact]
    public void TypedHashtableDeserialization()
    {
        var value = """{"Name":"Orange", "Hash":{"ExpiryDate":"01/24/2010 12:00:00","UntypedArray":["01/24/2010 12:00:00"]}}""";

        var p = (TypedSubHashtable)JsonConvert.DeserializeObject(value, typeof(TypedSubHashtable));

        Assert.Equal("01/24/2010 12:00:00", p.Hash["ExpiryDate"].ToString());
        XUnitAssert.AreEqualNormalized(
            """
            [
              "01/24/2010 12:00:00"
            ]
            """,
            p.Hash["UntypedArray"].ToString());
    }

    [Fact]
    public void SerializeDeserializeGetOnlyProperty()
    {
        var value = JsonConvert.SerializeObject(new GetOnlyPropertyClass());

        var c = JsonConvert.DeserializeObject<GetOnlyPropertyClass>(value);

        Assert.Equal("Field", c.Field);
        Assert.Equal("GetOnlyProperty", c.GetOnlyProperty);
    }

    [Fact]
    public void SerializeDeserializeSetOnlyProperty()
    {
        var value = JsonConvert.SerializeObject(new SetOnlyPropertyClass());

        var c = JsonConvert.DeserializeObject<SetOnlyPropertyClass>(value);

        Assert.Equal("Field", c.Field);
    }

    [Fact]
    public void JsonIgnoreAttributeTest()
    {
        var json = JsonConvert.SerializeObject(new JsonIgnoreAttributeTestClass());

        Assert.Equal("""{"Field":0,"Property":21}""", json);

        var c = JsonConvert.DeserializeObject<JsonIgnoreAttributeTestClass>(
            """{"Field":99,"Property":-1,"IgnoredField":-1,"IgnoredObject":[1,2,3,4,5]}""");

        Assert.Equal(0, c.IgnoredField);
        Assert.Equal(99, c.Field);
    }

    [Fact]
    public void TorrentDeserializeTest()
    {
        var jsonText = """
                       {
                           "":"",
                           "label": [
                                  ["SomeName",6]
                           ],
                           "torrents": [
                                  ["192D99A5C943555CB7F00A852821CF6D6DB3008A",201,"filename.avi",178311826,1000,178311826,72815250,408,1603,7,121430,"NameOfLabelPrevioslyDefined",3,6,0,8,128954,-1,0],
                           ],
                           "torrentc": "1816000723"
                       }
                       """;

        var o = (JObject) JsonConvert.DeserializeObject(jsonText);
        Assert.Equal(4, o.Children().Count());

        var torrentsArray = o["torrents"];
        var nestedTorrentsArray = torrentsArray[0];
        Assert.Equal(19, nestedTorrentsArray.Children().Count());
    }

    [Fact]
    public void JsonPropertyClassSerialize()
    {
        var test = new JsonPropertyClass
        {
            Pie = "Delicious",
            SweetCakesCount = int.MaxValue
        };

        var jsonText = JsonConvert.SerializeObject(test);

        Assert.Equal("""{"pie":"Delicious","pie1":"PieChart!","sweet_cakes_count":2147483647}""", jsonText);

        var test2 = JsonConvert.DeserializeObject<JsonPropertyClass>(jsonText);

        Assert.Equal(test.Pie, test2.Pie);
        Assert.Equal(test.SweetCakesCount, test2.SweetCakesCount);
    }

    [Fact]
    public void BadJsonPropertyClassSerialize()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(new BadJsonPropertyClass()));
        Assert.Equal("A member with the name 'pie' already exists on 'TestObjects.BadJsonPropertyClass'. Use the JsonPropertyAttribute to specify another name.", exception.Message);
    }

    [Fact]
    public void InvalidBackslash()
    {
        var json = """["vvv\jvvv"]""";

        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<List<string>>(json));
        Assert.Equal(@"Bad JSON escape sequence: \j. Path '', line 1, position 7.", exception.Message);
    }

    [Fact]
    public void Unicode()
    {
        var json = """["PRE\u003cPOST"]""";

        var s = new DataContractJsonSerializer(typeof(List<string>));
        var dataContractResult = (List<string>) s.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        var jsonNetResult = JsonConvert.DeserializeObject<List<string>>(json);

        Assert.Single(jsonNetResult);
        Assert.Equal(dataContractResult[0], jsonNetResult[0]);
    }

    [Fact]
    public void BackslashEquivalence()
    {
        var json = """["vvv\/vvv\tvvv\"vvv\bvvv\nvvv\rvvv\\vvv\fvvv"]""";

#if !NET6_0_OR_GREATER
        var javaScriptSerializer = new JavaScriptSerializer();
        var javaScriptSerializerResult = javaScriptSerializer.Deserialize<List<string>>(json);
#endif

        var s = new DataContractJsonSerializer(typeof(List<string>));
        var dataContractResult = (List<string>) s.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        var jsonNetResult = JsonConvert.DeserializeObject<List<string>>(json);

        Assert.Single(jsonNetResult);
        Assert.Equal(dataContractResult[0], jsonNetResult[0]);
#if !NET6_0_OR_GREATER
        Assert.Equal(javaScriptSerializerResult[0], jsonNetResult[0]);
#endif
    }

    [Fact]
    public void DateTimeTest()
    {
        var testDates = new List<DateTime>
        {
            new(100, 1, 1, 1, 1, 1, DateTimeKind.Local),
            new(100, 1, 1, 1, 1, 1, DateTimeKind.Unspecified),
            new(100, 1, 1, 1, 1, 1, DateTimeKind.Utc),
            new(2000, 1, 1, 1, 1, 1, DateTimeKind.Local),
            new(2000, 1, 1, 1, 1, 1, DateTimeKind.Unspecified),
            new(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc)
        };

        var ms = new MemoryStream();
        var s = new DataContractJsonSerializer(typeof(List<DateTime>));
        s.WriteObject(ms, testDates);
        ms.Seek(0, SeekOrigin.Begin);
        var sr = new StreamReader(ms);

        var expected = sr.ReadToEnd();
    }

    [Fact]
    public void DateTimeOffsetIso()
    {
        var testDates = new List<DateTimeOffset>
        {
            new(new(100, 1, 1, 1, 1, 1, DateTimeKind.Utc)),
            new(2000, 1, 1, 1, 1, 1, TimeSpan.Zero),
            new(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(13)),
            new(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(-3.5))
        };

        var result = JsonConvert.SerializeObject(testDates);
        Assert.Equal(
            """["0100-01-01T01:01:01+00:00","2000-01-01T01:01:01+00:00","2000-01-01T01:01:01+13:00","2000-01-01T01:01:01-03:30"]""",
            result);
    }

    [Fact]
    public void NonStringKeyDictionary()
    {
        var values = new Dictionary<int, int>
        {
            {
                -5, 6
            },
            {
                int.MinValue, int.MaxValue
            }
        };

        var json = JsonConvert.SerializeObject(values);

        Assert.Equal("""{"-5":6,"-2147483648":2147483647}""", json);

        var newValues = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

        Assert.Equal(values, newValues);
    }

    [Fact]
    public void AnonymousObjectSerialization()
    {
        var anonymous =
            new
            {
                StringValue = "I am a string",
                IntValue = int.MaxValue,
                NestedAnonymous = new
                {
                    NestedValue = byte.MaxValue
                },
                NestedArray = new[]
                {
                    1,
                    2
                },
                Product = new Product
                {
                    Name = "TestProduct"
                }
            };

        var json = JsonConvert.SerializeObject(anonymous);
        Assert.Equal(
            """{"StringValue":"I am a string","IntValue":2147483647,"NestedAnonymous":{"NestedValue":255},"NestedArray":[1,2],"Product":{"Name":"TestProduct","ExpiryDate":"2000-01-01T00:00:00Z","Price":0.0,"Sizes":null}}""",
            json);

        anonymous = JsonConvert.DeserializeAnonymousType(json, anonymous);
        Assert.Equal("I am a string", anonymous.StringValue);
        Assert.Equal(int.MaxValue, anonymous.IntValue);
        Assert.Equal(255, anonymous.NestedAnonymous.NestedValue);
        Assert.Equal(2, anonymous.NestedArray.Length);
        Assert.Equal(1, anonymous.NestedArray[0]);
        Assert.Equal(2, anonymous.NestedArray[1]);
        Assert.Equal("TestProduct", anonymous.Product.Name);
    }

    [Fact]
    public void AnonymousObjectSerializationWithSetting()
    {
        var d = new DateTime(2000, 1, 1);

        var anonymous =
            new
            {
                DateValue = d
            };

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new IsoDateTimeConverter
        {
            DateTimeFormat = "yyyy"
        });

        var json = JsonConvert.SerializeObject(anonymous, settings);
        Assert.Equal("""{"DateValue":"2000"}""", json);

        anonymous = JsonConvert.DeserializeAnonymousType(json, anonymous, settings);
        Assert.Equal(d, anonymous.DateValue);
    }

    [Fact]
    public void SerializeObject()
    {
        var json = JsonConvert.SerializeObject(new());
        Assert.Equal("{}", json);
    }

    [Fact]
    public void SerializeNull()
    {
        var json = JsonConvert.SerializeObject(null);
        Assert.Equal("null", json);
    }

    [Fact]
    public void CanDeserializeIntArrayWhenNotFirstPropertyInJson()
    {
        var json = "{foo:'hello',bar:[1,2,3]}";
        var wibble = JsonConvert.DeserializeObject<ClassWithArray>(json);
        Assert.Equal("hello", wibble.Foo);

        Assert.Equal(4, wibble.Bar.Count);
        Assert.Equal(int.MaxValue, wibble.Bar[0]);
        Assert.Equal(1, wibble.Bar[1]);
        Assert.Equal(2, wibble.Bar[2]);
        Assert.Equal(3, wibble.Bar[3]);
    }

    [Fact]
    public void CanDeserializeIntArray_WhenArrayIsFirstPropertyInJson()
    {
        var json = "{bar:[1,2,3], foo:'hello'}";
        var wibble = JsonConvert.DeserializeObject<ClassWithArray>(json);
        Assert.Equal("hello", wibble.Foo);

        Assert.Equal(4, wibble.Bar.Count);
        Assert.Equal(int.MaxValue, wibble.Bar[0]);
        Assert.Equal(1, wibble.Bar[1]);
        Assert.Equal(2, wibble.Bar[2]);
        Assert.Equal(3, wibble.Bar[3]);
    }

    [Fact]
    public void ObjectCreationHandlingReplace()
    {
        var json = "{bar:[1,2,3], foo:'hello'}";

        var s = new JsonSerializer
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        var wibble = (ClassWithArray) s.Deserialize(new StringReader(json), typeof(ClassWithArray));

        Assert.Equal("hello", wibble.Foo);

        Assert.Single(wibble.Bar);
    }

    [Fact]
    public void CanDeserializeSerializedJson()
    {
        var wibble = new ClassWithArray
        {
            Foo = "hello"
        };
        wibble.Bar.Add(1);
        wibble.Bar.Add(2);
        wibble.Bar.Add(3);
        var json = JsonConvert.SerializeObject(wibble);

        var wibbleOut = JsonConvert.DeserializeObject<ClassWithArray>(json);
        Assert.Equal("hello", wibbleOut.Foo);

        Assert.Equal(5, wibbleOut.Bar.Count);
        Assert.Equal(int.MaxValue, wibbleOut.Bar[0]);
        Assert.Equal(int.MaxValue, wibbleOut.Bar[1]);
        Assert.Equal(1, wibbleOut.Bar[2]);
        Assert.Equal(2, wibbleOut.Bar[3]);
        Assert.Equal(3, wibbleOut.Bar[4]);
    }

    [Fact]
    public void SerializeConverableObjects()
    {
        var json = JsonConvert.SerializeObject(new ConverableMembers(), Formatting.Indented);

#if (NET6_0_OR_GREATER)
        var expected = """
                       {
                         "String": "string",
                         "Int32": 2147483647,
                         "UInt32": 4294967295,
                         "Byte": 255,
                         "SByte": 127,
                         "Short": 32767,
                         "UShort": 65535,
                         "Long": 9223372036854775807,
                         "ULong": 9223372036854775807,
                         "Double": 1.7976931348623157E+308,
                         "Float": 3.4028235E+38,
                         "DBNull": null,
                         "Bool": true,
                         "Char": "\u0000"
                       }
                       """;
#elif !NET6_0_OR_GREATER
        var expected = """
                       {
                         "String": "string",
                         "Int32": 2147483647,
                         "UInt32": 4294967295,
                         "Byte": 255,
                         "SByte": 127,
                         "Short": 32767,
                         "UShort": 65535,
                         "Long": 9223372036854775807,
                         "ULong": 9223372036854775807,
                         "Double": 1.7976931348623157E+308,
                         "Float": 3.40282347E+38,
                         "DBNull": null,
                         "Bool": true,
                         "Char": "\u0000"
                       }
                       """;
#else
            expected = @"{
  ""String"": ""string"",
  ""Int32"": 2147483647,
  ""UInt32"": 4294967295,
  ""Byte"": 255,
  ""SByte"": 127,
  ""Short"": 32767,
  ""UShort"": 65535,
  ""Long"": 9223372036854775807,
  ""ULong"": 9223372036854775807,
  ""Double"": 1.7976931348623157E+308,
  ""Float"": 3.40282347E+38,
  ""Bool"": true,
  ""Char"": ""\u0000""
}";
#endif

        XUnitAssert.AreEqualNormalized(expected, json);

        var c = JsonConvert.DeserializeObject<ConverableMembers>(json);
        Assert.Equal("string", c.String);
        Assert.Equal(double.MaxValue, c.Double);
#if !NET6_0_OR_GREATER
        Assert.Equal(DBNull.Value, c.DBNull);
#endif
    }

    [Fact]
    public void SerializeStack()
    {
        var s = new Stack<object>();
        s.Push(1);
        s.Push(2);
        s.Push(3);

        var json = JsonConvert.SerializeObject(s);
        Assert.Equal("[3,2,1]", json);
    }

    [Fact]
    public void FormattingOverride()
    {
        var obj = new
        {
            Formatting = "test"
        };

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        var indented = JsonConvert.SerializeObject(obj, settings);

        var none = JsonConvert.SerializeObject(obj, Formatting.None, settings);
        Assert.NotEqual(indented, none);
    }

    [Fact]
    public void DateTimeTimeZone()
    {
        var date = new DateTime(2001, 4, 4, 0, 0, 0, DateTimeKind.Utc);

        var json = JsonConvert.SerializeObject(date);
        Assert.Equal(
            """
            "2001-04-04T00:00:00Z"
            """,
            json);
    }

    [Fact]
    public void GuidTest()
    {
        var guid = new Guid("BED7F4EA-1A96-11d2-8F08-00A0C9A6186D");

        var json = JsonConvert.SerializeObject(new ClassWithGuid
        {
            GuidField = guid
        });
        Assert.Equal("""{"GuidField":"bed7f4ea-1a96-11d2-8f08-00a0c9a6186d"}""", json);

        var c = JsonConvert.DeserializeObject<ClassWithGuid>(json);
        Assert.Equal(guid, c.GuidField);
    }

    [Fact]
    public void EnumTest()
    {
        var json = JsonConvert.SerializeObject(StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal("1", json);

        var s = JsonConvert.DeserializeObject<StringComparison>(json);
        Assert.Equal(StringComparison.CurrentCultureIgnoreCase, s);
    }

    [Fact]
    public void TimeSpanTest()
    {
        var ts = new TimeSpan(00, 23, 59, 1);

        var json = JsonConvert.SerializeObject(new ClassWithTimeSpan
        {
            TimeSpanField = ts
        }, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "TimeSpanField": "23:59:01"
            }
            """,
            json);

        var c = JsonConvert.DeserializeObject<ClassWithTimeSpan>(json);
        Assert.Equal(ts, c.TimeSpanField);
    }

    [Fact]
    public void JsonIgnoreAttributeOnClassTest()
    {
        var json = JsonConvert.SerializeObject(new JsonIgnoreAttributeOnClassTestClass());

        Assert.Equal("""{"TheField":0,"Property":21}""", json);

        var c = JsonConvert.DeserializeObject<JsonIgnoreAttributeOnClassTestClass>(
            """{"TheField":99,"Property":-1,"IgnoredField":-1}""");

        Assert.Equal(0, c.IgnoredField);
        Assert.Equal(99, c.Field);
    }

    [Fact]
    public void ConstructorCaseSensitivity()
    {
        var c = new ConstructorCaseSensitivityClass("param1", "Param1", "Param2");

        var json = JsonConvert.SerializeObject(c);

        var deserialized = JsonConvert.DeserializeObject<ConstructorCaseSensitivityClass>(json);

        Assert.Equal("param1", deserialized.param1);
        Assert.Equal("Param1", deserialized.Param1);
        Assert.Equal("Param2", deserialized.Param2);
    }

    [Fact]
    public void SerializerShouldUseClassConverter()
    {
        var c1 = new ConverterPrecedenceClass("!Test!");

        var json = JsonConvert.SerializeObject(c1);
        Assert.Equal("""["Class","!Test!"]""", json);

        var c2 = JsonConvert.DeserializeObject<ConverterPrecedenceClass>(json);

        Assert.Equal("!Test!", c2.TestValue);
    }

    [Fact]
    public void SerializerShouldUseClassConverterOverArgumentConverter()
    {
        var c1 = new ConverterPrecedenceClass("!Test!");

        var json = JsonConvert.SerializeObject(c1, new ArgumentConverterPrecedenceClassConverter());
        Assert.Equal("""["Class","!Test!"]""", json);

        var c2 = JsonConvert.DeserializeObject<ConverterPrecedenceClass>(json, new ArgumentConverterPrecedenceClassConverter());

        Assert.Equal("!Test!", c2.TestValue);
    }

    [Fact]
    public void SerializerShouldUseMemberConverter_IsoDate()
    {
        var testDate = new DateTime(ParseTests.InitialJavaScriptDateTicks, DateTimeKind.Utc);
        var m1 = new MemberConverterClass
        {
            DefaultConverter = testDate,
            MemberConverter = testDate
        };

        var json = JsonConvert.SerializeObject(m1);
        Assert.Equal(
            """{"DefaultConverter":"1970-01-01T00:00:00Z","MemberConverter":"1970-01-01T00:00:00Z"}""",
            json);

        var m2 = JsonConvert.DeserializeObject<MemberConverterClass>(json);

        Assert.Equal(testDate, m2.DefaultConverter);
        Assert.Equal(testDate, m2.MemberConverter);
    }

    [Fact]
    public void SerializerShouldUseMemberConverter_DateParseNone()
    {
        var testDate = new DateTime(ParseTests.InitialJavaScriptDateTicks, DateTimeKind.Utc);
        var m1 = new MemberConverterClass
        {
            DefaultConverter = testDate,
            MemberConverter = testDate
        };

        var json = JsonConvert.SerializeObject(m1, new JsonSerializerSettings());
        Assert.Equal(
            """{"DefaultConverter":"1970-01-01T00:00:00Z","MemberConverter":"1970-01-01T00:00:00Z"}""",
            json);

        var m2 = JsonConvert.DeserializeObject<MemberConverterClass>(json);

        Assert.Equal(testDate, m2.DefaultConverter);
        Assert.Equal(testDate, m2.MemberConverter);
    }

    [Fact]
    public void SerializerShouldUseMemberConverterOverArgumentConverter()
    {
        var testDate = new DateTime(ParseTests.InitialJavaScriptDateTicks, DateTimeKind.Utc);
        var m1 = new MemberConverterClass
        {
            DefaultConverter = testDate,
            MemberConverter = testDate
        };

        var json = JsonConvert.SerializeObject(m1);
        Assert.Equal(
            """{"DefaultConverter":"1970-01-01T00:00:00Z","MemberConverter":"1970-01-01T00:00:00Z"}""",
            json);

        var m2 = JsonConvert.DeserializeObject<MemberConverterClass>(json);

        Assert.Equal(testDate, m2.DefaultConverter);
        Assert.Equal(testDate, m2.MemberConverter);
    }

    [Fact]
    public void ConverterAttributeExample()
    {
        var date = Convert.ToDateTime("1970-01-01T00:00:00Z").ToUniversalTime();

        var c = new MemberConverterClass
        {
            DefaultConverter = date,
            MemberConverter = date
        };

        var json = JsonConvert.SerializeObject(c, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "DefaultConverter": "1970-01-01T00:00:00Z",
              "MemberConverter": "1970-01-01T00:00:00Z"
            }
            """,
            json);
    }

    [Fact]
    public void SerializerShouldUseMemberConverterOverClassAndArgumentConverter()
    {
        var c1 = new ClassAndMemberConverterClass
        {
            DefaultConverter = new("DefaultConverterValue"),
            MemberConverter = new("MemberConverterValue")
        };

        var json = JsonConvert.SerializeObject(c1, new ArgumentConverterPrecedenceClassConverter());
        Assert.Equal(
            """{"DefaultConverter":["Class","DefaultConverterValue"],"MemberConverter":["Member","MemberConverterValue"]}""",
            json);

        var c2 = JsonConvert.DeserializeObject<ClassAndMemberConverterClass>(json, new ArgumentConverterPrecedenceClassConverter());

        Assert.Equal("DefaultConverterValue", c2.DefaultConverter.TestValue);
        Assert.Equal("MemberConverterValue", c2.MemberConverter.TestValue);
    }

    [Fact]
    public void IncompatibleJsonAttributeShouldThrow()
    {
        var c = new IncompatibleJsonAttributeClass();
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(c));
        Assert.Equal("Unexpected value when converting date. Expected DateTime or DateTimeOffset, got TestObjects.IncompatibleJsonAttributeClass.", exception.Message);
    }

    [Fact]
    public void GenericAbstractProperty()
    {
        var json = JsonConvert.SerializeObject(new GenericImpl());
        Assert.Equal("""{"Id":0}""", json);
    }

    [Fact]
    public void DeserializeNullable()
    {
        var json = JsonConvert.SerializeObject(null);
        Assert.Equal("null", json);

        json = JsonConvert.SerializeObject((int?) 1);
        Assert.Equal("1", json);
    }

    [Fact]
    public void SerializeJsonRaw()
    {
        var personRaw = new PersonRaw
        {
            FirstName = "FirstNameValue",
            RawContent = new("[1,2,3,4,5]"),
            LastName = "LastNameValue"
        };

        var json = JsonConvert.SerializeObject(personRaw);
        Assert.Equal("""{"first_name":"FirstNameValue","RawContent":[1,2,3,4,5],"last_name":"LastNameValue"}""", json);
    }

    [Fact]
    public void NullableConverter()
    {
        var target = new MyStruct("the value");
        var converter = new MyStructConverter();
        var json = JsonConvert.SerializeObject(target, converter);
        var deserialize = JsonConvert.DeserializeObject<MyStruct>(json, converter);
        Assert.Equal("the value", deserialize.Value);
    }

    [Fact]
    public void NullableConverterWrapped()
    {
        var target = new TargetWithNullableStruct();
        var converter = new MyStructConverter();
        var json = JsonConvert.SerializeObject(target, converter);
        var deserialize = JsonConvert.DeserializeObject<TargetWithNullableStruct>(json, converter);
        Assert.Null(deserialize.Member);
    }

    [Fact]
    public void NullableConverterWrappedWithValue()
    {
        var target = new TargetWithNullableStruct
        {
            Member = new MyStruct("the value")
        };
        var converter = new MyStructConverter();
        var json = JsonConvert.SerializeObject(target, converter);
        var deserialize = JsonConvert.DeserializeObject<TargetWithNullableStruct>(json, converter);
        Assert.Equal("the value", deserialize.Member.Value.Value);
    }

    class MyStructConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var target = (MyStruct) value;

            writer.WriteValue(target.Value);
        }

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) =>
            new MyStruct((string)reader.Value);

        public override bool CanConvert(Type type) =>
            type == typeof(MyStruct);
    }

    public class TargetWithNullableStruct
    {
        public MyStruct? Member { get; set; }
    }
    public struct MyStruct(string value)
    {
        public string Value { get; } = value;
    }
    [Fact]
    public void DeserializeJsonRaw()
    {
        var json = """{"first_name":"FirstNameValue","RawContent":[1,2,3,4,5],"last_name":"LastNameValue"}""";

        var personRaw = JsonConvert.DeserializeObject<PersonRaw>(json);

        Assert.Equal("FirstNameValue", personRaw.FirstName);
        Assert.Equal("[1,2,3,4,5]", personRaw.RawContent.ToString());
        Assert.Equal("LastNameValue", personRaw.LastName);
    }

    [Fact]
    public void DeserializeNullableMember()
    {
        var userNullable = new UserNullable
        {
            Id = new("AD6205E8-0DF4-465d-AEA6-8BA18E93A7E7"),
            FName = "FirstValue",
            LName = "LastValue",
            RoleId = 5,
            NullableRoleId = 6,
            NullRoleId = null,
            Active = true
        };

        var json = JsonConvert.SerializeObject(userNullable);

        Assert.Equal("""{"Id":"ad6205e8-0df4-465d-aea6-8ba18e93a7e7","FName":"FirstValue","LName":"LastValue","RoleId":5,"NullableRoleId":6,"NullRoleId":null,"Active":true}""", json);

        var userNullableDeserialized = JsonConvert.DeserializeObject<UserNullable>(json);

        Assert.Equal(new("AD6205E8-0DF4-465d-AEA6-8BA18E93A7E7"), userNullableDeserialized.Id);
        Assert.Equal("FirstValue", userNullableDeserialized.FName);
        Assert.Equal("LastValue", userNullableDeserialized.LName);
        Assert.Equal(5, userNullableDeserialized.RoleId);
        Assert.Equal(6, userNullableDeserialized.NullableRoleId);
        Assert.Null(userNullableDeserialized.NullRoleId);
        Assert.True(userNullableDeserialized.Active);
    }

    [Fact]
    public void DeserializeInt64ToNullableDouble()
    {
        var json = """{"Height":1}""";

        var c = JsonConvert.DeserializeObject<DoubleClass>(json);
        Assert.Equal(1, c.Height);
    }

    [Fact]
    public void SerializeTypeProperty()
    {
        var boolRef = typeof(bool).AssemblyQualifiedName;
        var typeClass = new TypeClass
        {
            TypeProperty = typeof(bool)
        };

        var json = JsonConvert.SerializeObject(typeClass);
        Assert.Equal($$"""{"TypeProperty":"{{boolRef}}"}""", json);

        var typeClass2 = JsonConvert.DeserializeObject<TypeClass>(json);
        Assert.Equal(typeof(bool), typeClass2.TypeProperty);

        var jsonSerializerTestRef = typeof(JsonSerializerTest).AssemblyQualifiedName;
        typeClass = new()
        {
            TypeProperty = typeof(JsonSerializerTest)
        };

        json = JsonConvert.SerializeObject(typeClass);
        Assert.Equal($$"""{"TypeProperty":"{{jsonSerializerTestRef}}"}""", json);

        typeClass2 = JsonConvert.DeserializeObject<TypeClass>(json);
        Assert.Equal(typeof(JsonSerializerTest), typeClass2.TypeProperty);
    }

    [Fact]
    public void RequiredMembersClass()
    {
        var c = new RequiredMembersClass
        {
            BirthDate = new(2000, 12, 20, 10, 55, 55, DateTimeKind.Utc),
            FirstName = "Bob",
            LastName = "Smith",
            MiddleName = "Cosmo"
        };

        var json = JsonConvert.SerializeObject(c, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "FirstName": "Bob",
              "MiddleName": "Cosmo",
              "LastName": "Smith",
              "BirthDate": "2000-12-20T10:55:55Z"
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<RequiredMembersClass>(json);

        Assert.Equal("Bob", c2.FirstName);
        Assert.Equal(new(2000, 12, 20, 10, 55, 55, DateTimeKind.Utc), c2.BirthDate);
    }

    [Fact]
    public void DeserializeRequiredMembersClassWithNullValues()
    {
        var json = """
                   {
                     "FirstName": "I can't be null bro!",
                     "MiddleName": null,
                     "LastName": null,
                     "BirthDate": "2013-08-14T04:38:31.000+0000"
                   }
                   """;

        var c = JsonConvert.DeserializeObject<RequiredMembersClass>(json);

        Assert.Equal("I can't be null bro!", c.FirstName);
        Assert.Null(c.MiddleName);
        Assert.Null(c.LastName);
    }

    [Fact]
    public void DeserializeRequiredMembersClassNullRequiredValueProperty()
    {
        try
        {
            var json = """
                       {
                         "FirstName": null,
                         "MiddleName": null,
                         "LastName": null,
                         "BirthDate": "2013-08-14T04:38:31.000+0000"
                       }
                       """;

            JsonConvert.DeserializeObject<RequiredMembersClass>(json);
            Assert.Fail();
        }
        catch (JsonSerializationException exception)
        {
            Assert.StartsWith("Required property 'FirstName' expects a value but got null. Path ''", exception.Message);
        }
    }

    [Fact]
    public void SerializeRequiredMembersClassNullRequiredValueProperty()
    {
        var requiredMembersClass = new RequiredMembersClass
        {
            FirstName = null,
            BirthDate = new(2000, 10, 10, 10, 10, 10, DateTimeKind.Utc),
            LastName = null,
            MiddleName = null
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(requiredMembersClass));
        Assert.Equal("Cannot write a null value for property 'FirstName'. Property requires a value. Path ''.", exception.Message);
    }

    [Fact]
    public void RequiredMembersClassMissingRequiredProperty()
    {
        var json = """
                   {
                     "FirstName": "Bob"
                   }
                   """;

        try
        {
            JsonConvert.DeserializeObject<RequiredMembersClass>(json);
            Assert.Fail();
        }
        catch (JsonSerializationException exception)
        {
            Assert.StartsWith("Required property 'LastName' not found in JSON. Path ''", exception.Message);
        }
    }

    [Fact]
    public void SerializeJaggedArray()
    {
        var aa = new JaggedArray
        {
            Before = "Before!",
            After = "After!",
            Coordinates =
            [
                [
                    1,
                    1
                ],
                [
                    1,
                    2
                ],
                [
                    2,
                    1
                ],
                [
                    2,
                    2
                ]
            ]
        };

        var json = JsonConvert.SerializeObject(aa);

        Assert.Equal(
            """{"Before":"Before!","Coordinates":[[1,1],[1,2],[2,1],[2,2]],"After":"After!"}""",
            json);
    }

    [Fact]
    public void DeserializeJaggedArray()
    {
        var json = """{"Before":"Before!","Coordinates":[[1,1],[1,2],[2,1],[2,2]],"After":"After!"}""";

        var aa = JsonConvert.DeserializeObject<JaggedArray>(json);

        Assert.Equal("Before!", aa.Before);
        Assert.Equal("After!", aa.After);
        Assert.Equal(4, aa.Coordinates.Length);
        Assert.Equal(2, aa.Coordinates[0].Length);
        Assert.Equal(1, aa.Coordinates[0][0]);
        Assert.Equal(2, aa.Coordinates[1][1]);

        var after = JsonConvert.SerializeObject(aa);

        Assert.Equal(json, after);
    }

    [Fact]
    public void DeserializeGoogleGeoCode()
    {
        var json = """
                   {
                     "name": "1600 Amphitheatre Parkway, Mountain View, CA, USA",
                     "Status": {
                       "code": 200,
                       "request": "geocode"
                     },
                     "Placemark": [
                       {
                         "address": "1600 Amphitheatre Pkwy, Mountain View, CA 94043, USA",
                         "AddressDetails": {
                           "Country": {
                             "CountryNameCode": "US",
                             "AdministrativeArea": {
                               "AdministrativeAreaName": "CA",
                               "SubAdministrativeArea": {
                                 "SubAdministrativeAreaName": "Santa Clara",
                                 "Locality": {
                                   "LocalityName": "Mountain View",
                                   "Thoroughfare": {
                                     "ThoroughfareName": "1600 Amphitheatre Pkwy"
                                   },
                                   "PostalCode": {
                                     "PostalCodeNumber": "94043"
                                   }
                                 }
                               }
                             }
                           },
                           "Accuracy": 8
                         },
                         "Point": {
                           "coordinates": [-122.083739, 37.423021, 0]
                         }
                       }
                     ]
                   }
                   """;

        var jsonGoogleMapGeocoder = JsonConvert.DeserializeObject<GoogleMapGeocoderStructure>(json);
    }

    [Fact]
    public void DeserializeInterfaceProperty()
    {
        var testClass = new InterfacePropertyTestClass
        {
            co = new Co()
        };
        var strFromTest = JsonConvert.SerializeObject(testClass);

        var exception = Assert.Throws<JsonSerializationException>(() =>
        {
            var testFromDe = (InterfacePropertyTestClass) JsonConvert.DeserializeObject(strFromTest, typeof(InterfacePropertyTestClass));
        });
        Assert.Equal("Could not create an instance of type TestObjects.ICo. Type is an interface or abstract class and cannot be instantiated. Path 'co.Name', line 1, position 14.", exception.Message);
    }

    [Fact]
    public void WriteJsonDates()
    {
        var entry = new LogEntry
        {
            LogDate = new(2009, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            Details = "Application started."
        };

        var defaultJson = JsonConvert.SerializeObject(entry);
        // {"Details":"Application started.","LogDate":"\/Date(1234656000000)\/"}

        var isoJson = JsonConvert.SerializeObject(entry, new IsoDateTimeConverter());
        // {"Details":"Application started.","LogDate":"2009-02-15T00:00:00.0000000Z"}

        Assert.Equal(
            """{"Details":"Application started.","LogDate":"2009-02-15T00:00:00Z"}""",
            defaultJson);
        Assert.Equal(
            """{"Details":"Application started.","LogDate":"2009-02-15T00:00:00Z"}""",
            isoJson);
    }

    [Fact]
    public void GenericListAndDictionaryInterfaceProperties()
    {
        var o = new GenericListAndDictionaryInterfaceProperties
        {
            IDictionaryProperty = new Dictionary<string, int>
            {
                {
                    "one", 1
                },
                {
                    "two", 2
                },
                {
                    "three", 3
                }
            },
            IListProperty =
            [
                1,
                2,
                3
            ],
            IEnumerableProperty =
            [
                4,
                5,
                6
            ]
        };

        var json = JsonConvert.SerializeObject(o, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "IEnumerableProperty": [
                4,
                5,
                6
              ],
              "IListProperty": [
                1,
                2,
                3
              ],
              "IDictionaryProperty": {
                "one": 1,
                "two": 2,
                "three": 3
              }
            }
            """,
            json);

        var deserializedObject = JsonConvert.DeserializeObject<GenericListAndDictionaryInterfaceProperties>(json);
        Assert.NotNull(deserializedObject);

        Assert.Equal(o.IListProperty.ToArray(), deserializedObject.IListProperty.ToArray());
        Assert.Equal(o.IEnumerableProperty.ToArray(), deserializedObject.IEnumerableProperty.ToArray());
        Assert.Equal(o.IDictionaryProperty.ToArray(), deserializedObject.IDictionaryProperty.ToArray());
    }

    [Fact]
    public void DeserializeBestMatchPropertyCase()
    {
        var json = """
                   {
                     "firstName": "firstName",
                     "FirstName": "FirstName",
                     "LastName": "LastName",
                     "lastName": "lastName",
                   }
                   """;

        var o = JsonConvert.DeserializeObject<PropertyCase>(json);
        Assert.NotNull(o);

        Assert.Equal("firstName", o.firstName);
        Assert.Equal("FirstName", o.FirstName);
        Assert.Equal("LastName", o.LastName);
        Assert.Equal("lastName", o.lastName);
    }

    [Fact]
    public void PopulateDefaultValueWhenUsingConstructor()
    {
        var json = "{ 'testProperty1': 'value' }";

        var c = JsonConvert.DeserializeObject<ConstructorAndDefaultValueAttributeTestClass>(json, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate
        });
        Assert.Equal("value", c.TestProperty1);
        Assert.Equal(21, c.TestProperty2);

        c = JsonConvert.DeserializeObject<ConstructorAndDefaultValueAttributeTestClass>(json, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        });
        Assert.Equal("value", c.TestProperty1);
        Assert.Equal(21, c.TestProperty2);
    }

    [Fact]
    public void RequiredWhenUsingConstructor()
    {
        try
        {
            var json = "{ 'testProperty1': 'value' }";
            JsonConvert.DeserializeObject<ConstructorAndRequiredTestClass>(json);

            Assert.Fail();
        }
        catch (JsonSerializationException exception)
        {
            Assert.StartsWith("Required property 'TestProperty2' not found in JSON. Path ''", exception.Message);
        }
    }

    [Fact]
    public void DeserializePropertiesOnToNonDefaultConstructor()
    {
        var i = new SubKlass("my subprop")
        {
            SuperProp = "overrided superprop"
        };

        var json = JsonConvert.SerializeObject(i);
        Assert.Equal("""{"SubProp":"my subprop","SuperProp":"overrided superprop"}""", json);

        var ii = JsonConvert.DeserializeObject<SubKlass>(json);

        var newJson = JsonConvert.SerializeObject(ii);
        Assert.Equal("""{"SubProp":"my subprop","SuperProp":"overrided superprop"}""", newJson);
    }

    [Fact]
    public void DeserializePropertiesOnToNonDefaultConstructorWithReferenceTracking()
    {
        var i = new SubKlass("my subprop")
        {
            SuperProp = "overrided superprop"
        };

        var json = JsonConvert.SerializeObject(i, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });

        Assert.Equal("""{"$id":"1","SubProp":"my subprop","SuperProp":"overrided superprop"}""", json);

        var ii = JsonConvert.DeserializeObject<SubKlass>(json, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });

        var newJson = JsonConvert.SerializeObject(ii, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });
        Assert.Equal("""{"$id":"1","SubProp":"my subprop","SuperProp":"overrided superprop"}""", newJson);
    }

    [Fact]
    public void SerializeJsonPropertyWithHandlingValues()
    {
        var o = new JsonPropertyWithHandlingValues
        {
            DefaultValueHandlingIgnoreProperty = "Default!",
            DefaultValueHandlingIncludeProperty = "Default!",
            DefaultValueHandlingPopulateProperty = "Default!",
            DefaultValueHandlingIgnoreAndPopulateProperty = "Default!"
        };

        var json = JsonConvert.SerializeObject(o, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "DefaultValueHandlingIncludeProperty": "Default!",
              "DefaultValueHandlingPopulateProperty": "Default!",
              "NullValueHandlingIncludeProperty": null,
              "ReferenceLoopHandlingErrorProperty": null,
              "ReferenceLoopHandlingIgnoreProperty": null,
              "ReferenceLoopHandlingSerializeProperty": null
            }
            """,
            json);

        json = JsonConvert.SerializeObject(o, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "DefaultValueHandlingIncludeProperty": "Default!",
              "DefaultValueHandlingPopulateProperty": "Default!",
              "NullValueHandlingIncludeProperty": null
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeJsonPropertyWithHandlingValues()
    {
        var json = "{}";

        var o = JsonConvert.DeserializeObject<JsonPropertyWithHandlingValues>(json);
        Assert.Equal("Default!", o.DefaultValueHandlingIgnoreAndPopulateProperty);
        Assert.Equal("Default!", o.DefaultValueHandlingPopulateProperty);
        Assert.Null(o.DefaultValueHandlingIgnoreProperty);
        Assert.Null(o.DefaultValueHandlingIncludeProperty);
    }

    [Fact]
    public void JsonPropertyWithHandlingValues_ReferenceLoopError()
    {
        var classRef = typeof(JsonPropertyWithHandlingValues).FullName;
        var o = new JsonPropertyWithHandlingValues();
        o.ReferenceLoopHandlingErrorProperty = o;

        var messages = $"Self referencing loop detected for property 'ReferenceLoopHandlingErrorProperty' with type '{classRef}'. Path ''.";
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(o, Formatting.Indented, settings));
        Assert.Equal(messages, exception.Message);
    }

    [Fact]
    public void PartialClassDeserialize()
    {
        var json = """
                   {
                       "request": "ux.settings.update",
                       "sid": "14c561bd-32a8-457e-b4e5-4bba0832897f",
                       "uid": "30c39065-0f31-de11-9442-001e3786a8ec",
                       "fidOrder": [
                           "id",
                           "andytest_name",
                           "andytest_age",
                           "andytest_address",
                           "andytest_phone",
                           "date",
                           "title",
                           "titleId"
                       ],
                       "entityName": "Andy Test",
                       "setting": "entity.field.order"
                   }
                   """;

        var r = JsonConvert.DeserializeObject<RequestOnly>(json);
        Assert.Equal("ux.settings.update", r.Request);

        var n = JsonConvert.DeserializeObject<NonRequest>(json);
        Assert.Equal(new("14c561bd-32a8-457e-b4e5-4bba0832897f"), n.Sid);
        Assert.Equal(new("30c39065-0f31-de11-9442-001e3786a8ec"), n.Uid);
        Assert.Equal(8, n.FidOrder.Count);
        Assert.Equal("id", n.FidOrder[0]);
        Assert.Equal("titleId", n.FidOrder[^1]);
    }

    [Fact]
    public void SerializeDataContractPrivateMembers()
    {
        var c = new DataContractPrivateMembers("Jeff", 26, 10, "Dr")
        {
            NotIncluded = "Hi"
        };
        var json = JsonConvert.SerializeObject(c, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "_name": "Jeff",
              "_age": 26,
              "Rank": 10,
              "JsonTitle": "Dr"
            }
            """,
            json);

        var cc = JsonConvert.DeserializeObject<DataContractPrivateMembers>(json);
        Assert.Equal("_name: Jeff, _age: 26, Rank: 10, JsonTitle: Dr", cc.ToString());
    }

    [Fact]
    public void DeserializeDictionaryInterface()
    {
        var json = """
                   {
                     "Name": "Name!",
                     "Dictionary": {
                       "Item": 11
                     }
                   }
                   """;

        var c = JsonConvert.DeserializeObject<DictionaryInterfaceClass>(
            json,
            new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace
            });

        Assert.Equal("Name!", c.Name);
        Assert.Single(c.Dictionary);
        Assert.Equal(11, c.Dictionary["Item"]);
    }

    [Fact]
    public void DeserializeDictionaryInterfaceWithExistingValues()
    {
        var json = """
                   {
                     "Random": {
                       "blah": 1
                     },
                     "Name": "Name!",
                     "Dictionary": {
                       "Item": 11,
                       "Item1": 12
                     },
                     "Collection": [
                       999
                     ],
                     "Employee": {
                       "Manager": {
                         "Name": "ManagerName!"
                       }
                     }
                   }
                   """;

        var c = JsonConvert.DeserializeObject<DictionaryInterfaceClass>(json,
            new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Reuse
            });

        Assert.Equal("Name!", c.Name);
        Assert.Equal(3, c.Dictionary.Count);
        Assert.Equal(11, c.Dictionary["Item"]);
        Assert.Equal(1, c.Dictionary["existing"]);
        Assert.Equal(4, c.Collection.Count);
        Assert.Equal(1, c.Collection.ElementAt(0));
        Assert.Equal(999, c.Collection.ElementAt(3));
        Assert.Equal("EmployeeName!", c.Employee.Name);
        Assert.Equal("ManagerName!", c.Employee.Manager.Name);
        Assert.NotNull(c.Random);
    }

    [Fact]
    public void TypedObjectDeserializationWithComments()
    {
        var json = """
                   /*comment1*/ { /*comment2*/
                     "Name": /*comment3*/ "Apple" /*comment4*/, /*comment5*/
                     "ExpiryDate": "2008-12-28T00:00:00.000",
                     "Price": 3.99,
                     "Sizes": /*comment6*/ [ /*comment7*/
                       "Small", /*comment8*/
                       "Medium" /*comment9*/,
                       /*comment10*/ "Large"
                     /*comment11*/ ] /*comment12*/
                   } /*comment13*/
                   """;

        var deserializedProduct = (Product) JsonConvert.DeserializeObject(json, typeof(Product));

        Assert.Equal("Apple", deserializedProduct.Name);
        Assert.Equal(new(2008, 12, 28, 0, 0, 0, DateTimeKind.Utc), deserializedProduct.ExpiryDate);
        Assert.Equal(3.99m, deserializedProduct.Price);
        Assert.Equal("Small", deserializedProduct.Sizes[0]);
        Assert.Equal("Medium", deserializedProduct.Sizes[1]);
        Assert.Equal("Large", deserializedProduct.Sizes[2]);
    }

    [Fact]
    public void NestedInsideOuterObject()
    {
        var json = """
                   {
                     "short": {
                       "original": "http://www.contrast.ie/blog/online&#45;marketing&#45;2009/",
                       "short": "m2sqc6",
                       "shortened": "http://short.ie/m2sqc6",
                       "error": {
                         "code": 0,
                         "msg": "No action taken"
                       }
                     }
                   }
                   """;

        var o = JObject.Parse(json);

        var s = JsonConvert.DeserializeObject<Shortie>(o["short"].ToString());
        Assert.NotNull(s);

        Assert.Equal("http://www.contrast.ie/blog/online&#45;marketing&#45;2009/", s.Original);
        Assert.Equal("m2sqc6", s.Short);
        Assert.Equal("http://short.ie/m2sqc6", s.Shortened);
    }

    [Fact]
    public void UriSerialization()
    {
        var uri = new Uri("http://codeplex.com");
        var json = JsonConvert.SerializeObject(uri);

        Assert.Equal("http://codeplex.com/", uri.ToString());

        var newUri = JsonConvert.DeserializeObject<Uri>(json);
        Assert.Equal(uri, newUri);
    }

    [Fact]
    public void AnonymousPlusLinqToSql()
    {
        var value = new
        {
            bar = new JObject(new JProperty("baz", 13))
        };

        var json = JsonConvert.SerializeObject(value);

        Assert.Equal("""{"bar":{"baz":13}}""", json);
    }

    [Fact]
    public void SerializeEnumerableAsObject()
    {
        var content = new Content
        {
            Text = "Blah, blah, blah",
            Children =
            [
                new()
                {
                    Text = "First"
                },

                new()
                {
                    Text = "Second"
                }
            ]
        };

        var json = JsonConvert.SerializeObject(content, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Children": [
                {
                  "Children": null,
                  "Text": "First"
                },
                {
                  "Children": null,
                  "Text": "Second"
                }
              ],
              "Text": "Blah, blah, blah"
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeEnumerableAsObject()
    {
        var json = """
                   {
                     "Children": [
                       {
                         "Children": null,
                         "Text": "First"
                       },
                       {
                         "Children": null,
                         "Text": "Second"
                       }
                     ],
                     "Text": "Blah, blah, blah"
                   }
                   """;

        var content = JsonConvert.DeserializeObject<Content>(json);

        Assert.Equal("Blah, blah, blah", content.Text);
        Assert.Equal(2, content.Children.Count);
        Assert.Equal("First", content.Children[0].Text);
        Assert.Equal("Second", content.Children[1].Text);
    }

    [Fact]
    public void RoleTransferTest()
    {
        var json = """{"Operation":"1","RoleName":"Admin","Direction":"0"}""";

        var r = JsonConvert.DeserializeObject<RoleTransfer>(json);

        Assert.Equal(RoleTransferOperation.Second, r.Operation);
        Assert.Equal("Admin", r.RoleName);
        Assert.Equal(RoleTransferDirection.First, r.Direction);
    }

    [Fact]
    public void DeserializeGenericDictionary()
    {
        var json = """{"key1":"value1","key2":"value2"}""";

        var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        Assert.Equal(2, values.Count);
        Assert.Equal("value1", values["key1"]);
        Assert.Equal("value2", values["key2"]);
    }

    [Fact]
    public void DeserializeEmptyStringToNullableDateTime()
    {
        var json = """{"DateTimeField":""}""";

        var c = JsonConvert.DeserializeObject<NullableDateTimeTestClass>(json);
        Assert.Null(c.DateTimeField);
    }

    [Fact]
    public void FailWhenClassWithNoDefaultConstructorHasMultipleConstructorsWithArguments()
    {
        var json = """{"sublocation":"AlertEmailSender.Program.Main","userId":0,"type":0,"summary":"Loading settings variables","details":null,"stackTrace":"   at System.Environment.GetStackTrace(Exception e, Boolean needFileInfo)\r\n   at System.Environment.get_StackTrace()\r\n   at mr.Logging.Event..ctor(String summary) in C:\\Projects\\MRUtils\\Logging\\Event.vb:line 71\r\n   at AlertEmailSender.Program.Main(String[] args) in C:\\Projects\\AlertEmailSender\\AlertEmailSender\\Program.cs:line 25","tag":null,"time":"\/Date(1249591032026-0400)\/"}""";

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Event>(json));
        Assert.Equal("Unable to find a constructor to use for type TestObjects.Event. A class should either have a default constructor, one constructor with arguments or a constructor marked with the JsonConstructor attribute. Path 'sublocation', line 1, position 15.", exception.Message);
    }

    [Fact]
    public void DeserializeObjectSetOnlyProperty()
    {
        var json = "{'SetOnlyProperty':[1,2,3,4,5]}";

        var setOnly = JsonConvert.DeserializeObject<SetOnlyPropertyClass2>(json);
        var a = (JArray) setOnly.GetValue();
        Assert.Equal(5, a.Count);
        Assert.Equal(1, (int) a[0]);
        // ReSharper disable once UseIndexFromEndExpression
        Assert.Equal(5, (int) a[a.Count - 1]);
    }

    [Fact]
    public void DeserializeOptInClasses()
    {
        var json = """{id: "12", name: "test", items: [{id: "112", name: "testing"}]}""";

        var l = JsonConvert.DeserializeObject<ListTestClass>(json);
    }

    [Fact]
    public void DeserializeNullableListWithNulls()
    {
        var l = JsonConvert.DeserializeObject<List<decimal?>>("[ 3.3, null, 1.1 ] ");
        Assert.Equal(3, l.Count);

        Assert.Equal(3.3m, l[0]);
        Assert.Null(l[1]);
        Assert.Equal(1.1m, l[2]);
    }

    [Fact]
    public Task CannotDeserializeArrayIntoObject()
    {
        var json = "[]";

        return Throws(() => JsonConvert.DeserializeObject<Person>(json));
    }

    [Fact]
    public Task CannotDeserializeArrayIntoDictionary()
    {
        var json = "[]";

        return Throws(() => JsonConvert.DeserializeObject<Dictionary<string, string>>(json));
    }

    [Fact]
    public Task CannotDeserializeArrayIntoSerializable()
    {
        var json = "[]";

        return Throws(() => JsonConvert.DeserializeObject<Exception>(json));
    }

    [Fact]
    public void CannotDeserializeArrayIntoDouble()
    {
        var json = "[]";

        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<double>(json));
        Assert.Equal("Unexpected character encountered while parsing value: [. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    Task CannotDeserializeArrayIntoDynamic()
    {
        var json = "[]";

        return Throws(() => JsonConvert.DeserializeObject<DynamicDictionary>(json));
    }

    public class DynamicDictionary : DynamicObject
    {
        readonly IDictionary<string, object> values = new Dictionary<string, object>();

        public override IEnumerable<string> GetDynamicMemberNames() =>
            values.Keys;

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = values[binder.Name];
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            values[binder.Name] = value;
            return true;
        }
    }

    [Fact]
    public void CannotDeserializeArrayIntoLinqToJson()
    {
        var json = "[]";

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<JObject>(json));
        Assert.Equal("Deserialized JSON type 'Argon.JArray' is not compatible with expected type 'Argon.JObject'. Path '', line 1, position 2.", exception.Message);
    }

    [Fact]
    public void CannotDeserializeObjectIntoArray()
    {
        var json = "{}";

        try
        {
            JsonConvert.DeserializeObject<List<Person>>(json);
            Assert.Fail();
        }
        catch (JsonSerializationException exception)
        {
            Assert.StartsWith($$"""Cannot deserialize the current JSON object (e.g. {"name":"value"}) into type 'System.Collections.Generic.List`1[TestObjects.Person]' because the type requires a JSON array (e.g. [1,2,3]) to deserialize correctly.{{Environment.NewLine}}To fix this error either change the JSON to a JSON array (e.g. [1,2,3]) or change the deserialized type so that it is a normal .NET type (e.g. not a primitive type like integer, not a collection type like an array or List<T>) that can be deserialized from a JSON object. JsonObjectAttribute can also be added to the type to force it to deserialize from a JSON object.{{Environment.NewLine}}Path ''""", exception.Message);
        }
    }

    [Fact]
    public void DeserializeEmptyString()
    {
        var json = """{"Name":""}""";

        var p = JsonConvert.DeserializeObject<Person>(json);
        Assert.Equal("", p.Name);
    }

    [Fact]
    public void SerializePropertyGetError()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver()
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(new MemoryStream(), settings));
        Assert.Equal("Error getting value from 'ReadTimeout' on 'System.IO.MemoryStream'.", exception.Message);
    }

    [Fact]
    public void DeserializePropertySetError()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver()
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<MemoryStream>("{ReadTimeout:0}", settings));
        Assert.Equal("Error setting value to 'ReadTimeout' on 'System.IO.MemoryStream'.", exception.Message);
    }

    [Fact]
    public void DeserializeEnsureTypeEmptyStringToIntError()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver()
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<MemoryStream>("{ReadTimeout:''}", settings));
        Assert.Equal("Error converting value {null} to type 'System.Int32'. Path 'ReadTimeout', line 1, position 15.", exception.Message);
    }

    [Fact]
    public void DeserializeEnsureTypeNullToIntError()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver()
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<MemoryStream>("{ReadTimeout:null}", settings));
        Assert.Equal("Error converting value {null} to type 'System.Int32'. Path 'ReadTimeout', line 1, position 17.", exception.Message);
    }

    [Fact]
    public void SerializeGenericListOfStrings()
    {
        var strings = new List<string>
        {
            "str_1",
            "str_2",
            "str_3"
        };

        var json = JsonConvert.SerializeObject(strings);
        Assert.Equal("""["str_1","str_2","str_3"]""", json);
    }

    [Fact]
    public void IgnoreListItem()
    {
        var strings = new List<string>
        {
            "str_1",
            "ignore",
            "str_3"
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new IgnoreItemContractResolver()
        };
        var json = JsonConvert.SerializeObject(strings, settings);
        Assert.Equal("""["str_1","str_3"]""", json);
    }

    class IgnoreItemContractResolver : DefaultContractResolver
    {
        protected override JsonArrayContract CreateArrayContract(Type type)
        {
            var contract = base.CreateArrayContract(type);
            contract.InterceptSerializeItem = item =>
            {
                if (item is "ignore")
                {
                    return ItemInterceptResult.Ignore;
                }

                return ItemInterceptResult.Default;
            };
            return contract;
        }
    }
    [Fact]
    public void InterceptList()
    {
        var strings = new List<string>
        {
            "a",
            "c",
            "b"
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new InterceptListContractResolver()
        };
        var json = JsonConvert.SerializeObject(strings, settings);
        Assert.Equal("""["a","b","c"]""", json);
    }

    class InterceptListContractResolver : DefaultContractResolver
    {
        protected override JsonArrayContract CreateArrayContract(Type type)
        {
            var contract = base.CreateArrayContract(type);
            contract.InterceptSerializeItems = items =>
            {
                var value = (List<string>) items;
                return value.OrderBy(_ => _);
            };
            return contract;
        }
    }

    [Fact]
    public void ReplaceListItem()
    {
        var strings = new List<string>
        {
            "str_1",
            "toReplace",
            "str_3"
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new ReplaceItemContractResolver()
        };
        var json = JsonConvert.SerializeObject(strings, settings);
        Assert.Equal("""["str_1","10","str_3"]""", json);
    }

    class ReplaceItemContractResolver : DefaultContractResolver
    {
        protected override JsonArrayContract CreateArrayContract(Type type)
        {
            var contract = base.CreateArrayContract(type);
            contract.InterceptSerializeItem = item =>
            {
                if (item is "toReplace")
                {
                    return ItemInterceptResult.Replace("10");
                }

                return ItemInterceptResult.Default;
            };

            return contract;
        }
    }

    [Fact]
    public void IgnoreDictionaryItem()
    {
        var strings = new Dictionary<string, string>
        {
            {
                "key1", "value"
            },
            {
                "ignore", "value"
            },
            {
                "key2", "value"
            }
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new IgnoreDictionaryContractResolver()
        };
        var json = JsonConvert.SerializeObject(strings, settings);
        Assert.Equal("""{"key1":"value","key2":"value"}""", json);
    }

    class IgnoreDictionaryContractResolver : DefaultContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type type)
        {
            var contract = base.CreateDictionaryContract(type);
            contract.InterceptSerializeItem = (_, key, _) =>
            {
                if (key is "ignore")
                {
                    return KeyValueInterceptResult.Ignore;
                }

                return KeyValueInterceptResult.Default;
            };
            return contract;
        }
    }

    [Fact]
    public void ReplaceDictionaryItem()
    {
        var strings = new Dictionary<string, string>
        {
            {
                "key1", "value"
            },
            {
                "toReplace", "value"
            },
            {
                "key2", "value"
            }
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new ReplaceDictionaryContractResolver()
        };
        var json = JsonConvert.SerializeObject(strings, settings);
        Assert.Equal("""{"key1":"value","toReplace":"10","key2":"value"}""", json);
    }

    class ReplaceDictionaryContractResolver : DefaultContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type type)
        {
            var contract = base.CreateDictionaryContract(type);
            contract.InterceptSerializeItem = (_, key, _) =>
            {
                if (key is "toReplace")
                {
                    return KeyValueInterceptResult.ReplaceValue("10");
                }

                return KeyValueInterceptResult.Default;
            };

            return contract;
        }
    }

    [Fact]
    public void SortDictionary()
    {
        var target = new Dictionary<string, string>
        {
            {
                "keyD", "value"
            },
            {
                "keyA", "value"
            },
            {
                "keyB", "value"
            }
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new SortDictionaryContractResolver()
        };
        var json = JsonConvert.SerializeObject(target, settings);
        Assert.Equal("""{"keyA":"value","keyB":"value","keyD":"value"}""", json);
    }

#if Release

    [Fact]
    public void SymbolOrdering1()
    {
        var target = new Dictionary<string, int>
        {
            {"#", 1},
            {"@", 2}
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new SortDictionaryContractResolver()
        };
        var json = JsonConvert.SerializeObject(target, settings);
        Assert.Equal(@"{""#"":1,""@"":2}", json);
    }

    [Fact]
    public void SymbolOrdering2()
    {
        var target = new Dictionary<string, int>
        {
            {"@", 2},
            {"#", 1}
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new SortDictionaryContractResolver()
        };
        var json = JsonConvert.SerializeObject(target, settings);
        Assert.Equal(@"{""#"":1,""@"":2}", json);
    }

#endif

    [Fact]
    public void AlreadyOrderedDictionary()
    {
        var target = new SortedDictionary<string, string>(new ReverseComparer())
        {
            {
                "keyD", "value"
            },
            {
                "keyA", "value"
            },
            {
                "keyB", "value"
            }
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new SortDictionaryContractResolver()
        };
        var json = JsonConvert.SerializeObject(target, settings);
        Assert.Equal("""{"keyD":"value","keyB":"value","keyA":"value"}""", json);
    }

    class ReverseComparer : IComparer<string>
    {
        public int Compare(string x, string y) =>
            y.CompareTo(x);
    }

    class SortDictionaryContractResolver : DefaultContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type type)
        {
            var contract = base.CreateDictionaryContract(type);
            contract.OrderByKey = true;
            return contract;
        }
    }

    public class NonComparableKey(string member)
    {
        public override string ToString() =>
            member;

        public override int GetHashCode() =>
            member.GetHashCode();
    }

    [Fact]
    public void DictionaryOrderNonComparable()
    {
        var dictionary = new Dictionary<NonComparableKey, string>
        {
            [new("Foo2")] = "Bar",
            [new("Foo1")] = "Bar"
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new SortDictionaryContractResolver()
        };
        var json = JsonConvert.SerializeObject(dictionary, settings);
        Assert.Equal("""{"Foo2":"Bar","Foo1":"Bar"}""", json);
    }

    [Fact]
    public void ConstructorReadonlyFieldsTest()
    {
        var c1 = new ConstructorReadonlyFields("String!", int.MaxValue);
        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "A": "String!",
              "B": 2147483647
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<ConstructorReadonlyFields>(json);
        Assert.Equal("String!", c2.A);
        Assert.Equal(int.MaxValue, c2.B);
    }

    [Fact]
    public void SerializeStruct()
    {
        var structTest = new StructTest
        {
            StringProperty = "StringProperty!",
            StringField = "StringField",
            IntProperty = 5,
            IntField = 10
        };

        var json = JsonConvert.SerializeObject(structTest, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "StringField": "StringField",
              "IntField": 10,
              "StringProperty": "StringProperty!",
              "IntProperty": 5
            }
            """,
            json);

        var deserialized = JsonConvert.DeserializeObject<StructTest>(json);
        Assert.Equal(structTest.StringProperty, deserialized.StringProperty);
        Assert.Equal(structTest.StringField, deserialized.StringField);
        Assert.Equal(structTest.IntProperty, deserialized.IntProperty);
        Assert.Equal(structTest.IntField, deserialized.IntField);
    }

    [Fact]
    public void SerializeListWithJsonConverter()
    {
        var f = new Foo();
        f.Bars.Add(new()
        {
            Id = 0
        });
        f.Bars.Add(new()
        {
            Id = 1
        });
        f.Bars.Add(new()
        {
            Id = 2
        });

        var json = JsonConvert.SerializeObject(f, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Bars": [
                0,
                1,
                2
              ]
            }
            """,
            json);

        var newFoo = JsonConvert.DeserializeObject<Foo>(json);
        Assert.Equal(3, newFoo.Bars.Count);
        Assert.Equal(0, newFoo.Bars[0].Id);
        Assert.Equal(1, newFoo.Bars[1].Id);
        Assert.Equal(2, newFoo.Bars[2].Id);
    }

    [Fact]
    public void SerializeGuidKeyedDictionary()
    {
        var dictionary = new Dictionary<Guid, int>
        {
            {
                new("F60EAEE0-AE47-488E-B330-59527B742D77"), 1
            },
            {
                new("C2594C02-EBA1-426A-AA87-8DD8871350B0"), 2
            }
        };

        var json = JsonConvert.SerializeObject(dictionary, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "f60eaee0-ae47-488e-b330-59527b742d77": 1,
              "c2594c02-eba1-426a-aa87-8dd8871350b0": 2
            }
            """,
            json);
    }

    [Fact]
    public void SerializePersonKeyedDictionary()
    {
        var dictionary = new Dictionary<Person, int>
        {
            {
                new()
                {
                    Name = "p1"
                },
                1
            },
            {
                new()
                {
                    Name = "p2"
                },
                2
            }
        };

        var json = JsonConvert.SerializeObject(dictionary, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "TestObjects.Person": 1,
              "TestObjects.Person": 2
            }
            """,
            json);
    }

    [Fact]
    public void DeserializePersonKeyedDictionary()
    {
        try
        {
            var json = """
                       {
                         "TestObjects.Person": 1,
                         "TestObjects.Person": 2
                       }
                       """;

            JsonConvert.DeserializeObject<Dictionary<Person, int>>(json);
            Assert.Fail();
        }
        catch (JsonSerializationException exception)
        {
            Assert.StartsWith("Could not convert string 'TestObjects.Person' to dictionary key type 'TestObjects.Person'. Create a TypeConverter to convert from the string to the key type object. Path '['TestObjects.Person']'", exception.Message);
        }
    }

    [Fact]
    public void SerializeFragment()
    {
        var googleSearchText = """
                               {
                                   "responseData": {
                                     "results": [
                                       {
                                         "GsearchResultClass": "GwebSearch",
                                         "unescapedUrl": "http://en.wikipedia.org/wiki/Paris_Hilton",
                                         "url": "http://en.wikipedia.org/wiki/Paris_Hilton",
                                         "visibleUrl": "en.wikipedia.org",
                                         "cacheUrl": "http://www.google.com/search?q=cache:TwrPfhd22hYJ:en.wikipedia.org",
                                         "title": "<b>Paris Hilton</b> - Wikipedia, the free encyclopedia",
                                         "titleNoFormatting": "Paris Hilton - Wikipedia, the free encyclopedia",
                                         "content": "[1] In 2006, she released her debut album..."
                                       },
                                       {
                                         "GsearchResultClass": "GwebSearch",
                                         "unescapedUrl": "http://www.imdb.com/name/nm0385296/",
                                         "url": "http://www.imdb.com/name/nm0385296/",
                                         "visibleUrl": "www.imdb.com",
                                         "cacheUrl": "http://www.google.com/search?q=cache:1i34KkqnsooJ:www.imdb.com",
                                         "title": "<b>Paris Hilton</b>",
                                         "titleNoFormatting": "Paris Hilton",
                                         "content": "Self: Zoolander. Socialite <b>Paris Hilton</b>..."
                                       }
                                     ],
                                     "cursor": {
                                       "pages": [
                                         {
                                           "start": "0",
                                           "label": 1
                                         },
                                         {
                                           "start": "4",
                                           "label": 2
                                         },
                                         {
                                           "start": "8",
                                           "label": 3
                                         },
                                         {
                                           "start": "12",
                                           "label": 4
                                         }
                                       ],
                                       "estimatedResultCount": "59600000",
                                       "currentPageIndex": 0,
                                       "moreResultsUrl": "http://www.google.com/search?oe=utf8&ie=utf8..."
                                     }
                                   },
                                   "responseDetails": null,
                                   "responseStatus": 200
                                 }
                               """;

        var googleSearch = JObject.Parse(googleSearchText);

        // get JSON result objects into a list
        var results = googleSearch["responseData"]["results"].Children().ToList();

        // serialize JSON results into .NET objects
        var searchResults = new List<SearchResult>();
        foreach (var result in results)
        {
            var searchResult = JsonConvert.DeserializeObject<SearchResult>(result.ToString());
            searchResults.Add(searchResult);
        }

        // Title = <b>Paris Hilton</b> - Wikipedia, the free encyclopedia
        // Content = [1] In 2006, she released her debut album...
        // Url = http://en.wikipedia.org/wiki/Paris_Hilton

        // Title = <b>Paris Hilton</b>
        // Content = Self: Zoolander. Socialite <b>Paris Hilton</b>...
        // Url = http://www.imdb.com/name/nm0385296/

        Assert.Equal(2, searchResults.Count);
        Assert.Equal("<b>Paris Hilton</b> - Wikipedia, the free encyclopedia", searchResults[0].Title);
        Assert.Equal("<b>Paris Hilton</b>", searchResults[1].Title);
    }

    [Fact]
    public void DeserializeBaseReferenceWithDerivedValue()
    {
        var personPropertyClass = new PersonPropertyClass();
        var wagePerson = (WagePerson) personPropertyClass.Person;

        wagePerson.BirthDate = new(2000, 11, 29, 23, 59, 59, DateTimeKind.Utc);
        wagePerson.Department = "McDees";
        wagePerson.HourlyWage = 12.50m;
        wagePerson.LastModified = new(2000, 11, 29, 23, 59, 59, DateTimeKind.Utc);
        wagePerson.Name = "Jim Bob";

        var json = JsonConvert.SerializeObject(personPropertyClass, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Person": {
                "HourlyWage": 12.50,
                "Name": "Jim Bob",
                "BirthDate": "2000-11-29T23:59:59Z",
                "LastModified": "2000-11-29T23:59:59Z"
              }
            }
            """,
            json);

        var newPersonPropertyClass = JsonConvert.DeserializeObject<PersonPropertyClass>(json);
        Assert.Equal(wagePerson.HourlyWage, ((WagePerson) newPersonPropertyClass.Person).HourlyWage);
    }

    [Fact]
    public void DeserializePopulateDictionaryAndList()
    {
        var d = JsonConvert.DeserializeObject<ExistingValueClass>("{'Dictionary':{appended:'appended',existing:'new'}}");

        Assert.NotNull(d);
        Assert.NotNull(d.Dictionary);
        Assert.Equal(typeof(Dictionary<string, string>), d.Dictionary.GetType());
        Assert.Equal(typeof(List<string>), d.List.GetType());
        Assert.Equal(2, d.Dictionary.Count);
        Assert.Equal("new", d.Dictionary["existing"]);
        Assert.Equal("appended", d.Dictionary["appended"]);
        Assert.Single(d.List);
        Assert.Equal("existing", d.List[0]);
    }

    [Fact]
    public void IgnoreIndexedProperties()
    {
        var g = new ThisGenericTest<KeyValueId>();

        g.Add(new()
        {
            Id = 1,
            Key = "key1",
            Value = "value1"
        });
        g.Add(new()
        {
            Id = 2,
            Key = "key2",
            Value = "value2"
        });

        g.MyProperty = "some value";

        var json = g.ToJson();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "MyProperty": "some value",
              "TheItems": [
                {
                  "Id": 1,
                  "Key": "key1",
                  "Value": "value1"
                },
                {
                  "Id": 2,
                  "Key": "key2",
                  "Value": "value2"
                }
              ]
            }
            """,
            json);

        var gen = JsonConvert.DeserializeObject<ThisGenericTest<KeyValueId>>(json);
        Assert.Equal("some value", gen.MyProperty);
    }

    [Fact]
    public void JRawValue()
    {
        var deserialized = JsonConvert.DeserializeObject<JRawValueTestObject>("{value:3}");
        Assert.Equal("3", deserialized.Value.ToString());

        deserialized = JsonConvert.DeserializeObject<JRawValueTestObject>("{value:'3'}");
        Assert.Equal(
            """
            "3"
            """,
            deserialized.Value.ToString());
    }

    [Fact]
    public void DeserializeDictionaryWithNoDefaultConstructor()
    {
        var json = "{key1:'value1',key2:'value2',key3:'value3'}";

        var dic = JsonConvert.DeserializeObject<DictionaryWithNoDefaultConstructor>(json);

        Assert.Equal(3, dic.Count);
        Assert.Equal("value1", dic["key1"]);
        Assert.Equal("value2", dic["key2"]);
        Assert.Equal("value3", dic["key3"]);
    }

    [Fact]
    public void DeserializeDictionaryWithNoDefaultConstructor_PreserveReferences()
    {
        var json = "{'$id':'1',key1:'value1',key2:'value2',key3:'value3'}";

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DictionaryWithNoDefaultConstructor>(json, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            MetadataPropertyHandling = MetadataPropertyHandling.Default
        }));
        Assert.Equal("Cannot preserve reference to readonly dictionary, or dictionary created from a non-default constructor: TestObjects.DictionaryWithNoDefaultConstructor. Path 'key1', line 1, position 16.", exception.Message);
    }

    [Fact]
    public void SerializeNonPublicBaseJsonProperties()
    {
        var value = new B();
        var json = JsonConvert.SerializeObject(value, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "B2": null,
              "A1": null,
              "B3": null,
              "A2": null
            }
            """,
            json);
    }

    [Fact]
    public void CircularConstructorDeserialize()
    {
        var c1 = new CircularConstructor1(null)
        {
            StringProperty = "Value!"
        };

        var c2 = new CircularConstructor2(null)
        {
            IntProperty = 1
        };

        c1.C2 = c2;
        c2.C1 = c1;

        var json = JsonConvert.SerializeObject(c1, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "C2": {
                "IntProperty": 1
              },
              "StringProperty": "Value!"
            }
            """,
            json);

        var newC1 = JsonConvert.DeserializeObject<CircularConstructor1>(
            """
            {
              "C2": {
                "IntProperty": 1,
                "C1": {}
              },
              "StringProperty": "Value!"
            }
            """);

        Assert.Equal("Value!", newC1.StringProperty);
        Assert.Equal(1, newC1.C2.IntProperty);
        Assert.Null(newC1.C2.C1.StringProperty);
        Assert.Null(newC1.C2.C1.C2);
    }

    [Fact]
    public void DeserializeToObjectProperty()
    {
        var json = "{ Key: 'abc', Value: 123 }";
        var item = JsonConvert.DeserializeObject<KeyValueTestClass>(json);

        Assert.Equal(123L, item.Value);
    }

    [Fact]
    public void DataContractJsonSerializerTest()
    {
        var c = new DataContractJsonSerializerTestClass
        {
            TimeSpanProperty = new(200, 20, 59, 30, 900),
            GuidProperty = new("66143115-BE2A-4a59-AF0A-348E1EA15B1E"),
            AnimalProperty = new Human
            {
                Ethnicity = "European"
            }
        };
        var ms = new MemoryStream();
        var serializer = new DataContractJsonSerializer(
            typeof(DataContractJsonSerializerTestClass),
            [typeof(Human)]);
        serializer.WriteObject(ms, c);

        var jsonBytes = ms.ToArray();
        var json = Encoding.UTF8.GetString(jsonBytes, 0, jsonBytes.Length);

        //Console.WriteLine(JObject.Parse(json).ToString());
        //Console.WriteLine();

        //Console.WriteLine(JsonConvert.SerializeObject(c, Formatting.Indented, new JsonSerializerSettings
        //  {
        //    //               TypeNameHandling = TypeNameHandling.Objects
        //  }));
    }

    [Fact]
    public void SerializeNonIDictionary()
    {
        var modelStateDictionary = new ModelStateDictionary<string>
        {
            {
                "key", "value"
            }
        };

        var json = JsonConvert.SerializeObject(modelStateDictionary);

        Assert.Equal("""{"key":"value"}""", json);

        var newModelStateDictionary = JsonConvert.DeserializeObject<ModelStateDictionary<string>>(json);
        Assert.Single(newModelStateDictionary);
        Assert.Equal("value", newModelStateDictionary["key"]);
    }


    [Fact]
    public void DeserializeUsingNonDefaultConstructorWithLeftOverValues()
    {
        var kvPairs =
            JsonConvert.DeserializeObject<List<KVPair<string, string>>>(
                "[{\"Key\":\"Two\",\"Value\":\"2\"},{\"Key\":\"One\",\"Value\":\"1\"}]");

        Assert.Equal(2, kvPairs.Count);
        Assert.Equal("Two", kvPairs[0].Key);
        Assert.Equal("2", kvPairs[0].Value);
        Assert.Equal("One", kvPairs[1].Key);
        Assert.Equal("1", kvPairs[1].Value);
    }

    [Fact]
    public void SerializeClassWithInheritedProtectedMember()
    {
        var myA = new AATestClass(2);
        var json = JsonConvert.SerializeObject(myA, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "AA_field1": 2,
              "AA_property1": 2,
              "AA_property2": 2,
              "AA_property3": 2,
              "AA_property4": 2
            }
            """,
            json);

        var myB = new BBTestClass(3, 4);
        json = JsonConvert.SerializeObject(myB, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "BB_field1": 4,
              "BB_field2": 4,
              "AA_field1": 3,
              "BB_property1": 4,
              "BB_property2": 4,
              "BB_property3": 4,
              "BB_property4": 4,
              "BB_property5": 4,
              "BB_property7": 4,
              "AA_property1": 3,
              "AA_property2": 3,
              "AA_property3": 3,
              "AA_property4": 3
            }
            """,
            json);
    }

    [Fact]
    public void SerializeDeserializeXNodeProperties()
    {
        var testObject = new XNodeTestObject
        {
            Document = XDocument.Parse("<root>hehe, root</root>"),
            Element = XElement.Parse("""<fifth xmlns:json="http://json.org" json:Awesome="true">element</fifth>""")
        };

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(testObject, settings);
        var expected = """
                       {
                         "Document": {
                           "root": "hehe, root"
                         },
                         "Element": {
                           "fifth": {
                             "@xmlns:json": "http://json.org",
                             "@json:Awesome": "true",
                             "#text": "element"
                           }
                         }
                       }
                       """;
        XUnitAssert.AreEqualNormalized(expected, json);

        var newTestObject = JsonConvert.DeserializeObject<XNodeTestObject>(json, settings);
        Assert.Equal(testObject.Document.ToString(), newTestObject.Document.ToString());
        Assert.Equal(testObject.Element.ToString(), newTestObject.Element.ToString());

        Assert.Null(newTestObject.Element.Parent);
    }

    [Fact]
    public void SerializeDeserializeXmlNodeProperties()
    {
        var testObject = new XmlNodeTestObject();
        var document = new XmlDocument();
        document.LoadXml("<root>hehe, root</root>");
        testObject.Document = document;

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new XmlNodeConverter());
        var json = JsonConvert.SerializeObject(testObject, settings);
        var expected = """
                       {
                         "Document": {
                           "root": "hehe, root"
                         }
                       }
                       """;
        XUnitAssert.AreEqualNormalized(expected, json);

        var newTestObject = JsonConvert.DeserializeObject<XmlNodeTestObject>(json, settings);
        Assert.Equal(testObject.Document.InnerXml, newTestObject.Document.InnerXml);
    }

    [Fact]
    public void FullClientMapSerialization()
    {
        var source = new ClientMap
        {
            position = new()
            {
                X = 100,
                Y = 200
            },
            center = new()
            {
                X = 251.6,
                Y = 361.3
            }
        };

        var json = JsonConvert.SerializeObject(source, new PosConverter(), new PosDoubleConverter());
        Assert.Equal("{\"position\":new Pos(100,200),\"center\":new PosD(251.6,361.3)}", json);
    }

    [Fact]
    public void SerializeRefAdditionalContent()
    {
        //Additional text found in JSON string after finishing deserializing object.
        //Test 1
        var reference = new Dictionary<string, object>
        {
            {
                "$ref", "Persons"
            },
            {
                "$id", 1
            }
        };

        var child = new Dictionary<string, object>
        {
            {
                "_id", 2
            },
            {
                "Name", "Isabell"
            },
            {
                "Father", reference
            }
        };

        var json = JsonConvert.SerializeObject(child, Formatting.Indented);

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Dictionary<string, object>>(json));
        Assert.Equal("Additional content found in JSON reference object. A JSON reference object should only have a $ref property. Path 'Father.$id', line 6, position 10.", exception.Message);
    }

    [Fact]
    public void SerializeRefBadType()
    {
        //Additional text found in JSON string after finishing deserializing object.
        //Test 1
        var reference = new Dictionary<string, object>
        {
            {
                "$ref", 1
            },
            {
                "$id", 1
            }
        };

        var child = new Dictionary<string, object>
        {
            {
                "_id", 2
            },
            {
                "Name", "Isabell"
            },
            {
                "Father", reference
            }
        };

        var json = JsonConvert.SerializeObject(child, Formatting.Indented);
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Dictionary<string, object>>(json));
        Assert.Equal("JSON reference $ref property must have a string or null value. Path 'Father.$ref', line 5, position 13.", exception.Message);
    }

    [Fact]
    public void SerializeRefNull()
    {
        var reference = new Dictionary<string, object>
        {
            {
                "$ref", null
            },
            {
                "$id", null
            },
            {
                "blah", "blah!"
            }
        };

        var child = new Dictionary<string, object>
        {
            {
                "_id", 2
            },
            {
                "Name", "Isabell"
            },
            {
                "Father", reference
            }
        };

        var json = JsonConvert.SerializeObject(child);

        Assert.Equal(
            """{"_id":2,"Name":"Isabell","Father":{"$ref":null,"$id":null,"blah":"blah!"}}""",
            json);

        var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        Assert.Equal(3, result.Count);
        Assert.Single((JObject) result["Father"]);
        Assert.Equal("blah!", (string) ((JObject) result["Father"])["blah"]);
    }

    [Fact]
    public void DeserializeIgnoredPropertyInConstructor()
    {
        var json = """{"First":"First","Second":2,"Ignored":{"Name":"James"},"AdditionalContent":{"LOL":true}}""";

        var cc = JsonConvert.DeserializeObject<ConstructorComplexIgnoredProperty>(json);
        Assert.Equal("First", cc.First);
        Assert.Equal(2, cc.Second);
        Assert.Null(cc.Ignored);
    }

    [Fact]
    public void DeserializeIgnoredPropertyInConstructorWithoutThrowingMissingMemberError()
    {
        var json = """{"First":"First","Second":2,"Ignored":{"Name":"James"}}""";

        var cc = JsonConvert.DeserializeObject<ConstructorComplexIgnoredProperty>(
            json, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            });
        Assert.Equal("First", cc.First);
        Assert.Equal(2, cc.Second);
        Assert.Null(cc.Ignored);
    }

    [Fact]
    public void DeserializeFloatAsDecimal()
    {
        var json = "{'value':9.9}";

        var dic = JsonConvert.DeserializeObject<IDictionary<string, object>>(
            json, new JsonSerializerSettings
            {
                FloatParseHandling = FloatParseHandling.Decimal
            });

        Assert.Equal(typeof(decimal), dic["value"].GetType());
        Assert.Equal(9.9m, dic["value"]);
    }

    [Fact]
    public void SerializeDeserializeDictionaryKey()
    {
        var dictionary = new Dictionary<DictionaryKey, string>
        {
            {
                new()
                {
                    Value = "First!"
                },
                "First"
            },
            {
                new()
                {
                    Value = "Second!"
                },
                "Second"
            }
        };

        var json = JsonConvert.SerializeObject(dictionary, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "First!": "First",
              "Second!": "Second"
            }
            """,
            json);

        var newDictionary =
            JsonConvert.DeserializeObject<Dictionary<DictionaryKey, string>>(json);

        Assert.Equal(2, newDictionary.Count);
    }

    [Fact]
    public void SerializeNullableArray()
    {
        var jsonText = JsonConvert.SerializeObject(
            new double?[]
            {
                2.4,
                4.3,
                null
            },
            Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            [
              2.4,
              4.3,
              null
            ]
            """,
            jsonText);
    }

    [Fact]
    public void DeserializeNullableArray()
    {
        var d = (double?[]) JsonConvert.DeserializeObject(
            """
            [
              2.4,
              4.3,
              null
            ]
            """,
            typeof(double?[]));

        Assert.Equal(3, d.Length);
        Assert.Equal(2.4, d[0]);
        Assert.Equal(4.3, d[1]);
        Assert.Null(d[2]);
    }

    [Fact]
    public void SerializeHashSet()
    {
        var jsonText = JsonConvert.SerializeObject(
            new HashSet<string>
            {
                "One",
                "2",
                "III"
            },
            Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            [
              "One",
              "2",
              "III"
            ]
            """,
            jsonText);

        var d = JsonConvert.DeserializeObject<HashSet<string>>(jsonText);

        Assert.Equal(3, d.Count);
        Assert.Contains("One", d);
        Assert.Contains("2", d);
        Assert.Contains("III", d);
    }

    [Fact]
    public void DeserializeByteArray()
    {
        var serializer1 = new JsonSerializer();
        serializer1.Converters.Add(new IsoDateTimeConverter());
        serializer1.NullValueHandling = NullValueHandling.Ignore;

        var json = """[{"Prop1":""},{"Prop1":""}]""";

        var reader = new JsonTextReader(new StringReader(json));

        var z = serializer1.Deserialize<ByteArrayTestClass[]>(reader);
        Assert.Equal(2, z.Length);
        Assert.Empty(z[0].Prop1);
        Assert.Empty(z[1].Prop1);
    }

    [Fact]
    public void StringDictionaryTest()
    {
        var classRef = typeof(StringDictionary).FullName;

        var s1 = new StringDictionaryTestClass
        {
            StringDictionaryProperty = new()
            {
                {
                    "1", "One"
                },
                {
                    "2", "II"
                },
                {
                    "3", "3"
                }
            }
        };

        var json = JsonConvert.SerializeObject(s1, Formatting.Indented);

        // .NET 4.5.3 added IDictionary<string, string> to StringDictionary
        if (s1.StringDictionaryProperty is IDictionary<string, string>)
        {
            var d = JsonConvert.DeserializeObject<StringDictionaryTestClass>(json);

            Assert.Equal(3, d.StringDictionaryProperty.Count);
            Assert.Equal("One", d.StringDictionaryProperty["1"]);
            Assert.Equal("II", d.StringDictionaryProperty["2"]);
            Assert.Equal("3", d.StringDictionaryProperty["3"]);
        }
        else
        {
            var messages = $"Cannot create and populate list type {classRef}. Path 'StringDictionaryProperty', line 2, position 31.";
            var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<StringDictionaryTestClass>(json));
            Assert.Equal(messages, exception.Message);
        }
    }

    [Fact]
    public void SerializeStructWithJsonObjectAttribute()
    {
        var testStruct = new StructWithAttribute
        {
            MyInt = int.MaxValue
        };

        var json = JsonConvert.SerializeObject(testStruct, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "MyInt": 2147483647
            }
            """,
            json);

        var newStruct = JsonConvert.DeserializeObject<StructWithAttribute>(json);

        Assert.Equal(int.MaxValue, newStruct.MyInt);
    }

    [Fact]
    public void ReadWriteTimeZoneOffsetIso()
    {
        var serializeObject = JsonConvert.SerializeObject(new TimeZoneOffsetObject
        {
            Offset = new(new DateTime(2000, 1, 1), TimeSpan.FromHours(6))
        });

        Assert.Equal("{\"Offset\":\"2000-01-01T00:00:00+06:00\"}", serializeObject);

        var reader = new JsonTextReader(new StringReader(serializeObject));
        var serializer = new JsonSerializer();

        var deserializeObject = serializer.Deserialize<TimeZoneOffsetObject>(reader);

        Assert.Equal(TimeSpan.FromHours(6), deserializeObject.Offset.Offset);
        Assert.Equal(new(2000, 1, 1), deserializeObject.Offset.Date);
    }

    [Fact]
    public void DeserializePropertyNullableDateTimeOffsetExactIso()
    {
        var d = JsonConvert.DeserializeObject<NullableDateTimeTestClass>("{\"DateTimeOffsetField\":\"2000-01-01T00:00:00+06:00\"}");
        Assert.Equal(new DateTimeOffset(new DateTime(2000, 1, 1), TimeSpan.FromHours(6)), d.DateTimeOffsetField);
    }

    [Fact]
    public void OverridenPropertyMembers()
    {
        var json = JsonConvert.SerializeObject(new DerivedEvent(), Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "event": "derived"
            }
            """,
            json);
    }

    [Fact]
    public void SerializeExpandoObject()
    {
        dynamic expando = new ExpandoObject();
        expando.Int = 1;
        expando.Decimal = 99.9d;
        expando.Complex = new ExpandoObject();
        expando.Complex.String = "I am a string";
        expando.Complex.DateTime = new DateTime(2000, 12, 20, 18, 55, 0, DateTimeKind.Utc);

        string json = JsonConvert.SerializeObject(expando, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Int": 1,
              "Decimal": 99.9,
              "Complex": {
                "String": "I am a string",
                "DateTime": "2000-12-20T18:55:00Z"
              }
            }
            """,
            json);

        IDictionary<string, object> newExpando = JsonConvert.DeserializeObject<ExpandoObject>(json);

        Assert.IsType<long>(newExpando["Int"]);
        Assert.Equal((long) expando.Int, newExpando["Int"]);

        Assert.IsType<double>(newExpando["Decimal"]);
        Assert.Equal(expando.Decimal, newExpando["Decimal"]);

        Assert.IsType<ExpandoObject>(newExpando["Complex"]);
        IDictionary<string, object> o = (ExpandoObject) newExpando["Complex"];

        Assert.IsType<string>(o["String"]);
        Assert.Equal(expando.Complex.String, o["String"]);

        Assert.IsType<string>(o["DateTime"]);
        Assert.Equal("2000-12-20T18:55:00Z", o["DateTime"]);
    }

    [Fact]
    public void Test_Deserialize_Negative()
    {
        var d = JsonConvert.DeserializeObject<decimal>("-0.0");

        Assert.Equal("0.0", d.ToString());
    }

    [Fact]
    public void Test_Deserialize_NegativeNoTrailingZero()
    {
        var d = JsonConvert.DeserializeObject<decimal>("-0");

        Assert.Equal("0", d.ToString());
    }

    [Fact]
    public void ParseJsonDecimal()
    {
        var json = """{ "property": 0.0 }""";
        var reader = new JsonTextReader(new StringReader(json))
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };

        decimal? parsedValue = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.Float)
            {
                parsedValue = (decimal) reader.Value;
                break;
            }
        }

        Assert.Equal("0.0", parsedValue.ToString());
    }

    [Fact]
    public void Test_Deserialize_Double_Negative()
    {
        var d = JsonConvert.DeserializeObject<double>("-0.0");

#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal("-0", d.ToString());
#else
        Assert.Equal("0", d.ToString());
#endif
    }

    [Fact]
    public void Test_Deserialize_Double_NegativeNoTrailingZero()
    {
        var d = JsonConvert.DeserializeObject<double>("-0");

#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal("-0", d.ToString());
#else
        Assert.Equal("0", d.ToString());
#endif
    }

    [Fact]
    public void JValueDouble_ToString()
    {
        var d = new JValue(-0.0d);

#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal("-0", d.ToString());
#else
            Assert.Equal("0", d.ToString());
#endif
    }

    [Fact]
    public void DeserializeDecimalExact()
    {
        var d = JsonConvert.DeserializeObject<decimal>("123456789876543.21");
        Assert.Equal(123456789876543.21m, d);
    }

    [Fact]
    public void DeserializeNullableDecimalExact()
    {
        var d = JsonConvert.DeserializeObject<decimal?>("123456789876543.21");
        Assert.Equal(123456789876543.21m, d);
    }

    [Fact]
    public void DeserializeDecimalPropertyExact()
    {
        var json = "{Amount:123456789876543.21}";
        var reader = new JsonTextReader(new StringReader(json))
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };

        var serializer = new JsonSerializer();

        var i = serializer.Deserialize<Invoice>(reader);
        Assert.Equal(123456789876543.21m, i.Amount);
    }

    [Fact]
    public void DeserializeDecimalArrayExact()
    {
        var json = "[123456789876543.21]";
        var a = JsonConvert.DeserializeObject<IList<decimal>>(json);
        Assert.Equal(123456789876543.21m, a[0]);
    }

    [Fact]
    public void DeserializeDecimalDictionaryExact()
    {
        var json = "{'Value':123456789876543.21}";
        var reader = new JsonTextReader(new StringReader(json))
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };

        var serializer = new JsonSerializer();

        var d = serializer.Deserialize<IDictionary<string, decimal>>(reader);
        Assert.Equal(123456789876543.21m, d["Value"]);
    }

    [Fact]
    public void DeserializeStructProperty()
    {
        var obj = new VectorParent
        {
            Position = new()
            {
                X = 1,
                Y = 2,
                Z = 3
            }
        };

        var str = JsonConvert.SerializeObject(obj);

        obj = JsonConvert.DeserializeObject<VectorParent>(str);

        Assert.Equal(1, obj.Position.X);
        Assert.Equal(2, obj.Position.Y);
        Assert.Equal(3, obj.Position.Z);
    }

    [Fact]
    public void PrivateSetterOnBaseClassProperty()
    {
        var derived = new PrivateSetterDerived("meh", "woo");

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        var json = JsonConvert.SerializeObject(derived, Formatting.Indented, settings);

        var meh = JsonConvert.DeserializeObject<PrivateSetterBase>(json, settings);

        Assert.Equal("woo", ((PrivateSetterDerived) meh).IDoWork);
        Assert.Equal("meh", meh.IDontWork);
    }

    [Fact]
    public void DeserializeJToken()
    {
        var c = new JTokenTestClass
        {
            Name = "Success",
            Data = new JObject(new JProperty("First", "Value1"), new JProperty("Second", "Value2"))
        };

        var json = JsonConvert.SerializeObject(c, Formatting.Indented);

        var deserializedResponse = JsonConvert.DeserializeObject<JTokenTestClass>(json);

        Assert.Equal("Success", deserializedResponse.Name);
        Assert.True(deserializedResponse.Data.DeepEquals(c.Data));
    }

    [Fact]
    public void DeserializeMinValueDecimal()
    {
        var data = new DecimalTest(decimal.MinValue);
        var json = JsonConvert.SerializeObject(data);
        var obj = JsonConvert.DeserializeObject<DecimalTest>(json, new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Default
        });

        Assert.Equal(decimal.MinValue, obj.Value);
    }

    [Fact]
    public void NonPublicConstructorWithJsonConstructorTest()
    {
        var c = JsonConvert.DeserializeObject<NonPublicConstructorWithJsonConstructor>("{}");
        Assert.Equal("NonPublic", c.Constructor);
    }

    [Fact]
    public void PublicConstructorOverridenByJsonConstructorTest()
    {
        var c = JsonConvert.DeserializeObject<PublicConstructorOverridenByJsonConstructor>("{Value:'value!'}");
        Assert.Equal("Public Parameterized", c.Constructor);
        Assert.Equal("value!", c.Value);
    }

    [Fact]
    public void MultipleParametrizedConstructorsJsonConstructorTest()
    {
        var c = JsonConvert.DeserializeObject<MultipleParametrizedConstructorsJsonConstructor>("{Value:'value!', Age:1}");
        Assert.Equal("Public Parameterized 2", c.Constructor);
        Assert.Equal("value!", c.Value);
        Assert.Equal(1, c.Age);
    }

    [Fact]
    public void DeserializeEnumerable()
    {
        var c = new EnumerableClass
        {
            Enumerable =
            [
                "One",
                "Two",
                "Three"
            ]
        };

        var json = JsonConvert.SerializeObject(c, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Enumerable": [
                "One",
                "Two",
                "Three"
              ]
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<EnumerableClass>(json);

        Assert.Equal("One", c2.Enumerable.ElementAt(0));
        Assert.Equal("Two", c2.Enumerable.ElementAt(1));
        Assert.Equal("Three", c2.Enumerable.ElementAt(2));
    }

    [Fact]
    public void SerializeAttributesOnBase()
    {
        var i = new ComplexItem();

        var json = JsonConvert.SerializeObject(i, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": null
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeStringEnglish()
    {
        var json = """
                   {
                     'Name': 'James Hughes',
                     'Age': '40',
                     'Height': '44.4',
                     'Price': '4'
                   }
                   """;

        var p = JsonConvert.DeserializeObject<DeserializeStringConvert>(json);
        Assert.Equal(40, p.Age);
        Assert.Equal(44.4, p.Height);
        Assert.Equal(4m, p.Price);
    }

    [Fact]
    public void DeserializeNullDateTimeValueTest()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject("null", typeof(DateTime)));
        Assert.Equal("Error converting value {null} to type 'System.DateTime'. Path '', line 1, position 4.", exception.Message);
    }

    [Fact]
    public void DeserializeNullNullableDateTimeValueTest()
    {
        var dateTime = JsonConvert.TryDeserializeObject("null", typeof(DateTime?));

        Assert.Null(dateTime);
    }

    [Fact]
    public void MultiIndexSuperTest()
    {
        var e = new MultiIndexSuper();

        var json = JsonConvert.SerializeObject(e, Formatting.Indented);

        Assert.Equal("{}", json);
    }

    [Fact]
    public void CommentTestClassTest()
    {
        var json = """
                   {"indexed":true, "startYear":1939, "values":
                       [  3000,  /* 1940-1949 */
                          3000,   3600,   3600,   3600,   3600,   4200,   4200,   4200,   4200,   4800,  /* 1950-1959 */
                          4800,   4800,   4800,   4800,   4800,   4800,   6600,   6600,   7800,   7800,  /* 1960-1969 */
                          7800,   7800,   9000,  10800,  13200,  14100,  15300,  16500,  17700,  22900,  /* 1970-1979 */
                         25900,  29700,  32400,  35700,  37800,  39600,  42000,  43800,  45000,  48000,  /* 1980-1989 */
                         51300,  53400,  55500,  57600,  60600,  61200,  62700,  65400,  68400,  72600,  /* 1990-1999 */
                         76200,  80400,  84900,  87000,  87900,  90000,  94200,  97500, 102000, 106800,  /* 2000-2009 */
                        106800, 106800]  /* 2010-2011 */
                   }
                   """;

        var commentTestClass = JsonConvert.DeserializeObject<CommentTestClass>(json);

        Assert.True(commentTestClass.Indexed);
        Assert.Equal(1939, commentTestClass.StartYear);
        Assert.Equal(63, commentTestClass.Values.Count);
    }

    [Fact]
    public void PopulationBehaviourForOmittedPropertiesIsTheSameForParameterisedConstructorAsForDefaultConstructor()
    {
        var json = """{A:"Test"}""";

        var withoutParameterisedConstructor = JsonConvert.DeserializeObject<DTOWithoutParameterisedConstructor>(json);
        var withParameterisedConstructor = JsonConvert.DeserializeObject<DTOWithParameterisedConstructor>(json);
        Assert.Equal(withoutParameterisedConstructor.B, withParameterisedConstructor.B);
    }

    [Fact]
    public void SkipPopulatingArrayPropertyClass()
    {
        var json = JsonConvert.SerializeObject(new EnumerableArrayPropertyClass());
        JsonConvert.DeserializeObject<EnumerableArrayPropertyClass>(json);
    }

    [Fact]
    public void ChildObjectTest()
    {
        var cc = new VirtualOverrideNewChildObject
        {
            VirtualMember = "VirtualMember!",
            NonVirtualMember = "NonVirtualMember!"
        };

        var result = JsonConvert.SerializeObject(cc);
        Assert.Equal(
            """{"virtualMember":"VirtualMember!","nonVirtualMember":"NonVirtualMember!"}""",
            result);
    }

    [Fact]
    public void ChildWithDifferentOverrideObjectTest()
    {
        var cc = new VirtualOverrideNewChildWithDifferentOverrideObject
        {
            VirtualMember = "VirtualMember!",
            NonVirtualMember = "NonVirtualMember!"
        };

        var result = JsonConvert.SerializeObject(cc);
        Assert.Equal(
            """{"differentVirtualMember":"VirtualMember!","nonVirtualMember":"NonVirtualMember!"}""",
            result);
    }

    [Fact]
    public void ImplementInterfaceObjectTest()
    {
        var cc = new ImplementInterfaceObject
        {
            InterfaceMember = new(2010, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            NewMember = "NewMember!"
        };

        var result = JsonConvert.SerializeObject(cc, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "virtualMember": "2010-12-31T00:00:00Z",
              "newMemberWithProperty": null
            }
            """,
            result);
    }

    [Fact]
    public void NonDefaultConstructorWithReadOnlyCollectionPropertyTest()
    {
        var c1 = new NonDefaultConstructorWithReadOnlyCollectionProperty("blah");
        c1.Categories.Add("one");
        c1.Categories.Add("two");

        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Title": "blah",
              "Categories": [
                "one",
                "two"
              ]
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<NonDefaultConstructorWithReadOnlyCollectionProperty>(json);
        Assert.Equal(c1.Title, c2.Title);
        Assert.Equal(c1.Categories.Count, c2.Categories.Count);
        Assert.Equal("one", c2.Categories[0]);
        Assert.Equal("two", c2.Categories[1]);
    }

    [Fact]
    public void NonDefaultConstructorWithReadOnlyDictionaryPropertyTest()
    {
        var c1 = new NonDefaultConstructorWithReadOnlyDictionaryProperty("blah");
        c1.Categories.Add("one", 1);
        c1.Categories.Add("two", 2);

        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Title": "blah",
              "Categories": {
                "one": 1,
                "two": 2
              }
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<NonDefaultConstructorWithReadOnlyDictionaryProperty>(json);
        Assert.Equal(c1.Title, c2.Title);
        Assert.Equal(c1.Categories.Count, c2.Categories.Count);
        Assert.Equal(1, c2.Categories["one"]);
        Assert.Equal(2, c2.Categories["two"]);
    }

    [Fact]
    public void ClassAttributesInheritance()
    {
        var json = JsonConvert.SerializeObject(
            new ClassAttributeDerived
            {
                BaseClassValue = "BaseClassValue!",
                DerivedClassValue = "DerivedClassValue!",
                NonSerialized = "NonSerialized!"
            },
            Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "DerivedClassValue": "DerivedClassValue!",
              "BaseClassValue": "BaseClassValue!"
            }
            """,
            json);

        json = JsonConvert.SerializeObject(
            new CollectionClassAttributeDerived
            {
                BaseClassValue = "BaseClassValue!",
                CollectionDerivedClassValue = "CollectionDerivedClassValue!"
            },
            Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "CollectionDerivedClassValue": "CollectionDerivedClassValue!",
              "BaseClassValue": "BaseClassValue!"
            }
            """,
            json);
    }

    [Fact]
    public void PrivateMembersClassWithAttributesTest()
    {
        var c1 = new PrivateMembersClassWithAttributes("privateString!", "internalString!", "readonlyString!");

        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "_privateString": "privateString!",
              "_readonlyString": "readonlyString!",
              "_internalString": "internalString!"
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<PrivateMembersClassWithAttributes>(json);
        Assert.Equal("readonlyString!", c2.UseValue());
    }

    [Fact]
    public void DeserializeGenericEnumerableProperty()
    {
        var r = JsonConvert.DeserializeObject<BusRun>("{'Departures':['2013-08-14T04:38:31.000+0000','2013-08-14T04:38:31.000+0000',null],'WheelchairAccessible':true}");

        Assert.Equal(typeof(List<DateTime?>), r.Departures.GetType());
        Assert.Equal(3, r.Departures.Count());
        Assert.NotNull(r.Departures.ElementAt(0));
        Assert.NotNull(r.Departures.ElementAt(1));
        Assert.Null(r.Departures.ElementAt(2));
    }

    [Fact]
    public void JsonPropertyDataMemberOrder()
    {
        var d = new DerivedType();
        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "dinosaur": null,
              "dog": null,
              "cat": null,
              "zebra": null,
              "bird": null,
              "parrot": null,
              "albatross": null,
              "antelope": null
            }
            """,
            json);
    }

    public class CustomClass
    {
        [Required]
        public Guid? clientId { get; set; }
    }

    [Fact]
    public void DeserializeStringIntoNullableGuid()
    {
        var json = "{ 'clientId': 'bb2f3da7-bf79-4d14-9d54-0a1f7ff5f902' }";

        var c = JsonConvert.DeserializeObject<CustomClass>(json);

        Assert.Equal(new Guid("bb2f3da7-bf79-4d14-9d54-0a1f7ff5f902"), c.clientId);
    }

    [Fact]
    public void SerializeException1()
    {
        var classWithException = new ClassWithException();
        try
        {
            throw new("Test Exception");
        }
        catch (Exception exception)
        {
            classWithException.Exceptions.Add(exception);
        }

        var sex = JsonConvert.SerializeObject(classWithException);
        var dex = JsonConvert.DeserializeObject<ClassWithException>(sex);
        Assert.Equal(dex.Exceptions[0].ToString(), dex.Exceptions[0].ToString());

        sex = JsonConvert.SerializeObject(classWithException, Formatting.Indented);

        dex = JsonConvert.DeserializeObject<ClassWithException>(sex); // this fails!
        Assert.Equal(dex.Exceptions[0].ToString(), dex.Exceptions[0].ToString());
    }

    [Fact]
    public void UriGuidTimeSpanTestClassEmptyTest()
    {
        var c1 = new UriGuidTimeSpanTestClass();
        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Guid": "00000000-0000-0000-0000-000000000000",
              "NullableGuid": null,
              "TimeSpan": "00:00:00",
              "NullableTimeSpan": null,
              "Uri": null
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<UriGuidTimeSpanTestClass>(json);
        Assert.Equal(c1.Guid, c2.Guid);
        Assert.Equal(c1.NullableGuid, c2.NullableGuid);
        Assert.Equal(c1.TimeSpan, c2.TimeSpan);
        Assert.Equal(c1.NullableTimeSpan, c2.NullableTimeSpan);
        Assert.Equal(c1.Uri, c2.Uri);
    }

    [Fact]
    public void UriGuidTimeSpanTestClassValuesTest()
    {
        var c1 = new UriGuidTimeSpanTestClass
        {
            Guid = new("1924129C-F7E0-40F3-9607-9939C531395A"),
            NullableGuid = new Guid("9E9F3ADF-E017-4F72-91E0-617EBE85967D"),
            TimeSpan = TimeSpan.FromDays(1),
            NullableTimeSpan = TimeSpan.FromHours(1),
            Uri = new("http://testuri.com")
        };
        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Guid": "1924129c-f7e0-40f3-9607-9939c531395a",
              "NullableGuid": "9e9f3adf-e017-4f72-91e0-617ebe85967d",
              "TimeSpan": "1.00:00:00",
              "NullableTimeSpan": "01:00:00",
              "Uri": "http://testuri.com"
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<UriGuidTimeSpanTestClass>(json);
        Assert.Equal(c1.Guid, c2.Guid);
        Assert.Equal(c1.NullableGuid, c2.NullableGuid);
        Assert.Equal(c1.TimeSpan, c2.TimeSpan);
        Assert.Equal(c1.NullableTimeSpan, c2.NullableTimeSpan);
        Assert.Equal(c1.Uri, c2.Uri);
    }

    [Fact]
    public void UsingJsonTextWriter()
    {
        // The property of the object has to be a number for the cast exception to occure
        object o = new
        {
            p = 1
        };

        var json = JObject.FromObject(o);

        using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteToken(json.CreateReader());
        jsonWriter.Flush();

        var result = stringWriter.ToString();
        Assert.Equal("""{"p":1}""", result);
    }

    [Fact]
    public void SerializeUriWithQuotes()
    {
        var input = "http://test.com/%22foo+bar%22";
        var uri = new Uri(input);
        var json = JsonConvert.SerializeObject(uri);
        var output = JsonConvert.DeserializeObject<Uri>(json);

        Assert.Equal(uri, output);
    }

    [Fact]
    public void SerializeUriWithSlashes()
    {
        var input = @"http://tes/?a=b\\c&d=e\";
        var uri = new Uri(input);
        var json = JsonConvert.SerializeObject(uri);
        var output = JsonConvert.DeserializeObject<Uri>(json);

        Assert.Equal(uri, output);
    }

    [Fact]
    public void DeserializeByteArrayWithTypeNameHandling()
    {
        var test = new TestObject("Test", "H?>G\\7"u8.ToArray());

        var serializer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.All
        };

        byte[] objectBytes;
        using (var stream = new MemoryStream())
        using (var jsonWriter = new JsonTextWriter(new StreamWriter(stream)))
        {
            serializer.Serialize(jsonWriter, test);
            jsonWriter.Flush();

            objectBytes = stream.ToArray();
        }

        using (var stream = new MemoryStream(objectBytes))
        using (var jsonReader = new JsonTextReader(new StreamReader(stream)))
        {
            // Get exception here
            var newObject = (TestObject) serializer.Deserialize(jsonReader);

            Assert.Equal("Test", newObject.Name);
            Assert.Equal("H?>G\\7"u8.ToArray(), newObject.Data);
        }
    }

    [Fact]
    public void ReadForTypeHackFixDecimal()
    {
        var d1 = new List<decimal>
        {
            1.1m
        };

        var json = JsonConvert.SerializeObject(d1);

        var d2 = JsonConvert.DeserializeObject<IList<decimal>>(json);

        Assert.Equal(d1.Count, d2.Count);
        Assert.Equal(d1[0], d2[0]);
    }

    [Fact]
    public void ReadForTypeHackFixDateTimeOffset()
    {
        var d1 = new List<DateTimeOffset?>
        {
            null
        };

        var json = JsonConvert.SerializeObject(d1);

        var d2 = JsonConvert.DeserializeObject<IList<DateTimeOffset?>>(json);

        Assert.Equal(d1.Count, d2.Count);
        Assert.Equal(d1[0], d2[0]);
    }

    [Fact]
    public void ReadForTypeHackFixByteArray()
    {
        var d1 = new List<byte[]>
        {
            null
        };

        var json = JsonConvert.SerializeObject(d1);

        var d2 = JsonConvert.DeserializeObject<IList<byte[]>>(json);

        Assert.Equal(d1.Count, d2.Count);
        Assert.Equal(d1[0], d2[0]);
    }

    [Fact]
    public void SerializeInheritanceHierarchyWithDuplicateProperty()
    {
        var b = new Bb
        {
            no = true
        };
        Aa a = b;
        a.no = int.MaxValue;

        var json = JsonConvert.SerializeObject(b);

        Assert.Equal("""{"no":true}""", json);

        var b2 = JsonConvert.DeserializeObject<Bb>(json);

        Assert.True(b2.no);
    }

    [Fact]
    public void DeserializeNullInt()
    {
        var json = """
                   [
                     1,
                     2,
                     3,
                     null
                   ]
                   """;

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<List<int>>(json));
        Assert.Equal("Error converting value {null} to type 'System.Int32'. Path '[3]', line 5, position 6.", exception.Message);
    }

    [Fact]
    public void SerializeIConvertible()
    {
        var c = new ConvertibleIntTestClass
        {
            Integer = new(1),
            NullableInteger1 = new ConvertibleInt(2),
            NullableInteger2 = null
        };

        var json = JsonConvert.SerializeObject(c, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Integer": 1,
              "NullableInteger1": 2,
              "NullableInteger2": null
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeIConvertible()
    {
        var json = """
                   {
                     "Integer": 1,
                     "NullableInteger1": 2,
                     "NullableInteger2": null
                   }
                   """;

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<ConvertibleIntTestClass>(json));
        Assert.Equal("Error converting value 1 to type 'TestObjects.ConvertibleInt'. Path 'Integer', line 2, position 14.", exception.Message);
    }

    [Fact]
    public void SerializeNullableWidgetStruct()
    {
        var widget = new Widget
        {
            Id = new WidgetId
            {
                Value = "id"
            }
        };

        var json = JsonConvert.SerializeObject(widget);

        Assert.Equal("""{"Id":{"Value":"id"}}""", json);
    }

    [Fact]
    public void DeserializeNullableWidgetStruct()
    {
        var json = """{"Id":{"Value":"id"}}""";

        var w = JsonConvert.DeserializeObject<Widget>(json);

        Assert.Equal(new WidgetId
        {
            Value = "id"
        }, w.Id);
        Assert.Equal(new()
        {
            Value = "id"
        }, w.Id.Value);
        Assert.Equal("id", w.Id.Value.Value);
    }

    [Fact]
    public void DeserializeBoolInt()
    {
        var json = """
                   {
                     "PreProperty": true,
                     "PostProperty": "-1"
                   }
                   """;

        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<MyClass>(json));
        Assert.Equal("Unexpected character encountered while parsing value: t. Path 'PreProperty', line 2, position 18.", exception.Message);
    }

    [Fact]
    public void DeserializeUnexpectedEndInt()
    {
        var json = """
                   {
                     "PreProperty":
                   """;

        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<MyClass>(json));
    }

    [Fact]
    public void DeserializeNullableGuid()
    {
        var json = """{"Id":null}""";
        var c = JsonConvert.DeserializeObject<NullableGuid>(json);

        Assert.Null(c.Id);

        json = """{"Id":"d8220a4b-75b1-4b7a-8112-b7bdae956a45"}""";
        c = JsonConvert.DeserializeObject<NullableGuid>(json);

        Assert.Equal(new Guid("d8220a4b-75b1-4b7a-8112-b7bdae956a45"), c.Id);
    }

    [Fact]
    public void SerializeNullableGuidCustomWriterOverridesNullableGuid()
    {
        var ng = new NullableGuid
        {
            Id = Guid.Empty
        };
        var writer = new NullableGuidCountingJsonTextWriter(new StreamWriter(Stream.Null));
        var serializer = JsonSerializer.Create();
        serializer.Serialize(writer, ng);
        Assert.Equal(1, writer.NullableGuidCount);
        serializer.Serialize(writer, ng);
        Assert.Equal(2, writer.NullableGuidCount);
    }

    [Fact]
    public void DeserializeGuid()
    {
        var expected = new Item
        {
            SourceTypeID = new("d8220a4b-75b1-4b7a-8112-b7bdae956a45"),
            BrokerID = new("951663c4-924e-4c86-a57a-7ed737501dbd"),
            Latitude = 33.657145,
            Longitude = -117.766684,
            TimeStamp = new(2000, 3, 1, 23, 59, 59, DateTimeKind.Utc),
            Payload = new byte[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9
            }
        };

        var jsonString = JsonConvert.SerializeObject(expected, Formatting.Indented);

        XUnitAssert.AreEqualNormalized($$"""
                                         {
                                           "SourceTypeID": "d8220a4b-75b1-4b7a-8112-b7bdae956a45",
                                           "BrokerID": "951663c4-924e-4c86-a57a-7ed737501dbd",
                                           "Latitude": 33.657145,
                                           "Longitude": -117.766684,
                                           "TimeStamp": "2000-03-01T23:59:59Z",
                                           "Payload": {
                                             "$type": "{{typeof(byte[]).GetTypeName(0, DefaultSerializationBinder.Instance)}}",
                                             "$value": "AAECAwQFBgcICQ=="
                                           }
                                         }
                                         """,
            jsonString);

        var actual = JsonConvert.DeserializeObject<Item>(jsonString);

        Assert.Equal(new("d8220a4b-75b1-4b7a-8112-b7bdae956a45"), actual.SourceTypeID);
        Assert.Equal(new("951663c4-924e-4c86-a57a-7ed737501dbd"), actual.BrokerID);
        var bytes = (byte[]) actual.Payload;
        Assert.Equal(new byte[]
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9
        }.ToList(), bytes.ToList());
    }

    [Fact]
    public void DeserializeObjectDictionary()
    {
        var serializer = JsonSerializer.Create(new());
        var dict = serializer.Deserialize<Dictionary<string, string>>(new JsonTextReader(new StringReader("{'k1':'','k2':'v2'}")));

        Assert.Equal("", dict["k1"]);
        Assert.Equal("v2", dict["k2"]);
    }

    [Fact]
    public void DeserializeNullableEnum()
    {
        var json = JsonConvert.SerializeObject(new WithEnums
        {
            Id = 7,
            NullableEnum = null
        });

        Assert.Equal("""{"Id":7,"NullableEnum":null}""", json);

        var e = JsonConvert.DeserializeObject<WithEnums>(json);

        Assert.Null(e.NullableEnum);

        json = JsonConvert.SerializeObject(new WithEnums
        {
            Id = 7,
            NullableEnum = MyEnum.Value2
        });

        Assert.Equal("""{"Id":7,"NullableEnum":1}""", json);

        e = JsonConvert.DeserializeObject<WithEnums>(json);

        Assert.Equal(MyEnum.Value2, e.NullableEnum);
    }

    [Fact]
    public void NullableStructWithConverter()
    {
        var json = JsonConvert.SerializeObject(new Widget1
        {
            Id = new WidgetId1
            {
                Value = 1234
            }
        });

        Assert.Equal("""{"Id":"1234"}""", json);

        var w = JsonConvert.DeserializeObject<Widget1>("""{"Id":"1234"}""");

        Assert.Equal(new WidgetId1
        {
            Value = 1234
        }, w.Id);
    }

    [Fact]
    public void SerializeDictionaryStringStringAndStringObject()
    {
        var serializer = JsonSerializer.Create(new());
        var dict = serializer.Deserialize<Dictionary<string, string>>(new JsonTextReader(new StringReader("{'k1':'','k2':'v2'}")));

        var reader = new JsonTextReader(new StringReader("{'k1':'','k2':'v2'}"));
        var dict2 = serializer.Deserialize<Dictionary<string, object>>(reader);

        Assert.Equal(dict["k1"], dict2["k1"]);
    }

    [Fact]
    public void DeserializeEmptyStrings()
    {
        var expected = "Expected the input to start with a valid JSON token.";
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<object>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<StringComparison?>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<StringComparison>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<char?>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<char>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<int?>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<int>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<double?>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<decimal?>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<decimal>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DateTime?>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DateTime>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DateTimeOffset?>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DateTimeOffset>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<byte[]>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<double?>(""));
        Assert.StartsWith(expected, exception.Message);

        exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<double>(""));
        Assert.StartsWith(expected, exception.Message);
    }

    [Fact]
    public void DeserializeIsoDatesWithIsoConverter()
    {
        var jsonIsoText =
            """{"Value":"2012-02-25T19:55:50.6095676+13:00"}""";

        var c = JsonConvert.DeserializeObject<DateTimeWrapper>(jsonIsoText, new IsoDateTimeConverter());
        Assert.Equal(DateTimeKind.Local, c.Value.Kind);
    }

    [Fact]
    public void PrivateConstructor()
    {
        var person = PersonWithPrivateConstructor.CreatePerson();
        person.Name = "John Doe";
        person.Age = 25;

        var serializedPerson = JsonConvert.SerializeObject(person);
        var roundtrippedPerson = JsonConvert.DeserializeObject<PersonWithPrivateConstructor>(serializedPerson);

        Assert.Equal(person.Name, roundtrippedPerson.Name);
    }

#if !NET6_0_OR_GREATER
    [Fact]
    public void MetroBlogPost()
    {
        var product = new Product
        {
            Name = "Apple",
            ExpiryDate = new(2012, 4, 1),
            Price = 3.99M,
            Sizes = ["Small", "Medium", "Large"]
        };

        var json = JsonConvert.SerializeObject(product);
        //{
        //  "Name": "Apple",
        //  "ExpiryDate": "2012-04-01T00:00:00",
        //  "Price": 3.99,
        //  "Sizes": [ "Small", "Medium", "Large" ]
        //}

        var metroJson = JsonConvert.SerializeObject(product, new JsonSerializerSettings
        {
            ContractResolver = new MetroPropertyNameResolver(),
            Converters = {new MetroStringConverter()},
            Formatting = Formatting.Indented
        });
        XUnitAssert.AreEqualNormalized(
            """
            {
              ":::NAME:::": ":::APPLE:::",
              ":::EXPIRYDATE:::": "2012-04-01T00:00:00",
              ":::PRICE:::": 3.99,
              ":::SIZES:::": [
                ":::SMALL:::",
                ":::MEDIUM:::",
                ":::LARGE:::"
              ]
            }
            """,
            metroJson);
        //{
        //  ":::NAME:::": ":::APPLE:::",
        //  ":::EXPIRYDATE:::": "2012-04-01T00:00:00",
        //  ":::PRICE:::": 3.99,
        //  ":::SIZES:::": [ ":::SMALL:::", ":::MEDIUM:::", ":::LARGE:::" ]
        //}

        var colors = new[] {Color.Blue, Color.Red, Color.Yellow, Color.Green, Color.Black, Color.Brown};

        var json2 = JsonConvert.SerializeObject(colors, new JsonSerializerSettings
        {
            ContractResolver = new MetroPropertyNameResolver(),
            Converters = {new MetroStringConverter(), new MetroColorConverter()},
            Formatting = Formatting.Indented
        });

        XUnitAssert.AreEqualNormalized(
            """
            [
              ":::GRAY:::",
              ":::GRAY:::",
              ":::GRAY:::",
              ":::GRAY:::",
              ":::BLACK:::",
              ":::GRAY:::"
            ]
            """,
            json2);
    }
#endif

    [Fact]
    public void MultipleItems()
    {
        var values = new List<MultipleItemsClass>();

        var reader = new JsonTextReader(new StringReader("""{ "name": "bar" }{ "name": "baz" }"""))
        {
            SupportMultipleContent = true
        };

        while (true)
        {
            if (!reader.Read())
            {
                break;
            }

            var serializer = new JsonSerializer();
            var foo = serializer.Deserialize<MultipleItemsClass>(reader);

            values.Add(foo);
        }

        Assert.Equal(2, values.Count);
        Assert.Equal("bar", values[0].Name);
        Assert.Equal("baz", values[1].Name);
    }

    [Fact]
    public void ObjectRequiredDeserializeMissing()
    {
        var json = "{}";
        var errors = new List<string>();

        var o = JsonConvert.DeserializeObject<RequiredObject>(
            json,
            new JsonSerializerSettings
            {
                DeserializeError = (_, _, _, _, exception, markAsHandled) =>
                {
                    errors.Add(exception.Message);
                    markAsHandled();
                }
            });

        Assert.NotNull(o);
        Assert.Equal(4, errors.Count);
        Assert.StartsWith("Required property 'NonAttributeProperty' not found in JSON. Path ''", errors[0]);
        Assert.StartsWith("Required property 'UnsetProperty' not found in JSON. Path ''", errors[1]);
        Assert.StartsWith("Required property 'AllowNullProperty' not found in JSON. Path ''", errors[2]);
        Assert.StartsWith("Required property 'AlwaysProperty' not found in JSON. Path ''", errors[3]);
    }

    [Fact]
    public void ObjectRequiredDeserializeNull()
    {
        var json = "{'NonAttributeProperty':null,'UnsetProperty':null,'AllowNullProperty':null,'AlwaysProperty':null}";
        var errors = new List<string>();

        var o = JsonConvert.DeserializeObject<RequiredObject>(
            json,
            new JsonSerializerSettings
            {
                DeserializeError = (_, _, _, _, exception, markAsHandled) =>
                {
                    errors.Add(exception.Message);
                    markAsHandled();
                }
            });

        Assert.NotNull(o);
        Assert.Equal(3, errors.Count);
        Assert.StartsWith("Required property 'NonAttributeProperty' expects a value but got null. Path ''", errors[0]);
        Assert.StartsWith("Required property 'UnsetProperty' expects a value but got null. Path ''", errors[1]);
        Assert.StartsWith("Required property 'AlwaysProperty' expects a value but got null. Path ''", errors[2]);
    }

    [Fact]
    public void ObjectRequiredSerialize()
    {
        var errors = new List<string>();

        var json = JsonConvert.SerializeObject(
            new RequiredObject(),
            new JsonSerializerSettings
            {
                SerializeError = (_, _, _, _, exception, markAsHandled) =>
                {
                    errors.Add(exception.Message);
                    markAsHandled();
                },
                Formatting = Formatting.Indented
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "DefaultProperty": null,
              "AllowNullProperty": null
            }
            """,
            json);

        Assert.Equal(3, errors.Count);
        Assert.Equal("Cannot write a null value for property 'NonAttributeProperty'. Property requires a value. Path ''.", errors[0]);
        Assert.Equal("Cannot write a null value for property 'UnsetProperty'. Property requires a value. Path ''.", errors[1]);
        Assert.Equal("Cannot write a null value for property 'AlwaysProperty'. Property requires a value. Path ''.", errors[2]);
    }

    [Fact]
    public void DeserializeCollectionItemConverter()
    {
        var c = new PropertyItemConverter
        {
            Data =
                [
                    "one",
                    "two",
                    "three"
                ]
        };

        var c2 = JsonConvert.DeserializeObject<PropertyItemConverter>("{'Data':['::ONE::','::TWO::']}");

        Assert.NotNull(c2);
        Assert.Equal(2, c2.Data.Count);
        Assert.Equal("one", c2.Data[0]);
        Assert.Equal("two", c2.Data[1]);
    }

    [Fact]
    public void SerializeCollectionItemConverter()
    {
        var c = new PropertyItemConverter
        {
            Data =
            [
                "one",
                "two",
                "three"
            ]
        };

        var json = JsonConvert.SerializeObject(c);

        Assert.Equal("""{"Data":[":::ONE:::",":::TWO:::",":::THREE:::"]}""", json);
    }

    [Fact]
    public void DateTimeDictionaryKey_DateTimeOffset_Iso()
    {
        var dic1 = new Dictionary<DateTimeOffset, int>
        {
            {
                new(2000, 12, 12, 12, 12, 12, TimeSpan.Zero), 1
            },
            {
                new(2013, 12, 12, 12, 12, 12, TimeSpan.Zero), 2
            }
        };

        var json = JsonConvert.SerializeObject(dic1, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "2000-12-12T12:12:12+00:00": 1,
              "2013-12-12T12:12:12+00:00": 2
            }
            """,
            json);

        var dic2 = JsonConvert.DeserializeObject<IDictionary<DateTimeOffset, int>>(json);

        Assert.Equal(2, dic2.Count);
        Assert.Equal(1, dic2[new(2000, 12, 12, 12, 12, 12, TimeSpan.Zero)]);
        Assert.Equal(2, dic2[new(2013, 12, 12, 12, 12, 12, TimeSpan.Zero)]);
    }

    [Fact]
    public void DateTimeDictionaryKey_DateTime_Iso()
    {
        var dic1 = new Dictionary<DateTime, int>
        {
            {
                new(2000, 12, 12, 12, 12, 12, DateTimeKind.Utc), 1
            },
            {
                new(2013, 12, 12, 12, 12, 12, DateTimeKind.Utc), 2
            }
        };

        var json = JsonConvert.SerializeObject(dic1, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "2000-12-12T12:12:12Z": 1,
              "2013-12-12T12:12:12Z": 2
            }
            """,
            json);

        var dic2 = JsonConvert.DeserializeObject<IDictionary<DateTime, int>>(json);

        Assert.Equal(2, dic2.Count);
        Assert.Equal(1, dic2[new(2000, 12, 12, 12, 12, 12, DateTimeKind.Utc)]);
        Assert.Equal(2, dic2[new(2013, 12, 12, 12, 12, 12, DateTimeKind.Utc)]);
    }

    [Fact]
    public void DeserializeEmptyJsonString()
    {
        var s = (string) new JsonSerializer().Deserialize(new JsonTextReader(new StringReader("''")));
        Assert.Equal("", s);
    }

    [Fact]
    public void PropertyItemConverter()
    {
        var e = new Event1
        {
            EventName = "Blackadder III",
            Venue = "Gryphon Theatre",
            Performances =
            [
                new(2000, 1, 1),
                new(2000, 1, 2),
                new(2000, 1, 3)
            ]
        };

        var json = JsonConvert.SerializeObject(e, Formatting.Indented);
        //{
        //  "EventName": "Blackadder III",
        //  "Venue": "Gryphon Theatre",
        //  "Performances": [
        //    new Date(1336458600000),
        //    new Date(1336545000000),
        //    new Date(1336636800000)
        //  ]
        //}

        XUnitAssert.AreEqualNormalized(
            """
            {
              "EventName": "Blackadder III",
              "Venue": "Gryphon Theatre",
              "Performances": [
                "2000-01-01T00:00:00",
                "2000-01-02T00:00:00",
                "2000-01-03T00:00:00"
              ]
            }
            """,
            json);
    }

    [Fact]
    public void IgnoreDataMemberTest()
    {
        var json = JsonConvert.SerializeObject(new IgnoreDataMemberTestClass
        {
            Ignored = int.MaxValue
        }, Formatting.Indented);
        Assert.Equal("{}", json);
    }

    [Fact]
    public void SerializeDataContractSerializationAttributes()
    {
        var dataContract = new DataContractSerializationAttributesClass
        {
            NoAttribute = "Value!",
            IgnoreDataMemberAttribute = "Value!",
            DataMemberAttribute = "Value!",
            IgnoreDataMemberAndDataMemberAttribute = "Value!"
        };

        var json = JsonConvert.SerializeObject(dataContract, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "DataMemberAttribute": "Value!",
              "IgnoreDataMemberAndDataMemberAttribute": "Value!"
            }
            """,
            json);

        var poco = new PocoDataContractSerializationAttributesClass
        {
            NoAttribute = "Value!",
            IgnoreDataMemberAttribute = "Value!",
            DataMemberAttribute = "Value!",
            IgnoreDataMemberAndDataMemberAttribute = "Value!"
        };

        json = JsonConvert.SerializeObject(poco, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "NoAttribute": "Value!",
              "DataMemberAttribute": "Value!"
            }
            """,
            json);
    }

    [Fact]
    public void CheckAdditionalContent()
    {
        var json = "{one:1}{}";

        var settings = new JsonSerializerSettings();
        var s = JsonSerializer.Create(settings);
        var o = s.Deserialize<Dictionary<string, int>>(new JsonTextReader(new StringReader(json)));

        Assert.NotNull(o);
        Assert.Equal(1, o["one"]);

        settings.CheckAdditionalContent = true;
        s = JsonSerializer.Create(settings);
        var exception = Assert.Throws<JsonReaderException>(() => s.Deserialize<Dictionary<string, int>>(new JsonTextReader(new StringReader(json))));
        Assert.Equal("Additional text encountered after finished reading JSON content: {. Path '', line 1, position 7.", exception.Message);
    }

    [Fact]
    public void CheckAdditionalContentJustComment()
    {
        var json = "{one:1} // This is just a comment";

        var settings = new JsonSerializerSettings
        {
            CheckAdditionalContent = true
        };
        var s = JsonSerializer.Create(settings);
        var o = s.Deserialize<Dictionary<string, int>>(new JsonTextReader(new StringReader(json)));

        Assert.NotNull(o);
        Assert.Equal(1, o["one"]);
    }

    [Fact]
    public void CheckAdditionalContentJustMultipleComments()
    {
        var json = """
                   {one:1} // This is just a comment
                   /* This is just a comment
                   over multiple
                   lines.*/

                   // This is just another comment.
                   """;

        var settings = new JsonSerializerSettings
        {
            CheckAdditionalContent = true
        };
        var s = JsonSerializer.Create(settings);
        var o = s.Deserialize<Dictionary<string, int>>(new JsonTextReader(new StringReader(json)));

        Assert.NotNull(o);
        Assert.Equal(1, o["one"]);
    }

    [Fact]
    public void CheckAdditionalContentCommentsThenAnotherObject()
    {
        var json = """
                   {one:1} // This is just a comment
                   /* This is just a comment
                   over multiple
                   lines.*/

                   // This is just another comment. But here comes an empty object.
                   {}
                   """;

        var settings = new JsonSerializerSettings
        {
            CheckAdditionalContent = true
        };
        var s = JsonSerializer.Create(settings);
        var exception = Assert.Throws<JsonReaderException>(() => s.Deserialize<Dictionary<string, int>>(new JsonTextReader(new StringReader(json))));
        Assert.Equal("Additional text encountered after finished reading JSON content: {. Path '', line 7, position 0.", exception.Message);
    }

    [Fact]
    public void AdditionalContentAfterFinish()
    {
        var json = "[{},1]";

        var serializer = new JsonSerializer
        {
            CheckAdditionalContent = true
        };

        var reader = new JsonTextReader(new StringReader(json));
        reader.Read();
        reader.Read();
        var exception = Assert.Throws<JsonSerializationException>(() => serializer.Deserialize<ItemConverterTestClass>(reader));
        Assert.Equal("Additional text found in JSON string after finishing deserializing object. Path '[1]', line 1, position 5.", exception.Message);
    }

    [Fact]
    public void AdditionalContentAfterFinishCheckNotRequested()
    {
        var json = """{ "MyProperty":{"Key":"Value"}} A bunch of junk at the end of the json""";

        var serializer = new JsonSerializer();

        var reader = new JsonTextReader(new StringReader(json));

        var mt = serializer.Deserialize<ItemConverterTestClass>(reader);
        Assert.Single(mt.MyProperty);
    }

    [Fact]
    public void AdditionalContentAfterCommentsCheckNotRequested()
    {
        var json = """
                   { "MyProperty":{"Key":"Value"}} /*this is a comment */
                   // this is also a comment
                   This is just junk, though.
                   """;

        var serializer = new JsonSerializer();

        var reader = new JsonTextReader(new StringReader(json));

        var mt = serializer.Deserialize<ItemConverterTestClass>(reader);
        Assert.Single(mt.MyProperty);
    }

    [Fact]
    public void AdditionalContentAfterComments()
    {
        var json = """
                   [{ "MyProperty":{"Key":"Value"}} /*this is a comment */
                   // this is also a comment
                   ,{}
                   """;

        var serializer = new JsonSerializer
        {
            CheckAdditionalContent = true
        };
        var reader = new JsonTextReader(new StringReader(json));
        reader.Read();
        reader.Read();

        var exception = Assert.Throws<JsonSerializationException>(() => serializer.Deserialize<ItemConverterTestClass>(reader));
        Assert.Equal("Additional text found in JSON string after finishing deserializing object. Path '[1]', line 3, position 2.", exception.Message);
    }

    [Fact]
    public void DeserializeRelativeUri()
    {
        var uris = JsonConvert.DeserializeObject<IList<Uri>>("""["http://localhost/path?query#hash"]""");
        Assert.Single(uris);
        Assert.Equal(new("http://localhost/path?query#hash"), uris[0]);

        var uri = JsonConvert.DeserializeObject<Uri>(
            """
            "http://localhost/path?query#hash"
            """);
        Assert.NotNull(uri);

        var i1 = new Uri("http://localhost/path?query#hash", UriKind.RelativeOrAbsolute);
        var i2 = new Uri("http://localhost/path?query#hash");
        Assert.Equal(i1, i2);

        uri = JsonConvert.DeserializeObject<Uri>(
            """
            "/path?query#hash"
            """);
        Assert.NotNull(uri);
        Assert.Equal(new("/path?query#hash", UriKind.RelativeOrAbsolute), uri);
    }

    [Fact]
    public void DeserializeDictionaryItemConverter()
    {
        var actual = JsonConvert.DeserializeObject<ItemConverterTestClass>("""{ "MyProperty":{"Key":"Y"}}""");
        Assert.Equal("X", actual.MyProperty["Key"]);
    }

    [Fact]
    public void DeserializeCaseInsensitiveKeyValuePairConverter()
    {
        var result =
            JsonConvert.DeserializeObject<KeyValuePair<int, string>>(
                "{key: 123, \"VALUE\": \"test value\"}"
            );

        Assert.Equal(123, result.Key);
        Assert.Equal("test value", result.Value);
    }

    [Fact]
    public void SerializeKeyValuePairConverterWithCamelCase()
    {
        var json =
            JsonConvert.SerializeObject(new KeyValuePair<int, string>(123, "test value"), Formatting.Indented, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "key": 123,
              "value": "test value"
            }
            """,
            json);
    }

    [Fact]
    public void SerializeFloatingPointHandling()
    {
        var d = new List<double>
        {
            1.1,
            double.NaN,
            double.PositiveInfinity
        };

        var json = JsonConvert.SerializeObject(d);
        Assert.Equal("[1.1,\"NaN\",\"Infinity\"]", json);

        json = JsonConvert.SerializeObject(
            d,
            new JsonSerializerSettings
            {
                FloatFormatHandling = FloatFormatHandling.Symbol
            });
        Assert.Equal("[1.1,NaN,Infinity]", json);

        json = JsonConvert.SerializeObject(
            d,
            new JsonSerializerSettings
            {
                FloatFormatHandling = FloatFormatHandling.DefaultValue
            });

        Assert.Equal("[1.1,0.0,0.0]", json);
    }

    // [Fact]
    // public void SerializeNumberPointsDefault()
    // {
    //     var numbers = new List<object>
    //     {
    //         1.1234567f,
    //         1.1234567d,
    //     };
    //
    //     var json = JsonConvert.SerializeObject(numbers);
    //     Assert.Equal("[1.1234567,1.1234567]", json);
    // }

    #region FloatPrecision

    [Fact]
    public void FloatPrecision()
    {
        var numbers = new List<object>
        {
            1.1234567f,
            1.1234567d,
        };

        var json = JsonConvert.SerializeObject(
            numbers,
            new JsonSerializerSettings
            {
                FloatPrecision = 3
            });
        Assert.Equal("[1.123,1.123]", json);
    }

    #endregion

    [Fact]
    public void DeserializeReadOnlyListWithBigInteger()
    {
        var json = "[9000000000000000000000000000000000000000000000000]";

        var l = JsonConvert.DeserializeObject<IReadOnlyList<BigInteger>>(json);

        var nineQuindecillion = l[0];
        // 9000000000000000000000000000000000000000000000000

        Assert.Equal(BigInteger.Parse("9000000000000000000000000000000000000000000000000"), nineQuindecillion);
    }

    [Fact]
    public void DeserializeReadOnlyListWithInt()
    {
        var json = "[900]";

        var l = JsonConvert.DeserializeObject<IReadOnlyList<int>>(json);

        var i = l[0];
        // 900

        Assert.Equal(900, i);
    }

    [Fact]
    public void DeserializeReadOnlyListWithNullableType()
    {
        var json = "[1,null]";

        var l = JsonConvert.DeserializeObject<IReadOnlyList<int?>>(json);

        Assert.Equal(1, l[0]);
        Assert.Null(l[1]);
    }

    [Fact]
    public void SerializeCustomTupleWithSerializableAttribute()
    {
        var tuple = new MyTuple<int>(500);
        var json = JsonConvert.SerializeObject(tuple);
        Assert.Equal("""{"m_Item1":500}""", json);

        MyTuple<int> obj = null;

        var doStuff = () =>
        {
            obj = JsonConvert.DeserializeObject<MyTuple<int>>(json);
        };

        doStuff();
        Assert.Equal(500, obj.Item1);
    }

    [Fact]
    public void NullableFloatingPoint()
    {
        var floats = new NullableFloats
        {
            Object = double.NaN,
            ObjectNull = null,
            Float = float.NaN,
            NullableDouble = double.NaN,
            NullableFloat = null
        };

        var json = JsonConvert.SerializeObject(
            floats,
            Formatting.Indented,
            new JsonSerializerSettings
            {
                FloatFormatHandling = FloatFormatHandling.DefaultValue
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Object": 0.0,
              "Float": 0.0,
              "Double": 0.0,
              "NullableFloat": null,
              "NullableDouble": null,
              "ObjectNull": null
            }
            """,
            json);
    }

    [Fact]
    public void SerializeDeserializeTuple()
    {
        var tuple = Tuple.Create(500, 20);
        var json = JsonConvert.SerializeObject(tuple);
        Assert.Equal("""{"Item1":500,"Item2":20}""", json);

        var tuple2 = JsonConvert.DeserializeObject<Tuple<int, int>>(json);
        Assert.Equal(500, tuple2.Item1);
        Assert.Equal(20, tuple2.Item2);
    }

    [Fact]
    public void JsonSerializerEscapeHandling()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);

        var serializer = JsonSerializer.Create(
            new()
            {
                EscapeHandling = EscapeHandling.EscapeHtml,
                Formatting = Formatting.Indented
            });
        serializer.Serialize(
            jsonWriter,
            new
            {
                html = "<html></html>"
            });

        Assert.Equal(EscapeHandling.Default, jsonWriter.EscapeHandling);

        var json = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "html": "\u003chtml\u003e\u003c/html\u003e"
            }
            """,
            json);
    }

    [Fact]
    public void NoConstructorReadOnlyCollectionTest()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NoConstructorReadOnlyCollection<int>>("[1]"));
        Assert.Equal("Cannot deserialize readonly or fixed size list: TestObjects.NoConstructorReadOnlyCollection`1[System.Int32]. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void NoConstructorReadOnlyDictionaryTest()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NoConstructorReadOnlyDictionary<int, int>>("{'1':1}"));
        Assert.Equal("Cannot deserialize readonly or fixed size dictionary: TestObjects.NoConstructorReadOnlyDictionary`2[System.Int32,System.Int32]. Path '1', line 1, position 5.", exception.Message);
    }

    [Fact]
    public void ReadTooLargeInteger()
    {
        var json = "[999999999999999999999999999999999999999999999999]";

        var l = JsonConvert.DeserializeObject<IList<BigInteger>>(json);

        Assert.Equal(BigInteger.Parse("999999999999999999999999999999999999999999999999"), l[0]);

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IList<long>>(json));
        Assert.Equal("Error converting value 999999999999999999999999999999999999999999999999 to type 'System.Int64'. Path '[0]', line 1, position 49.", exception.Message);
    }

    [Fact]
    public void ReadStringFloatingPointSymbols()
    {
        var json = """
                   [
                     "NaN",
                     "Infinity",
                     "-Infinity"
                   ]
                   """;

        var floats = JsonConvert.DeserializeObject<IList<float>>(json);
        Assert.Equal(float.NaN, floats[0]);
        Assert.Equal(float.PositiveInfinity, floats[1]);
        Assert.Equal(float.NegativeInfinity, floats[2]);

        var doubles = JsonConvert.DeserializeObject<IList<double>>(json);
        Assert.Equal(float.NaN, doubles[0]);
        Assert.Equal(float.PositiveInfinity, doubles[1]);
        Assert.Equal(float.NegativeInfinity, doubles[2]);
    }

    [Fact]
    public void TestStringToNullableDeserialization()
    {
        var json = """
                   {
                     "MyNullableBool": "",
                     "MyNullableInteger": "",
                     "MyNullableDateTime": "",
                     "MyNullableDateTimeOffset": "",
                     "MyNullableDecimal": ""
                   }
                   """;

        var c2 = JsonConvert.DeserializeObject<NullableTestClass>(json);
        Assert.Null(c2.MyNullableBool);
        Assert.Null(c2.MyNullableInteger);
        Assert.Null(c2.MyNullableDateTime);
        Assert.Null(c2.MyNullableDateTimeOffset);
        Assert.Null(c2.MyNullableDecimal);
    }

    [Fact]
    public void HashSetInterface()
    {
        ISet<string> s1 = new HashSet<string>(
        [
            "1",
            "two",
            "III"
        ]);

        var json = JsonConvert.SerializeObject(s1);

        var s2 = JsonConvert.DeserializeObject<ISet<string>>(json);

        Assert.Equal(s1.Count, s2.Count);
        foreach (var s in s1)
        {
            Assert.True(s2.Contains(s));
        }
    }

    [Fact]
    public void DeserializeDecimal()
    {
        var reader = new JsonTextReader(new StringReader("1234567890.123456"));
        var settings = new JsonSerializerSettings();
        var serializer = JsonSerializer.Create(settings);
        var d = serializer.Deserialize<decimal?>(reader);

        Assert.Equal(1234567890.123456m, d);
    }

    [Fact]
    public void SerializeBigInteger()
    {
        var i = BigInteger.Parse("123456789999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999990");

        var json = JsonConvert.SerializeObject(
            new[]
            {
                i
            },
            Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            [
              123456789999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999990
            ]
            """,
            json);
    }

    [Fact]
    public void DeserializeWithConstructor()
    {
        const string json = """{"something_else":"my value"}""";
        var foo = JsonConvert.DeserializeObject<FooConstructor>(json);
        Assert.Equal("my value", foo.Bar);
    }

    [Fact]
    public void SerializeCustomReferenceResolver()
    {
        var john = new PersonReference
        {
            Id = new("0B64FFDF-D155-44AD-9689-58D9ADB137F3"),
            Name = "John Smith"
        };

        var jane = new PersonReference
        {
            Id = new("AE3C399C-058D-431D-91B0-A36C266441B9"),
            Name = "Jane Smith"
        };

        john.Spouse = jane;
        jane.Spouse = john;

        var people = new List<PersonReference>
        {
            john,
            jane
        };

        var json = JsonConvert.SerializeObject(
            people,
            new JsonSerializerSettings
            {
                ReferenceResolverProvider = () => new IdReferenceResolver(),
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.Indented
            });

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "$id": "0b64ffdf-d155-44ad-9689-58d9adb137f3",
                "Name": "John Smith",
                "Spouse": {
                  "$id": "ae3c399c-058d-431d-91b0-a36c266441b9",
                  "Name": "Jane Smith",
                  "Spouse": {
                    "$ref": "0b64ffdf-d155-44ad-9689-58d9adb137f3"
                  }
                }
              },
              {
                "$ref": "ae3c399c-058d-431d-91b0-a36c266441b9"
              }
            ]
            """,
            json);
    }

    [Fact]
    public void NullReferenceResolver()
    {
        var john = new PersonReference
        {
            Id = new("0B64FFDF-D155-44AD-9689-58D9ADB137F3"),
            Name = "John Smith"
        };

        var jane = new PersonReference
        {
            Id = new("AE3C399C-058D-431D-91B0-A36C266441B9"),
            Name = "Jane Smith"
        };

        john.Spouse = jane;
        jane.Spouse = john;

        var people = new List<PersonReference>
        {
            john,
            jane
        };

        var json = JsonConvert.SerializeObject(
            people,
            new JsonSerializerSettings
            {
                ReferenceResolverProvider = null,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.Indented
            });

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "$id": "1",
                "Name": "John Smith",
                "Spouse": {
                  "$id": "2",
                  "Name": "Jane Smith",
                  "Spouse": {
                    "$ref": "1"
                  }
                }
              },
              {
                "$ref": "2"
              }
            ]
            """,
            json);
    }

#if !NET6_0_OR_GREATER
    [Fact]
    public void SerializeDictionaryWithStructKey()
    {
        var json = JsonConvert.SerializeObject(
            new Dictionary<Size, Size> {{new(1, 2), new(3, 4)}}
        );

        Assert.Equal("""{"1, 2":"3, 4"}""", json);

        var d = JsonConvert.DeserializeObject<Dictionary<Size, Size>>(json);

        Assert.Equal(new(1, 2), d.Keys.First());
        Assert.Equal(new(3, 4), d.Values.First());
    }
#endif

    [Fact]
    public void SerializeDictionaryWithStructKey_Custom()
    {
        var json = JsonConvert.SerializeObject(
            new Dictionary<TypeConverterSize, TypeConverterSize>
            {
                {
                    new(1, 2), new(3, 4)
                }
            }
        );

        Assert.Equal("""{"1, 2":"3, 4"}""", json);

        var d = JsonConvert.DeserializeObject<Dictionary<TypeConverterSize, TypeConverterSize>>(json);

        Assert.Equal(new(1, 2), d.Keys.First());
        Assert.Equal(new(3, 4), d.Values.First());
    }

    [Fact]
    public void DeserializeCustomReferenceResolver()
    {
        var json = """
                   [
                     {
                       "$id": "0b64ffdf-d155-44ad-9689-58d9adb137f3",
                       "Name": "John Smith",
                       "Spouse": {
                         "$id": "ae3c399c-058d-431d-91b0-a36c266441b9",
                         "Name": "Jane Smith",
                         "Spouse": {
                           "$ref": "0b64ffdf-d155-44ad-9689-58d9adb137f3"
                         }
                       }
                     },
                     {
                       "$ref": "ae3c399c-058d-431d-91b0-a36c266441b9"
                     }
                   ]
                   """;

        var people = JsonConvert.DeserializeObject<IList<PersonReference>>(
            json,
            new JsonSerializerSettings
            {
                ReferenceResolverProvider = () => new IdReferenceResolver(),
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.Indented
            });

        Assert.Equal(2, people.Count);

        var john = people[0];
        var jane = people[1];

        Assert.Equal(john, jane.Spouse);
        Assert.Equal(jane, john.Spouse);
    }

    [Fact]
    public void DeserializeCustomReferenceResolver_ViaProvider()
    {
        var json = """
                   [
                     {
                       "$id": "0b64ffdf-d155-44ad-9689-58d9adb137f3",
                       "Name": "John Smith",
                       "Spouse": {
                         "$id": "ae3c399c-058d-431d-91b0-a36c266441b9",
                         "Name": "Jane Smith",
                         "Spouse": {
                           "$ref": "0b64ffdf-d155-44ad-9689-58d9adb137f3"
                         }
                       }
                     },
                     {
                       "$ref": "ae3c399c-058d-431d-91b0-a36c266441b9"
                     }
                   ]
                   """;

        var people = JsonConvert.DeserializeObject<IList<PersonReference>>(
            json,
            new JsonSerializerSettings
            {
                ReferenceResolverProvider = () => new IdReferenceResolver(),
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.Indented
            });

        Assert.Equal(2, people.Count);

        var john = people[0];
        var jane = people[1];

        Assert.Equal(john, jane.Spouse);
        Assert.Equal(jane, john.Spouse);
    }

    [Fact]
    public void TypeConverterOnInterface()
    {
        var consoleWriter = new ConsoleWriter();

        // If dynamic type handling is enabled, case 1 and 3 work fine
        var options = new JsonSerializerSettings
        {
            Converters = [new TypeConverterJsonConverter()]
            //TypeNameHandling = TypeNameHandling.All
        };

        //
        // Case 1: Serialize the concrete value and restore it from the interface
        // Therefore we need dynamic handling of type information if the type is not serialized with the type converter directly
        //
        var text1 = JsonConvert.SerializeObject(consoleWriter, Formatting.Indented, options);
        Assert.Equal(
            """
            "Console Writer"
            """,
            text1);

        var restoredWriter = JsonConvert.DeserializeObject<IMyInterface>(text1, options);
        Assert.Equal("ConsoleWriter", restoredWriter.PrintTest());

        //
        // Case 2: Serialize a dictionary where the interface is the key
        // The key is always serialized with its ToString() method and therefore needs a mechanism to be restored from that (using the type converter)
        //
        var dict2 = new Dictionary<IMyInterface, string>
        {
            {
                consoleWriter, "Console"
            }
        };

        var text2 = JsonConvert.SerializeObject(dict2, Formatting.Indented, options);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Console Writer": "Console"
            }
            """,
            text2);

        var restoredObject = JsonConvert.DeserializeObject<Dictionary<IMyInterface, string>>(text2, options);
        Assert.Equal("ConsoleWriter", restoredObject.First().Key.PrintTest());

        //
        // Case 3 Serialize a dictionary where the interface is the value
        // The key is always serialized with its ToString() method and therefore needs a mechanism to be restored from that (using the type converter)
        //
        var dict3 = new Dictionary<string, IMyInterface>
        {
            {
                "Console", consoleWriter
            }
        };

        var text3 = JsonConvert.SerializeObject(dict3, Formatting.Indented, options);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Console": "Console Writer"
            }
            """,
            text3);

        var restoredDict2 = JsonConvert.DeserializeObject<Dictionary<string, IMyInterface>>(text3, options);
        Assert.Equal("ConsoleWriter", restoredDict2.First().Value.PrintTest());
    }

    [Fact]
    public void Main()
    {
        var product = new ParticipantEntity
        {
            Properties = new()
            {
                {
                    "s", "d"
                }
            }
        };
        var json = JsonConvert.SerializeObject(product);

        Assert.Equal("""{"pa_info":{"s":"d"}}""", json);
        var deserializedProduct = JsonConvert.DeserializeObject<ParticipantEntity>(json);
    }

    [Fact]
    public void ConvertibleIdTest()
    {
        var c = new TestClassConvertible
        {
            Id = new()
            {
                Value = 1
            },
            X = 2
        };
        var s = JsonConvert.SerializeObject(c, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Id": "1",
              "X": 2
            }
            """,
            s);
    }

    [Fact]
    public void DuplicatePropertiesInNestedObject()
    {
        var content = """{"result":{"time":1408188592,"time":1408188593},"error":null,"id":"1"}""";
        var o = JsonConvert.DeserializeObject<JObject>(content);
        var time = (int) o["result"]["time"];

        Assert.Equal(1408188593, time);
    }

    [Fact]
    public void RoundtripUriOriginalString()
    {
        var originalUri = "https://test.com?m=a%2bb";

        var uriWithPlus = new Uri(originalUri);

        var jsonWithPlus = JsonConvert.SerializeObject(uriWithPlus);

        var uriWithPlus2 = JsonConvert.DeserializeObject<Uri>(jsonWithPlus);

        Assert.Equal(originalUri, uriWithPlus2.OriginalString);
    }

    [Fact]
    public void SerializeObjectWithEvent()
    {
        var o = new MyObservableObject
        {
            TestString = "Test string"
        };

        var json = JsonConvert.SerializeObject(o, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "PropertyChanged": null,
              "TestString": "Test string"
            }
            """,
            json);

        var o2 = JsonConvert.DeserializeObject<MyObservableObject>(json);
        Assert.Equal("Test string", o2.TestString);
    }

    [Fact]
    public void ParameterizedConstructorWithBasePrivateProperties()
    {
        var original = new DerivedConstructorType("Base", "Derived");

        var settings = new JsonSerializerSettings();
        var jsonCopy = JsonConvert.SerializeObject(original, settings);

        var clonedObject = JsonConvert.DeserializeObject<DerivedConstructorType>(jsonCopy, settings);

        Assert.Equal("Base", clonedObject.BaseProperty);
        Assert.Equal("Derived", clonedObject.DerivedProperty);
    }

    [Fact]
    public void ErrorCreatingJsonConverter()
    {
        var exception = Assert.Throws<ArgumentException>(() => JsonConvert.SerializeObject(new ErroringTestClass()));
        Assert.Equal("Could not get constructor for TestObjects.ErroringJsonConverter.", exception.Message);
    }

    [Fact]
    public void DeserializedDerivedWithPrivate()
    {
        var json = """
                   {
                     "DerivedProperty": "derived",
                     "BaseProperty": "base"
                   }
                   """;

        var d = JsonConvert.DeserializeObject<DerivedWithPrivate>(json);

        Assert.Equal("base", d.BaseProperty);
        Assert.Equal("derived", d.DerivedProperty);
    }

    [Fact]
    public void DeserializeNullableUnsignedLong()
    {
        var instance = new NullableLongTestClass
        {
            Value = ulong.MaxValue
        };
        var output = JsonConvert.SerializeObject(instance);
        var result = JsonConvert.DeserializeObject<NullableLongTestClass>(output);

        Assert.Equal(ulong.MaxValue, result.Value);
    }

    [Fact]
    public void MailMessageConverterTest()
    {
        const string JsonMessage =
            """
            {
              "From": {
                "Address": "askywalker@theEmpire.gov",
                "DisplayName": "Darth Vader"
              },
              "Sender": null,
              "ReplyTo": null,
              "ReplyToList": [],
              "To": [
                {
                  "Address": "lskywalker@theRebellion.org",
                  "DisplayName": "Luke Skywalker"
                }
              ],
              "Bcc": [],
              "CC": [
                {
                  "Address": "lorgana@alderaan.gov",
                  "DisplayName": "Princess Leia"
                }
              ],
              "Priority": 0,
              "DeliveryNotificationOptions": 0,
              "Subject": "Family tree",
              "SubjectEncoding": null,
              "Headers": [],
              "HeadersEncoding": null,
              "Body": "<strong>I am your father!</strong>",
              "BodyEncoding": "US-ASCII",
              "BodyTransferEncoding": -1,
              "IsBodyHtml": true,
              "Attachments": [
                {
                  "FileName": "skywalker family tree.jpg",
                  "ContentBase64": "AQIDBAU="
                }
              ],
              "AlternateViews": []
            }
            """;

        var exception = Assert.Throws<JsonSerializationException>(() =>
        {
            JsonConvert.TryDeserializeObject<MailMessage>(
                JsonMessage,
                new MailAddressReadConverter(),
                new AttachmentReadConverter(),
                new EncodingReadConverter());
        });
        Assert.Equal("Cannot populate list type System.Net.Mime.HeaderCollection. Path 'Headers', line 26, position 14.", exception.Message);
    }

    [Fact]
    public void ParametrizedConstructor_IncompleteJson()
    {
        var s = """{"text":"s","cursorPosition":189,"dataSource":"json_northwind",""";

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<CompletionDataRequest>(s));
        Assert.Equal("Unexpected end when deserializing object. Path 'dataSource', line 1, position 63.", exception.Message);
    }

    [Fact]
    public void ChildClassWithProtectedOverridePlusJsonProperty_Serialize()
    {
        var c = (JsonObjectContract) DefaultContractResolver.Instance.ResolveContract(typeof(ChildClassWithProtectedOverridePlusJsonProperty));
        Assert.Single(c.Properties);

        var propertyValue = "test";
        var testJson = $"{{ 'MyProperty' : '{propertyValue}' }}";

        var testObject = JsonConvert.DeserializeObject<ChildClassWithProtectedOverridePlusJsonProperty>(testJson);

        Assert.Equal(propertyValue, testObject.GetPropertyValue());
    }

    [Fact]
    public void JsonPropertyConverter()
    {
        var dt = new DateTime(2000, 12, 20, 0, 0, 0, DateTimeKind.Utc);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        var c1 = new JsonPropertyConverterTestClass
        {
            NormalDate = dt
        };

        var json = JsonConvert.SerializeObject(c1, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "NormalDate": "2000-12-20T00:00:00Z"
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<JsonPropertyConverterTestClass>(json, settings);

        Assert.Equal(dt, c2.NormalDate);
    }

    [Fact]
    public void StringEmptyValue()
    {
        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<EmptyJsonValueTestClass>("{ A: , B: 1, C: 123, D: 1.23, E: 3.45, F: null }"));
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path 'A', line 1, position 6.", exception.Message);
    }

    [Fact]
    public void NullableIntEmptyValue()
    {
        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<EmptyJsonValueTestClass>("{ A: \"\", B: , C: 123, D: 1.23, E: 3.45, F: null }"));
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path 'B', line 1, position 13.", exception.Message);
    }

    [Fact]
    public void NullableLongEmptyValue()
    {
        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<EmptyJsonValueTestClass>("{ A: \"\", B: 1, C: , D: 1.23, E: 3.45, F: null }"));
        Assert.Equal("An undefined token is not a valid System.Nullable`1[System.Int64]. Path 'C', line 1, position 18.", exception.Message);
    }

    [Fact]
    public void NullableDecimalEmptyValue()
    {
        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<EmptyJsonValueTestClass>("{ A: \"\", B: 1, C: 123, D: , E: 3.45, F: null }"));
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path 'D', line 1, position 27.", exception.Message);
    }

    [Fact]
    public void NullableDoubleEmptyValue()
    {
        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<EmptyJsonValueTestClass>("{ A: \"\", B: 1, C: 123, D: 1.23, E: , F: null }"));
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path 'E', line 1, position 36.", exception.Message);
    }

    [Fact]
    public void SetMaxDepth_DepthExceeded()
    {
        var reader = new JsonTextReader(new StringReader("[[['text']]]"));
        Assert.Equal(64, reader.MaxDepth);

        var settings = new JsonSerializerSettings();
        Assert.Equal(64, settings.MaxDepth);
        Assert.False( settings.maxDepthSet);

        // Default should be the same
        Assert.Equal(reader.MaxDepth, settings.MaxDepth);

        settings.MaxDepth = 2;
        Assert.Equal(2, settings.MaxDepth);
        Assert.True(settings.maxDepthSet);

        var serializer = JsonSerializer.Create(settings);
        Assert.Equal(2, serializer.MaxDepth);

        var exception = Assert.Throws<JsonReaderException>(() => serializer.Deserialize(reader));
        Assert.Equal("The reader's MaxDepth of 2 has been exceeded. Path '[0][0]', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void SetMaxDepth_DepthNotExceeded()
    {
        var reader = new JsonTextReader(new StringReader("['text']"));
        var settings = new JsonSerializerSettings
        {
            MaxDepth = 2
        };

        var serializer = JsonSerializer.Create(settings);
        Assert.Equal(2, serializer.MaxDepth);

        serializer.Deserialize(reader);

        Assert.Equal(64, reader.MaxDepth);
    }

    [Fact]
    public void SetMaxDepth_DefaultDepthExceeded()
    {
        var json = NestedJson.Build(150);

        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<JObject>(json));
        Assert.Equal("The reader's MaxDepth of 64 has been exceeded. Path '0.1.2.3.4.5.6.7.8.9.10.11.12.13.14.15.16.17.18.19.20.21.22.23.24.25.26.27.28.29.30.31.32.33.34.35.36.37.38.39.40.41.42.43.44.45.46.47.48.49.50.51.52.53.54.55.56.57.58.59.60.61.62.63', line 65, position 135.", exception.Message);
    }

    [Fact]
    public void SetMaxDepth_IncreasedDepthNotExceeded()
    {
        var json = NestedJson.Build(150);

        var o = JsonConvert.DeserializeObject<JObject>(json, new JsonSerializerSettings
        {
            MaxDepth = 150
        });
        var depth = GetDepth(o);

        Assert.Equal(150, depth);
    }

    [Fact]
    public void SetMaxDepth_NullDepthNotExceeded()
    {
        var json = NestedJson.Build(150);

        var o = JsonConvert.DeserializeObject<JObject>(
            json,
            new JsonSerializerSettings
            {
                MaxDepth = null
            });
        var depth = GetDepth(o);

        Assert.Equal(150, depth);
    }

    [Fact]
    public void SetMaxDepth_MaxValueDepthNotExceeded()
    {
        var json = NestedJson.Build(150);

        var o = JsonConvert.DeserializeObject<JObject>(
            json,
            new JsonSerializerSettings
            {
                MaxDepth = int.MaxValue
            });
        var depth = GetDepth(o);

        Assert.Equal(150, depth);
    }

    static int GetDepth(JToken o)
    {
        var depth = 1;
        while (o.First != null)
        {
            o = o.First;
            if (o.Type == JTokenType.Object)
            {
                depth++;
            }
        }

        return depth;
    }

    [Fact]
    public void ShallowCopy_CopyAllProperties()
    {
        var propertyNames = typeof(JsonSerializerSettings)
            .GetProperties()
            .Select(_ => _.Name)
            .ToList();

        var settings = new JsonSerializerSettings();

        var clone = new JsonSerializerSettings(settings);

        Assert.Equal(settings.ReferenceLoopHandling, clone.ReferenceLoopHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.ReferenceLoopHandling)));

        Assert.Equal(settings.MissingMemberHandling, clone.MissingMemberHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.MissingMemberHandling)));

        Assert.Equal(settings.ObjectCreationHandling, clone.ObjectCreationHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.ObjectCreationHandling)));

        Assert.Equal(settings.NullValueHandling, clone.NullValueHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.NullValueHandling)));

        Assert.Equal(settings.DefaultValueHandling, clone.DefaultValueHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.DefaultValueHandling)));

        Assert.Equal(settings.Converters, clone.Converters);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.Converters)));

        Assert.Equal(settings.PreserveReferencesHandling, clone.PreserveReferencesHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.PreserveReferencesHandling)));

        Assert.Equal(settings.TypeNameHandling, clone.TypeNameHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.TypeNameHandling)));

        Assert.Equal(settings.MetadataPropertyHandling, clone.MetadataPropertyHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.MetadataPropertyHandling)));

        Assert.Equal(settings.TypeNameAssemblyFormatHandling, clone.TypeNameAssemblyFormatHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.TypeNameAssemblyFormatHandling)));

        Assert.Equal(settings.ConstructorHandling, clone.ConstructorHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.ConstructorHandling)));

        Assert.Equal(settings.ContractResolver, clone.ContractResolver);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.ContractResolver)));

        Assert.Equal(settings.EqualityComparer, clone.EqualityComparer);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.EqualityComparer)));

        Assert.Equal(settings.ReferenceResolverProvider, clone.ReferenceResolverProvider);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.ReferenceResolverProvider)));

        Assert.Equal(settings.SerializationBinder, clone.SerializationBinder);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.SerializationBinder)));

        Assert.Equal(settings.SerializeError, clone.SerializeError);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.SerializeError)));
        Assert.Equal(settings.DeserializeError, clone.DeserializeError);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.DeserializeError)));

        Assert.Equal(settings.Serialized, clone.Serialized);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.Serialized)));

        Assert.Equal(settings.Serializing, clone.Serializing);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.Serializing)));

        Assert.Equal(settings.Deserialized, clone.Deserialized);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.Deserialized)));

        Assert.Equal(settings.Deserializing, clone.Deserializing);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.Deserializing)));

        Assert.Equal(settings.MaxDepth, clone.MaxDepth);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.MaxDepth)));

        Assert.Equal(settings.Formatting, clone.Formatting);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.Formatting)));

        Assert.Equal(settings.FloatFormatHandling, clone.FloatFormatHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.FloatFormatHandling)));

        Assert.Equal(settings.FloatParseHandling, clone.FloatParseHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.FloatParseHandling)));

        Assert.Equal(settings.CheckAdditionalContent, clone.CheckAdditionalContent);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.CheckAdditionalContent)));

        Assert.Equal(settings.EscapeHandling, clone.EscapeHandling);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.EscapeHandling)));

        Assert.Equal(settings.FloatPrecision, clone.FloatPrecision);
        Assert.True(propertyNames.Remove(nameof(JsonSerializerSettings.FloatPrecision)));

        Assert.Empty(propertyNames);
    }
}