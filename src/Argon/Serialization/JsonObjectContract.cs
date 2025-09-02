// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Contract details for a <see cref="System.Type" /> used by the <see cref="JsonSerializer" />.
/// </summary>
public class JsonObjectContract : JsonContainerContract
{
    /// <summary>
    /// Gets or sets the object member serialization.
    /// </summary>
    public MemberSerialization MemberSerialization { get; set; }

    /// <summary>
    /// Gets or sets the missing member handling used when deserializing this object.
    /// </summary>
    public MissingMemberHandling? MissingMemberHandling { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the object's properties are required.
    /// </summary>
    public Required? ItemRequired { get; set; }

    /// <summary>
    /// Gets or sets how the object's properties with null values are handled during serialization and deserialization.
    /// </summary>
    public NullValueHandling? ItemNullValueHandling { get; set; }

    /// <summary>
    /// Gets the object's properties.
    /// </summary>
    public JsonPropertyCollection Properties { get; }

    /// <summary>
    /// Gets a collection of <see cref="JsonProperty" /> instances that define the parameters used with <see cref="JsonObjectContract.OverrideCreator" />.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public JsonPropertyCollection CreatorParameters => field ??= new(UnderlyingType);

    /// <summary>
    /// Gets or sets the function used to create the object. When set this function will override <see cref="JsonContract.DefaultCreator" />.
    /// This function is called with a collection of arguments which are defined by the <see cref="JsonObjectContract.CreatorParameters" /> collection.
    /// </summary>
    public ObjectConstructor? OverrideCreator { get; set; }

    internal ObjectConstructor? ParameterizedCreator { get; set; }

    bool? hasRequiredOrDefaultValueProperties;

    internal bool HasRequiredOrDefaultValueProperties
    {
        get
        {
            if (hasRequiredOrDefaultValueProperties == null)
            {
                hasRequiredOrDefaultValueProperties = false;

                if (ItemRequired.GetValueOrDefault(Required.Default) == Required.Default)
                {
                    foreach (var property in Properties)
                    {
                        if (property.Required != Required.Default || (property.DefaultValueHandling & DefaultValueHandling.Populate) == DefaultValueHandling.Populate)
                        {
                            hasRequiredOrDefaultValueProperties = true;
                            break;
                        }
                    }
                }
                else
                {
                    hasRequiredOrDefaultValueProperties = true;
                }
            }

            return hasRequiredOrDefaultValueProperties.GetValueOrDefault();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonObjectContract" /> class.
    /// </summary>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public JsonObjectContract(Type underlyingType)
        : base(underlyingType)
    {
        ContractType = JsonContractType.Object;

        Properties = new(UnderlyingType);
    }

#pragma warning disable SYSLIB0050
    internal object GetUninitializedObject() =>
        FormatterServices.GetUninitializedObject(NonNullableUnderlyingType);
#pragma warning restore SYSLIB0050
}