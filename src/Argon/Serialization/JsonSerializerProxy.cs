// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
class JsonSerializerProxy : JsonSerializer
{
    readonly JsonSerializerInternalReader? serializerReader;
    readonly JsonSerializerInternalWriter? serializerWriter;
    internal readonly JsonSerializer serializer;

    public override OnSerializeError? SerializeError
    {
        get => serializer.SerializeError;
        set => serializer.SerializeError = value;
    }

    public override OnDeserializeError? DeserializeError
    {
        get => serializer.DeserializeError;
        set => serializer.DeserializeError = value;
    }

    public override OnSerialized? Serialized
    {
        get => serializer.Serialized;
        set => serializer.Serialized = value;
    }
    public override OnSerializing? Serializing
    {
        get => serializer.Serializing;
        set => serializer.Serializing = value;
    }
    public override OnDeserialized? Deserialized
    {
        get => serializer.Deserialized;
        set => serializer.Deserialized = value;
    }
    public override OnDeserializing? Deserializing
    {
        get => serializer.Deserializing;
        set => serializer.Deserializing = value;
    }

    public override IReferenceResolver? ReferenceResolver
    {
        get => serializer.ReferenceResolver;
        set => serializer.ReferenceResolver = value;
    }

    public override IEqualityComparer? EqualityComparer
    {
        get => serializer.EqualityComparer;
        set => serializer.EqualityComparer = value;
    }

    public override DefaultValueHandling? DefaultValueHandling
    {
        get => serializer.DefaultValueHandling;
        set => serializer.DefaultValueHandling = value;
    }

    public override IContractResolver? ContractResolver
    {
        get => serializer.ContractResolver;
        set => serializer.ContractResolver = value;
    }

    public override MissingMemberHandling? MissingMemberHandling
    {
        get => serializer.MissingMemberHandling;
        set => serializer.MissingMemberHandling = value;
    }

    public override NullValueHandling? NullValueHandling
    {
        get => serializer.NullValueHandling;
        set => serializer.NullValueHandling = value;
    }

    public override ObjectCreationHandling? ObjectCreationHandling
    {
        get => serializer.ObjectCreationHandling;
        set => serializer.ObjectCreationHandling = value;
    }

    public override ReferenceLoopHandling? ReferenceLoopHandling
    {
        get => serializer.ReferenceLoopHandling;
        set => serializer.ReferenceLoopHandling = value;
    }

    public override PreserveReferencesHandling? PreserveReferencesHandling
    {
        get => serializer.PreserveReferencesHandling;
        set => serializer.PreserveReferencesHandling = value;
    }

    public override TypeNameHandling? TypeNameHandling
    {
        get => serializer.TypeNameHandling;
        set => serializer.TypeNameHandling = value;
    }

    public override MetadataPropertyHandling? MetadataPropertyHandling
    {
        get => serializer.MetadataPropertyHandling;
        set => serializer.MetadataPropertyHandling = value;
    }

    public override TypeNameAssemblyFormatHandling? TypeNameAssemblyFormatHandling
    {
        get => serializer.TypeNameAssemblyFormatHandling;
        set => serializer.TypeNameAssemblyFormatHandling = value;
    }

    public override ConstructorHandling? ConstructorHandling
    {
        get => serializer.ConstructorHandling;
        set => serializer.ConstructorHandling = value;
    }

    public override ISerializationBinder? SerializationBinder
    {
        get => serializer.SerializationBinder;
        set => serializer.SerializationBinder = value;
    }

    public override Formatting? Formatting
    {
        get => serializer.Formatting;
        set => serializer.Formatting = value;
    }

    public override FloatFormatHandling? FloatFormatHandling
    {
        get => serializer.FloatFormatHandling;
        set => serializer.FloatFormatHandling = value;
    }

    public override FloatParseHandling? FloatParseHandling
    {
        get => serializer.FloatParseHandling;
        set => serializer.FloatParseHandling = value;
    }

    public override byte? FloatPrecision
    {
        get => serializer.FloatPrecision;
        set => serializer.FloatPrecision = value;
    }

    public override EscapeHandling? EscapeHandling
    {
        get => serializer.EscapeHandling;
        set => serializer.EscapeHandling = value;
    }

    public override int? MaxDepth
    {
        get => serializer.MaxDepth;
        set => serializer.MaxDepth = value;
    }

    public override bool? CheckAdditionalContent
    {
        get => serializer.CheckAdditionalContent;
        set => serializer.CheckAdditionalContent = value;
    }

    internal JsonSerializerInternalBase GetInternalSerializer()
    {
        if (serializerReader == null)
        {
            return serializerWriter!;
        }

        return serializerReader;
    }

    public JsonSerializerProxy(JsonSerializerInternalReader serializerReader)
    {
        this.serializerReader = serializerReader;
        serializer = serializerReader.Serializer;
    }

    public JsonSerializerProxy(JsonSerializerInternalWriter serializerWriter)
    {
        this.serializerWriter = serializerWriter;
        serializer = serializerWriter.Serializer;
    }

    internal override object? TryDeserializeInternal(JsonReader reader, Type? type)
    {
        if (serializerReader == null)
        {
            return serializer.TryDeserialize(reader, type);
        }

        return serializerReader.Deserialize(reader, type, false);
    }

    internal override void SerializeInternal(JsonWriter jsonWriter, object? value, Type? rootType)
    {
        if (serializerWriter == null)
        {
            serializer.Serialize(jsonWriter, value);
        }
        else
        {
            serializerWriter.Serialize(jsonWriter, value, rootType);
        }
    }
}