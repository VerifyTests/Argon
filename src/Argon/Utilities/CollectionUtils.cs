// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class CollectionUtils
{
    public static bool IsDictionary(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) =>
        type.IsAssignableTo<IDictionary>() ||
        type.ImplementsGeneric(typeof(IDictionary<,>)) ||
        type.ImplementsGeneric(typeof(IReadOnlyDictionary<,>));

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static ConstructorInfo? ResolveEnumerableCollectionConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] this Type collectionType,
        Type collectionItemType)
    {
        var genericConstructorArgument = typeof(IList<>).MakeGenericType(collectionItemType);

        return ResolveEnumerableCollectionConstructor(collectionType, collectionItemType, genericConstructorArgument);
    }

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static ConstructorInfo? ResolveEnumerableCollectionConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        this Type collectionType, Type collectionItemType, Type constructorArgumentType)
    {
        var genericEnumerable = typeof(IEnumerable<>).MakeGenericType(collectionItemType);
        ConstructorInfo? match = null;

        foreach (var constructor in collectionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameters = constructor.GetParameters();

            if (parameters.Length != 1)
            {
                continue;
            }

            var parameterType = parameters[0].ParameterType;

            if (genericEnumerable == parameterType)
            {
                // exact match
                match = constructor;
                break;
            }

            // in case we can't find an exact match, use first inexact
            if (match != null)
            {
                continue;
            }

            if (parameterType.IsAssignableFrom(constructorArgumentType))
            {
                match = constructor;
            }
        }

        return match;
    }

    public static bool Contains<T>(this List<T> list, T value, IEqualityComparer comparer)
    {
        foreach (var item in list)
        {
            if (comparer.Equals(value, item))
            {
                return true;
            }
        }

        return false;
    }

    public static int IndexOfReference<T>(this List<T> list, T item)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(item, list[i]))
            {
                return i;
            }
        }

        return -1;
    }

    static IList<int> GetDimensions(IList values, int dimensionsCount)
    {
        var dimensions = new List<int>();

        var currentArray = values;
        while (true)
        {
            dimensions.Add(currentArray.Count);

            // don't keep calculating dimensions for arrays inside the value array
            if (dimensions.Count == dimensionsCount)
            {
                break;
            }

            if (currentArray.Count == 0)
            {
                break;
            }

            var v = currentArray[0];
            if (v is IList list)
            {
                currentArray = list;
            }
            else
            {
                break;
            }
        }

        return dimensions;
    }

    static void CopyFromJaggedToMultidimensionalArray(IList values, Array multidimensionalArray, int[] indices)
    {
        var dimension = indices.Length;
        if (dimension == multidimensionalArray.Rank)
        {
            multidimensionalArray.SetValue(JaggedArrayGetValue(values, indices), indices);
            return;
        }

        var dimensionLength = multidimensionalArray.GetLength(dimension);
        var list = (IList) JaggedArrayGetValue(values, indices);
        var currentValuesLength = list.Count;
        if (currentValuesLength != dimensionLength)
        {
            throw new("Cannot deserialize non-cubical array as multidimensional array.");
        }

        var newIndices = new int[dimension + 1];
        for (var i = 0; i < dimension; i++)
        {
            newIndices[i] = indices[i];
        }

        for (var i = 0; i < multidimensionalArray.GetLength(dimension); i++)
        {
            newIndices[dimension] = i;
            CopyFromJaggedToMultidimensionalArray(values, multidimensionalArray, newIndices);
        }
    }

    static object JaggedArrayGetValue(IList values, int[] indices)
    {
        var currentList = values;
        for (var i = 0; i < indices.Length; i++)
        {
            var index = indices[i];
            if (i == indices.Length - 1)
            {
                return currentList[index]!;
            }

            currentList = (IList) currentList[index]!;
        }

        return currentList;
    }

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static Array ToMultidimensionalArray(IList values, Type type, int rank)
    {
        var dimensions = GetDimensions(values, rank);

        while (dimensions.Count < rank)
        {
            dimensions.Add(0);
        }

        var multidimensionalArray = Array.CreateInstance(type, dimensions.ToArray());
        CopyFromJaggedToMultidimensionalArray(values, multidimensionalArray, []);

        return multidimensionalArray;
    }

    public static T[] ArrayEmpty<T>() =>
#if !HAS_ARRAY_EMPTY
        // Enumerable.Empty<T> no longer returns an empty array in .NET Core 3.0
        [];
#else
        return Array.Empty<T>();
#endif


#if !HAS_ARRAY_EMPTY
    private static class EmptyArrayContainer<T>
    {
#pragma warning disable CA1825 // Avoid zero-length array allocations.
        public static readonly T[] Empty = [];
#pragma warning restore CA1825 // Avoid zero-length array allocations.
    }
#endif
}