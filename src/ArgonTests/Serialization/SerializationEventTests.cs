// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;

public class SerializationEventTests : TestFixtureBase
{
    public class ConverterHandled
    {
        public string Inner { get; set; } = "x";
    }

    public class ConverterHandledConverter :
        JsonConverter<ConverterHandled>
    {
        public override void WriteJson(JsonWriter writer, ConverterHandled value, JsonSerializer serializer) =>
            writer.WriteValue(value.Inner);

        public override ConverterHandled ReadJson(JsonReader reader, Type type, ConverterHandled existing, bool hasExisting, JsonSerializer serializer) =>
            new() {Inner = (string) reader.Value ?? ""};
    }

    [Fact]
    public void SerializingFiresForConverterHandledValue()
    {
        var settings = new JsonSerializerSettings
        {
            Converters =
            {
                new ConverterHandledConverter()
            }
        };
        var serializing = new List<object>();
        var serialized = new List<object>();
        settings.Serializing += (_, value) => serializing.Add(value);
        settings.Serialized += (_, value) => serialized.Add(value);

        JsonConvert.SerializeObject(new ConverterHandled(), settings);

        Assert.Contains(serializing, _ => _ is ConverterHandled);
        Assert.Contains(serialized, _ => _ is ConverterHandled);
    }

    [Fact]
    public void SerializingFiresForJObjectRoot()
    {
        // Repro for VerifyTests/Verify.Assertions#2: a JsonConverter registered for JObject
        // (as Verify does internally) must not swallow the Serializing event for the root JObject.
        var settings = new JsonSerializerSettings
        {
            Converters =
            {
                new JObjectAsStringConverter()
            }
        };
        var serializing = new List<object>();
        settings.Serializing += (_, value) => serializing.Add(value);

        var jObject = JObject.Parse("""{"a":1}""");
        JsonConvert.SerializeObject(jObject, settings);

        Assert.Contains(serializing, _ => ReferenceEquals(_, jObject));
    }

    class JObjectAsStringConverter :
        JsonConverter<JObject>
    {
        public override void WriteJson(JsonWriter writer, JObject value, JsonSerializer serializer) =>
            writer.WriteValue(value.ToString(Formatting.None));

        public override JObject ReadJson(JsonReader reader, Type type, JObject existing, bool hasExisting, JsonSerializer serializer) =>
            JObject.Parse((string) reader.Value);
    }

    [Fact]
    public void ObjectEvents()
    {
        var objs = new[]
        {
            new SerializationEventTestObject(),
            new DerivedSerializationEventTestObject()
        };

        foreach (var current in objs)
        {
            var obj = current;

            Assert.Equal(11, obj.Member1);
            Assert.Equal("Hello World!", obj.Member2);
            Assert.Equal("This is a nonserialized value", obj.Member3);
            Assert.Null(obj.Member4);
            Assert.Null(obj.Member5);

            var settings = new JsonSerializerSettings();
            settings.AddInterfaceCallbacks();
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
            XUnitAssert.AreEqualNormalized(
                """
                {
                  "Member1": 11,
                  "Member2": "This value went into the data file during serialization.",
                  "Member4": null
                }
                """,
                json);

            Assert.Equal(11, obj.Member1);
            Assert.Equal("This value was reset after serialization.", obj.Member2);
            Assert.Equal("This is a nonserialized value", obj.Member3);
            Assert.Null(obj.Member4);

            var expectedError = $"Error message for member Member6 = Error getting value from 'Member6' on '{obj.GetType().FullName}'.";
            Assert.Equal(expectedError, obj.Member5);

            var o = JObject.Parse(
                """
                {
                  "Member1": 11,
                  "Member2": "This value went into the data file during serialization.",
                  "Member4": null
                }
                """);
            o["Member6"] = "Dummy text for error";

            obj = (SerializationEventTestObject) JsonConvert.DeserializeObject(o.ToString(), obj.GetType(), settings);

            Assert.Equal(11, obj.Member1);
            Assert.Equal("This value went into the data file during serialization.", obj.Member2);
            Assert.Equal("This value was set during deserialization", obj.Member3);
            Assert.Equal("This value was set after deserialization.", obj.Member4);

            expectedError = $"Error message for member Member6 = Error setting value to 'Member6' on '{obj.GetType()}'.";
            Assert.Equal(expectedError, obj.Member5);

            if (obj is DerivedSerializationEventTestObject derivedObj)
            {
                Assert.Equal("This value was set after deserialization.", derivedObj.Member7);
            }
        }
    }

    [Fact]
    public void ObjectWithConstructorEvents()
    {
        var obj = new SerializationEventTestObjectWithConstructor(11, "Hello World!", null);

        Assert.Equal(11, obj.Member1);
        Assert.Equal("Hello World!", obj.Member2);
        Assert.Equal("This is a nonserialized value", obj.Member3);
        Assert.Null(obj.Member4);

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Member1": 11,
              "Member2": "This value went into the data file during serialization.",
              "Member4": null
            }
            """,
            json);

        Assert.Equal(11, obj.Member1);
        Assert.Equal("This value was reset after serialization.", obj.Member2);
        Assert.Equal("This is a nonserialized value", obj.Member3);
        Assert.Null(obj.Member4);

        obj = JsonConvert.DeserializeObject<SerializationEventTestObjectWithConstructor>(json, settings);

        Assert.Equal(11, obj.Member1);
        Assert.Equal("This value went into the data file during serialization.", obj.Member2);
        Assert.Equal("This value was set during deserialization", obj.Member3);
        Assert.Equal("This value was set after deserialization.", obj.Member4);
    }

    [Fact]
    public void ListEvents()
    {
        var obj = new SerializationEventTestList
        {
            1.1m,
            2.222222222m,
            int.MaxValue,
            (decimal) Math.PI
        };

        Assert.Equal(11, obj.Member1);
        Assert.Equal("Hello World!", obj.Member2);
        Assert.Equal("This is a nonserialized value", obj.Member3);
        Assert.Null(obj.Member4);

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();

        var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
        XUnitAssert.AreEqualNormalized(
            """
            [
              -1.0,
              1.1,
              2.222222222,
              2147483647.0,
              3.1415926535897931159979634685
            ]
            """,
            json);

        Assert.Equal(11, obj.Member1);
        Assert.Equal("This value was reset after serialization.", obj.Member2);
        Assert.Equal("This is a nonserialized value", obj.Member3);
        Assert.Null(obj.Member4);

        obj = JsonConvert.DeserializeObject<SerializationEventTestList>(json, settings);

        Assert.Equal(11, obj.Member1);
        Assert.Equal("Hello World!", obj.Member2);
        Assert.Equal("This value was set during deserialization", obj.Member3);
        Assert.Equal("This value was set after deserialization.", obj.Member4);
    }

    [Fact]
    public void DictionaryEvents()
    {
        var obj = new SerializationEventTestDictionary
        {
            {1.1m, "first"},
            {2.222222222m, "second"},
            {int.MaxValue, "third"},
            {(decimal) Math.PI, "fourth"}
        };

        Assert.Equal(11, obj.Member1);
        Assert.Equal("Hello World!", obj.Member2);
        Assert.Equal("This is a nonserialized value", obj.Member3);
        Assert.Null(obj.Member4);

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "1.1": "first",
              "2.222222222": "second",
              "2147483647": "third",
              "3.1415926535897931159979634685": "fourth",
              "79228162514264337593543950335": "Inserted on serializing"
            }
            """,
            json);

        Assert.Equal(11, obj.Member1);
        Assert.Equal("This value was reset after serialization.", obj.Member2);
        Assert.Equal("This is a nonserialized value", obj.Member3);
        Assert.Null(obj.Member4);

        obj = JsonConvert.DeserializeObject<SerializationEventTestDictionary>(json, settings);

        Assert.Equal(11, obj.Member1);
        Assert.Equal("Hello World!", obj.Member2);
        Assert.Equal("This value was set during deserialization", obj.Member3);
        Assert.Equal("This value was set after deserialization.", obj.Member4);
    }

    [Fact]
    public void ObjectEventsDocumentationExample()
    {
        var obj = new SerializationEventTestObject();

        Assert.Equal(11, obj.Member1);
        Assert.Equal("Hello World!", obj.Member2);
        Assert.Equal("This is a nonserialized value", obj.Member3);
        Assert.Null(obj.Member4);
        Assert.Null(obj.Member5);

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Member1": 11,
              "Member2": "This value went into the data file during serialization.",
              "Member4": null
            }
            """,
            json);

        Assert.Equal(11, obj.Member1);
        Assert.Equal("This value was reset after serialization.", obj.Member2);
        Assert.Equal("This is a nonserialized value", obj.Member3);
        Assert.Null(obj.Member4);
        Assert.Equal("Error message for member Member6 = Error getting value from 'Member6' on 'TestObjects.SerializationEventTestObject'.", obj.Member5);

        obj = JsonConvert.DeserializeObject<SerializationEventTestObject>(json, settings);

        Assert.Equal(11, obj.Member1);
        Assert.Equal("This value went into the data file during serialization.", obj.Member2);
        Assert.Equal("This value was set during deserialization", obj.Member3);
        Assert.Equal("This value was set after deserialization.", obj.Member4);
        Assert.Null(obj.Member5);
    }

    public class SerializationEventBaseTestObject :
        IJsonOnSerializing
    {
        public string TestMember { get; set; }

        public void OnSerializing() =>
            TestMember = "Set!";
    }

    public class SerializationEventContextSubClassTestObject : SerializationEventBaseTestObject;

    [Fact]
    public void SerializationEventContextTestObjectSubClassTest()
    {
        var obj = new SerializationEventContextSubClassTestObject();

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "TestMember": "Set!"
            }
            """,
            json);
    }

    [Fact]
    public void WhenSerializationErrorDetectedBySerializer_ThenCallbackIsCalled()
    {
        var settings = new JsonSerializerSettings
        {
            // If I don't specify Error here, the callback isn't called
            // either, but no exception is thrown.
            MissingMemberHandling = MissingMemberHandling.Error
        };
        settings.AddInterfaceCallbacks();
        var serializer = JsonSerializer.Create(settings);

        // This throws with missing member exception, rather than calling my callback.
        var foo = serializer.Deserialize<FooEvent>(
            new JsonTextReader(
                new StringReader("{ Id: 25 }")));

        // When fixed, this would pass.
        Assert.Equal(25, foo.Identifier);
    }

    public class FooEvent:
        IJsonOnDeserializeError
    {
        public int Identifier { get; set; }

        public void OnDeserializeError(object originalObject, string path, object member, Exception exception, Action markAsHandled)
        {
            Identifier = 25;

            // Here we could for example manually copy the
            // persisted "Id" value into the renamed "Identifier"
            // property, etc.
            markAsHandled();
        }
    }

    [Fact]
    public void DerivedSerializationEvents()
    {
        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();

        var c = JsonConvert.DeserializeObject<DerivedSerializationEventOrderTestObject>("{}", settings);

        JsonConvert.SerializeObject(c, Formatting.Indented, settings);

        var e = c.GetEvents();

        XUnitAssert.AreEqualNormalized(
            """
            OnDeserializing
            OnDeserializing_Derived
            OnDeserialized
            OnDeserialized_Derived
            OnSerializing
            OnSerializing_Derived
            OnSerialized
            OnSerialized_Derived
            """,
            string.Join(Environment.NewLine, e.ToArray()));
    }

    [Fact]
    public Task DerivedDerivedSerializationEvents()
    {
        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();

        var c = JsonConvert.DeserializeObject<DerivedDerivedSerializationEventOrderTestObject>("{}", settings);

        JsonConvert.SerializeObject(c, Formatting.Indented, settings);

        var e = c.GetEvents();

        return Verify(e);
    }

    public class SerializationEventOrderTestObject :
        IJsonOnSerializing,
        IJsonOnSerialized,
        IJsonOnDeserializing,
        IJsonOnDeserialized
    {
        protected IList<string> Events { get; } = [];

        public IList<string> GetEvents() =>
            Events;

        public virtual void OnSerializing() =>
            Events.Add("OnSerializing");

        public virtual void OnSerialized() =>
            Events.Add("OnSerialized");

        public virtual void OnDeserializing() =>
            Events.Add("OnDeserializing");

        public virtual void OnDeserialized() =>
            Events.Add("OnDeserialized");
    }

    public class DerivedSerializationEventOrderTestObject : SerializationEventOrderTestObject
    {
        public override void OnSerializing()
        {
            base.OnSerializing();
            Events.Add("OnSerializing_Derived");
        }

        public override void OnSerialized()
        {
            base.OnSerialized();
            Events.Add("OnSerialized_Derived");
        }

        public override void OnDeserializing()
        {
            base.OnDeserializing();
            Events.Add("OnDeserializing_Derived");
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            Events.Add("OnDeserialized_Derived");
        }
    }

    public class DerivedDerivedSerializationEventOrderTestObject : DerivedSerializationEventOrderTestObject
    {
        public override void OnSerializing()
        {
            base.OnSerializing();
            Events.Add("OnSerializing_Derived_Derived");
        }

        public override void OnSerialized()
        {
            base.OnSerialized();
            Events.Add("OnSerialized_Derived_Derived");
        }

        public override void OnDeserializing()
        {
            base.OnDeserializing();
            Events.Add("OnDeserializing_Derived_Derived");
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            Events.Add("OnDeserialized_Derived_Derived");
        }
    }
}
