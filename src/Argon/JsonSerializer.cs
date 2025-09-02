// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Serializes and deserializes objects into and from the JSON format.
/// The <see cref="JsonSerializer" /> enables you to control how objects are encoded into JSON.
/// </summary>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
public class JsonSerializer
{
    IContractResolver? contractResolver;

    int? maxDepth;
    bool maxDepthSet;

    /// <summary>
    /// Occurs when the <see cref="JsonSerializer" /> errors during deserialization.
    /// </summary>
    public virtual OnDeserializeError? DeserializeError { get; set; }

    /// <summary>
    /// Occurs when the <see cref="JsonSerializer" /> errors during serialization.
    /// </summary>
    public virtual OnSerializeError? SerializeError { get; set; }

    public virtual OnSerialized? Serialized { get; set; }
    public virtual OnSerializing? Serializing { get; set; }
    public virtual OnDeserialized? Deserialized { get; set; }
    public virtual OnDeserializing? Deserializing { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IReferenceResolver" /> used by the serializer when resolving references.
    /// </summary>
    public virtual IReferenceResolver? ReferenceResolver { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ISerializationBinder" /> used by the serializer when resolving type names.
    /// </summary>
    public virtual ISerializationBinder? SerializationBinder { get; set; }

    /// <summary>
    /// Gets or sets the equality comparer used by the serializer when comparing references.
    /// </summary>
    public virtual IEqualityComparer? EqualityComparer { get; set; }

    /// <summary>
    /// Gets or sets how type name writing and reading is handled by the serializer.
    /// The default value is <see cref="Argon.TypeNameHandling.None" />.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonSerializer.TypeNameHandling" /> should be used with caution when your application deserializes JSON from an external source.
    /// Incoming types should be validated with a custom <see cref="JsonSerializer.SerializationBinder" />
    /// when deserializing with a value other than <see cref="Argon.TypeNameHandling.None" />.
    /// </remarks>
    public virtual TypeNameHandling? TypeNameHandling { get; set; }

    /// <summary>
    /// Gets or sets how a type name assembly is written and resolved by the serializer.
    /// The default value is <see cref="Argon.TypeNameAssemblyFormatHandling.Simple" />.
    /// </summary>
    public virtual TypeNameAssemblyFormatHandling? TypeNameAssemblyFormatHandling { get; set; }

    /// <summary>
    /// Gets or sets how object references are preserved by the serializer.
    /// The default value is <see cref="Argon.PreserveReferencesHandling.None" />.
    /// </summary>
    public virtual PreserveReferencesHandling? PreserveReferencesHandling { get; set; }

    /// <summary>
    /// Gets or sets how reference loops (e.g. a class referencing itself) is handled.
    /// The default value is <see cref="Argon.ReferenceLoopHandling.Error" />.
    /// </summary>
    public virtual ReferenceLoopHandling? ReferenceLoopHandling { get; set; }

    /// <summary>
    /// Gets or sets how missing members (e.g. JSON contains a property that isn't a member on the object) are handled during deserialization.
    /// The default value is <see cref="Argon.MissingMemberHandling.Ignore" />.
    /// </summary>
    public virtual MissingMemberHandling? MissingMemberHandling { get; set; }

    /// <summary>
    /// Gets or sets how null values are handled during serialization and deserialization.
    /// The default value is <see cref="Argon.NullValueHandling.Include" />.
    /// </summary>
    public virtual NullValueHandling? NullValueHandling { get; set; }

    /// <summary>
    /// Gets or sets how default values are handled during serialization and deserialization.
    /// The default value is <see cref="Argon.DefaultValueHandling.Include" />.
    /// </summary>
    public virtual DefaultValueHandling? DefaultValueHandling { get; set; }

    /// <summary>
    /// Gets or sets how objects are created during deserialization.
    /// The default value is <see cref="Argon.ObjectCreationHandling.Auto" />.
    /// </summary>
    public virtual ObjectCreationHandling? ObjectCreationHandling { get; set; }

    /// <summary>
    /// Gets or sets how constructors are used during deserialization.
    /// The default value is <see cref="Argon.ConstructorHandling.Default" />.
    /// </summary>
    public virtual ConstructorHandling? ConstructorHandling { get; set; }

    /// <summary>
    /// Gets or sets how metadata properties are used during deserialization.
    /// The default value is <see cref="Argon.MetadataPropertyHandling.Default" />.
    /// </summary>
    public virtual MetadataPropertyHandling? MetadataPropertyHandling { get; set; }

    /// <summary>
    /// Gets a collection <see cref="JsonConverter" /> that will be used during serialization.
    /// </summary>
    public virtual List<JsonConverter> Converters { get; } = [];

    /// <summary>
    /// Gets or sets the contract resolver used by the serializer when
    /// serializing .NET objects to JSON and vice versa.
    /// </summary>
    public virtual IContractResolver? ContractResolver
    {
        get => contractResolver;
        set => contractResolver = value;
    }

    public JsonContract ResolveContract(Type type)
    {
        if (contractResolver == null)
        {
            return DefaultContractResolver.Instance.ResolveContract(type);
        }

        return contractResolver.ResolveContract(type);
    }

    /// <summary>
    /// Indicates how JSON text output is formatted.
    /// The default value is <see cref="Argon.Formatting.None" />.
    /// </summary>
    public virtual Formatting? Formatting { get; set; }

    /// <summary>
    /// Gets or sets how floating point numbers, e.g. 1.0 and 9.9, are parsed when reading JSON text.
    /// The default value is <see cref="Argon.FloatParseHandling.Double" />.
    /// </summary>
    public virtual FloatParseHandling? FloatParseHandling { get; set; }

    /// <summary>
    /// Gets or sets how many decimal points to use when serializing floats and doubles.
    /// </summary>
    public virtual byte? FloatPrecision { get; set; }

    /// <summary>
    /// Gets or sets how special floating point numbers, e.g. <see cref="Double.NaN" />,
    /// <see cref="Double.PositiveInfinity" /> and <see cref="Double.NegativeInfinity" />,
    /// are written as JSON text.
    /// The default value is <see cref="Argon.FloatFormatHandling.String" />.
    /// </summary>
    public virtual FloatFormatHandling? FloatFormatHandling { get; set; }

    /// <summary>
    /// Gets or sets how strings are escaped when writing JSON text.
    /// The default value is <see cref="Argon.EscapeHandling.Default" />.
    /// </summary>
    public virtual EscapeHandling? EscapeHandling { get; set; }

    /// <summary>
    /// Gets or sets the maximum depth allowed when reading JSON. Reading past this depth will throw a <see cref="JsonReaderException" />.
    /// A null value means there is no maximum.
    /// The default value is <c>64</c>.
    /// </summary>
    public virtual int? MaxDepth
    {
        get => maxDepth;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentException("Value must be positive.", nameof(value));
            }

            maxDepth = value;
            maxDepthSet = true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether there will be a check for additional JSON content after deserializing an object.
    /// The default value is <c>false</c>.
    /// </summary>
    public virtual bool? CheckAdditionalContent { get; set; }

    internal bool IsCheckAdditionalContentSet() =>
        CheckAdditionalContent != null;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializer" /> class.
    /// </summary>
    public JsonSerializer() =>
        contractResolver = DefaultContractResolver.Instance;

    /// <summary>
    /// Creates a new <see cref="JsonSerializer" /> instance.
    /// The <see cref="JsonSerializer" /> will not use default settings
    /// from <see cref="JsonConvert.DefaultSettings" />.
    /// </summary>
    /// <returns>
    /// A new <see cref="JsonSerializer" /> instance.
    /// The <see cref="JsonSerializer" /> will not use default settings
    /// from <see cref="JsonConvert.DefaultSettings" />.
    /// </returns>
    public static JsonSerializer Create() =>
        new();

    /// <summary>
    /// Creates a new <see cref="JsonSerializer" /> instance using the specified <see cref="JsonSerializerSettings" />.
    /// The <see cref="JsonSerializer" /> will not use default settings
    /// from <see cref="JsonConvert.DefaultSettings" />.
    /// </summary>
    /// <returns>
    /// A new <see cref="JsonSerializer" /> instance using the specified <see cref="JsonSerializerSettings" />.
    /// The <see cref="JsonSerializer" /> will not use default settings
    /// from <see cref="JsonConvert.DefaultSettings" />.
    /// </returns>
    public static JsonSerializer Create(JsonSerializerSettings? settings)
    {
        var serializer = Create();

        if (settings != null)
        {
            ApplySerializerSettings(serializer, settings);
        }

        return serializer;
    }

    /// <summary>
    /// Creates a new <see cref="JsonSerializer" /> instance.
    /// The <see cref="JsonSerializer" /> will use default settings
    /// from <see cref="JsonConvert.DefaultSettings" />.
    /// </summary>
    /// <returns>
    /// A new <see cref="JsonSerializer" /> instance.
    /// The <see cref="JsonSerializer" /> will use default settings
    /// from <see cref="JsonConvert.DefaultSettings" />.
    /// </returns>
    public static JsonSerializer CreateDefault()
    {
        // copy static to local variable to avoid concurrency issues
        var defaultSettings = JsonConvert.DefaultSettings?.Invoke();

        return Create(defaultSettings);
    }

    /// <summary>
    /// Creates a new <see cref="JsonSerializer" /> instance using the specified <see cref="JsonSerializerSettings" />.
    /// The <see cref="JsonSerializer" /> will use default settings
    /// from <see cref="JsonConvert.DefaultSettings" /> as well as the specified <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <returns>
    /// A new <see cref="JsonSerializer" /> instance using the specified <see cref="JsonSerializerSettings" />.
    /// The <see cref="JsonSerializer" /> will use default settings
    /// from <see cref="JsonConvert.DefaultSettings" /> as well as the specified <see cref="JsonSerializerSettings" />.
    /// </returns>
    public static JsonSerializer CreateDefault(JsonSerializerSettings? settings)
    {
        var serializer = CreateDefault();
        if (settings != null)
        {
            ApplySerializerSettings(serializer, settings);
        }

        return serializer;
    }

    static void ApplySerializerSettings(JsonSerializer serializer, JsonSerializerSettings settings)
    {
        if (settings.Converters.Count != 0)
        {
            // insert settings converters at the beginning so they take precedence
            // if user wants to remove one of the default converters they will have to do it manually
            for (var i = 0; i < settings.Converters.Count; i++)
            {
                serializer.Converters.Insert(i, settings.Converters[i]);
            }
        }

        // serializer specific
        if (settings.TypeNameHandling != null)
        {
            serializer.TypeNameHandling = settings.TypeNameHandling;
        }

        if (settings.MetadataPropertyHandling != null)
        {
            serializer.MetadataPropertyHandling = settings.MetadataPropertyHandling;
        }

        if (settings.TypeNameAssemblyFormatHandling != null)
        {
            serializer.TypeNameAssemblyFormatHandling = settings.TypeNameAssemblyFormatHandling;
        }

        if (settings.PreserveReferencesHandling != null)
        {
            serializer.PreserveReferencesHandling = settings.PreserveReferencesHandling;
        }

        if (settings.ReferenceLoopHandling != null)
        {
            serializer.ReferenceLoopHandling = settings.ReferenceLoopHandling;
        }

        if (settings.MissingMemberHandling != null)
        {
            serializer.MissingMemberHandling = settings.MissingMemberHandling;
        }

        if (settings.ObjectCreationHandling != null)
        {
            serializer.ObjectCreationHandling = settings.ObjectCreationHandling;
        }

        if (settings.NullValueHandling != null)
        {
            serializer.NullValueHandling = settings.NullValueHandling;
        }

        if (settings.DefaultValueHandling != null)
        {
            serializer.DefaultValueHandling = settings.DefaultValueHandling;
        }

        if (settings.ConstructorHandling != null)
        {
            serializer.ConstructorHandling = settings.ConstructorHandling;
        }

        if (settings.CheckAdditionalContent != null)
        {
            serializer.CheckAdditionalContent = settings.CheckAdditionalContent;
        }

        if (settings.SerializeError != null)
        {
            serializer.SerializeError = settings.SerializeError;
        }
        if (settings.DeserializeError != null)
        {
            serializer.DeserializeError = settings.DeserializeError;
        }
        if (settings.Serialized != null)
        {
            serializer.Serialized = settings.Serialized;
        }
        if (settings.Serializing != null)
        {
            serializer.Serializing = settings.Serializing;
        }
        if (settings.Deserialized != null)
        {
            serializer.Deserialized = settings.Deserialized;
        }
        if (settings.Deserializing != null)
        {
            serializer.Deserializing = settings.Deserializing;
        }

        if (settings.ContractResolver != null)
        {
            serializer.ContractResolver = settings.ContractResolver;
        }

        if (settings.ReferenceResolverProvider != null)
        {
            serializer.ReferenceResolver = settings.ReferenceResolverProvider();
        }

        if (settings.EqualityComparer != null)
        {
            serializer.EqualityComparer = settings.EqualityComparer;
        }

        if (settings.SerializationBinder != null)
        {
            serializer.SerializationBinder = settings.SerializationBinder;
        }

        // reader/writer specific
        // unset values won't override reader/writer set values
        if (settings.Formatting != null)
        {
            serializer.Formatting = settings.Formatting;
        }

        if (settings.FloatFormatHandling != null)
        {
            serializer.FloatFormatHandling = settings.FloatFormatHandling;
        }

        if (settings.FloatParseHandling != null)
        {
            serializer.FloatParseHandling = settings.FloatParseHandling;
        }

        if (settings.FloatPrecision != null)
        {
            serializer.FloatPrecision = settings.FloatPrecision;
        }

        if (settings.EscapeHandling != null)
        {
            serializer.EscapeHandling = settings.EscapeHandling;
        }

        if (settings.maxDepthSet)
        {
            serializer.maxDepth = settings.maxDepth;
            serializer.maxDepthSet = settings.maxDepthSet;
        }
    }

    /// <summary>
    /// Deserializes the JSON structure contained by the specified <see cref="JsonReader" />.
    /// </summary>
    /// <returns>The <see cref="Object" /> being deserialized.</returns>
    [DebuggerStepThrough]
    public object Deserialize(JsonReader reader) =>
        Deserialize(reader, null);

    /// <summary>
    /// Deserializes the JSON structure contained by the specified <see cref="JsonReader" />.
    /// </summary>
    /// <returns>The <see cref="Object" /> being deserialized.</returns>
    [DebuggerStepThrough]
    public object? TryDeserialize(JsonReader reader) =>
        TryDeserialize(reader, null);

    /// <summary>
    /// Deserializes the JSON structure contained by the specified <see cref="TextReader" />
    /// into an instance of the specified type.
    /// </summary>
    [DebuggerStepThrough]
    public object Deserialize(TextReader reader, Type type) =>
        Deserialize(new JsonTextReader(reader), type);

    /// <summary>
    /// Deserializes the JSON structure contained by the specified <see cref="TextReader" />
    /// into an instance of the specified type.
    /// </summary>
    [DebuggerStepThrough]
    public object? TryDeserialize(TextReader reader, Type type) =>
        TryDeserialize(new JsonTextReader(reader), type);

    /// <summary>
    /// Deserializes the JSON structure contained by the specified <see cref="JsonReader" />
    /// into an instance of the specified type.
    /// </summary>
    [DebuggerStepThrough]
    public T Deserialize<T>(JsonReader reader) =>
        (T) Deserialize(reader, typeof(T));

    /// <summary>
    /// Deserializes the JSON structure contained by the specified <see cref="JsonReader" />
    /// into an instance of the specified type.
    /// </summary>
    [DebuggerStepThrough]
    public T? TryDeserialize<T>(JsonReader reader) =>
        (T?) TryDeserialize(reader, typeof(T));

    /// <summary>
    /// Deserializes the JSON structure contained by the specified <see cref="JsonReader" />
    /// into an instance of the specified type.
    /// </summary>
    [DebuggerStepThrough]
    public object Deserialize(JsonReader reader, Type? type) =>
        DeserializeInternal(reader, type);

    /// <summary>
    /// Deserializes the JSON structure contained by the specified <see cref="JsonReader" />
    /// into an instance of the specified type.
    /// </summary>
    [DebuggerStepThrough]
    public object? TryDeserialize(JsonReader reader, Type? type) =>
        TryDeserializeInternal(reader, type);

    internal virtual object DeserializeInternal(JsonReader reader, Type? type)
    {
        var result = TryDeserializeInternal(reader, type);
        if (result == null)
        {
            throw new("The value resulted in null.");
        }

        return result;
    }

    internal virtual object? TryDeserializeInternal(JsonReader reader, Type? type)
    {
        SetupReader(
            reader,
            out var previousFloatParseHandling,
            out var previousMaxDepth);

        var serializerReader = new JsonSerializerInternalReader(this);
        var value = serializerReader.Deserialize(reader, type, CheckAdditionalContent);

        ResetReader(
            reader,
            previousFloatParseHandling,
            previousMaxDepth);

        return value;
    }

    internal void SetupReader(
        JsonReader reader,
        out FloatParseHandling? previousFloatParseHandling,
        out int? previousMaxDepth)
    {
        if (FloatParseHandling == null ||
            reader.FloatParseHandling == FloatParseHandling)
        {
            previousFloatParseHandling = null;
        }
        else
        {
            previousFloatParseHandling = reader.FloatParseHandling;
            reader.FloatParseHandling = FloatParseHandling.GetValueOrDefault();
        }

        if (maxDepthSet &&
            reader.MaxDepth != maxDepth)
        {
            previousMaxDepth = reader.MaxDepth;
            reader.MaxDepth = maxDepth;
        }
        else
        {
            previousMaxDepth = null;
        }

        if (reader is JsonTextReader textReader)
        {
            if (textReader.PropertyNameTable == null &&
                contractResolver is not null)
            {
                textReader.PropertyNameTable = contractResolver.GetNameTable();
            }
        }
    }

    void ResetReader(
        JsonReader reader,
        FloatParseHandling? previousFloatParseHandling,
        int? previousMaxDepth)
    {
        // reset reader back to previous options
        if (previousFloatParseHandling != null)
        {
            reader.FloatParseHandling = previousFloatParseHandling.GetValueOrDefault();
        }

        if (maxDepthSet)
        {
            reader.MaxDepth = previousMaxDepth;
        }

        if (reader is JsonTextReader {PropertyNameTable: not null} textReader &&
            contractResolver is not null &&
            textReader.PropertyNameTable == contractResolver.GetNameTable())
        {
            textReader.PropertyNameTable = null;
        }
    }

    /// <summary>
    /// Serializes the specified <see cref="Object" /> and writes the JSON structure
    /// using the specified <see cref="TextWriter" />.
    /// </summary>
    public void Serialize(TextWriter textWriter, object? value) =>
        Serialize(new JsonTextWriter(textWriter), value);

    /// <summary>
    /// Serializes the specified <see cref="Object" /> and writes the JSON structure
    /// using the specified <see cref="JsonWriter" />.
    /// </summary>
    /// <param name="type">
    /// The type of the value being serialized.
    /// This parameter is used when <see cref="JsonSerializer.TypeNameHandling" /> is <see cref="Argon.TypeNameHandling.Auto" /> to write out the type name if the type of the value does not match.
    /// Specifying the type is optional.
    /// </param>
    public void Serialize(JsonWriter jsonWriter, object? value, Type? type) =>
        SerializeInternal(jsonWriter, value, type);

    /// <summary>
    /// Serializes the specified <see cref="Object" /> and writes the JSON structure
    /// using the specified <see cref="TextWriter" />.
    /// </summary>
    /// <param name="type">
    /// The type of the value being serialized.
    /// This parameter is used when <see cref="TypeNameHandling" /> is Auto to write out the type name if the type of the value does not match.
    /// Specifying the type is optional.
    /// </param>
    public void Serialize(TextWriter textWriter, object? value, Type type) =>
        Serialize(new JsonTextWriter(textWriter), value, type);

    /// <summary>
    /// Serializes the specified <see cref="Object" /> and writes the JSON structure
    /// using the specified <see cref="JsonWriter" />.
    /// </summary>
    public void Serialize(JsonWriter jsonWriter, object? value) =>
        SerializeInternal(jsonWriter, value, null);

    internal virtual void SerializeInternal(JsonWriter jsonWriter, object? value, Type? type)
    {
        // set serialization options onto writer
        Formatting? previousFormatting = null;
        if (Formatting != null && jsonWriter.Formatting != Formatting)
        {
            previousFormatting = jsonWriter.Formatting;
            jsonWriter.Formatting = Formatting.GetValueOrDefault();
        }

        FloatFormatHandling? previousFloatFormatHandling = null;
        if (FloatFormatHandling != null && jsonWriter.FloatFormatHandling != FloatFormatHandling)
        {
            previousFloatFormatHandling = jsonWriter.FloatFormatHandling;
            jsonWriter.FloatFormatHandling = FloatFormatHandling.GetValueOrDefault();
        }

        byte? previousFloatPrecision = null;
        if (FloatPrecision != null && jsonWriter.FloatPrecision != FloatPrecision)
        {
            previousFloatPrecision = jsonWriter.FloatPrecision;
            jsonWriter.FloatPrecision = FloatPrecision;
        }

        EscapeHandling? previousEscapeHandling = null;
        if (EscapeHandling != null && jsonWriter.EscapeHandling != EscapeHandling)
        {
            previousEscapeHandling = jsonWriter.EscapeHandling;
            jsonWriter.EscapeHandling = EscapeHandling.GetValueOrDefault();
        }

        var serializerWriter = new JsonSerializerInternalWriter(this);
        serializerWriter.Serialize(jsonWriter, value, type);

        // reset writer back to previous options
        if (previousFormatting != null)
        {
            jsonWriter.Formatting = previousFormatting.GetValueOrDefault();
        }

        if (previousFloatFormatHandling != null)
        {
            jsonWriter.FloatFormatHandling = previousFloatFormatHandling.GetValueOrDefault();
        }

        if (previousFloatPrecision != null)
        {
            jsonWriter.FloatPrecision = previousFloatPrecision;
        }

        if (previousEscapeHandling != null)
        {
            jsonWriter.EscapeHandling = previousEscapeHandling.GetValueOrDefault();
        }
    }

    internal IReferenceResolver GetReferenceResolver() =>
        ReferenceResolver ??= new DefaultReferenceResolver();

    internal JsonConverter? GetMatchingConverter(Type type) =>
        GetMatchingConverter(Converters, type);

    internal static JsonConverter? GetMatchingConverter(IList<JsonConverter>? converters, Type type)
    {
        if (converters != null)
        {
            foreach (var converter in converters)
            {
                if (converter.CanConvert(type))
                {
                    return converter;
                }
            }
        }

        return null;
    }
}