// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Contract details for a <see cref="System.Type" /> used by the <see cref="JsonSerializer" />.
/// </summary>
public abstract class JsonContract
{
    internal bool IsNullable;
    internal bool IsConvertible;
    internal bool IsEnum;
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
    internal Type NonNullableUnderlyingType;
    internal ReadType InternalReadType;
    internal JsonContractType ContractType;
    internal bool IsReadOnlyOrFixedSize;
    internal bool IsSealed;
    internal bool IsInstantiable;

    /// <summary>
    /// Gets the underlying type for the contract.
    /// </summary>
    public Type UnderlyingType { get; }

    /// <summary>
    /// Gets or sets the type created during deserialization.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
    public Type CreatedType
    {
        get;
        set
        {
            field = value;

            IsSealed = field.IsSealed;
            IsInstantiable = !(field.IsInterface || field.IsAbstract);
        }
    }

    /// <summary>
    /// Gets or sets whether this type contract is serialized as a reference.
    /// </summary>
    public bool? IsReference { get; set; }

    /// <summary>
    /// Gets or sets the default <see cref="JsonConverter" /> for this contract.
    /// </summary>
    public JsonConverter? Converter { get; set; }

    /// <summary>
    /// Gets the internally resolved <see cref="JsonConverter" /> for the contract's type.
    /// This converter is used as a fallback converter when no other converter is resolved.
    /// Setting <see cref="Converter" /> will always override this converter.
    /// </summary>
    public JsonConverter? InternalConverter { get; internal set; }

    /// <summary>
    /// Gets or sets the default creator method used to create the object.
    /// </summary>
    public Func<object>? DefaultCreator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the default creator is non-public.
    /// </summary>
    public bool DefaultCreatorNonPublic { get; set; }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    internal JsonContract(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type underlyingType)
    {
        UnderlyingType = underlyingType;

        // resolve ByRef types
        // typically comes from in and ref parameters on methods/ctors
        underlyingType = underlyingType.EnsureNotByRefType();

        IsNullable = underlyingType.IsNullable();

        if (IsNullable && underlyingType.IsNullableType())
        {
            NonNullableUnderlyingType = Nullable.GetUnderlyingType(underlyingType)!;
        }
        else
        {
            NonNullableUnderlyingType = underlyingType;
        }

        CreatedType = CreatedType = NonNullableUnderlyingType;

        IsConvertible = NonNullableUnderlyingType.IsConvertible();
        IsEnum = NonNullableUnderlyingType.IsEnum;

        InternalReadType = ReadType.Read;
    }
}