// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Collections.Immutable;

/// <summary>
/// Helper class for serializing immutable collections.
/// Note that this is used by all builds, even those that don't support immutable collections, in case the DLL is GACed
/// https://github.com/JamesNK/Newtonsoft.Json/issues/652
/// </summary>
static class ImmutableCollectionsUtils
{
    class TypeInfo(Type createdType, MethodInfo createRange)
    {
        public Type CreatedType { get; } = createdType;
        public MethodInfo CreateRange { get; } = createRange;
    }

    static FrozenDictionary<Type, TypeInfo> arrayDefinitions;

    static FrozenDictionary<Type, TypeInfo> dictionaryDefinitions;

    static ImmutableCollectionsUtils()
    {
        var immutableDictionaryInfo = new TypeInfo(typeof(ImmutableDictionary<,>), GetDictionaryCreateRange(typeof(ImmutableDictionary)));
        dictionaryDefinitions = new Dictionary<Type, TypeInfo>
            {
                {
                    typeof(IImmutableDictionary<,>), immutableDictionaryInfo
                },
                {
                    typeof(ImmutableSortedDictionary<,>), new(typeof(ImmutableSortedDictionary<,>), GetDictionaryCreateRange(typeof(ImmutableSortedDictionary)))
                },
                {
                    typeof(ImmutableDictionary<,>), immutableDictionaryInfo
                }
            }
            .ToFrozenDictionary();

        var immutableListInfo = new TypeInfo(typeof(ImmutableList<>), GetArrayCreateRange(typeof(ImmutableList)));
        var immutableStackInfo = new TypeInfo(typeof(ImmutableStack<>), GetArrayCreateRange(typeof(ImmutableStack)));
        var immutableHashSetInfo = new TypeInfo(typeof(ImmutableHashSet<>), GetArrayCreateRange(typeof(ImmutableHashSet)));
        var immutableQueueCreateRange = GetArrayCreateRange(typeof(ImmutableQueue));
        arrayDefinitions = new Dictionary<Type, TypeInfo>
            {
                {
                    typeof(IImmutableList<>), immutableListInfo
                },
                {
                    typeof(ImmutableList<>), immutableListInfo
                },
                {
                    typeof(IImmutableQueue<>), new(typeof(IImmutableQueue<>), immutableQueueCreateRange)
                },
                {
                    typeof(ImmutableQueue<>), new(typeof(ImmutableQueue<>), immutableQueueCreateRange)
                },
                {
                    typeof(IImmutableStack<>), immutableStackInfo
                },
                {
                    typeof(ImmutableStack<>), immutableStackInfo
                },
                {
                    typeof(IImmutableSet<>), immutableHashSetInfo
                },
                {
                    typeof(ImmutableSortedSet<>), new(typeof(ImmutableSortedSet<>), GetArrayCreateRange(typeof(ImmutableSortedSet)))
                },
                {
                    typeof(ImmutableHashSet<>), immutableHashSetInfo
                },
                {
                    typeof(ImmutableArray<>), new(typeof(ImmutableArray<>), GetArrayCreateRange(typeof(ImmutableArray)))
                }
            }
            .ToFrozenDictionary();
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    internal static bool TryBuildImmutableForArrayContract(Type targetType, Type collectionItemType, [NotNullWhen(true)] out Type? createdType, [NotNullWhen(true)] out ObjectConstructor? parameterizedCreator)
    {
        if (targetType.IsGenericType &&
            arrayDefinitions.TryGetValue(targetType.GetGenericTypeDefinition(), out var definition))
        {
            createdType = definition.CreatedType.MakeGenericType(collectionItemType);
            var method = definition.CreateRange.MakeGenericMethod(collectionItemType);
            parameterizedCreator = DelegateFactory.CreateParameterizedConstructor(method);
            return true;
        }

        createdType = null;
        parameterizedCreator = null;

        return false;
    }

    static MethodInfo GetArrayCreateRange(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods )] Type type) =>
        type
            .GetMethods()
            .Single(_ => _.Name == "CreateRange" &&
                         _.GetParameters()
                             .Length == 1);

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    internal static bool TryBuildImmutableForDictionaryContract(Type targetType, Type keyItemType, Type valueItemType, [NotNullWhen(true)] out Type? createdType, [NotNullWhen(true)] out ObjectConstructor? parameterizedCreator)
    {
        if (targetType.IsGenericType &&
            dictionaryDefinitions.TryGetValue(targetType.GetGenericTypeDefinition(), out var definition))
        {
            createdType = definition.CreatedType.MakeGenericType(keyItemType, valueItemType);
            var create = definition.CreateRange.MakeGenericMethod(keyItemType, valueItemType);
            parameterizedCreator = DelegateFactory.CreateParameterizedConstructor(create);
            return true;
        }

        createdType = null;
        parameterizedCreator = null;
        return false;
    }

    static MethodInfo GetDictionaryCreateRange(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods )] Type type) =>
        type
            .GetMethods()
            .Single(_ =>
            {
                var parameters = _.GetParameters();

                if (_.Name != "CreateRange" ||
                    parameters.Length != 1)
                {
                    return false;
                }

                var parameterType = parameters[0].ParameterType;
                return parameterType.IsGenericType &&
                       parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
            });
}