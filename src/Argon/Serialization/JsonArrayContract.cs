// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

public delegate ItemInterceptResult InterceptSerializeArrayItem(object? value);
public delegate IEnumerable InterceptSerializeArrayItems(IEnumerable value);

/// <summary>
/// Contract details for a <see cref="System.Type" /> used by the <see cref="JsonSerializer" />.
/// </summary>
public class JsonArrayContract : JsonContainerContract
{
    /// <summary>
    /// Gets the <see cref="System.Type" /> of the collection items.
    /// </summary>
    public Type? CollectionItemType { get; }

    /// <summary>
    /// Gets a value indicating whether the collection type is a multidimensional array.
    /// </summary>
    public bool IsMultidimensionalArray { get; }

    readonly Type? genericCollectionDefinition;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    Type? genericWrapperType;
    ObjectConstructor? genericWrapperCreator;
    Func<object>? genericTemporaryCollectionCreator;

    internal bool IsArray { get; }
    internal bool ShouldCreateWrapper { get; }
    internal bool CanDeserialize { get; private set; }

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
    public ObjectConstructor? OverrideCreator
    {
        get;
        set
        {
            field = value;
            // hacky
            CanDeserialize = true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the creator has a parameter with the collection values.
    /// </summary>
    public bool HasParameterizedCreator { get; set; }


    internal bool HasParameterizedCreatorInternal => HasParameterizedCreator ||
                                                     parameterizedCreator != null ||
                                                     parameterizedConstructor != null;

    public InterceptSerializeArrayItem InterceptSerializeItem { get; set; } = _ => ItemInterceptResult.Default;
    public InterceptSerializeArrayItems InterceptSerializeItems { get; set; } = value => value;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonArrayContract" /> class.
    /// </summary>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public JsonArrayContract(Type underlyingType)
        : base(underlyingType)
    {
        ContractType = JsonContractType.Array;

        // netcoreapp3.0 uses EmptyPartition for empty enumerable. Treat as an empty array.
        IsArray = CreatedType.IsArray ||
                  (NonNullableUnderlyingType.IsGenericType &&
                   NonNullableUnderlyingType.GetGenericTypeDefinition().FullName == "System.Linq.EmptyPartition`1");

        if (IsArray)
        {
            CollectionItemType = UnderlyingType.GetCollectionItemType()!;
            IsReadOnlyOrFixedSize = true;
            genericCollectionDefinition = typeof(List<>).MakeGenericType(CollectionItemType);

            CanDeserialize = true;
            IsMultidimensionalArray = CreatedType.IsArray && UnderlyingType.GetArrayRank() > 1;
        }
        else if (NonNullableUnderlyingType.IsAssignableTo<IList>())
        {
            if (NonNullableUnderlyingType.ImplementsGeneric(typeof(ICollection<>), out genericCollectionDefinition))
            {
                CollectionItemType = genericCollectionDefinition.GetGenericArguments()[0];
            }
            else
            {
                CollectionItemType = NonNullableUnderlyingType.GetCollectionItemType();
            }

            if (NonNullableUnderlyingType == typeof(IList))
            {
                CreatedType = typeof(List<object>);
            }

            if (CollectionItemType != null)
            {
                parameterizedConstructor = NonNullableUnderlyingType.ResolveEnumerableCollectionConstructor(CollectionItemType);
            }

            IsReadOnlyOrFixedSize = NonNullableUnderlyingType.InheritsGenericDefinition(typeof(ReadOnlyCollection<>));
            CanDeserialize = true;
        }
        else if (NonNullableUnderlyingType.ImplementsGeneric(typeof(ICollection<>), out genericCollectionDefinition))
        {
            CollectionItemType = genericCollectionDefinition.GetGenericArguments()[0];

            if (NonNullableUnderlyingType.IsGenericDefinition(typeof(ICollection<>))
                || NonNullableUnderlyingType.IsGenericDefinition(typeof(IList<>)))
            {
                CreatedType = typeof(List<>).MakeGenericType(CollectionItemType);
            }

            if (NonNullableUnderlyingType.IsGenericDefinition(typeof(ISet<>)))
            {
                CreatedType = typeof(HashSet<>).MakeGenericType(CollectionItemType);
            }

            parameterizedConstructor = NonNullableUnderlyingType.ResolveEnumerableCollectionConstructor(CollectionItemType);
            CanDeserialize = true;
            ShouldCreateWrapper = true;
        }
        else if (NonNullableUnderlyingType.ImplementsGeneric(typeof(IReadOnlyCollection<>), out var tempCollectionType))
        {
            CollectionItemType = tempCollectionType.GetGenericArguments()[0];

            if (NonNullableUnderlyingType.IsGenericDefinition(typeof(IReadOnlyCollection<>))
                || NonNullableUnderlyingType.IsGenericDefinition(typeof(IReadOnlyList<>)))
            {
                CreatedType = typeof(ReadOnlyCollection<>).MakeGenericType(CollectionItemType);
            }

            genericCollectionDefinition = typeof(List<>).MakeGenericType(CollectionItemType);
            parameterizedConstructor = CreatedType.ResolveEnumerableCollectionConstructor(CollectionItemType);

            IsReadOnlyOrFixedSize = true;
            CanDeserialize = HasParameterizedCreatorInternal;
        }
        else if (NonNullableUnderlyingType.ImplementsGeneric(typeof(IEnumerable<>), out tempCollectionType))
        {
            CollectionItemType = tempCollectionType.GetGenericArguments()[0];

            if (UnderlyingType.IsGenericDefinition(typeof(IEnumerable<>)))
            {
                CreatedType = typeof(List<>).MakeGenericType(CollectionItemType);
            }

            parameterizedConstructor = NonNullableUnderlyingType.ResolveEnumerableCollectionConstructor(CollectionItemType);

            if (NonNullableUnderlyingType.IsGenericType &&
                NonNullableUnderlyingType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                genericCollectionDefinition = tempCollectionType;

                IsReadOnlyOrFixedSize = false;
                ShouldCreateWrapper = false;
                CanDeserialize = true;
            }
            else
            {
                genericCollectionDefinition = typeof(List<>).MakeGenericType(CollectionItemType);

                IsReadOnlyOrFixedSize = true;
                ShouldCreateWrapper = true;
                CanDeserialize = HasParameterizedCreatorInternal;
            }
        }
        else
        {
            // types that implement IEnumerable and nothing else
            CanDeserialize = false;
            ShouldCreateWrapper = true;
        }

        if (CollectionItemType != null &&
            ImmutableCollectionsUtils.TryBuildImmutableForArrayContract(
                NonNullableUnderlyingType,
                CollectionItemType,
                out var immutableCreatedType,
                out var immutableParameterizedCreator))
        {
            CreatedType = immutableCreatedType;
            parameterizedCreator = immutableParameterizedCreator;
            IsReadOnlyOrFixedSize = true;
            CanDeserialize = true;
        }
    }

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    internal IWrappedCollection CreateWrapper(object list)
    {
        if (genericWrapperCreator == null)
        {
            MiscellaneousUtils.Assert(genericCollectionDefinition != null);

            genericWrapperType = typeof(CollectionWrapper<>).MakeGenericType(CollectionItemType!);

            Type constructorArgument;

            if (genericCollectionDefinition.InheritsGenericDefinition(typeof(List<>)) ||
                genericCollectionDefinition.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                constructorArgument = typeof(ICollection<>).MakeGenericType(CollectionItemType!);
            }
            else
            {
                constructorArgument = genericCollectionDefinition;
            }

            var genericWrapperConstructor = genericWrapperType.GetConstructor([constructorArgument])!;
            genericWrapperCreator = DelegateFactory.CreateParameterizedConstructor(genericWrapperConstructor);
        }

        return (IWrappedCollection) genericWrapperCreator(list);
    }

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    internal IList CreateTemporaryCollection()
    {
        if (genericTemporaryCollectionCreator == null)
        {
            // multidimensional array will also have array instances in it
            var collectionItemType = IsMultidimensionalArray || CollectionItemType == null
                ? typeof(object)
                : CollectionItemType;

            var temporaryListType = typeof(List<>).MakeGenericType(collectionItemType);
            genericTemporaryCollectionCreator = DelegateFactory.CreateDefaultConstructor<object>(temporaryListType);
        }

        return (IList) genericTemporaryCollectionCreator();
    }
}