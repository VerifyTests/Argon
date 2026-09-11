// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Specifies the settings on a <see cref="JsonSerializer" /> object.
/// </summary>
public class JsonSerializerSettings
{
    internal int? maxDepth;
    internal bool maxDepthSet;

    /// <summary>
    /// Gets or sets how reference loops (e.g. a class referencing itself) are handled.
    /// The default value is <see cref="Argon.ReferenceLoopHandling.Error" />.
    /// </summary>
    public ReferenceLoopHandling? ReferenceLoopHandling { get; set; }

    /// <summary>
    /// Gets or sets how missing members (e.g. JSON contains a property that isn't a member on the object) are handled during deserialization.
    /// The default value is <see cref="Argon.MissingMemberHandling.Ignore" />.
    /// </summary>
    public MissingMemberHandling? MissingMemberHandling { get; set; }

    /// <summary>
    /// Gets or sets how objects are created during deserialization.
    /// The default value is <see cref="Argon.ObjectCreationHandling.Auto" />.
    /// </summary>
    public ObjectCreationHandling? ObjectCreationHandling { get; set; }

    /// <summary>
    /// Gets or sets how null values are handled during serialization and deserialization.
    /// The default value is <see cref="Argon.NullValueHandling.Include" />.
    /// </summary>
    public NullValueHandling? NullValueHandling { get; set; }

    /// <summary>
    /// Gets or sets how default values are handled during serialization and deserialization.
    /// The default value is <see cref="Argon.DefaultValueHandling.Include" />.
    /// </summary>
    public DefaultValueHandling? DefaultValueHandling { get; set; }

    /// <summary>
    /// Gets or sets a <see cref="JsonConverter" /> collection that will be used during serialization.
    /// </summary>
    public List<JsonConverter> Converters { get; set; } = [];

    /// <summary>
    /// Gets or sets how object references are preserved by the serializer.
    /// The default value is <see cref="Argon.PreserveReferencesHandling.None" />.
    /// </summary>
    public PreserveReferencesHandling? PreserveReferencesHandling { get; set; }

    /// <summary>
    /// Gets or sets how type name writing and reading is handled by the serializer.
    /// The default value is <see cref="Argon.TypeNameHandling.None" />.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonSerializerSettings.TypeNameHandling" /> should be used with caution when your application deserializes JSON from an external source.
    /// Incoming types should be validated with a custom <see cref="JsonSerializerSettings.SerializationBinder" />
    /// when deserializing with a value other than <see cref="Argon.TypeNameHandling.None" />.
    /// </remarks>
    public TypeNameHandling? TypeNameHandling { get; set; }

    /// <summary>
    /// Gets or sets whether a type discriminator is inferred for <c>closed</c> type hierarchies.
    /// The default value is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// When enabled, a value whose declared type is a closed base is written with a <c>$type</c>
    /// property holding the simple name of its runtime type, and read back by matching that name
    /// against the derived types the compiler recorded on the base. Unlike
    /// <see cref="JsonSerializerSettings.TypeNameHandling" /> the candidate types are fixed at compile time, so an
    /// incoming discriminator can never name an arbitrary type and no
    /// <see cref="ISerializationBinder" /> is needed to validate it.
    /// </remarks>
    public bool? InferClosedTypePolymorphism { get; set; }

    /// <summary>
    /// Gets or sets how metadata properties are used during deserialization.
    /// The default value is <see cref="Argon.MetadataPropertyHandling.Default" />.
    /// </summary>
    public MetadataPropertyHandling? MetadataPropertyHandling { get; set; }

    /// <summary>
    /// Gets or sets how a type name assembly is written and resolved by the serializer.
    /// The default value is <see cref="Argon.TypeNameAssemblyFormatHandling.Simple" />.
    /// </summary>
    public TypeNameAssemblyFormatHandling? TypeNameAssemblyFormatHandling { get; set; }

    /// <summary>
    /// Gets or sets how constructors are used during deserialization.
    /// The default value is <see cref="Argon.ConstructorHandling.Default" />.
    /// </summary>
    public ConstructorHandling? ConstructorHandling { get; set; }

    /// <summary>
    /// Gets or sets the contract resolver used by the serializer when
    /// serializing .NET objects to JSON and vice versa.
    /// </summary>
    public IContractResolver? ContractResolver { get; set; }

    /// <summary>
    /// Gets or sets the equality comparer used by the serializer when comparing references.
    /// </summary>
    public IEqualityComparer? EqualityComparer { get; set; }

    /// <summary>
    /// Gets or sets a function that creates the <see cref="IReferenceResolver" /> used by the serializer when resolving references.
    /// </summary>
    public Func<IReferenceResolver?>? ReferenceResolverProvider { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ISerializationBinder" /> used by the serializer when resolving type names.
    /// </summary>
    public ISerializationBinder? SerializationBinder { get; set; }

    /// <summary>
    /// Gets or sets the error handler called during deserialization.
    /// </summary>
    public OnDeserializeError? DeserializeError { get; set; }
    /// <summary>
    /// Gets or sets the error handler called during serialization.
    /// </summary>
    public OnSerializeError? SerializeError { get; set; }
    public OnSerialized? Serialized { get; set; }
    public OnSerializing? Serializing { get; set; }
    public OnDeserialized? Deserialized { get; set; }
    public OnDeserializing? Deserializing { get; set; }

    const int DefaultMaxDepth = 64;

    /// <summary>
    /// Gets or sets the maximum depth allowed when reading JSON. Reading past this depth will throw a <see cref="JsonReaderException" />.
    /// A null value means there is no maximum.
    /// The default value is <c>64</c>.
    /// </summary>
    public int? MaxDepth
    {
        get => maxDepthSet ? maxDepth : DefaultMaxDepth;
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
    /// Indicates how JSON text output is formatted.
    /// The default value is <see cref="Argon.Formatting.None" />.
    /// </summary>
    public Formatting? Formatting { get; set; }

    /// <summary>
    /// Gets or sets how special floating point numbers, e.g. <see cref="Double.NaN" />,
    /// <see cref="Double.PositiveInfinity" /> and <see cref="Double.NegativeInfinity" />,
    /// are written as JSON.
    /// The default value is <see cref="Argon.FloatFormatHandling.String" />.
    /// </summary>
    public FloatFormatHandling? FloatFormatHandling { get; set; }

    /// <summary>
    /// Gets or sets how floating point numbers, e.g. 1.0 and 9.9, are parsed when reading JSON text.
    /// The default value is <see cref="Argon.FloatParseHandling.Double" />.
    /// </summary>
    public FloatParseHandling? FloatParseHandling { get; set; }

    /// <summary>
    /// Gets or sets how many decimal points to use when serializing floats and doubles.
    /// </summary>
    public byte? FloatPrecision { get; set; }

    /// <summary>
    /// Gets or sets how strings are escaped when writing JSON text.
    /// The default value is <see cref="Argon.EscapeHandling.Default" />.
    /// </summary>
    public EscapeHandling? EscapeHandling { get; set; }

    /// <summary>
    /// Gets a value indicating whether there will be a check for additional content after deserializing an object.
    /// The default value is <c>false</c>.
    /// </summary>
    public bool? CheckAdditionalContent { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializerSettings" /> class.
    /// </summary>
    public JsonSerializerSettings()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializerSettings"/> class
    /// using values copied from the passed in <see cref="JsonSerializerSettings"/>.
    /// </summary>
    public JsonSerializerSettings(JsonSerializerSettings original)
    {
        FloatPrecision = original.FloatPrecision;
        FloatParseHandling = original.FloatParseHandling;
        FloatFormatHandling = original.FloatFormatHandling;
        DefaultValueHandling = original.DefaultValueHandling;
        Formatting = original.Formatting;
        maxDepth = original.maxDepth;
        maxDepthSet = original.maxDepthSet;
        SerializeError = original.SerializeError;
        DeserializeError = original.DeserializeError;
        Serialized = original.Serialized;
        Serializing = original.Serializing;
        Deserialized = original.Deserialized;
        Deserializing = original.Deserializing;
        EscapeHandling = original.EscapeHandling;
        SerializationBinder = original.SerializationBinder;
        ReferenceResolverProvider = original.ReferenceResolverProvider;
        EqualityComparer = original.EqualityComparer;
        ContractResolver = original.ContractResolver;
        ConstructorHandling = original.ConstructorHandling;
        TypeNameAssemblyFormatHandling = original.TypeNameAssemblyFormatHandling;
        MetadataPropertyHandling = original.MetadataPropertyHandling;
        TypeNameHandling = original.TypeNameHandling;
        InferClosedTypePolymorphism = original.InferClosedTypePolymorphism;
        PreserveReferencesHandling = original.PreserveReferencesHandling;
        Converters = original.Converters.ToList();
        DefaultValueHandling = original.DefaultValueHandling;
        NullValueHandling = original.NullValueHandling;
        ObjectCreationHandling = original.ObjectCreationHandling;
        MissingMemberHandling = original.MissingMemberHandling;
        ReferenceLoopHandling = original.ReferenceLoopHandling;
        CheckAdditionalContent = original.CheckAdditionalContent;
    }
}