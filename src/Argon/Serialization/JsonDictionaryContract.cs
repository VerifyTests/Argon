// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Collections.Immutable;
using System.Collections.Specialized;

namespace Argon;

public delegate KeyValueInterceptResult InterceptSerializeDictionaryItem(JsonWriter writer, object key, object? value);
//TODO: should defer execution of name
public delegate string DictionaryKeyResolver(JsonWriter writer, string name, object original);

/// <summary>
/// Contract details for a <see cref="System.Type" /> used by the <see cref="JsonSerializer" />.
/// </summary>
public class JsonDictionaryContract : JsonContainerContract
{
    /// <summary>
    /// Gets or sets the dictionary key resolver.
    /// </summary>
    public DictionaryKeyResolver? DictionaryKeyResolver { get; set; }

    public InterceptSerializeDictionaryItem InterceptSerializeItem { get; set; } = (_, _, _) => KeyValueInterceptResult.Default;

    /// <summary>
    /// Gets the <see cref="System.Type" /> of the dictionary keys.
    /// </summary>
    public Type? DictionaryKeyType { get; }

    /// <summary>
    /// Gets the <see cref="System.Type" /> of the dictionary values.
    /// </summary>
    public Type? DictionaryValueType { get; }

    internal JsonContract? KeyContract { get; set; }

    readonly Type? dictionaryDefinition;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    Type? genericWrapperType;
    ObjectConstructor? genericWrapperCreator;

    Func<object>? genericTemporaryDictionaryCreator;

    internal bool ShouldCreateWrapper { get; }

    public bool OrderByKey { get; set; }

    readonly ConstructorInfo? parameterizedConstructor;

    ObjectConstructor? parameterizedCreator;

    internal ObjectConstructor? ParameterizedCreator
    {
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        get
        {
            if (parameterizedCreator == null &&
                parameterizedConstructor != null)
            {
                parameterizedCreator = DelegateFactory.CreateParameterizedConstructor(parameterizedConstructor);
            }

            return parameterizedCreator;
        }
    }

    /// <summary>
    /// Gets or sets the function used to create the object. When set this function will override <see cref="JsonContract.DefaultCreator" />.
    /// </summary>
    public ObjectConstructor? OverrideCreator { get; set; }

    /// <summary>
    /// Gets a value indicating whether the creator has a parameter with the dictionary values.
    /// </summary>
    public bool HasParameterizedCreator { get; set; }

    internal bool HasParameterizedCreatorInternal => HasParameterizedCreator ||
                                                     parameterizedCreator != null ||
                                                     parameterizedConstructor != null;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDictionaryContract" /> class.
    /// </summary>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public JsonDictionaryContract(Type underlyingType)
        : base(underlyingType)
    {
        ContractType = JsonContractType.Dictionary;

        Type? keyType;
        Type? valueType;

        if (NonNullableUnderlyingType.ImplementsGeneric(typeof(IDictionary<,>), out dictionaryDefinition))
        {
            var genericArguments = dictionaryDefinition.GetGenericArguments();
            keyType = genericArguments[0];
            valueType = genericArguments[1];

            if (NonNullableUnderlyingType.IsGenericDefinition(typeof(IDictionary<,>)))
            {
                CreatedType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            }
            else if (NonNullableUnderlyingType.IsGenericType)
            {
                // ConcurrentDictionary<,> + IDictionary setter + null value = error
                // wrap to use generic setter
                // https://github.com/JamesNK/Newtonsoft.Json/issues/1582
                var typeDefinition = NonNullableUnderlyingType.GetGenericTypeDefinition();
                if (typeDefinition.FullName == JsonTypeReflector.ConcurrentDictionaryTypeName)
                {
                    ShouldCreateWrapper = true;
                }
            }

            IsReadOnlyOrFixedSize = NonNullableUnderlyingType.InheritsGenericDefinition(typeof(ReadOnlyDictionary<,>));
        }
        else if (NonNullableUnderlyingType.ImplementsGeneric(typeof(IReadOnlyDictionary<,>), out dictionaryDefinition))
        {
            var genericArguments = dictionaryDefinition.GetGenericArguments();
            keyType = genericArguments[0];
            valueType = genericArguments[1];

            if (NonNullableUnderlyingType.IsGenericDefinition(typeof(IReadOnlyDictionary<,>)))
            {
                CreatedType = typeof(ReadOnlyDictionary<,>).MakeGenericType(keyType, valueType);
            }

            IsReadOnlyOrFixedSize = true;
        }
        else
        {
            NonNullableUnderlyingType.GetDictionaryKeyValueTypes(out keyType, out valueType);

            if (NonNullableUnderlyingType == typeof(IDictionary))
            {
                CreatedType = typeof(Dictionary<object, object>);
            }
        }

        if (keyType != null &&
            valueType != null)
        {
            parameterizedConstructor = CreatedType.ResolveEnumerableCollectionConstructor(
                typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType),
                typeof(IDictionary<,>).MakeGenericType(keyType, valueType));
        }

        if (!CreatedType.IsAssignableTo<IDictionary>())
        {
            ShouldCreateWrapper = true;
        }

        DictionaryKeyType = keyType;
        DictionaryValueType = valueType;

        if (keyType != null &&
            !IsSortedDictionary(underlyingType))
        {
            if (keyType == typeof(string) ||
                keyType.IsAssignableTo<IComparable>())
            {
                IsSortable = true;
            }
        }

        if (DictionaryKeyType != null &&
            DictionaryValueType != null &&
            ImmutableCollectionsUtils.TryBuildImmutableForDictionaryContract(
                NonNullableUnderlyingType,
                DictionaryKeyType,
                DictionaryValueType,
                out var immutableCreatedType,
                out var immutableParameterizedCreator))
        {
            CreatedType = immutableCreatedType;
            parameterizedCreator = immutableParameterizedCreator;
            IsReadOnlyOrFixedSize = true;
        }
    }

    internal bool IsSortable { get; set; }

    static bool IsSortedDictionary(Type type)
    {
        if (type.IsAssignableTo<OrderedDictionary>())
        {
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(SortedDictionary<,>) ||
               definition == typeof(ImmutableSortedDictionary<,>);
    }

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    internal IWrappedDictionary CreateWrapper(object dictionary)
    {
        if (genericWrapperCreator == null)
        {
            genericWrapperType = typeof(DictionaryWrapper<,>).MakeGenericType(DictionaryKeyType!, DictionaryValueType!);

            var genericWrapperConstructor = genericWrapperType.GetConstructor([dictionaryDefinition!])!;
            genericWrapperCreator = DelegateFactory.CreateParameterizedConstructor(genericWrapperConstructor);
        }

        return (IWrappedDictionary) genericWrapperCreator(dictionary);
    }

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    internal IDictionary CreateTemporaryDictionary()
    {
        if (genericTemporaryDictionaryCreator == null)
        {
            var temporaryDictionaryType = typeof(Dictionary<,>).MakeGenericType(DictionaryKeyType ?? typeof(object), DictionaryValueType ?? typeof(object));

            genericTemporaryDictionaryCreator = DelegateFactory.CreateDefaultConstructor<object>(temporaryDictionaryType);
        }

        return (IDictionary) genericTemporaryDictionaryCreator();
    }
}