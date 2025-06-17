// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Resolves member mappings for a type, camel casing property names.
/// </summary>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
public class CamelCasePropertyNamesContractResolver :
    DefaultContractResolver
{
    static readonly object typeContractCacheLock = new();
    static readonly DefaultJsonNameTable NameTable = new();
    static Dictionary<Tuple<Type, Type>, JsonContract>? contractCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CamelCasePropertyNamesContractResolver" /> class.
    /// </summary>
    public CamelCasePropertyNamesContractResolver() =>
        NamingStrategy = new CamelCaseNamingStrategy
        {
            ProcessDictionaryKeys = true,
            OverrideSpecifiedNames = true
        };

    /// <summary>
    /// Resolves the contract for a given type.
    /// </summary>
    /// <param name="type">The type to resolve a contract for.</param>
    /// <returns>The contract for a given type.</returns>
    public override JsonContract ResolveContract(Type type)
    {
        // for backwards compatibility the CamelCasePropertyNamesContractResolver shares contracts between instances
        var key = new Tuple<Type, Type>(GetType(), type);
        var cache = contractCache;
        if (cache == null ||
            !cache.TryGetValue(key, out var contract))
        {
            contract = CreateContract(type);

            // avoid the possibility of modifying the cache dictionary while another thread is accessing it
            lock (typeContractCacheLock)
            {
                cache = contractCache;
                Dictionary<Tuple<Type, Type>, JsonContract> updatedCache;
                if (cache == null)
                {
                    updatedCache = [];
                }
                else
                {
                    updatedCache = new(cache);
                }

                updatedCache[key] = contract;

                contractCache = updatedCache;
            }
        }

        return contract;
    }

    public override JsonNameTable GetNameTable() => NameTable;
}