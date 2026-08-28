// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedVariable

public class SerializationErrorHandlingTests : TestFixtureBase
{
    [Fact]
    public void ErrorHandlingMetadata()
    {
        var errors = new List<Exception>();

        var a2 = JsonConvert.DeserializeObject<AAA>(
            """{"MyTest":{"$type":"<Namespace>.JsonTest+MyTest2, <Assembly>"}}""",
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                DeserializeError = (_, _, _, _, exception, markAsHandled) =>
                {
                    errors.Add(exception);
                    markAsHandled();
                }
            });

        Assert.NotNull(a2);
        Assert.Single(errors);
        Assert.Equal("Error resolving type specified in JSON '<Namespace>.JsonTest+MyTest2, <Assembly>'. Path 'MyTest.$type', line 1, position 61.", errors[0].Message);
    }

    [Fact]
    public void ErrorHandlingMetadata_TopLevel()
    {
        var errors = new List<Exception>();

        var a2 = (JObject) JsonConvert.TryDeserializeObject(
            """{"$type":"<Namespace>.JsonTest+MyTest2, <Assembly>"}""",
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                DeserializeError = (_, _, _, _, exception, markAsHandled) =>
                {
                    errors.Add(exception);
                    markAsHandled();
                }
            });

        Assert.Null(a2);
        Assert.Single(errors);
        Assert.Equal("Error resolving type specified in JSON '<Namespace>.JsonTest+MyTest2, <Assembly>'. Path '$type', line 1, position 51.", errors[0].Message);
    }

    public class AAA
    {
        public ITest MyTest { get; set; }
    }

    public interface ITest;

    public class MyTest : ITest;

    public class MyClass1
    {
        [JsonProperty("myint")]
        public int MyInt { get; set; }

        [JsonProperty("Mybool")]
        public bool Mybool { get; set; }
    }

    [Fact]
    public void ErrorDeserializingIntegerInObject()
    {
        var errors = new List<string>();
        var json = "{\"myint\":3554860000,\"Mybool\":false}";
        JsonConvert.DeserializeObject<MyClass1>(
            json,
            new JsonSerializerSettings
            {
                DeserializeError = (_, _, _, _, exception, markAsHandled) =>
                {
                    errors.Add(exception.Message);
                    markAsHandled();
                }
            });

        Assert.Single(errors);
        Assert.Equal("JSON integer 3554860000 is too large or small for an Int32. Path 'myint', line 1, position 19.", errors[0]);
    }

    [Fact]
    public void ErrorDeserializingListHandled()
    {
        var json = """
                   [
                     {
                       "Name": "Jim",
                       "BirthDate": "2013-08-14T00:00:00.000",
                       "LastModified": "2013-08-14T00:00:00.000"
                     },
                     {
                       "Name": "Jim",
                       "BirthDate": "2013-08-14T00:00:00.000",
                       "LastModified": "2013-08-14T00:00:00.000"
                     }
                   ]
                   """;

        var possibleMsgs = new[]
        {
            "[1] - Error message for member 1 = An item with the same key has already been added.",
            "[1] - Error message for member 1 = An element with the same key already exists in the dictionary.", // mono
            "[1] - Error message for member 1 = An item with the same key has already been added. Key: Jim" // netcore
        };

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var c = JsonConvert.DeserializeObject<VersionKeyedCollection>(json, settings);
#pragma warning disable xUnit2013
        Assert.Equal(1, c.Count);
        Assert.Equal(1, c.Messages.Count);
#pragma warning restore xUnit2013

        Console.WriteLine(c.Messages[0]);
        Assert.True(
            possibleMsgs.Any(_ => _ == c.Messages[0]),
            $"Expected One of: {Environment.NewLine}{string.Join(Environment.NewLine, possibleMsgs)}{Environment.NewLine}Was: {Environment.NewLine}{c.Messages[0]}");
    }

    [Fact]
    public void DeserializingErrorInChildObject()
    {
        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var c = JsonConvert.DeserializeObject<ListErrorObjectCollection>(
            """
            [
              {
                "Member": "Value1",
                "Member2": null
              },
              {
                "Member": "Value2"
              },
              {
                "ThrowError": "Value",
                "Object": {
                  "Array": [
                    1,
                    2
                  ]
                }
              },
              {
                "ThrowError": "Handle this!",
                "Member": "Value3"
              }
            ]
            """,
            settings);

        Assert.Equal(3, c.Count);
        Assert.Equal("Value1", c[0].Member);
        Assert.Equal("Value2", c[1].Member);
        Assert.Equal("Value3", c[2].Member);
        Assert.Equal("Handle this!", c[2].ThrowError);
    }

    [Fact]
    public void SerializingErrorIn3DArray()
    {
        var c = new ListErrorObject[,,]
        {
            {
                {
                    new()
                    {
                        Member = "Value1",
                        ThrowError = "Handle this!",
                        Member2 = "Member1"
                    },
                    new()
                    {
                        Member = "Value2",
                        Member2 = "Member2"
                    },
                    new()
                    {
                        Member = "Value3",
                        ThrowError = "Handle that!",
                        Member2 = "Member3"
                    }
                },
                {
                    new()
                    {
                        Member = "Value1",
                        ThrowError = "Handle this!",
                        Member2 = "Member1"
                    },
                    new()
                    {
                        Member = "Value2",
                        Member2 = "Member2"
                    },
                    new()
                    {
                        Member = "Value3",
                        ThrowError = "Handle that!",
                        Member2 = "Member3"
                    }
                }
            }
        };

        var json = JsonConvert.SerializeObject(
            c,
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                SerializeError = (currentObject, _, _, _, _, markAsHandled) =>
                {
                    if (currentObject.GetType().IsArray)
                    {
                        markAsHandled();
                    }
                }
            });

        XUnitAssert.AreEqualNormalized(
            """
            [
              [
                [
                  {
                    "Member": "Value1",
                    "ThrowError": "Handle this!",
                    "Member2": "Member1"
                  },
                  {
                    "Member": "Value2"
                  },
                  {
                    "Member": "Value3",
                    "ThrowError": "Handle that!",
                    "Member2": "Member3"
                  }
                ],
                [
                  {
                    "Member": "Value1",
                    "ThrowError": "Handle this!",
                    "Member2": "Member1"
                  },
                  {
                    "Member": "Value2"
                  },
                  {
                    "Member": "Value3",
                    "ThrowError": "Handle that!",
                    "Member2": "Member3"
                  }
                ]
              ]
            ]
            """, json);
    }

    [Fact]
    public void SerializingErrorInChildObject()
    {
        var c = new ListErrorObjectCollection
        {
            new()
            {
                Member = "Value1",
                ThrowError = "Handle this!",
                Member2 = "Member1"
            },
            new()
            {
                Member = "Value2",
                Member2 = "Member2"
            },
            new()
            {
                Member = "Value3",
                ThrowError = "Handle that!",
                Member2 = "Member3"
            }
        };

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var json = JsonConvert.SerializeObject(c, Formatting.Indented, settings);

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "Member": "Value1",
                "ThrowError": "Handle this!",
                "Member2": "Member1"
              },
              {
                "Member": "Value2"
              },
              {
                "Member": "Value3",
                "ThrowError": "Handle that!",
                "Member2": "Member3"
              }
            ]
            """,
            json);
    }

    [Fact]
    public void DeserializingErrorInDateTimeCollection()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new IsoDateTimeConverter());
        settings.AddInterfaceCallbacks();
        var c = JsonConvert.DeserializeObject<DateTimeErrorObjectCollection>(
            """
            [
              "2009-09-09T00:00:00Z",
              "kjhkjhkjhkjh",
              [
                1
              ],
              "1977-02-20T00:00:00Z",
              null,
              "2000-12-01T00:00:00Z"
            ]
            """,
            settings);

        Assert.Equal(3, c.Count);
        Assert.Equal(new(2009, 9, 9, 0, 0, 0, DateTimeKind.Utc), c[0]);
        Assert.Equal(new(1977, 2, 20, 0, 0, 0, DateTimeKind.Utc), c[1]);
        Assert.Equal(new(2000, 12, 1, 0, 0, 0, DateTimeKind.Utc), c[2]);
    }

    [Fact]
    public void DeserializingErrorHandlingUsingEvent()
    {
        var errors = new List<string>();

        var serializer = JsonSerializer.Create(
            new()
        {
            DeserializeError = (_, _, path, member, exception, markAsHanded) =>
            {
                errors.Add($"{path} - {member} - {exception.Message}");
                markAsHanded();
            },
            Converters =
            {
                new IsoDateTimeConverter()
            }
        });
        var c = serializer.Deserialize<List<DateTime>>(
            new JsonTextReader(
                new StringReader(
                    """
                    [
                      "2009-09-09T00:00:00Z",
                      "I am not a date and will error!",
                      [
                        1
                      ],
                      "1977-02-20T00:00:00Z",
                      null,
                      "2000-12-01T00:00:00Z"
                    ]
                    """)));

        // 2009-09-09T00:00:00Z
        // 1977-02-20T00:00:00Z
        // 2000-12-01T00:00:00Z

        // The string was not recognized as a valid DateTime. There is a unknown word starting at index 0.
        // Unexpected token parsing date. Expected String, got StartArray.
        // Cannot convert null value to System.DateTime.

        Assert.Equal(3, c.Count);
        Assert.Equal(new(2009, 9, 9, 0, 0, 0, DateTimeKind.Utc), c[0]);
        Assert.Equal(new(1977, 2, 20, 0, 0, 0, DateTimeKind.Utc), c[1]);
        Assert.Equal(new(2000, 12, 1, 0, 0, 0, DateTimeKind.Utc), c[2]);

        Assert.Equal(3, errors.Count);
        var possibleErrs = new[]
        {
            "[1] - 1 - The string was not recognized as a valid DateTime. There is an unknown word starting at index 0.",
            "[1] - 1 - String was not recognized as a valid DateTime.",
            "[1] - 1 - The string 'I am not a date and will error!' was not recognized as a valid DateTime. There is an unknown word starting at index '0'."
        };

        Assert.True(possibleErrs.Any(_ => _ == errors[0]),
            $"Expected One of: {string.Join(Environment.NewLine, possibleErrs)}{Environment.NewLine}But was: {errors[0]}");

        Assert.Equal(
            "[2] - 2 - Unexpected token parsing date. Expected String, got StartArray. Path '[2]', line 4, position 3.",
            errors[1]);
        Assert.Equal(
            "[4] - 4 - Cannot convert null value to System.DateTime. Path '[4]', line 8, position 6.",
            errors[2]);
    }

    [Fact]
    public void DeserializingErrorInDateTimeCollectionWithAttributeWithEventNotCalled()
    {
        var eventErrorHandlerCalled = false;

        var settings = new JsonSerializerSettings
        {
            DeserializeError = (_, _, _, _, _, handled) =>
            {
                eventErrorHandlerCalled = true;
                handled();
            },
            Converters =
            {
                new IsoDateTimeConverter()
            }
        };
        var c = JsonConvert.DeserializeObject<DateTimeErrorObjectCollection>(
            """
            [
              "2009-09-09T00:00:00Z",
              "kjhkjhkjhkjh",
              [
                1
              ],
              "1977-02-20T00:00:00Z",
              null,
              "2000-12-01T00:00:00Z"
            ]
            """,
            settings);

        Assert.Equal(3, c.Count);
        Assert.Equal(new(2009, 9, 9, 0, 0, 0, DateTimeKind.Utc), c[0]);
        Assert.Equal(new(1977, 2, 20, 0, 0, 0, DateTimeKind.Utc), c[1]);
        Assert.Equal(new(2000, 12, 1, 0, 0, 0, DateTimeKind.Utc), c[2]);

        Assert.True(eventErrorHandlerCalled);
    }

    [Fact]
    public void SerializePerson()
    {
        var person = new PersonError
        {
            Name = "George Michael Bluth",
            Age = 16,
            Roles = null,
            Title = "Mister Manager"
        };

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var json = JsonConvert.SerializeObject(person, Formatting.Indented, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "George Michael Bluth",
              "Age": 16,
              "Title": "Mister Manager"
            }
            """,
            json);
    }

    [Fact]
    public Task DeserializeNestedUnhandled()
    {
        var errors = new List<string>();

        var json = """[["kjhkjhkjhkjh"]]""";

        Exception exception = null;
        try
        {
            var serializer = new JsonSerializer
            {
                DeserializeError = (currentObject, originalObject, path, member, exception, _) =>
                {
                    // only log an error once
                    if (currentObject == originalObject)
                    {
                        errors.Add($"{path} - {member} - {exception.Message}");
                    }
                }
            };

            serializer.Deserialize(new StringReader(json), typeof(List<List<DateTime>>));
        }
        catch (Exception e)
        {
            exception = e;
        }

        return Verify(
                new
                {
                    exception,
                    errors
                })
            .IgnoreStackTrace();
    }

    [Fact]
    public void MultipleRequiredPropertyErrors()
    {
        var json = "{}";
        var errors = new List<string>();
        var serializer = new JsonSerializer
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Default,
            DeserializeError = (currentObject, originalObject, path, member, exception, markAsHandled) =>
            {
                errors.Add($"{path} - {member} - {exception.Message}");
                markAsHandled();
            }
        };
        serializer.Deserialize<MyTypeWithRequiredMembers>(new JsonTextReader(new StringReader(json)));

        Assert.Equal(2, errors.Count);
        Assert.StartsWith(" - Required1 - Required property 'Required1' not found in JSON. Path '', line 1, position 2.", errors[0]);
        Assert.StartsWith(" - Required2 - Required property 'Required2' not found in JSON. Path '', line 1, position 2.", errors[1]);
    }

    [Fact]
    public void HandlingArrayErrors()
    {
        var json = "[\"a\",\"b\",\"45\",34]";

        var errors = new List<string>();

        var serializer = new JsonSerializer
        {
            DeserializeError = (currentObject, originalObject, path, member, exception, markAsHandled) =>
            {
                errors.Add($"{path} - {member} - {exception.Message}");
                markAsHandled();
            }
        };

        serializer.Deserialize<int[]>(new JsonTextReader(new StringReader(json)));

        Assert.Equal(2, errors.Count);
        Assert.Equal("[0] - 0 - Could not convert string to integer: a. Path '[0]', line 1, position 4.", errors[0]);
        Assert.Equal("[1] - 1 - Could not convert string to integer: b. Path '[1]', line 1, position 8.", errors[1]);
    }

    [Fact]
    public void HandlingMultidimensionalArrayErrors()
    {
        var json = "[[\"a\",\"45\"],[\"b\",34]]";

        var errors = new List<string>();

        var serializer = new JsonSerializer
        {
            DeserializeError = (currentObject, originalObject, path, member, exception, markAsHandled) =>
            {
                errors.Add($"{path} - {member} - {exception.Message}");
                markAsHandled();
            }
        };

        serializer.Deserialize<int[,]>(new JsonTextReader(new StringReader(json)));

        Assert.Equal(2, errors.Count);
        Assert.Equal("[0][0] - 0 - Could not convert string to integer: a. Path '[0][0]', line 1, position 5.", errors[0]);
        Assert.Equal("[1][0] - 0 - Could not convert string to integer: b. Path '[1][0]', line 1, position 16.", errors[1]);
    }

    [Fact]
    public void ErrorHandlingAndAvoidingRecursiveDepthError()
    {
        var json = "{'A':{'A':{'A':{'A':{'A':{}}}}}}";
        var serializer = new JsonSerializer();
        var errors = new List<string>();
        serializer.DeserializeError = (currentObject, originalObject, path, member, exception, markAsHandled) =>
        {
            markAsHandled();
            errors.Add(path);
        };

        serializer.Deserialize<Nest>(new JsonTextReader(new StringReader(json))
        {
            MaxDepth = 3
        });

        Assert.Single(errors);
        Assert.Equal("A.A.A", errors[0]);
    }

    public class Nest
    {
        public Nest A { get; set; }
    }

    [Fact]
    public void InfiniteErrorHandlingLoopFromInputError()
    {
        var errors = new List<string>();

        var serializer = new JsonSerializer
        {
            DeserializeError = (_, _, _, _, exception, markAsHandled) =>
            {
                errors.Add(exception.Message);
                markAsHandled();
            }
        };

        var result = serializer.TryDeserialize<ErrorPerson[]>(new JsonTextReader(new ThrowingReader()));

        Assert.Null(result);
        Assert.Equal(3, errors.Count);
        Assert.Equal("too far", errors[0]);
        Assert.Equal("too far", errors[1]);
        Assert.Equal("Infinite loop detected from error handling. Path '[1023]', line 1, position 65536.", errors[2]);
    }

    [Fact]
    public void ArrayHandling()
    {
        var errors = new List<string>();

        var o = JsonConvert.DeserializeObject(
            "[0,x]",
            typeof(int[]),
            new JsonSerializerSettings
            {
                DeserializeError = (_, _, _, _, exception, markAsHanded) =>
                {
                    errors.Add(exception.Message);
                    markAsHanded();
                }
            });

        Assert.NotNull(o);

        Assert.Single(errors);
        Assert.Equal("Unexpected character encountered while parsing value: x. Path '[0]', line 1, position 4.", errors[0]);

        Assert.Single((int[]) o);
        Assert.Equal(0, ((int[]) o)[0]);
    }

    [Fact]
    public void ArrayHandling_JTokenReader()
    {
        var errors = new List<string>();

        var reader = new JTokenReader(new JArray(0, true));

        var serializer = JsonSerializer.Create(new()
        {
            DeserializeError = (_, _, _, _, exception, markAsHanded) =>
            {
                errors.Add(exception.Message);
                markAsHanded();
            }
        });
        var o = serializer.Deserialize<int[]>(reader);

        Assert.NotNull(o);

        Assert.Single(errors);
        Assert.Equal("Error reading integer. Unexpected token: Boolean. Path '[1]'.", errors[0]);

        Assert.Single(o);
        Assert.Equal(0, o[0]);
    }

    [Fact]
    public void ArrayHandlingInObject()
    {
        var errors = new List<string>();

        var o = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(
            "{'badarray':[0,x,2],'goodarray':[0,1,2]}",
            new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Default,
                DeserializeError = (_, _, _, _, exception, markAsHanded) =>
                {
                    errors.Add(exception.Message);
                    markAsHanded();
                }
            });

        Assert.NotNull(o);

        Assert.Equal(2, errors.Count);
        Assert.Equal("Unexpected character encountered while parsing value: x. Path 'badarray[0]', line 1, position 16.", errors[0]);
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path 'badarray[1]', line 1, position 17.", errors[1]);

        Assert.Equal(2, o.Count);
        Assert.Equal(2, o["badarray"].Length);
        Assert.Equal(0, o["badarray"][0]);
        Assert.Equal(2, o["badarray"][1]);
    }

    [Fact]
    public void ErrorHandlingEndOfContent()
    {
        var errors = new List<string>();

        const string input = "{\"events\":[{\"code\":64411},{\"code\":64411,\"prio";

        const int maxDepth = 256;
        using (var jsonTextReader = new JsonTextReader(new StringReader(input))
               {
                   MaxDepth = maxDepth
               })
        {
            var serializer = JsonSerializer.Create(new()
            {
                MaxDepth = maxDepth,
                MetadataPropertyHandling = MetadataPropertyHandling.Default
            });
            serializer.DeserializeError = (_, _, _, _, exception, markAsHandled) =>
            {
                errors.Add(exception.Message);
                markAsHandled();
            };

            var logMessage = serializer.Deserialize<LogMessage>(jsonTextReader);

            Assert.NotNull(logMessage.Events);
            Assert.Single(logMessage.Events);
            Assert.Equal("64411", logMessage.Events[0].Code);
        }

        Assert.Equal(3, errors.Count);
        Assert.Equal(@"Unterminated string. Expected delimiter: "". Path 'events[1].code', line 1, position 45.", errors[0]);
        Assert.Equal("Unexpected end when deserializing array. Path 'events[1].code', line 1, position 45.", errors[1]);
        Assert.Equal("Unexpected end when deserializing object. Path 'events[1].code', line 1, position 45.", errors[2]);
    }

    [Fact]
    public void ErrorHandlingEndOfContentDictionary()
    {
        var errors = new List<string>();

        const string input = "{\"events\":{\"code\":64411},\"events2\":{\"code\":64412,";

        const int maxDepth = 256;
        using (var jsonTextReader = new JsonTextReader(new StringReader(input))
               {
                   MaxDepth = maxDepth
               })
        {
            var serializer = JsonSerializer.Create(new()
            {
                MaxDepth = maxDepth,
                MetadataPropertyHandling = MetadataPropertyHandling.Default
            });
            serializer.DeserializeError = (_, _, _, _, exception, markAsHandled) =>
            {
                errors.Add(exception.Message);
                markAsHandled();
            };

            var events = serializer.Deserialize<IDictionary<string, LogEvent>>(jsonTextReader);

            Assert.NotNull(events);
            Assert.Equal(2, events.Count);
            Assert.Equal("64411", events["events"].Code);
            Assert.Equal("64412", events["events2"].Code);
        }

        Assert.Equal(2, errors.Count);
        Assert.Equal("Unexpected end when deserializing object. Path 'events2.code', line 1, position 49.", errors[0]);
        Assert.Equal("Unexpected end when deserializing object. Path 'events2.code', line 1, position 49.", errors[1]);
    }

    [Fact]
    public void ErrorHandlingEndOfContentDynamic()
    {
        var errors = new List<string>();

        var json = """
                   {
                     "Explicit": true,
                     "Decimal": 99.9,
                     "Int": 1,
                     "ChildObject": {
                       "Integer": 123
                   """;

        var newDynamicObject = JsonConvert.DeserializeObject<TestDynamicObject>(
            json,
            new JsonSerializerSettings
            {
                DeserializeError = (_, _, _, _, exception, markAsHanded) =>
                {
                    errors.Add(exception.Message);
                    markAsHanded();
                },
                MetadataPropertyHandling = MetadataPropertyHandling.Default
            });
        Assert.True(newDynamicObject.Explicit);

        dynamic d = newDynamicObject;

        Assert.Equal(99.9, d.Decimal);
        Assert.Equal(1, d.Int);
        Assert.Equal(123, d.ChildObject.Integer);

        Assert.Equal(2, errors.Count);
        Assert.Equal("Unexpected end when deserializing object. Path 'ChildObject.Integer', line 6, position 18.", errors[0]);
        Assert.Equal("Unexpected end when deserializing object. Path 'ChildObject.Integer', line 6, position 18.", errors[1]);
    }

    [Fact]
    public void WriteEndOnPropertyState()
    {
        var settings = new JsonSerializerSettings();
        settings.SerializeError += (_, _, _, _, _, markAsHanded) => markAsHanded();

        var data = new List<ErrorPerson2>
        {
            new()
            {
                FirstName = "Scott",
                LastName = "Hanselman"
            },
            new()
            {
                FirstName = "Scott",
                LastName = "Hunter"
            },
            new()
            {
                FirstName = "Scott",
                LastName = "Guthrie"
            }
        };

        var dictionary = data.GroupBy(person => person.FirstName)
            .ToDictionary(group => group.Key, group => group.Cast<IErrorPerson2>());
        var output = JsonConvert.SerializeObject(dictionary, Formatting.None, settings);
        Assert.Equal("""{"Scott":[]}""", output);
    }

    [Fact]
    public void WriteEndOnPropertyState2()
    {
        var settings = new JsonSerializerSettings
        {
            SerializeError = (_, _, _, _, _, markAsHanded) => markAsHanded()
        };

        var data = new List<ErrorPerson2>
        {
            new()
            {
                FirstName = "Scott",
                LastName = "Hanselman"
            },
            new()
            {
                FirstName = "Scott",
                LastName = "Hunter"
            },
            new()
            {
                FirstName = "Scott",
                LastName = "Guthrie"
            },
            new()
            {
                FirstName = "James",
                LastName = "Newton-King"
            }
        };

        var dictionary = data
            .GroupBy(person => person.FirstName)
            .ToDictionary(group => group.Key, group => group.Cast<IErrorPerson2>());
        var output = JsonConvert.SerializeObject(dictionary, Formatting.None, settings);

        Assert.Equal("""{"Scott":[],"James":[]}""", output);
    }

    [Fact]
    public void NoObjectWithEvent()
    {
        var json = "{\"}";
        var byteArray = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(byteArray);
        var jReader = new JsonTextReader(new StreamReader(stream));
        var s = new JsonSerializer
        {
            DeserializeError = (_, _, _, _, _, markAsHandled) =>
            {
                markAsHandled();
            }
        };
        var obj = s.TryDeserialize<ErrorPerson2>(jReader);

        Assert.Null(obj);
    }

    [Fact]
    public void NoObjectWithAttribute()
    {
        var json = "{\"}";
        var byteArray = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(byteArray);
        var jReader = new JsonTextReader(new StreamReader(stream));
        var s = new JsonSerializer();

        var exception = Assert.Throws<JsonReaderException>(() => s.Deserialize<ErrorTestObject>(jReader));
        Assert.Equal(@"Unterminated string. Expected delimiter: "". Path '', line 1, position 3.", exception.Message);
    }

    public class RootThing
    {
        public Something Something { get; set; }
    }

    public class RootSomethingElse
    {
        public SomethingElse SomethingElse { get; set; }
    }

    /// <summary>
    /// This could be an object we are passing up in an interface.
    /// </summary>
    [JsonConverter(typeof(SomethingConverter))]
    public class Something
    {
        public class SomethingConverter : JsonConverter
        {
            public override bool CanConvert(Type type) =>
                true;

            public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
            {
                try
                {
                    // Do own stuff.
                    // Then call deserialise for inner object.
                    var innerObject = serializer.Deserialize<SomethingElse>(reader);

                    return null;
                }
                catch (Exception exception)
                {
                    // If we get an error wrap it in something less scary.
                    throw new("An error occurred.", exception);
                }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                try
                {
                    var s = (Something) value;

                    // Do own stuff.
                    // Then call serialise for inner object.
                    serializer.Serialize(writer, s.RootSomethingElse);
                }
                catch (Exception exception)
                {
                    // If we get an error wrap it in something less scary.
                    throw new("An error occurred.", exception);
                }
            }
        }

        public RootSomethingElse RootSomethingElse { get; set; } = new();
    }

    /// <summary>
    /// This is an object that is contained in the interface object.
    /// </summary>
    [JsonConverter(typeof(SomethingElseConverter))]
    public class SomethingElse
    {
        public class SomethingElseConverter : JsonConverter
        {
            public override bool CanConvert(Type type) =>
                true;

            public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) =>
                throw new NotImplementedException();

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
                throw new NotImplementedException();
        }
    }

    [Fact]
    public void DeserializeWrappingErrorsAndErrorHandling()
    {
        var serializer = JsonSerializer.Create(new());

        var foo = "{ something: { rootSomethingElse { somethingElse: 0 } } }";
        var reader = new StringReader(foo);

        var exception = Assert.Throws<Exception>(() => serializer.Deserialize(reader, typeof(Something)));
        Assert.Equal("An error occurred.", exception.Message);
    }

    [Fact]
    public void SerializeWrappingErrorsAndErrorHandling()
    {
        var serializer = JsonSerializer.Create(new());

        var s = new Something
        {
            RootSomethingElse = new()
            {
                SomethingElse = new()
            }
        };
        var r = new RootThing
        {
            Something = s
        };

        var writer = new StringWriter();

        var exception = Assert.Throws<Exception>(() => serializer.Serialize(writer, r));
        Assert.Equal("An error occurred.", exception.Message);
    }

    [Fact]
    public void DeserializeRootConverter()
    {
        var result = JsonConvert.TryDeserializeObject<SomethingElse>("{}",
            new JsonSerializerSettings
            {
                DeserializeError = (_, _, _, _, _, markAsHanded) =>
                {
                    markAsHanded();
                }
            });

        Assert.Null(result);
    }

    [Fact]
    public void SerializeRootConverter()
    {
        var result = JsonConvert.SerializeObject(
            new SomethingElse(),
            new JsonSerializerSettings
        {
            SerializeError = (_, _, _, _, _, markAsHanded) => markAsHanded()
        });

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void HandleErrorInDictionaryObject()
    {
        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var json = """
                   {
                       model1: { String1: 's1', Int1: 'x' },
                       model2: { String1: 's2', Int1: 2 }
                   }
                   """;
        var dictionary = JsonConvert.DeserializeObject<TolerantDictionary<string, DataModel>>(json, settings);

        Assert.Single(dictionary);
        Assert.True(dictionary.ContainsKey("model2"));
        Assert.Equal("s2", dictionary["model2"].String1);
        Assert.Equal(2, dictionary["model2"].Int1);
    }

    class DataModel
    {
        public string String1 { get; set; }
        public int Int1 { get; set; }
        public string String2 { get; set; }
        public int Int2 { get; set; }
        public string String3 { get; set; }
        public DateTime DateTime1 { get; set; }
        public string String4 { get; set; }
    }

    interface IErrorPerson2;

    class ErrorPerson2 //:IPerson - oops! Forgot to implement the person interface
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
    }

    public class ThrowingReader : TextReader
    {
        int _position;
        static string element = "{\"FirstName\":\"Din\",\"LastName\":\"Rav\",\"Item\":{\"ItemName\":\"temp\"}}";
        bool _firstRead = true;
        bool _readComma;

        public override int Read(char[] buffer, int index, int count)
        {
            var temp = new char[buffer.Length];
            int charsRead;
            if (_firstRead)
            {
                charsRead = new StringReader("[").Read(temp, index, count);
                _firstRead = false;
            }
            else
            {
                if (_readComma)
                {
                    charsRead = new StringReader(",").Read(temp, index, count);
                    _readComma = false;
                }
                else
                {
                    charsRead = new StringReader(element).Read(temp, index, count);
                    _readComma = true;
                }
            }

            _position += charsRead;
            if (_position > 65536)
            {
                throw new("too far");
            }

            Array.Copy(temp, index, buffer, index, charsRead);
            return charsRead;
        }
    }

    public class ErrorPerson
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ErrorItem Item { get; set; }
    }

    public class ErrorItem
    {
        public string ItemName { get; set; }
    }

    [JsonObject]
    public class MyTypeWithRequiredMembers
    {
        [JsonProperty(Required = Required.AllowNull)]
        public string Required1;

        [JsonProperty(Required = Required.AllowNull)]
        public string Required2;
    }

    public class LogMessage
    {
        public string DeviceId { get; set; }
        public IList<LogEvent> Events { get; set; }
    }

    public class LogEvent
    {
        public string Code { get; set; }
        public int Priority { get; set; }
    }


    public class ErrorTestObject :
        IJsonOnDeserializeError
    {
        public void OnDeserializeError(object originalObject, string path, object member, Exception exception, Action markAsHanded)
        {
        }
    }

    /// <summary>
    /// A dictionary that ignores deserialization errors and excludes bad items
    /// </summary>
    public class TolerantDictionary<TKey, TValue> :
        Dictionary<TKey, TValue>,
        IJsonOnDeserializeError
    {
        public void OnDeserializeError(object originalObject, string path, object member, Exception exception, Action markAsHandled) =>
            markAsHandled();
    }
}