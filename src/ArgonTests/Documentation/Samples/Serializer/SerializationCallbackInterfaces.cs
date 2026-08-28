// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class SerializationCallbackInterfaces : TestFixtureBase
{
    #region SerializationCallbackInterfaces

    public class SerializationEventTestObject :
        IJsonOnSerializing,
        IJsonOnSerialized,
        IJsonOnDeserializing,
        IJsonOnDeserialized
    {
        // 2222
        // This member is serialized and deserialized with no change.
        public int Member1 { get; set; } = 11;

        // The value of this field is set and reset during and
        // after serialization.
        public string Member2 { get; set; } = "Hello World!";

        // This field is not serialized. The OnDeserializedAttribute
        // is used to set the member value after serialization.
        [JsonIgnore]
        public string Member3 { get; set; } = "This is a nonserialized value";

        // This field is set to null, but populated after deserialization.
        public string Member4 { get; set; }

        public void OnSerializing() =>
            Member2 = "This value went into the data file during serialization.";

        public void OnSerialized() =>
            Member2 = "This value was reset after serialization.";

        public void OnDeserializing() =>
            Member3 = "This value was set during deserialization";

        public void OnDeserialized() =>
            Member4 = "This value was set after deserialization.";
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region SerializationCallbackInterfacesUsage

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();

        var obj = new SerializationEventTestObject();

        Console.WriteLine(obj.Member1);
        // 11
        Console.WriteLine(obj.Member2);
        // Hello World!
        Console.WriteLine(obj.Member3);
        // This is a nonserialized value
        Console.WriteLine(obj.Member4);
        // null

        var json = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
        // {
        //   "Member1": 11,
        //   "Member2": "This value went into the data file during serialization.",
        //   "Member4": null
        // }

        Console.WriteLine(obj.Member1);
        // 11
        Console.WriteLine(obj.Member2);
        // This value was reset after serialization.
        Console.WriteLine(obj.Member3);
        // This is a nonserialized value
        Console.WriteLine(obj.Member4);
        // null

        obj = JsonConvert.DeserializeObject<SerializationEventTestObject>(json, settings);

        Console.WriteLine(obj.Member1);
        // 11
        Console.WriteLine(obj.Member2);
        // This value went into the data file during serialization.
        Console.WriteLine(obj.Member3);
        // This value was set during deserialization
        Console.WriteLine(obj.Member4);
        // This value was set after deserialization.

        #endregion

        Assert.Equal("This value was set after deserialization.", obj.Member4);
    }
}