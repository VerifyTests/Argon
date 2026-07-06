// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class SerializationEventTestList :
    Collection<decimal>,
    IJsonOnSerializing,
    IJsonOnSerialized,
    IJsonOnDeserializing,
    IJsonOnDeserialized
{
    // This member is serialized and deserialized with no change.
    public int Member1 { get; }

    // The value of this field is set and reset during and
    // after serialization.
    public string Member2 { get; private set; }

    // This field is not serialized. The OnDeserializedAttribute
    // is used to set the member value after serialization.
    public string Member3 { get; private set; }

    // This field is set to null, but populated after deserialization.
    public string Member4 { get; private set; }

    public SerializationEventTestList()
    {
        Member1 = 11;
        Member2 = "Hello World!";
        Member3 = "This is a nonserialized value";
        Member4 = null;
    }

    public void OnSerializing()
    {
        Member2 = "This value went into the data file during serialization.";
        Insert(0, -1);
    }

    public void OnSerialized() =>
        Member2 = "This value was reset after serialization.";

    public void OnDeserializing() =>
        Member3 = "This value was set during deserialization";

    public void OnDeserialized() =>
        Member4 = "This value was set after deserialization.";

}