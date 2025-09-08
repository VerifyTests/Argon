// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Collections.ObjectModel;
using System.Dynamic;
using System.Net;
using TestObjects;
// ReSharper disable UnusedVariable

public class TypeNameHandlingTests : TestFixtureBase
{
    [Fact]
    public void SerializeMultidimensionalByteArrayWithTypeName()
    {
        var array2dRef = typeof(byte[,]).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);
        var array3dRef = typeof(byte[,,]).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        var o = new HasMultidimensionalByteArray
        {
            Array2D = new byte[,]
            {
                {
                    1,
                    2
                },
                {
                    2,
                    4
                },
                {
                    3,
                    6
                }
            },
            Array3D = new byte[,,]
            {
                {
                    {
                        1,
                        2,
                        3
                    },
                    {
                        4,
                        5,
                        6
                    }
                }
            }
        };

        var json = JsonConvert.SerializeObject(o, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        });

        var expectedJson = $$"""
            {
              "$type": "TestObjects.HasMultidimensionalByteArray, ArgonTests",
              "Array2D": {
                "$type": "{{array2dRef}}",
                "$values": [
                  [
                    1,
                    2
                  ],
                  [
                    2,
                    4
                  ],
                  [
                    3,
                    6
                  ]
                ]
              },
              "Array3D": {
                "$type": "{{array3dRef}}",
                "$values": [
                  [
                    [
                      1,
                      2,
                      3
                    ],
                    [
                      4,
                      5,
                      6
                    ]
                  ]
                ]
              }
            }
            """;

        XUnitAssert.AreEqualNormalized(expectedJson, json);
    }

    [Fact]
    public void DeserializeMultidimensionalByteArrayWithTypeName()
    {
        var json = """
            {
              "$type": "TestObjects.HasMultidimensionalByteArray, ArgonTests",
              "Array2D": {
                "$type": "System.Byte[,], mscorlib",
                "$values": [
                  [
                    1,
                    2
                  ],
                  [
                    2,
                    4
                  ],
                  [
                    3,
                    6
                  ]
                ]
              },
              "Array3D": {
                "$type": "System.Byte[,,], mscorlib",
                "$values": [
                  [
                    [
                      1,
                      2,
                      3
                    ],
                    [
                      4,
                      5,
                      6
                    ]
                  ]
                ]
              }
            }
            """;
        var value = JsonConvert.DeserializeObject<HasMultidimensionalByteArray>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        });

        Assert.Equal(1, value.Array2D[0, 0]);
        Assert.Equal(2, value.Array2D[0, 1]);
        Assert.Equal(2, value.Array2D[1, 0]);
        Assert.Equal(4, value.Array2D[1, 1]);
        Assert.Equal(3, value.Array2D[2, 0]);
        Assert.Equal(6, value.Array2D[2, 1]);

        Assert.Equal(1, value.Array3D[0, 0, 0]);
        Assert.Equal(2, value.Array3D[0, 0, 1]);
        Assert.Equal(3, value.Array3D[0, 0, 2]);
        Assert.Equal(4, value.Array3D[0, 1, 0]);
        Assert.Equal(5, value.Array3D[0, 1, 1]);
        Assert.Equal(6, value.Array3D[0, 1, 2]);
    }

    [Fact]
    public void DeserializeByteArrayWithTypeName()
    {
        var json = """
            {
              "$type": "TestObjects.HasByteArray, ArgonTests",
              "EncryptedPassword": {
                "$type": "System.Byte[], mscorlib",
                "$value": "cGFzc3dvcmQ="
              }
            }
            """;
        var value = JsonConvert.DeserializeObject<HasByteArray>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        });

        Assert.Equal(Convert.FromBase64String("cGFzc3dvcmQ="), value.EncryptedPassword);
    }

    [Fact]
    public void DeserializeByteArrayWithTypeName_BadAdditionalContent()
    {
        var json = """
            {
              "$type": "TestObjects.HasByteArray, ArgonTests",
              "EncryptedPassword": {
                "$type": "System.Byte[], mscorlib",
                "$value": "cGFzc3dvcmQ=",
                "$value": "cGFzc3dvcmQ="
              }
            }
            """;

        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<HasByteArray>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        }));
        Assert.Equal("Error reading bytes. Unexpected token: PropertyName. Path 'EncryptedPassword.$value', line 6, position 13.", exception.Message);
    }

    [Fact]
    public void DeserializeByteArrayWithTypeName_ExtraProperty()
    {
        var json = """
            {
              "$type": "TestObjects.HasByteArray, ArgonTests",
              "EncryptedPassword": {
                "$type": "System.Byte[], mscorlib",
                "$value": "cGFzc3dvcmQ="
              },
              "Pie": null
            }
            """;
        var value = JsonConvert.DeserializeObject<HasByteArray>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        });

        Assert.NotNull(value.EncryptedPassword);
        Assert.Equal(Convert.FromBase64String("cGFzc3dvcmQ="), value.EncryptedPassword);
    }

    [Fact]
    public void SerializeValueTupleWithTypeName()
    {
        var tupleRef = typeof(ValueTuple<int, int, string>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        var tuple = ValueTuple.Create(1, 2, "string");

        var json = JsonConvert.SerializeObject(tuple, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });

        XUnitAssert.AreEqualNormalized($$"""
            {
              "$type": "{{tupleRef}}",
              "Item1": 1,
              "Item2": 2,
              "Item3": "string"
            }
            """,
            json);

        var t2 = (ValueTuple<int, int, string>) JsonConvert.DeserializeObject(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });

        Assert.Equal(1, t2.Item1);
        Assert.Equal(2, t2.Item2);
        Assert.Equal("string", t2.Item3);
    }

    public class KnownAutoTypes
    {
        public ICollection<string> Collection { get; set; }
        public IList<string> List { get; set; }
        public IDictionary<string, string> Dictionary { get; set; }
        public ISet<string> Set { get; set; }
        public IReadOnlyCollection<string> ReadOnlyCollection { get; set; }
        public IReadOnlyList<string> ReadOnlyList { get; set; }
        public IReadOnlyDictionary<string, string> ReadOnlyDictionary { get; set; }
    }

    [Fact]
    public void KnownAutoTypesTest()
    {
        var c = new KnownAutoTypes
        {
            Collection =
            [
                "Collection value!"
            ],
            List =
            [
                "List value!"
            ],
            Dictionary = new Dictionary<string, string>
            {
                {
                    "Dictionary key!", "Dictionary value!"
                }
            },
            ReadOnlyCollection = new ReadOnlyCollection<string>(["Read Only Collection value!"]),
            ReadOnlyList = new ReadOnlyCollection<string>(["Read Only List value!"]),
            Set = new HashSet<string>
            {
                "Set value!"
            },
            ReadOnlyDictionary = new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>
                {
                    {
                        "Read Only Dictionary key!", "Read Only Dictionary value!"
                    }
                })
        };

        var json = JsonConvert.SerializeObject(
            c,
            Formatting.Indented,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Collection": [
                "Collection value!"
              ],
              "List": [
                "List value!"
              ],
              "Dictionary": {
                "Dictionary key!": "Dictionary value!"
              },
              "Set": [
                "Set value!"
              ],
              "ReadOnlyCollection": [
                "Read Only Collection value!"
              ],
              "ReadOnlyList": [
                "Read Only List value!"
              ],
              "ReadOnlyDictionary": {
                "Read Only Dictionary key!": "Read Only Dictionary value!"
              }
            }
            """,
            json);
    }

    [Fact]
    public void DictionaryAuto()
    {
        var dic = new Dictionary<string, object>
        {
            {
                "movie", new Movie
                {
                    Name = "Die Hard"
                }
            }
        };

        var json = JsonConvert.SerializeObject(dic, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "movie": {
                "$type": "TestObjects.Movie, ArgonTests",
                "Name": "Die Hard",
                "Description": null,
                "Classification": null,
                "Studio": null,
                "ReleaseDate": null,
                "ReleaseCountries": null
              }
            }
            """,
            json);
    }

    [Fact]
    public void KeyValuePairAuto()
    {
        var dic = new List<KeyValuePair<string, object>>
        {
            new("movie", new Movie
            {
                Name = "Die Hard"
            })
        };

        var json = JsonConvert.SerializeObject(dic, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "Key": "movie",
                "Value": {
                  "$type": "TestObjects.Movie, ArgonTests",
                  "Name": "Die Hard",
                  "Description": null,
                  "Classification": null,
                  "Studio": null,
                  "ReleaseDate": null,
                  "ReleaseCountries": null
                }
              }
            ]
            """,
            json);
    }

    [Fact]
    public void NestedValueObjects()
    {
        var stringBuilder = new StringBuilder();
        for (var i = 0; i < 3; i++)
        {
            stringBuilder.Append("""{"$value":""");
        }

        var reader = new JsonTextReader(new StringReader(stringBuilder.ToString()));
        var serializer = new JsonSerializer
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Default
        };
        var exception = Assert.Throws<JsonSerializationException>(() => serializer.Deserialize<sbyte>(reader));
        Assert.Equal("Unexpected token when deserializing primitive value: StartObject. Path '$value', line 1, position 11.", exception.Message);
    }

    [Fact]
    public void SerializeRootTypeNameIfDerivedWithAuto()
    {
        var serializer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
        var stringWriter = new StringWriter();
        serializer.Serialize(new JsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented
        }, new WagePerson(), typeof(Person));
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$type": "TestObjects.WagePerson, ArgonTests",
              "HourlyWage": 0.0,
              "Name": null,
              "BirthDate": "0001-01-01T00:00:00",
              "LastModified": "0001-01-01T00:00:00"
            }
            """,
            result);

        Assert.Contains("WagePerson", result);
        using var rd = new JsonTextReader(new StringReader(result));
        var person = serializer.Deserialize<Person>(rd);

        Assert.IsType<WagePerson>(person);
    }

    [Fact]
    public void SerializeRootTypeNameAutoWithJsonConvert()
    {
        var json = JsonConvert.SerializeObject(new WagePerson(), typeof(object), Formatting.Indented, new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$type": "TestObjects.WagePerson, ArgonTests",
              "HourlyWage": 0.0,
              "Name": null,
              "BirthDate": "0001-01-01T00:00:00",
              "LastModified": "0001-01-01T00:00:00"
            }
            """,
            json);
    }

    [Fact]
    public void SerializeRootTypeNameAutoWithJsonConvert_Generic()
    {
        var json = JsonConvert.SerializeObject(new WagePerson(), typeof(object), Formatting.Indented, new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$type": "TestObjects.WagePerson, ArgonTests",
              "HourlyWage": 0.0,
              "Name": null,
              "BirthDate": "0001-01-01T00:00:00",
              "LastModified": "0001-01-01T00:00:00"
            }
            """,
            json);
    }

    [Fact]
    public void SerializeRootTypeNameAutoWithJsonConvert_Generic2()
    {
        var json = JsonConvert.SerializeObject(new WagePerson(), typeof(object), new()
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        XUnitAssert.AreEqualNormalized("""{"$type":"TestObjects.WagePerson, ArgonTests","HourlyWage":0.0,"Name":null,"BirthDate":"0001-01-01T00:00:00","LastModified":"0001-01-01T00:00:00"}""", json);
    }

    public class Wrapper
    {
        public IList<EmployeeReference> Array { get; set; }
        public IDictionary<string, EmployeeReference> Dictionary { get; set; }
    }

    [Fact]
    public void SerializeWrapper()
    {
        var wrapper = new Wrapper
        {
            Array = new List<EmployeeReference>
            {
                new()
            },
            Dictionary = new Dictionary<string, EmployeeReference>
            {
                {
                    "First", new()
                }
            }
        };

        var json = JsonConvert.SerializeObject(wrapper, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Array": [
                {
                  "$id": "1",
                  "Name": null,
                  "Manager": null
                }
              ],
              "Dictionary": {
                "First": {
                  "$id": "2",
                  "Name": null,
                  "Manager": null
                }
              }
            }
            """,
            json);

        var w2 = JsonConvert.DeserializeObject<Wrapper>(json);
        Assert.IsType<List<EmployeeReference>>(w2.Array);
        Assert.IsType<Dictionary<string, EmployeeReference>>(w2.Dictionary);
    }

    [Fact]
    public void WriteTypeNameForObjects()
    {
        var employeeRef = typeof(EmployeeReference).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        var employee = new EmployeeReference();

        var json = JsonConvert.SerializeObject(employee, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        });

        XUnitAssert.AreEqualNormalized($$"""
            {
              "$id": "1",
              "$type": "{{employeeRef}}",
              "Name": null,
              "Manager": null
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeTypeName()
    {
        var employeeRef = typeof(EmployeeReference).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        var json = $$"""
            {
              "$id": "1",
              "$type": "{{employeeRef}}",
              "Name": "Name!",
              "Manager": null
            }
            """;

        var employee = JsonConvert.DeserializeObject(json, null, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        });

        Assert.IsType<EmployeeReference>(employee);
        Assert.Equal("Name!", ((EmployeeReference) employee).Name);
    }

    [Fact]
    public void DeserializeTypeNameFromGacAssembly()
    {
        var cookieRef = typeof(Cookie).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        var json = $$"""
            {
              "$id": "1",
              "$type": "{{cookieRef}}"
            }
            """;

        var cookie = JsonConvert.DeserializeObject(json, null, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        });

        Assert.IsType<Cookie>(cookie);
    }

    [Fact]
    public void SerializeGenericObjectListWithTypeName()
    {
        var employeeRef = typeof(EmployeeReference).AssemblyQualifiedName;
        var personRef = typeof(Person).AssemblyQualifiedName;

        var values = new List<object>
        {
            new EmployeeReference
            {
                Name = "Bob",
                Manager = new()
                {
                    Name = "Frank"
                }
            },
            new Person
            {
                Department = "Department",
                BirthDate = new(2000, 12, 30, 0, 0, 0, DateTimeKind.Utc),
                LastModified = new(2000, 12, 30, 0, 0, 0, DateTimeKind.Utc)
            },
            "String!",
            int.MinValue
        };

        var json = JsonConvert.SerializeObject(values, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        });

        XUnitAssert.AreEqualNormalized(
            $$"""
            [
              {
                "$id": "1",
                "$type": "{{employeeRef}}",
                "Name": "Bob",
                "Manager": {
                  "$id": "2",
                  "$type": "{{employeeRef}}",
                  "Name": "Frank",
                  "Manager": null
                }
              },
              {
                "$type": "{{personRef}}",
                "Name": null,
                "BirthDate": "2000-12-30T00:00:00Z",
                "LastModified": "2000-12-30T00:00:00Z"
              },
              "String!",
              -2147483648
            ]
            """,
            json);
    }

    [Fact]
    public void DeserializeGenericObjectListWithTypeName()
    {
        var employeeRef = typeof(EmployeeReference).AssemblyQualifiedName;
        var personRef = typeof(Person).AssemblyQualifiedName;

        var json = $$"""
            [
              {
                "$id": "1",
                "$type": "{{employeeRef}}",
                "Name": "Bob",
                "Manager": {
                  "$id": "2",
                  "$type": "{{employeeRef}}",
                  "Name": "Frank",
                  "Manager": null
                }
              },
              {
                "$type": "{{personRef}}",
                "Name": null,
                "BirthDate": "2013-08-14T00:00:00.000",
                "LastModified": "2013-08-14T00:00:00.000"
              },
              "String!",
              -2147483648
            ]
            """;

        var values = (List<object>) JsonConvert.DeserializeObject(json, typeof(List<object>), new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        });

        Assert.Equal(4, values.Count);

        var e = (EmployeeReference) values[0];
        var p = (Person) values[1];

        Assert.Equal("Bob", e.Name);
        Assert.Equal("Frank", e.Manager.Name);

        Assert.Null(p.Name);
        Assert.Equal(new(2013, 8, 14, 0, 0, 0, DateTimeKind.Utc), p.BirthDate);
        Assert.Equal(new(2013, 8, 14, 0, 0, 0, DateTimeKind.Utc), p.LastModified);

        Assert.Equal("String!", values[2]);
        Assert.Equal((long) int.MinValue, values[3]);
    }

    [Fact]
    public void DeserializeWithBadTypeName()
    {
        var employeeRef = typeof(EmployeeReference).AssemblyQualifiedName;
        var personRef = typeof(Person).AssemblyQualifiedName;

        var json = $$"""
            {
              "$id": "1",
              "$type": "{{employeeRef}}",
              "Name": "Name!",
              "Manager": null
            }
            """;

        try
        {
            JsonConvert.DeserializeObject(json, typeof(Person), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
            });
        }
        catch (JsonSerializationException exception)
        {
            Assert.StartsWith($"Type specified in JSON '{employeeRef}' is not compatible with '{personRef}'.", exception.Message);
        }
    }

    [Fact]
    public void DeserializeTypeNameWithNoTypeNameHandling()
    {
        var employeeRef = typeof(EmployeeReference).AssemblyQualifiedName;

        var json = $$"""
            {
              "$id": "1",
              "$type": "{{employeeRef}}",
              "Name": "Name!",
              "Manager": null
            }
            """;

        var o = (JObject) JsonConvert.DeserializeObject(json);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "Name!",
              "Manager": null
            }
            """,
            o.ToString());
    }

    [Fact]
    public void DeserializeTypeNameOnly()
    {
        var json = """
            {
              "$id": "1",
              "$type": "TestObjects.Employee",
              "Name": "Name!",
              "Manager": null
            }
            """;

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject(json, null, settings));
        Assert.Equal("Type specified in JSON 'TestObjects.Employee' was not resolved. Path '$type', line 3, position 33.", exception.Message);
    }

    public interface ICorrelatedMessage
    {
        string CorrelationId { get; set; }
    }

    public class SendHttpRequest : ICorrelatedMessage
    {
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> RequestData;
        public string RequestBodyText { get; set; }
        public string User { get; set; }
        public string Passwd { get; set; }
        public string RequestEncoding { get; set; } = "UTF-8";
        public string CorrelationId { get; set; }
    }

    [Fact]
    public void DeserializeGenericTypeName()
    {
        var typeName = typeof(SendHttpRequest).AssemblyQualifiedName;

        var json = $$"""
            {
                "$type": "{{typeName}}",
                "RequestData": {
                    "$type": "System.Collections.Generic.Dictionary`2[[System.String, mscorlib,Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                    "Id": "siedemnaście",
                    "X": "323"
                },
                "Method": "GET",
                "Url": "http://www.onet.pl",
                "RequestEncoding": "UTF-8",
                "CorrelationId": "xyz"
            }
            """;

        var message = JsonConvert.DeserializeObject<ICorrelatedMessage>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        });

        Assert.IsType<SendHttpRequest>(message);

        var request = (SendHttpRequest) message;
        Assert.Equal("xyz", request.CorrelationId);
        Assert.Equal(2, request.RequestData.Count);
        Assert.Equal("siedemnaście", request.RequestData["Id"]);
    }

    [Fact]
    public void SerializeObjectWithMultipleGenericLists()
    {
        var containerTypeName = typeof(Container).AssemblyQualifiedName;
        var productListTypeName = typeof(List<Product>).AssemblyQualifiedName;

        var container = new Container
        {
            In = new List<Product>(),
            Out = new List<Product>()
        };

        var json = JsonConvert.SerializeObject(container, Formatting.Indented,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
            });

        XUnitAssert.AreEqualNormalized($$"""
            {
              "$type": "{{containerTypeName}}",
              "In": {
                "$type": "{{productListTypeName}}",
                "$values": []
              },
              "Out": {
                "$type": "{{productListTypeName}}",
                "$values": []
              }
            }
            """,
            json);
    }

    public class TypeNameProperty
    {
        public string Name { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
        public object Value { get; set; }
    }

    [Fact]
    public void WriteObjectTypeNameForProperty()
    {
        var typeNamePropertyRef = typeof(TypeNameProperty).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        var typeNameProperty = new TypeNameProperty
        {
            Name = "Name!",
            Value = new TypeNameProperty
            {
                Name = "Nested!"
            }
        };

        var json = JsonConvert.SerializeObject(typeNameProperty, Formatting.Indented);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "Name": "Name!",
              "Value": {
                "$type": "{{typeNamePropertyRef}}",
                "Name": "Nested!",
                "Value": null
              }
            }
            """,
            json);

        var deserialized = JsonConvert.DeserializeObject<TypeNameProperty>(json);
        Assert.Equal("Name!", deserialized.Name);
        Assert.IsType<TypeNameProperty>(deserialized.Value);

        var nested = (TypeNameProperty) deserialized.Value;
        Assert.Equal("Nested!", nested.Name);
        Assert.Null(nested.Value);
    }

    [Fact]
    public void WriteListTypeNameForProperty()
    {
        var listRef = typeof(List<int>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        var typeNameProperty = new TypeNameProperty
        {
            Name = "Name!",
            Value = new List<int>
            {
                1,
                2,
                3,
                4,
                5
            }
        };

        var json = JsonConvert.SerializeObject(typeNameProperty, Formatting.Indented);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "Name": "Name!",
              "Value": {
                "$type": "{{listRef}}",
                "$values": [
                  1,
                  2,
                  3,
                  4,
                  5
                ]
              }
            }
            """,
            json);

        var deserialized = JsonConvert.DeserializeObject<TypeNameProperty>(json);
        Assert.Equal("Name!", deserialized.Name);
        Assert.IsType<List<int>>(deserialized.Value);

        var nested = (List<int>) deserialized.Value;
        Assert.Equal(5, nested.Count);
        Assert.Equal(1, nested[0]);
        Assert.Equal(2, nested[1]);
        Assert.Equal(3, nested[2]);
        Assert.Equal(4, nested[3]);
        Assert.Equal(5, nested[4]);
    }

    [Fact]
    public void DeserializeUsingCustomBinder()
    {
        var json = """
            {
              "$id": "1",
              "$type": "TestObjects.Employee",
              "Name": "Name!"
            }
            """;

        var p = JsonConvert.DeserializeObject(json, null, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            SerializationBinder = new CustomSerializationBinder()
        });

        Assert.IsType<Person>(p);

        var person = (Person) p;

        Assert.Equal("Name!", person.Name);
    }

    public class CustomSerializationBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName) =>
            typeof(Person);

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }

    [Fact]
    public void SerializeUsingCustomBinder()
    {
        var binder = new TypeNameSerializationBinder("Argon.Tests.Serialization.{0}, ArgonTests");

        var values = new List<object>
        {
            new Customer
            {
                Name = "Caroline Customer"
            },
            new Purchase
            {
                ProductName = "Elbow Grease",
                Price = 5.99m,
                Quantity = 1
            }
        };

        var json = JsonConvert.SerializeObject(values, Formatting.Indented,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = binder
            });

        //[
        //  {
        //    "$type": "Customer",
        //    "Name": "Caroline Customer"
        //  },
        //  {
        //    "$type": "Purchase",
        //    "ProductName": "Elbow Grease",
        //    "Price": 5.99,
        //    "Quantity": 1
        //  }
        //]

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "$type": "Customer",
                "Name": "Caroline Customer"
              },
              {
                "$type": "Purchase",
                "ProductName": "Elbow Grease",
                "Price": 5.99,
                "Quantity": 1
              }
            ]
            """,
            json);

        var newValues = JsonConvert.DeserializeObject<IList<object>>(
            json,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = new TypeNameSerializationBinder("TypeNameHandlingTests+{0}, ArgonTests")
            });

        Assert.IsType<Customer>(newValues[0]);
        var customer = (Customer) newValues[0];
        Assert.Equal("Caroline Customer", customer.Name);

        Assert.IsType<Purchase>(newValues[1]);
        var purchase = (Purchase) newValues[1];
        Assert.Equal("Elbow Grease", purchase.ProductName);
    }

    public class TypeNameSerializationBinder(string typeFormat) : ISerializationBinder
    {
        public string TypeFormat { get; } = typeFormat;

        public Type BindToType(string assemblyName, string typeName)
        {
            var resolvedTypeName = string.Format(TypeFormat, typeName);

            return Type.GetType(resolvedTypeName, true);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }

    [Fact]
    public void NewSerializeUsingCustomBinder()
    {
        var binder = new NewTypeNameSerializationBinder("Argon.Tests.Serialization.{0}, ArgonTests");

        var values = new List<object>
        {
            new Customer
            {
                Name = "Caroline Customer"
            },
            new Purchase
            {
                ProductName = "Elbow Grease",
                Price = 5.99m,
                Quantity = 1
            }
        };

        var json = JsonConvert.SerializeObject(values, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = binder
        });

        //[
        //  {
        //    "$type": "Customer",
        //    "Name": "Caroline Customer"
        //  },
        //  {
        //    "$type": "Purchase",
        //    "ProductName": "Elbow Grease",
        //    "Price": 5.99,
        //    "Quantity": 1
        //  }
        //]

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "$type": "Customer",
                "Name": "Caroline Customer"
              },
              {
                "$type": "Purchase",
                "ProductName": "Elbow Grease",
                "Price": 5.99,
                "Quantity": 1
              }
            ]
            """,
            json);

        var newValues = JsonConvert.DeserializeObject<IList<object>>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = new NewTypeNameSerializationBinder("TypeNameHandlingTests+{0}, ArgonTests")
        });

        Assert.IsType<Customer>(newValues[0]);
        var customer = (Customer) newValues[0];
        Assert.Equal("Caroline Customer", customer.Name);

        Assert.IsType<Purchase>(newValues[1]);
        var purchase = (Purchase) newValues[1];
        Assert.Equal("Elbow Grease", purchase.ProductName);
    }

    public class NewTypeNameSerializationBinder(string typeFormat) : ISerializationBinder
    {
        public string TypeFormat { get; } = typeFormat;

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            var resolvedTypeName = string.Format(TypeFormat, typeName);

            return Type.GetType(resolvedTypeName, true);
        }
    }

    [Fact]
    public void CollectionWithAbstractItems()
    {
        var testObject = new HolderClass
        {
            TestMember = new ContentSubClass("First One"),
            AnotherTestMember = new()
            {
                {
                    1, new List<ContentBaseClass>()
                }
            }
        };
        testObject.AnotherTestMember[1].Add(new ContentSubClass("Second One"));
        testObject.AThirdTestMember = new ContentSubClass("Third One");

        var serializingTester = new JsonSerializer
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            serializingTester.TypeNameHandling = TypeNameHandling.Auto;
            serializingTester.Serialize(jsonWriter, testObject);
        }

        var json = stringWriter.ToString();

        var contentSubClassRef = typeof(ContentSubClass).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);
        var dictionaryRef = typeof(Dictionary<int, IList<ContentBaseClass>>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);
        var listRef = typeof(List<ContentBaseClass>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        var expected = $$"""
            {
              "TestMember": {
                "$type": "{{contentSubClassRef}}",
                "SomeString": "First One"
              },
              "AnotherTestMember": {
                "$type": "{{dictionaryRef}}",
                "1": [
                  {
                    "$type": "{{contentSubClassRef}}",
                    "SomeString": "Second One"
                  }
                ]
              },
              "AThirdTestMember": {
                "$type": "{{contentSubClassRef}}",
                "SomeString": "Third One"
              }
            }
            """;

        XUnitAssert.AreEqualNormalized(expected, json);

        var sr = new StringReader(json);

        var deserializingTester = new JsonSerializer();

        HolderClass anotherTestObject;

        using (var jsonReader = new JsonTextReader(sr))
        {
            deserializingTester.TypeNameHandling = TypeNameHandling.Auto;

            anotherTestObject = deserializingTester.Deserialize<HolderClass>(jsonReader);
        }

        Assert.NotNull(anotherTestObject);
        Assert.IsType<ContentSubClass>(anotherTestObject.TestMember);
        Assert.IsType<Dictionary<int, IList<ContentBaseClass>>>(anotherTestObject.AnotherTestMember);
        Assert.Single(anotherTestObject.AnotherTestMember);

        var list = anotherTestObject.AnotherTestMember[1];

        Assert.IsType<List<ContentBaseClass>>(list);
        Assert.Single(list);
        Assert.IsType<ContentSubClass>(list[0]);
    }

    [Fact]
    public void WriteObjectTypeNameForPropertyDemo()
    {
        var message = new Message
        {
            Address = "http://www.google.com",
            Body = new SearchDetails
            {
                Query = "Json.NET",
                Language = "en-us"
            }
        };

        var json = JsonConvert.SerializeObject(message, Formatting.Indented);
        // {
        //   "Address": "http://www.google.com",
        //   "Body": {
        //     "$type": "Argon.Tests.Serialization.SearchDetails, ArgonTests",
        //     "Query": "Json.NET",
        //     "Language": "en-us"
        //   }
        // }

        var deserialized = JsonConvert.DeserializeObject<Message>(json);

        var searchDetails = (SearchDetails) deserialized.Body;
        // Json.NET
    }

    public class UrlStatus
    {
        public int Status { get; set; }
        public string Url { get; set; }
    }

    [Fact]
    public void GenericDictionaryObject()
    {
        var collection = new Dictionary<string, object>
        {
            {
                "First", new UrlStatus
                {
                    Status = 404,
                    Url = "http://www.bing.com"
                }
            },
            {
                "Second", new UrlStatus
                {
                    Status = 400,
                    Url = "http://www.google.com"
                }
            },
            {
                "List", new List<UrlStatus>
                {
                    new()
                    {
                        Status = 300,
                        Url = "http://www.yahoo.com"
                    },
                    new()
                    {
                        Status = 200,
                        Url = "http://www.askjeeves.com"
                    }
                }
            }
        };

        var json = JsonConvert.SerializeObject(collection, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        });

        var dictionaryTypeName = typeof(Dictionary<string, object>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);
        var urlStatusTypeName = typeof(UrlStatus).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);
        var listTypeName = typeof(List<UrlStatus>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "$type": "{{dictionaryTypeName}}",
              "First": {
                "$type": "{{urlStatusTypeName}}",
                "Status": 404,
                "Url": "http://www.bing.com"
              },
              "Second": {
                "$type": "{{urlStatusTypeName}}",
                "Status": 400,
                "Url": "http://www.google.com"
              },
              "List": {
                "$type": "{{listTypeName}}",
                "$values": [
                  {
                    "$type": "{{urlStatusTypeName}}",
                    "Status": 300,
                    "Url": "http://www.yahoo.com"
                  },
                  {
                    "$type": "{{urlStatusTypeName}}",
                    "Status": 200,
                    "Url": "http://www.askjeeves.com"
                  }
                ]
              }
            }
            """,
            json);

        var c = JsonConvert.DeserializeObject(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        });

        Assert.IsType<Dictionary<string, object>>(c);

        var newCollection = (Dictionary<string, object>) c;
        Assert.Equal(3, newCollection.Count);
        Assert.Equal("http://www.bing.com", ((UrlStatus) newCollection["First"]).Url);

        var statues = (List<UrlStatus>) newCollection["List"];
        Assert.Equal(2, statues.Count);
    }

    [Fact]
    public void SerializingIEnumerableOfTShouldRetainGenericTypeInfo()
    {
        var productClassRef = typeof(CustomEnumerable<Product>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        var products = new CustomEnumerable<Product>();

        var json = JsonConvert.SerializeObject(products, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });

        XUnitAssert.AreEqualNormalized($$"""
            {
              "$type": "{{productClassRef}}",
              "$values": []
            }
            """,
            json);
    }

    public class CustomEnumerable<T> : IEnumerable<T>
    {
        //NOTE: a simple linked list
        readonly T value;
        readonly CustomEnumerable<T> next;
        readonly int count;

        CustomEnumerable(T value, CustomEnumerable<T> next)
        {
            this.value = value;
            this.next = next;
            count = this.next.count + 1;
        }

        public CustomEnumerable() =>
            count = 0;

        public CustomEnumerable<T> AddFirst(T newVal) =>
            new(newVal, this);

        public IEnumerator<T> GetEnumerator()
        {
            if (count == 0) // last node
            {
                yield break;
            }

            yield return value;

            var nextInLine = next;
            while (nextInLine != null)
            {
                if (nextInLine.count != 0)
                {
                    yield return nextInLine.value;
                }

                nextInLine = nextInLine.next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    public class Car
    {
        // included in JSON
        public string Model { get; set; }
        public DateTime Year { get; set; }
        public List<string> Features { get; set; }
        public object[] Objects { get; set; }

        // ignored
        [JsonIgnore]
        public DateTime LastModified { get; set; }
    }

    [Fact]
    public void ByteArrays()
    {
        var testerObject = new Car
        {
            Year = new(2000, 10, 5, 1, 1, 1, DateTimeKind.Utc)
        };
        var data = "KARIRA"u8.ToArray();
        testerObject.Objects = [data, "prueba"];

        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.All
        };

        var output = JsonConvert.SerializeObject(testerObject, Formatting.Indented, settings);

        var carClassRef = typeof(Car).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);
        var objectArrayRef = typeof(object[]).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);
        var byteArrayRef = typeof(byte[]).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        XUnitAssert.AreEqualNormalized(output, $$"""
            {
              "$type": "{{carClassRef}}",
              "Year": "2000-10-05T01:01:01Z",
              "Objects": {
                "$type": "{{objectArrayRef}}",
                "$values": [
                  {
                    "$type": "{{byteArrayRef}}",
                    "$value": "S0FSSVJB"
                  },
                  "prueba"
                ]
              }
            }
            """);
        var obj = JsonConvert.DeserializeObject<Car>(output, settings);

        Assert.NotNull(obj);

        Assert.True(obj.Objects[0] is byte[]);

        var d = (byte[]) obj.Objects[0];
        Assert.Equal(data, d);
    }

    [Fact]
    public void SerializationBinderWithFullName()
    {
        var message = new Message
        {
            Address = "jamesnk@testtown.com",
            Body = new Version(1, 2, 3, 4)
        };

        var json = JsonConvert.SerializeObject(message, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
            SerializationBinder = new MetroBinder(),
            ContractResolver = new DefaultContractResolver()
        });

        var o = JObject.Parse(json);

        Assert.Equal(":::MESSAGE:::, AssemblyName", (string) o["$type"]);
    }

    class MetroBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName) =>
            null;

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = "AssemblyName";
#if NET6_0_OR_GREATER
            typeName = $":::{serializedType.Name.ToUpper()}:::";
#else
            typeName = $":::{serializedType.Name.ToUpper(InvariantCulture)}:::";
#endif
        }
    }

    [Fact]
    public void TypeNameIntList()
    {
        var l = new TypeNameList<int>
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
    public void TypeNameComponentList()
    {
        var c1 = new TestComponentSimple();

        var l = new TypeNameList<object>
        {
            c1,
            new Employee
            {
                BirthDate = new(2000, 12, 12, 12, 12, 12, DateTimeKind.Utc),
                Department = "Department!"
            },
            "String!",
            long.MaxValue
        };

        var json = JsonConvert.SerializeObject(l, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "$type": "TestObjects.TestComponentSimple, ArgonTests",
                "MyProperty": 0
              },
              {
                "$type": "TestObjects.Employee, ArgonTests",
                "FirstName": null,
                "LastName": null,
                "BirthDate": "2000-12-12T12:12:12Z",
                "Department": "Department!",
                "JobTitle": null
              },
              "String!",
              9223372036854775807
            ]
            """,
            json);

        var l2 = JsonConvert.DeserializeObject<TypeNameList<object>>(json);
        Assert.Equal(4, l2.Count);

        Assert.IsType<TestComponentSimple>(l2[0]);
        Assert.IsType<Employee>(l2[1]);
        Assert.IsType<string>(l2[2]);
        Assert.IsType<long>(l2[3]);
    }

    [Fact]
    public void TypeNameDictionaryTest()
    {
        var l = new TypeNameDictionary<object>
        {
            {
                "First", new TestComponentSimple
                {
                    MyProperty = 1
                }
            },
            {
                "Second", "String!"
            },
            {
                "Third", long.MaxValue
            }
        };

        var json = JsonConvert.SerializeObject(l, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "First": {
                "$type": "TestObjects.TestComponentSimple, ArgonTests",
                "MyProperty": 1
              },
              "Second": "String!",
              "Third": 9223372036854775807
            }
            """,
            json);

        var l2 = JsonConvert.DeserializeObject<TypeNameDictionary<object>>(json);
        Assert.Equal(3, l2.Count);

        Assert.IsType<TestComponentSimple>(l2["First"]);
        Assert.Equal(1, ((TestComponentSimple) l2["First"]).MyProperty);
        Assert.IsType<string>(l2["Second"]);
        Assert.IsType<long>(l2["Third"]);
    }

    [Fact]
    public void TypeNameObjectItems()
    {
        var o1 = new TypeNameObject
        {
            Object1 = new TestComponentSimple
            {
                MyProperty = 1
            },
            Object2 = 123,
            ObjectNotHandled = new TestComponentSimple
            {
                MyProperty = int.MaxValue
            },
            String = "String!",
            Integer = int.MaxValue
        };

        var json = JsonConvert.SerializeObject(o1, Formatting.Indented);
        var expected = """
            {
              "Object1": {
                "$type": "TestObjects.TestComponentSimple, ArgonTests",
                "MyProperty": 1
              },
              "Object2": 123,
              "ObjectNotHandled": {
                "MyProperty": 2147483647
              },
              "String": "String!",
              "Integer": 2147483647
            }
            """;
        XUnitAssert.AreEqualNormalized(expected, json);

        var o2 = JsonConvert.DeserializeObject<TypeNameObject>(json);
        Assert.NotNull(o2);

        Assert.IsType<TestComponentSimple>(o2.Object1);
        Assert.Equal(1, ((TestComponentSimple) o2.Object1).MyProperty);
        Assert.IsType<long>(o2.Object2);
        Assert.IsType<JObject>(o2.ObjectNotHandled);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "MyProperty": 2147483647
            }
            """,
            o2.ObjectNotHandled.ToString());
    }

    [Fact]
    public void PropertyItemTypeNameHandlingTest()
    {
        var c1 = new PropertyItemTypeNameHandling
        {
            Data = new List<object>
            {
                1,
                "two",
                new TestComponentSimple
                {
                    MyProperty = 1
                }
            }
        };

        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Data": [
                1,
                "two",
                {
                  "$type": "TestObjects.TestComponentSimple, ArgonTests",
                  "MyProperty": 1
                }
              ]
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<PropertyItemTypeNameHandling>(json);
        Assert.Equal(3, c2.Data.Count);

        Assert.IsType<long>(c2.Data[0]);
        Assert.IsType<string>(c2.Data[1]);
        Assert.IsType<TestComponentSimple>(c2.Data[2]);
        var c = (TestComponentSimple) c2.Data[2];
        Assert.Equal(1, c.MyProperty);
    }

    [Fact]
    public void PropertyItemTypeNameHandlingNestedCollections()
    {
        var c1 = new PropertyItemTypeNameHandling
        {
            Data = new List<object>
            {
                new TestComponentSimple
                {
                    MyProperty = 1
                },
                new List<object>
                {
                    new List<object>
                    {
                        new List<object>()
                    }
                }
            }
        };

        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);

        var listTypeName = typeof(List<object>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "Data": [
                {
                  "$type": "TestObjects.TestComponentSimple, ArgonTests",
                  "MyProperty": 1
                },
                {
                  "$type": "{{listTypeName}}",
                  "$values": [
                    [
                      []
                    ]
                  ]
                }
              ]
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<PropertyItemTypeNameHandling>(json);
        Assert.Equal(2, c2.Data.Count);

        Assert.IsType<TestComponentSimple>(c2.Data[0]);
        Assert.IsType<List<object>>(c2.Data[1]);
        var c = (List<object>) c2.Data[1];
        Assert.IsType<JArray>(c[0]);

        json = $$"""
            {
              "Data": [
                {
                  "$type": "TestObjects.TestComponentSimple, ArgonTests",
                  "MyProperty": 1
                },
                {
                  "$type": "{{listTypeName}}",
                  "$values": [
                    {
                      "$type": "TestObjects.TestComponentSimple, ArgonTests",
                      "MyProperty": 1
                    }
                  ]
                }
              ]
            }
            """;

        c2 = JsonConvert.DeserializeObject<PropertyItemTypeNameHandling>(json);
        Assert.Equal(2, c2.Data.Count);

        Assert.IsType<TestComponentSimple>(c2.Data[0]);
        Assert.IsType<List<object>>(c2.Data[1]);
        c = (List<object>) c2.Data[1];
        Assert.IsType<JObject>(c[0]);
        var o = (JObject) c[0];
        Assert.Equal(1, (int) o["MyProperty"]);
    }

    [Fact]
    public void PropertyItemTypeNameHandlingNestedDictionaries()
    {
        var c1 = new PropertyItemTypeNameHandlingDictionary
        {
            Data = new Dictionary<string, object>
            {
                {
                    "one", new TestComponentSimple
                    {
                        MyProperty = 1
                    }
                },
                {
                    "two", new Dictionary<string, object>
                    {
                        {
                            "one", new Dictionary<string, object>
                            {
                                {
                                    "one", 1
                                }
                            }
                        }
                    }
                }
            }
        };

        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);

        var dictionaryTypeName = typeof(Dictionary<string, object>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "Data": {
                "one": {
                  "$type": "TestObjects.TestComponentSimple, ArgonTests",
                  "MyProperty": 1
                },
                "two": {
                  "$type": "{{dictionaryTypeName}}",
                  "one": {
                    "one": 1
                  }
                }
              }
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<PropertyItemTypeNameHandlingDictionary>(json);
        Assert.Equal(2, c2.Data.Count);

        Assert.IsType<TestComponentSimple>(c2.Data["one"]);
        Assert.IsType<Dictionary<string, object>>(c2.Data["two"]);
        var c = (Dictionary<string, object>) c2.Data["two"];
        Assert.IsType<JObject>(c["one"]);

        json = $$"""
            {
              "Data": {
                "one": {
                  "$type": "TestObjects.TestComponentSimple, ArgonTests",
                  "MyProperty": 1
                },
                "two": {
                  "$type": "{{dictionaryTypeName}}",
                  "one": {
                    "$type": "TestObjects.TestComponentSimple, ArgonTests",
                    "MyProperty": 1
                  }
                }
              }
            }
            """;

        c2 = JsonConvert.DeserializeObject<PropertyItemTypeNameHandlingDictionary>(json);
        Assert.Equal(2, c2.Data.Count);

        Assert.IsType<TestComponentSimple>(c2.Data["one"]);
        Assert.IsType<Dictionary<string, object>>(c2.Data["two"]);
        c = (Dictionary<string, object>) c2.Data["two"];
        Assert.IsType<JObject>(c["one"]);

        var o = (JObject) c["one"];
        Assert.Equal(1, (int) o["MyProperty"]);
    }

    [Fact]
    public void PropertyItemTypeNameHandlingObjectTest()
    {
        var o1 = new PropertyItemTypeNameHandlingObject
        {
            Data = new()
            {
                Prop1 = new List<object>
                {
                    new TestComponentSimple
                    {
                        MyProperty = 1
                    }
                },
                Prop2 = new TestComponentSimple
                {
                    MyProperty = 1
                },
                Prop3 = 3,
                Prop4 = new JObject()
            }
        };

        var json = JsonConvert.SerializeObject(o1, Formatting.Indented);

        var listTypeName = typeof(List<object>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "Data": {
                "Prop1": {
                  "$type": "{{listTypeName}}",
                  "$values": [
                    {
                      "MyProperty": 1
                    }
                  ]
                },
                "Prop2": {
                  "$type": "TestObjects.TestComponentSimple, ArgonTests",
                  "MyProperty": 1
                },
                "Prop3": 3,
                "Prop4": {}
              }
            }
            """,
            json);

        var o2 = JsonConvert.DeserializeObject<PropertyItemTypeNameHandlingObject>(json);
        Assert.NotNull(o2);
        Assert.NotNull(o2.Data);

        Assert.IsType<List<object>>(o2.Data.Prop1);
        Assert.IsType<TestComponentSimple>(o2.Data.Prop2);
        Assert.IsType<long>(o2.Data.Prop3);
        Assert.IsType<JObject>(o2.Data.Prop4);

        var o = (List<object>) o2.Data.Prop1;
        var j = (JObject) o[0];
        Assert.Equal(1, (int) j["MyProperty"]);
    }

    [Fact]
    public void PropertyItemTypeNameHandlingDynamicTest()
    {
        var d1 = new PropertyItemTypeNameHandlingDynamic();

        dynamic data = new DynamicDictionary();
        data.one = new TestComponentSimple
        {
            MyProperty = 1
        };

        dynamic data2 = new DynamicDictionary();
        data2.one = new TestComponentSimple
        {
            MyProperty = 2
        };

        data.two = data2;

        d1.Data = (DynamicDictionary) data;

        var json = JsonConvert.SerializeObject(d1, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Data": {
                "one": {
                  "$type": "TestObjects.TestComponentSimple, ArgonTests",
                  "MyProperty": 1
                },
                "two": {
                  "$type": "TypeNameHandlingTests+DynamicDictionary, ArgonTests",
                  "one": {
                    "MyProperty": 2
                  }
                }
              }
            }
            """,
            json);

        var d2 = JsonConvert.DeserializeObject<PropertyItemTypeNameHandlingDynamic>(json);
        Assert.NotNull(d2);
        Assert.NotNull(d2.Data);

        dynamic data3 = d2.Data;
        var c = (TestComponentSimple) data3.one;
        Assert.Equal(1, c.MyProperty);

        var data4 = data3.two;
        var o = (JObject) data4.one;
        Assert.Equal(2, (int) o["MyProperty"]);

        json = """
            {
              "Data": {
                "one": {
                  "$type": "TestObjects.TestComponentSimple, ArgonTests",
                  "MyProperty": 1
                },
                "two": {
                  "$type": "TypeNameHandlingTests+DynamicDictionary, ArgonTests",
                  "one": {
                    "$type": "TestObjects.TestComponentSimple, ArgonTests",
                    "MyProperty": 2
                  }
                }
              }
            }
            """;

        d2 = JsonConvert.DeserializeObject<PropertyItemTypeNameHandlingDynamic>(json);
        data3 = d2.Data;
        data4 = data3.two;
        o = (JObject) data4.one;
        Assert.Equal(2, (int) o["MyProperty"]);
    }

    public class DynamicDictionary : DynamicObject
    {
        readonly IDictionary<string, object> _values = new Dictionary<string, object>();

        public override IEnumerable<string> GetDynamicMemberNames() =>
            _values.Keys;

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _values[binder.Name];
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _values[binder.Name] = value;
            return true;
        }
    }

    [Fact]
    public void SerializeDeserialize_DictionaryContextContainsGuid_DeserializesItemAsGuid()
    {
        const string contextKey = "k1";
        var someValue = new Guid("a6e986df-fc2c-4906-a1ef-9492388f7833");

        var inputContext = new Dictionary<string, Guid>
        {
            {
                contextKey, someValue
            }
        };

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.All
        };
        var serializedString = JsonConvert.SerializeObject(inputContext, settings);

        var dictionaryTypeName = typeof(Dictionary<string, Guid>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "$type": "{{dictionaryTypeName}}",
              "k1": "a6e986df-fc2c-4906-a1ef-9492388f7833"
            }
            """,
            serializedString);

        var deserializedObject = (Dictionary<string, Guid>) JsonConvert.DeserializeObject(serializedString, settings);

        Assert.Equal(someValue, deserializedObject[contextKey]);
    }

    [Fact]
    public void ListOfStackWithFullAssemblyName()
    {
        var input = new List<Stack<string>>
        {
            new(new List<string>
            {
                "One",
                "Two",
                "Three"
            }),
            new(new List<string>
            {
                "Four",
                "Five",
                "Six"
            }),
            new(new List<string>
            {
                "Seven",
                "Eight",
                "Nine"
            })
        };

        var serialized = JsonConvert.SerializeObject(input,
            Formatting.Indented,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
            });

        var output = JsonConvert.DeserializeObject<List<Stack<string>>>(serialized,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            }
        );

        var strings = output.SelectMany(s => s).ToList();

        Assert.Equal(9, strings.Count);
        Assert.Equal("One", strings[0]);
        Assert.Equal("Nine", strings[8]);
    }

    [Fact]
    public void ExistingBaseValue()
    {
        var json = """
            {
                "itemIdentifier": {
                    "$type": "TypeNameHandlingTests+ReportItemKeys, ArgonTests",
                    "dataType": 0,
                    "wantedUnitID": 1,
                    "application": 3,
                    "id": 101,
                    "name": "Machine"
                },
                "isBusinessEntity": false,
                "isKeyItem": true,
                "summarizeOnThisItem": false
            }
            """;

        var g = JsonConvert.DeserializeObject<GroupingInfo>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        });

        var item = (ReportItemKeys) g.ItemIdentifier;
        Assert.Equal(1UL, item.WantedUnitID);
    }

    [Fact]
    public void GenericItemTypeCollection()
    {
        var data = new DataType();
        data.Rows.Add("key", new List<MyInterfaceImplementationType>
        {
            new()
            {
                SomeProperty = "property"
            }
        });
        var serialized = JsonConvert.SerializeObject(data, Formatting.Indented);

        var listTypeName = typeof(List<MyInterfaceImplementationType>).GetTypeName(TypeNameAssemblyFormatHandling.Simple, null);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "Rows": {
                "key": {
                  "$type": "{{listTypeName}}",
                  "$values": [
                    {
                      "SomeProperty": "property"
                    }
                  ]
                }
              }
            }
            """,
            serialized);

        var deserialized = JsonConvert.DeserializeObject<DataType>(serialized);

        Assert.Equal("property", deserialized.Rows["key"].First().SomeProperty);
    }

#if !NET6_0_OR_GREATER
    [Fact]
    public void DeserializeComplexGenericDictionary_Simple()
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };

        var dictionary = new Dictionary<int, HashSet<string>>
        {
            {
                1, [
                    ..new[]
                    {
                        "test"
                    }
                ]
            },
        };

        var obtainedJson = JsonConvert.SerializeObject(dictionary, settings);

        var obtainedDictionary = (Dictionary<int, HashSet<string>>) JsonConvert.DeserializeObject(obtainedJson, settings);

        Assert.NotNull(obtainedDictionary);
    }

    [Fact]
    public void DeserializeComplexGenericDictionary_Full()
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        };

        var dictionary = new Dictionary<int, HashSet<string>>
        {
            {
                1, [
                    ..new[]
                    {
                        "test"
                    }
                ]
            },
        };

        var obtainedJson = JsonConvert.SerializeObject(dictionary, settings);

        var obtainedDictionary = (Dictionary<int, HashSet<string>>) JsonConvert.DeserializeObject(obtainedJson, settings);

        Assert.NotNull(obtainedDictionary);
    }

    [Fact]
    public void SerializeNullableStructProperty_Auto()
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        var objWithMessage = new ObjectWithOptionalMessage(new Message2("Hello!"));

        var json = JsonConvert.SerializeObject(objWithMessage, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Message": {
                "Value": "Hello!"
              }
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeNullableStructProperty_Auto()
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        var json = """
            {
              "Message": {
                "Value": "Hello!"
              }
            }
            """;
        var objWithMessage = JsonConvert.DeserializeObject<ObjectWithOptionalMessage>(json, settings);

        XUnitAssert.AreEqualNormalized("Hello!", objWithMessage.Message.Value.Value);
    }
#endif

    [Fact]
    public void SerializerWithDefaultBinder()
    {
        var serializer = JsonSerializer.Create();
        Assert.Null(serializer.SerializationBinder);
    }

    public class FancyBinder : ISerializationBinder
    {
        static readonly string Annotate = new(':', 3);

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = $"FancyAssemblyName=>{Assembly.GetAssembly(serializedType)?.GetName().Name}";
            typeName = $"{Annotate}{serializedType.Name}{Annotate}";
        }

        public Type BindToType(string assemblyName, string typeName) =>
            null;
    }


    [method: Argon.JsonConstructor]
    public struct Message2(string value)
    {
        public string Value { get; } = value;
    }

    public class ObjectWithOptionalMessage(Message2? message)
    {
        public Message2? Message { get; } = message;
    }

    public class DataType
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto, TypeNameHandling = TypeNameHandling.Auto)]
        public Dictionary<string, IEnumerable<IMyInterfaceType>> Rows { get; } = new();
    }

    public interface IMyInterfaceType
    {
        string SomeProperty { get; set; }
    }

    public class MyInterfaceImplementationType : IMyInterfaceType
    {
        public string SomeProperty { get; set; }
    }

    public class MyChild : ISomeBase
    {
        [JsonProperty("p")]
        public string MyProperty { get; internal set; }
    }

    public class MyChildList : List<string>, ISomeBase;

    public interface ISomeBase;

    public class Message
    {
        public string Address { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
        public object Body { get; set; }
    }

    public class SearchDetails
    {
        public string Query { get; set; }
        public string Language { get; set; }
    }

    public class Customer
    {
        public string Name { get; set; }
    }

    public class Purchase
    {
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class SerializableWrapper
    {
        public object Content { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is not SerializableWrapper w)
            {
                return false;
            }

            return Equals(w.Content, Content);
        }

        public override int GetHashCode()
        {
            if (Content == null)
            {
                return 0;
            }

            return Content.GetHashCode();
        }
    }

    public class PropertyItemTypeNameHandlingObject
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
        public TypeNameHandlingTestObject Data { get; set; }
    }

    public class PropertyItemTypeNameHandlingDynamic
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
        public DynamicDictionary Data { get; set; }
    }

    public class TypeNameHandlingTestObject
    {
        public object Prop1 { get; set; }
        public object Prop2 { get; set; }
        public object Prop3 { get; set; }
        public object Prop4 { get; set; }
    }

    public class PropertyItemTypeNameHandlingDictionary
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
        public IDictionary<string, object> Data { get; set; }
    }

    public class PropertyItemTypeNameHandling
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
        public IList<object> Data { get; set; }
    }

    [JsonArray(ItemTypeNameHandling = TypeNameHandling.All)]
    public class TypeNameList<T> : List<T>;

    [JsonDictionary(ItemTypeNameHandling = TypeNameHandling.All)]
    public class TypeNameDictionary<T> : Dictionary<string, T>;

    [JsonObject(ItemTypeNameHandling = TypeNameHandling.All)]
    public class TypeNameObject
    {
        public object Object1 { get; set; }
        public object Object2 { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.None)]
        public object ObjectNotHandled { get; set; }

        public string String { get; set; }
        public int Integer { get; set; }
    }

    [DataContract]
    public class GroupingInfo
    {
        [DataMember]
        public ApplicationItemKeys ItemIdentifier { get; set; } = new();
    }

    [DataContract]
    public class ApplicationItemKeys
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }
    }

    [DataContract]
    public class ReportItemKeys : ApplicationItemKeys
    {
        protected ulong wantedUnit;

        [DataMember]
        public ulong WantedUnitID
        {
            get => wantedUnit;
            set => wantedUnit = value;
        }
    }
}