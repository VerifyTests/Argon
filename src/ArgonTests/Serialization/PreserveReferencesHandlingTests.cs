// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

// ReSharper disable UnusedVariable

// ReSharper disable UseObjectOrCollectionInitializer

public class PreserveReferencesHandlingTests : TestFixtureBase
{
    public class ContentB
    {
        public bool SomeValue { get; set; }
    }

    [JsonConverter(typeof(ListConverter))]
    public class ContentA : List<object>
    {
        public ContentB B { get; set; } = new();
    }

    public class ListConverter : JsonConverter
    {
        public override bool CanConvert(Type type) =>
            true;

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) =>
            new ContentA {B = serializer.Deserialize<ContentB>(reader)}; // Construct my data back.

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var b = ((ContentA) value).B;
            serializer.Serialize(writer, b); // My Content.B contains all useful data.
        }
    }

    public class Container
    {
        public List<ContentA> ListA { get; set; } = [];
        public List<ContentA> ListB { get; set; } = [];
    }

    [Fact]
    public void SerializeReferenceInConvert()
    {
        var settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };

        var c1 = new Container();
        var content = new ContentA
        {
            B =
            {
                SomeValue = true
            }
        };
        c1.ListA.Add(content);
        c1.ListB.Add(content);

        var s = JsonConvert.SerializeObject(c1, settings);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "$id": "1",
              "$type": "PreserveReferencesHandlingTests+Container, ArgonTests",
              "ListA": {
                "$id": "2",
                "$type": "{{typeof(List<ContentA>).GetTypeName(0, DefaultSerializationBinder.Instance)}}",
                "$values": [
                  {
                    "$id": "3",
                    "$type": "PreserveReferencesHandlingTests+ContentB, ArgonTests",
                    "SomeValue": true
                  }
                ]
              },
              "ListB": {
                "$id": "4",
                "$type": "{{typeof(List<ContentA>).GetTypeName(0, DefaultSerializationBinder.Instance)}}",
                "$values": [
                  {
                    "$ref": "3"
                  }
                ]
              }
            }
            """,
            s);

        var c2 = JsonConvert.DeserializeObject<Container>(s, settings);

        Assert.Equal(c2.ListA[0], c2.ListB[0]);
        Assert.True(c2.ListA[0].B.SomeValue);
    }

    public class Parent
    {
        public Child ReadOnlyChild => Child1;

        public Child Child1 { get; set; }
        public Child Child2 { get; set; }

        public IList<string> ReadOnlyList => List1;

        public IList<string> List1 { get; set; }
        public IList<string> List2 { get; set; }
    }

    public class Child
    {
        public string PropertyName { get; set; }
    }

    [Fact]
    public void SerializeReadOnlyProperty()
    {
        var c = new Child
        {
            PropertyName = "value?"
        };
        var l = new List<string>
        {
            "value!"
        };
        var p = new Parent
        {
            Child1 = c,
            Child2 = c,
            List1 = l,
            List2 = l
        };

        var json = JsonConvert.SerializeObject(p, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            PreserveReferencesHandling = PreserveReferencesHandling.All
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "ReadOnlyChild": {
                "PropertyName": "value?"
              },
              "Child1": {
                "$id": "2",
                "PropertyName": "value?"
              },
              "Child2": {
                "$ref": "2"
              },
              "ReadOnlyList": [
                "value!"
              ],
              "List1": {
                "$id": "3",
                "$values": [
                  "value!"
                ]
              },
              "List2": {
                "$ref": "3"
              }
            }
            """,
            json);

        var newP = JsonConvert.DeserializeObject<Parent>(json, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            PreserveReferencesHandling = PreserveReferencesHandling.All
        });

        Assert.Equal("value?", newP.Child1.PropertyName);
        Assert.Equal(newP.Child1, newP.Child2);
        Assert.Equal(newP.Child1, newP.ReadOnlyChild);

        Assert.Equal("value!", newP.List1[0]);
        Assert.Equal(newP.List1, newP.List2);
        Assert.Equal(newP.List1, newP.ReadOnlyList);
    }

    [Fact]
    public void SerializeDictionaryWithPreserveObjectReferences()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var circularDictionary = new CircularDictionary
        {
            { "other", new() { { "blah", null } } }
        };
        circularDictionary.Add("self", circularDictionary);

        var json = JsonConvert.SerializeObject(circularDictionary, Formatting.Indented,
            new JsonSerializerSettings {PreserveReferencesHandling = PreserveReferencesHandling.All});

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "other": {
                "$id": "2",
                "blah": null
              },
              "self": {
                "$ref": "1"
              }
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeDictionaryWithPreserveObjectReferences()
    {
        var json = """
            {
              "$id": "1",
              "other": {
                "$id": "2",
                "blah": null
              },
              "self": {
                "$ref": "1"
              }
            }
            """;

        var circularDictionary = JsonConvert.DeserializeObject<CircularDictionary>(json,
            new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All
            });

        Assert.Equal(2, circularDictionary.Count);
        Assert.Single(circularDictionary["other"]);
        Assert.True(ReferenceEquals(circularDictionary, circularDictionary["self"]));
    }

    public class CircularList : List<CircularList>;

    [Fact]
    public void SerializeCircularListsError()
    {
        var classRef = typeof(CircularList).FullName;

        var circularList = new CircularList
        {
            null,
            ([null])
        };
        circularList.Add([[circularList]]);

        var messages = $"Self referencing loop detected with type '{classRef}'. Path '[2][0]'.";
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(circularList, Formatting.Indented));
        Assert.Equal(messages, exception.Message);
    }

    [Fact]
    public void SerializeCircularListsIgnore()
    {
        var circularList = new CircularList
        {
            null,
            ([null])
        };
        circularList.Add([[circularList]]);

        var json = JsonConvert.SerializeObject(circularList,
            Formatting.Indented,
            new JsonSerializerSettings {ReferenceLoopHandling = ReferenceLoopHandling.Ignore});

        XUnitAssert.AreEqualNormalized(
            """
            [
              null,
              [
                null
              ],
              [
                []
              ]
            ]
            """,
            json);
    }

    [Fact]
    public void SerializeListsWithPreserveObjectReferences()
    {
        var circularList = new CircularList
        {
            null,
            ([null])
        };
        circularList.Add([[circularList]]);

        var json = JsonConvert.SerializeObject(circularList, Formatting.Indented,
            new JsonSerializerSettings {PreserveReferencesHandling = PreserveReferencesHandling.All});

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "$values": [
                null,
                {
                  "$id": "2",
                  "$values": [
                    null
                  ]
                },
                {
                  "$id": "3",
                  "$values": [
                    {
                      "$id": "4",
                      "$values": [
                        {
                          "$ref": "1"
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeListsWithPreserveObjectReferences()
    {
        var json = """
            {
              "$id": "1",
              "$values": [
                null,
                {
                  "$id": "2",
                  "$values": [
                    null
                  ]
                },
                {
                  "$id": "3",
                  "$values": [
                    {
                      "$id": "4",
                      "$values": [
                        {
                          "$ref": "1"
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        var circularList = JsonConvert.DeserializeObject<CircularList>(json,
            new JsonSerializerSettings {PreserveReferencesHandling = PreserveReferencesHandling.All});

        Assert.Equal(3, circularList.Count);
        Assert.Null(circularList[0]);
        Assert.Single(circularList[1]);
        Assert.Single(circularList[2]);
        Assert.Single(circularList[2][0]);
        Assert.True(ReferenceEquals(circularList, circularList[2][0][0]));
    }

    [Fact]
    public void DeserializeArraysWithPreserveObjectReferences()
    {
        var json = """
            {
              "$id": "1",
              "$values": [
                null,
                {
                  "$id": "2",
                  "$values": [
                    null
                  ]
                },
                {
                  "$id": "3",
                  "$values": [
                    {
                      "$id": "4",
                      "$values": [
                        {
                          "$ref": "1"
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        var settings = new JsonSerializerSettings {PreserveReferencesHandling = PreserveReferencesHandling.All};
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<string[][]>(json, settings));
        Assert.Equal("Cannot preserve reference to array or readonly list, or list created from a non-default constructor: System.String[][]. Path '$values', line 3, position 14.", exception.Message);
    }

    public class CircularDictionary : Dictionary<string, CircularDictionary>;

    [Fact]
    public void SerializeCircularDictionaryError()
    {
        var classRef = typeof(CircularDictionary).FullName;

        var circularDictionary = new CircularDictionary
        {
            { "other", new() { { "blah", null } } }
        };
        circularDictionary.Add("self", circularDictionary);

        var messages = $"Self referencing loop detected with type '{classRef}'. Path ''.";
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(circularDictionary, Formatting.Indented));
        Assert.Equal(messages, exception.Message);
    }

    [Fact]
    public void SerializeCircularDictionaryIgnore()
    {
        var circularDictionary = new CircularDictionary
        {
            { "other", new() { { "blah", null } } }
        };
        circularDictionary.Add("self", circularDictionary);

        var json = JsonConvert.SerializeObject(circularDictionary, Formatting.Indented,
            new JsonSerializerSettings {ReferenceLoopHandling = ReferenceLoopHandling.Ignore});

        XUnitAssert.AreEqualNormalized(
            """
            {
              "other": {
                "blah": null
              }
            }
            """,
            json);
    }

    [Fact]
    public void UnexpectedEnd()
    {
        var json = """
            {
              "$id":
            """;

        var settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            MetadataPropertyHandling = MetadataPropertyHandling.Default
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<string[][]>(json, settings));
        Assert.Equal("Unexpected end when reading JSON. Path '$id', line 2, position 8.", exception.Message);
    }

    [Fact]
    public void SerializeEmployeeReference()
    {
        var mikeManager = new EmployeeReference
        {
            Name = "Mike Manager"
        };
        var joeUser = new EmployeeReference
        {
            Name = "Joe User",
            Manager = mikeManager
        };

        var employees = new List<EmployeeReference>
        {
            mikeManager,
            joeUser
        };

        var json = JsonConvert.SerializeObject(employees, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "$id": "1",
                "Name": "Mike Manager",
                "Manager": null
              },
              {
                "$id": "2",
                "Name": "Joe User",
                "Manager": {
                  "$ref": "1"
                }
              }
            ]
            """,
            json);
    }

    [Fact]
    public void DeserializeEmployeeReference()
    {
        var json = """
            [
              {
                "$id": "1",
                "Name": "Mike Manager",
                "Manager": null
              },
              {
                "$id": "2",
                "Name": "Joe User",
                "Manager": {
                  "$ref": "1"
                }
              }
            ]
            """;

        var employees = JsonConvert.DeserializeObject<List<EmployeeReference>>(json);

        Assert.Equal(2, employees.Count);
        Assert.Equal("Mike Manager", employees[0].Name);
        Assert.Equal("Joe User", employees[1].Name);
        Assert.Equal(employees[0], employees[1].Manager);
    }

    [JsonObject(IsReference = true)]
    class Condition(int value)
    {
        public int Value { get; } = value;
    }

    class ClassWithConditions(Condition condition1, Condition condition2)
    {
        public Condition Condition1 { get; } = condition1;

        public Condition Condition2 { get; } = condition2;
    }

    [Fact]
    public void SerializeIsReferenceReadonlyProperty()
    {
        var condition = new Condition(1);
        var value = new ClassWithConditions(condition, condition);

        var json = JsonConvert.SerializeObject(value, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Condition1": {
                "$id": "1",
                "Value": 1
              },
              "Condition2": {
                "$ref": "1"
              }
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeIsReferenceReadonlyProperty()
    {
        var json = """
            {
              "Condition1": {
                "$id": "1",
                "Value": 1
              },
              "Condition2": {
                "$ref": "1"
              }
            }
            """;

        var value = JsonConvert.DeserializeObject<ClassWithConditions>(json);
        Assert.Equal(1, value.Condition1.Value);
        Assert.Equal(value.Condition1, value.Condition2);
    }

    [Fact]
    public void SerializeCircularReference()
    {
        var c1 = new CircularReferenceClass {Name = "c1"};
        var c2 = new CircularReferenceClass {Name = "c2"};
        var c3 = new CircularReferenceClass {Name = "c3"};

        c1.Child = c2;
        c2.Child = c3;
        c3.Child = c1;

        var json = JsonConvert.SerializeObject(c1, Formatting.Indented, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "Name": "c1",
              "Child": {
                "$id": "2",
                "Name": "c2",
                "Child": {
                  "$id": "3",
                  "Name": "c3",
                  "Child": {
                    "$ref": "1"
                  }
                }
              }
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeCircularReference()
    {
        var json = """
            {
              "$id": "1",
              "Name": "c1",
              "Child": {
                "$id": "2",
                "Name": "c2",
                "Child": {
                  "$id": "3",
                  "Name": "c3",
                  "Child": {
                    "$ref": "1"
                  }
                }
              }
            }
            """;

        var c1 =
            JsonConvert.DeserializeObject<CircularReferenceClass>(
                json,
                new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });

        Assert.Equal("c1", c1.Name);
        Assert.Equal("c2", c1.Child.Name);
        Assert.Equal("c3", c1.Child.Child.Name);
        Assert.Equal("c1", c1.Child.Child.Child.Name);
    }

    [Fact]
    public void SerializeReferenceInList()
    {
        var e1 = new EmployeeReference {Name = "e1"};
        var e2 = new EmployeeReference {Name = "e2"};

        var employees = new List<EmployeeReference> {e1, e2, e1, e2};

        var json = JsonConvert.SerializeObject(employees, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "$id": "1",
                "Name": "e1",
                "Manager": null
              },
              {
                "$id": "2",
                "Name": "e2",
                "Manager": null
              },
              {
                "$ref": "1"
              },
              {
                "$ref": "2"
              }
            ]
            """,
            json);
    }

    [Fact]
    public void DeserializeReferenceInList()
    {
        var json = """
            [
              {
                "$id": "1",
                "Name": "e1",
                "Manager": null
              },
              {
                "$id": "2",
                "Name": "e2",
                "Manager": null
              },
              {
                "$ref": "1"
              },
              {
                "$ref": "2"
              }
            ]
            """;

        var employees = JsonConvert.DeserializeObject<List<EmployeeReference>>(json);
        Assert.Equal(4, employees.Count);

        Assert.Equal("e1", employees[0].Name);
        Assert.Equal("e2", employees[1].Name);
        Assert.Equal("e1", employees[2].Name);
        Assert.Equal("e2", employees[3].Name);

        Assert.Equal(employees[0], employees[2]);
        Assert.Equal(employees[1], employees[3]);
    }

    [Fact]
    public void SerializeReferenceInDictionary()
    {
        var e1 = new EmployeeReference {Name = "e1"};
        var e2 = new EmployeeReference {Name = "e2"};

        var employees = new Dictionary<string, EmployeeReference>
        {
            {"One", e1},
            {"Two", e2},
            {"Three", e1},
            {"Four", e2}
        };

        var json = JsonConvert.SerializeObject(employees, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "One": {
                "$id": "1",
                "Name": "e1",
                "Manager": null
              },
              "Two": {
                "$id": "2",
                "Name": "e2",
                "Manager": null
              },
              "Three": {
                "$ref": "1"
              },
              "Four": {
                "$ref": "2"
              }
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeReferenceInDictionary()
    {
        var json = """
            {
              "One": {
                "$id": "1",
                "Name": "e1",
                "Manager": null
              },
              "Two": {
                "$id": "2",
                "Name": "e2",
                "Manager": null
              },
              "Three": {
                "$ref": "1"
              },
              "Four": {
                "$ref": "2"
              }
            }
            """;

        var employees = JsonConvert.DeserializeObject<Dictionary<string, EmployeeReference>>(json);
        Assert.Equal(4, employees.Count);

        var e1 = employees["One"];
        var e2 = employees["Two"];

        Assert.Equal("e1", e1.Name);
        Assert.Equal("e2", e2.Name);

        Assert.Equal(e1, employees["Three"]);
        Assert.Equal(e2, employees["Four"]);
    }

    [Fact]
    public void ExampleWithout()
    {
        var p = new Person
        {
            BirthDate = new(1980, 12, 23, 0, 0, 0, DateTimeKind.Utc),
            LastModified = new(2009, 2, 20, 12, 59, 21, DateTimeKind.Utc),
            Department = "IT",
            Name = "James"
        };

        var people = new List<Person>
        {
            p,
            p
        };

        var json = JsonConvert.SerializeObject(people, Formatting.Indented);
        //[
        //  {
        //    "Name": "James",
        //    "BirthDate": "\/Date(346377600000)\/",
        //    "LastModified": "\/Date(1235134761000)\/"
        //  },
        //  {
        //    "Name": "James",
        //    "BirthDate": "\/Date(346377600000)\/",
        //    "LastModified": "\/Date(1235134761000)\/"
        //  }
        //]
    }

    [Fact]
    public void ExampleWith()
    {
        var p = new Person
        {
            BirthDate = new(1980, 12, 23, 0, 0, 0, DateTimeKind.Utc),
            LastModified = new(2009, 2, 20, 12, 59, 21, DateTimeKind.Utc),
            Department = "IT",
            Name = "James"
        };

        var people = new List<Person>
        {
            p,
            p
        };

        var json = JsonConvert.SerializeObject(people, Formatting.Indented,
            new JsonSerializerSettings {PreserveReferencesHandling = PreserveReferencesHandling.Objects});
        //[
        //  {
        //    "$id": "1",
        //    "Name": "James",
        //    "BirthDate": "\/Date(346377600000)\/",
        //    "LastModified": "\/Date(1235134761000)\/"
        //  },
        //  {
        //    "$ref": "1"
        //  }
        //]

        var deserializedPeople = JsonConvert.DeserializeObject<List<Person>>(json,
            new JsonSerializerSettings {PreserveReferencesHandling = PreserveReferencesHandling.Objects});

        Assert.Equal(2, deserializedPeople.Count);

        var p1 = deserializedPeople[0];
        var p2 = deserializedPeople[1];

        Assert.Equal("James", p1.Name);
        Assert.Equal("James", p2.Name);

        var equal = ReferenceEquals(p1, p2);
        Assert.True(equal);
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class User
    {
        #region properties

        [JsonProperty(Required = Required.Always, PropertyName = "SecretType")]
        string secretType;

        [JsonProperty(Required = Required.Always)]
        public string Login { get; set; }

        public Type SecretType
        {
            get => Type.GetType(secretType);
            set => secretType = value.AssemblyQualifiedName;
        }

        [JsonProperty] public User Friend { get; set; }

        #endregion

        #region constructors

        public User()
        {
        }

        public User(string login, Type secretType)
            : this()
        {
            Login = login;
            SecretType = secretType;
        }

        #endregion

        #region methods

        public override int GetHashCode() =>
            SecretType.GetHashCode();

        public override string ToString() =>
            $"SecretType: {secretType}, Login: {Login}";

        #endregion
    }

    [Fact]
    public void DeserializeTypeWithDubiousGetHashcode()
    {
        var user1 = new User("Peter", typeof(Version));
        var user2 = new User("Michael", typeof(Version));

        user1.Friend = user2;

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        var json = JsonConvert.SerializeObject(user1, Formatting.Indented, settings);

        var deserializedUser = JsonConvert.DeserializeObject<User>(json, settings);
        Assert.NotNull(deserializedUser);
    }

    [Fact]
    public void PreserveReferencesHandlingWithReusedJsonSerializer()
    {
        var c = new MyClass();

        var myClasses1 = new List<MyClass>
        {
            c,
            c
        };

        var serializer = new JsonSerializer
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All
        };

        var memoryStream = new MemoryStream();

        using (var streamWriter = new StreamWriter(memoryStream))
        using (var jsonWriter = new JsonTextWriter(streamWriter) {Formatting = Formatting.Indented})
        {
            serializer.Serialize(jsonWriter, myClasses1);
        }

        var data = memoryStream.ToArray();
        var json = Encoding.UTF8.GetString(data, 0, data.Length);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "$values": [
                {
                  "$id": "2",
                  "PreProperty": 0,
                  "PostProperty": 0
                },
                {
                  "$ref": "2"
                }
              ]
            }
            """,
            json);

        memoryStream = new(data);
        IList<MyClass> myClasses2;

        using (var sr = new StreamReader(memoryStream))
        using (var reader = new JsonTextReader(sr))
        {
            myClasses2 = serializer.Deserialize<IList<MyClass>>(reader);
        }

        Assert.Equal(2, myClasses2.Count);
        Assert.Equal(myClasses2[0], myClasses2[1]);

        Assert.NotEqual(myClasses1[0], myClasses2[0]);
    }

    [Fact]
    public void ReferencedIntList()
    {
        var l = new ReferencedList<int>
        {
            1,
            2,
            3
        };

        var json = JsonConvert.SerializeObject(l, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            [
              1,
              2,
              3
            ]
            """,
            json);
    }

    [Fact]
    public void ReferencedComponentList()
    {
        var c1 = new TestComponentSimple();

        var l = new ReferencedList<TestComponentSimple>
        {
            c1,
            new(),
            c1
        };

        var json = JsonConvert.SerializeObject(l, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "$id": "1",
                "MyProperty": 0
              },
              {
                "$id": "2",
                "MyProperty": 0
              },
              {
                "$ref": "1"
              }
            ]
            """,
            json);
    }

    [Fact]
    public void ReferencedIntDictionary()
    {
        var l = new ReferencedDictionary<int>
        {
            {"First", 1},
            {"Second", 2},
            {"Third", 3}
        };

        var json = JsonConvert.SerializeObject(l, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "First": 1,
              "Second": 2,
              "Third": 3
            }
            """,
            json);
    }

    [Fact]
    public void ReferencedComponentDictionary()
    {
        var c1 = new TestComponentSimple();

        var l = new ReferencedDictionary<TestComponentSimple>
        {
            {"First", c1},
            {"Second", new TestComponentSimple()},
            {"Third", c1}
        };

        var json = JsonConvert.SerializeObject(l, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "First": {
                "$id": "1",
                "MyProperty": 0
              },
              "Second": {
                "$id": "2",
                "MyProperty": 0
              },
              "Third": {
                "$ref": "1"
              }
            }
            """,
            json);

        var d = JsonConvert.DeserializeObject<ReferencedDictionary<TestComponentSimple>>(json);
        Assert.Equal(3, d.Count);
        Assert.True(ReferenceEquals(d["First"], d["Third"]));
    }

    [Fact]
    public void ReferencedObjectItems()
    {
        var o1 = new ReferenceObject
        {
            Component1 = new() {MyProperty = 1}
        };

        o1.Component2 = o1.Component1;
        o1.ComponentNotReference = new();
        o1.String = "String!";
        o1.Integer = int.MaxValue;

        var json = JsonConvert.SerializeObject(o1, Formatting.Indented);
        var expected = """
            {
              "Component1": {
                "$id": "1",
                "MyProperty": 1
              },
              "Component2": {
                "$ref": "1"
              },
              "ComponentNotReference": {
                "MyProperty": 0
              },
              "String": "String!",
              "Integer": 2147483647
            }
            """;
        XUnitAssert.AreEqualNormalized(expected, json);

        var referenceObject = JsonConvert.DeserializeObject<ReferenceObject>(json);
        Assert.NotNull(referenceObject);

        Assert.True(ReferenceEquals(referenceObject.Component1, referenceObject.Component2));
    }

    [Fact]
    public void PropertyItemIsReferenceObject()
    {
        var c1 = new TestComponentSimple();

        var o1 = new PropertyItemIsReferenceObject
        {
            Data = new()
            {
                Prop1 = c1,
                Prop2 = c1,
                Data =
                [
                    c1
                ]
            }
        };

        var json = JsonConvert.SerializeObject(o1, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Data": {
                "Prop1": {
                  "$id": "1",
                  "MyProperty": 0
                },
                "Prop2": {
                  "$ref": "1"
                },
                "Data": {
                  "$id": "2",
                  "$values": [
                    {
                      "MyProperty": 0
                    }
                  ]
                }
              }
            }
            """,
            json);

        var o2 = JsonConvert.DeserializeObject<PropertyItemIsReferenceObject>(json);

        var c2 = o2.Data.Prop1;
        var c3 = o2.Data.Prop2;
        var c4 = o2.Data.Data[0];

        Assert.True(ReferenceEquals(c2, c3));
        Assert.False(ReferenceEquals(c2, c4));
    }

    [Fact]
    public void DuplicateId()
    {
        var json = """
            {
              "Data": {
                "Prop1": {
                  "$id": "1",
                  "MyProperty": 0
                },
                "Prop2": {
                  "$id": "1",
                  "MyProperty": 0
                }
              }
            }
            """;

        var settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Default
        };

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<PropertyItemIsReferenceObject>(json, settings));
        Assert.Equal("Error reading object reference '1'. Path 'Data.Prop2.MyProperty', line 9, position 19.", exception.Message);
    }
}

public class PropertyItemIsReferenceBody
{
    public TestComponentSimple Prop1 { get; set; }
    public TestComponentSimple Prop2 { get; set; }
    public IList<TestComponentSimple> Data { get; set; }
}

public class PropertyItemIsReferenceObject
{
    [JsonProperty(ItemIsReference = true)] public PropertyItemIsReferenceBody Data { get; set; }
}

public class PropertyItemIsReferenceList
{
    [JsonProperty(ItemIsReference = true)] public IList<IList<object>> Data { get; set; }
}

[JsonArray(ItemIsReference = true)]
public class ReferencedList<T> : List<T>;

[JsonDictionary(ItemIsReference = true)]
public class ReferencedDictionary<T> : Dictionary<string, T>;

[JsonObject(ItemIsReference = true)]
public class ReferenceObject
{
    public TestComponentSimple Component1 { get; set; }
    public TestComponentSimple Component2 { get; set; }

    [JsonProperty(IsReference = false)] public TestComponentSimple ComponentNotReference { get; set; }

    public string String { get; set; }
    public int Integer { get; set; }
}
