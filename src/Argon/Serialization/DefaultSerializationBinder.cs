// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// The default serialization binder used when resolving and loading classes from type names.
/// </summary>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
public class DefaultSerializationBinder :
    ISerializationBinder
{
    internal static readonly DefaultSerializationBinder Instance = new();

    static ThreadSafeStore<TypeNameKey, Type> typeCache = new(GetTypeFromTypeNameKey);

    static Type GetTypeFromTypeNameKey(TypeNameKey key)
    {
        var assemblyName = key.Assembly;
        var typeName = key.Type;

        if (assemblyName == null)
        {
            return Type.GetType(typeName)!;
        }

        // look, I don't like using obsolete methods as much as you do but this is the only way
        // Assembly.Load won't check the GAC for a partial name
#pragma warning disable 618,612
        var assembly = Assembly.LoadWithPartialName(assemblyName);
#pragma warning restore 618,612

        if (assembly == null)
        {
            // will find assemblies loaded with Assembly.LoadFile outside of the main directory
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in loadedAssemblies)
            {
                // check for both full name or partial name match
                if (a.FullName == assemblyName ||
                    a.GetName().Name == assemblyName)
                {
                    assembly = a;
                    break;
                }
            }
        }

        if (assembly == null)
        {
            throw new JsonSerializationException($"Could not load assembly '{assemblyName}'.");
        }

        var type = assembly.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        // if generic type, try manually parsing the type arguments for the case of dynamically loaded assemblies
        // example generic typeName format: System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
        if (typeName.IndexOf('`') >= 0)
        {
            try
            {
                type = GetGenericTypeFromTypeName(typeName, assembly);
            }
            catch (Exception exception)
            {
                throw new JsonSerializationException($"Could not find type '{typeName}' in assembly '{assembly.FullName}'.", exception);
            }
        }

        if (type == null)
        {
            throw new JsonSerializationException($"Could not find type '{typeName}' in assembly '{assembly.FullName}'.");
        }

        return type;
    }

    static Type? GetGenericTypeFromTypeName(string typeName, Assembly assembly)
    {
        var openBracketIndex = typeName.IndexOf('[');
        if (openBracketIndex < 0)
        {
            return null;
        }

        var genericTypeDefName = typeName[..openBracketIndex];
        var genericTypeDef = assembly.GetType(genericTypeDefName);
        if (genericTypeDef == null)
        {
            return null;
        }

        var genericTypeArguments = new List<Type>();
        var scope = 0;
        var typeArgStartIndex = 0;
        var endIndex = typeName.Length - 1;
        for (var i = openBracketIndex + 1; i < endIndex; ++i)
        {
            var current = typeName[i];
            switch (current)
            {
                case '[':
                    if (scope == 0)
                    {
                        typeArgStartIndex = i + 1;
                    }

                    ++scope;
                    break;
                case ']':
                    --scope;
                    if (scope == 0)
                    {
                        var typeArgAssemblyQualifiedName = typeName.Substring(typeArgStartIndex, i - typeArgStartIndex);

                        var typeNameKey = ReflectionUtils.SplitFullyQualifiedTypeName(typeArgAssemblyQualifiedName);
                        genericTypeArguments.Add(GetTypeByName(typeNameKey));
                    }

                    break;
            }
        }

        return genericTypeDef.MakeGenericType(genericTypeArguments.ToArray());
    }

    static Type GetTypeByName(TypeNameKey typeNameKey) =>
        typeCache.Get(typeNameKey);

    /// <summary>
    /// When overridden in a derived class, controls the binding of a serialized object to a type.
    /// </summary>
    /// <param name="assemblyName">Specifies the <see cref="Assembly" /> name of the serialized object.</param>
    /// <param name="typeName">Specifies the <see cref="System.Type" /> name of the serialized object.</param>
    /// <returns>
    /// The type of the object the formatter creates a new instance of.
    /// </returns>
    public Type BindToType(string? assemblyName, string typeName) =>
        GetTypeByName(new(assemblyName, typeName));

    /// <summary>
    /// When overridden in a derived class, controls the binding of a serialized object to a type.
    /// </summary>
    /// <param name="serializedType">The type of the object the formatter creates a new instance of.</param>
    /// <param name="assemblyName">Specifies the <see cref="Assembly" /> name of the serialized object.</param>
    /// <param name="typeName">Specifies the <see cref="System.Type" /> name of the serialized object.</param>
    public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
    {
        assemblyName = serializedType.Assembly.FullName;
        typeName = serializedType.FullName;
    }
}