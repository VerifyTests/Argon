// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class ReflectionUtils
{
    public static bool ImplementInterface(this Type type, Type interfaceType)
    {
        if (type == interfaceType)
        {
            return false;
        }

        return interfaceType.IsAssignableFrom(type);
    }

    public static T GetValueOrDefault<T>(this T? target, T? fallback)
        where T : struct, Enum
    {
        if (target.HasValue)
        {
            return target.Value;
        }

        if (fallback.HasValue)
        {
            return fallback.Value;
        }

        return default;
    }

    public static bool IsVirtual(this PropertyInfo property) =>
        property.Method().IsVirtual;

    static MethodInfo Method(this PropertyInfo property)
    {
        if (property.GetMethod == null)
        {
            return property.SetMethod!;
        }

        return property.GetMethod;
    }

    static MethodInfo GetBaseDefinition(this PropertyInfo property) =>
        property.Method().GetBaseDefinition();

    static bool IsPublic(this PropertyInfo property) =>
        property.Method().IsPublic;

    public static string GetTypeName(this Type type, TypeNameAssemblyFormatHandling? assemblyFormat, ISerializationBinder? binder)
    {
        var fullyQualifiedTypeName = GetFullyQualifiedTypeName(type, binder);

        return assemblyFormat switch
        {
            null => RemoveAssemblyDetails(fullyQualifiedTypeName),
            TypeNameAssemblyFormatHandling.Simple => RemoveAssemblyDetails(fullyQualifiedTypeName),
            TypeNameAssemblyFormatHandling.Full => fullyQualifiedTypeName,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    static string GetFullyQualifiedTypeName(this Type type, ISerializationBinder? binder)
    {
        if (binder == null)
        {
            return type.AssemblyQualifiedName!;
        }

        binder.BindToName(type, out var assemblyName, out var typeName);
        return typeName + (assemblyName == null ? "" : $", {assemblyName}");
    }

    static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
    {
        var builder = new StringBuilder();

        // loop through the type name and filter out qualified assembly details from nested type names
        var writingAssemblyName = false;
        var skippingAssemblyDetails = false;
        var followBrackets = false;
        foreach (var current in fullyQualifiedTypeName)
        {
            switch (current)
            {
                case '[':
                    writingAssemblyName = false;
                    skippingAssemblyDetails = false;
                    followBrackets = true;
                    builder.Append(current);
                    break;
                case ']':
                    writingAssemblyName = false;
                    skippingAssemblyDetails = false;
                    followBrackets = false;
                    builder.Append(current);
                    break;
                case ',':
                    if (followBrackets)
                    {
                        builder.Append(current);
                    }
                    else if (!writingAssemblyName)
                    {
                        writingAssemblyName = true;
                        builder.Append(current);
                    }
                    else
                    {
                        skippingAssemblyDetails = true;
                    }

                    break;
                default:
                    followBrackets = false;
                    if (!skippingAssemblyDetails)
                    {
                        builder.Append(current);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    public static bool HasDefaultConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.None | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] this Type type)
    {
        if (type.IsValueType)
        {
            return true;
        }

        return GetDefaultConstructor(type, false) != null;
    }

    public static ConstructorInfo? GetDefaultConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.None | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] this Type type,
        bool nonPublic)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
        if (nonPublic)
        {
            bindingFlags |= BindingFlags.NonPublic;
        }

        return type.GetConstructor(bindingFlags, null, [], null);
    }

    public static bool IsNullable(this Type type) =>
        !type.IsValueType ||
        IsNullableType(type);

    public static bool IsNullableType(this Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(Nullable<>);

    public static Type EnsureNotNullableType(this Type type)
    {
        if (IsNullableType(type))
        {
            return Nullable.GetUnderlyingType(type)!;
        }

        return type;
    }

    public static Type EnsureNotByRefType(this Type type)
    {
        if (type is {IsByRef: true, HasElementType: true})
        {
            return type.GetElementType()!;
        }

        return type;
    }

    public static bool IsGenericDefinition(this Type type, Type genericInterfaceDefinition) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == genericInterfaceDefinition;

    public static bool ImplementsGeneric(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type,
        Type genericInterfaceDefinition) =>
        type.ImplementsGeneric(genericInterfaceDefinition, out _);

    public static bool ImplementsGeneric(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type,
        Type genericInterfaceDefinition,
        [NotNullWhen(true)] out Type? implementingType)
    {
        if (!genericInterfaceDefinition.IsInterface ||
            !genericInterfaceDefinition.IsGenericTypeDefinition)
        {
            throw new ArgumentNullException($"'{genericInterfaceDefinition}' is not a generic interface definition.");
        }

        if (type is
            {
                IsInterface: true,
                IsGenericType: true
            })
        {
            var interfaceDefinition = type.GetGenericTypeDefinition();

            if (genericInterfaceDefinition == interfaceDefinition)
            {
                implementingType = type;
                return true;
            }
        }

        foreach (var i in type.GetInterfaces())
        {
            if (i.IsGenericType)
            {
                var interfaceDefinition = i.GetGenericTypeDefinition();

                if (genericInterfaceDefinition == interfaceDefinition)
                {
                    implementingType = i;
                    return true;
                }
            }
        }

        implementingType = null;
        return false;
    }

    public static bool InheritsGenericDefinition(this Type type, Type genericClassDefinition)
    {
        if (!genericClassDefinition.IsClass ||
            !genericClassDefinition.IsGenericTypeDefinition)
        {
            throw new ArgumentNullException($"'{genericClassDefinition}' is not a generic class definition.");
        }

        do
        {
            if (type.IsGenericType &&
                genericClassDefinition == type.GetGenericTypeDefinition())
            {
                return true;
            }

            var baseType = type.BaseType;
            if (baseType == null)
            {
                break;
            }
            type = baseType;
        } while (true);

        return false;
    }

    /// <summary>
    /// Gets the type of the typed collection's items.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The type of the typed collection's items.</returns>
    public static Type? GetCollectionItemType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.ImplementsGeneric(typeof(IEnumerable<>), out var genericListType))
        {
            if (genericListType.IsGenericTypeDefinition)
            {
                throw new($"Type {type} is not a collection.");
            }

            return genericListType.GetGenericArguments()[0];
        }

        if (type.IsAssignableTo<IEnumerable>())
        {
            return null;
        }

        throw new($"Type {type} is not a collection.");
    }

    public static void GetDictionaryKeyValueTypes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        this Type dictionaryType,
        out Type? keyType,
        out Type? valueType)
    {
        if (dictionaryType.ImplementsGeneric(typeof(IDictionary<,>), out var genericDictionaryType))
        {
            if (genericDictionaryType.IsGenericTypeDefinition)
            {
                throw new($"Type {dictionaryType} is not a dictionary.");
            }

            var dictionaryGenericArguments = genericDictionaryType.GetGenericArguments();

            keyType = dictionaryGenericArguments[0];
            valueType = dictionaryGenericArguments[1];
            return;
        }

        if (dictionaryType.IsAssignableTo<IDictionary>())
        {
            keyType = null;
            valueType = null;
            return;
        }

        throw new($"Type {dictionaryType} is not a dictionary.");
    }

    /// <summary>
    /// Gets the member's underlying type.
    /// </summary>
    /// <returns>The underlying type of the member.</returns>
    public static Type GetMemberUnderlyingType(this MemberInfo member) =>
        member.MemberType switch
        {
            MemberTypes.Field => ((FieldInfo) member).FieldType,
            MemberTypes.Property => ((PropertyInfo) member).PropertyType,
            // ReSharper disable once RedundantSuppressNullableWarningExpression
            MemberTypes.Event => ((EventInfo) member).EventHandlerType!,
            MemberTypes.Method => ((MethodInfo) member).ReturnType,
            _ => throw new ArgumentException("MemberInfo must be of type FieldInfo, PropertyInfo, EventInfo or MethodInfo", nameof(member))
        };

    public static bool IsByRefLikeType(this Type type)
    {
#if NET6_0_OR_GREATER
        return type.IsByRefLike;
#else
        if (!type.IsValueType)
        {
            return false;
        }
        var attributes = type.GetCustomAttributesData();
        return attributes.Any(t => string.Equals(t.AttributeType.FullName, "System.Runtime.CompilerServices.IsByRefLikeAttribute", StringComparison.Ordinal));
#endif
    }

    /// <summary>
    /// Determines whether the property is an indexed property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>
    /// <c>true</c> if the property is an indexed property; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsIndexedProperty(this PropertyInfo property) =>
        property.GetIndexParameters().Length > 0;

    /// <summary>
    /// Determines whether the specified MemberInfo can be read.
    /// </summary>
    /// <param name="member">The MemberInfo to determine whether can be read.</param>
    /// <param name="nonPublic">if set to <c>true</c> then allow the member to be gotten non-publicly.</param>
    /// <returns>
    /// <c>true</c> if the specified MemberInfo can be read; otherwise, <c>false</c>.
    /// </returns>
    public static bool CanReadMemberValue(this MemberInfo member, bool nonPublic)
    {
        if (member is PropertyInfo property)
        {
            if (!property.CanRead)
            {
                return false;
            }

            if (nonPublic)
            {
                return true;
            }

            return property.GetGetMethod(nonPublic) != null;
        }

        if (member is FieldInfo field)
        {
            return nonPublic || field.IsPublic;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified MemberInfo can be set.
    /// </summary>
    /// <param name="member">The MemberInfo to determine whether can be set.</param>
    /// <param name="nonPublic">if set to <c>true</c> then allow the member to be set non-publicly.</param>
    /// <param name="canSetReadOnly">if set to <c>true</c> then allow the member to be set if read-only.</param>
    /// <returns>
    /// <c>true</c> if the specified MemberInfo can be set; otherwise, <c>false</c>.
    /// </returns>
    public static bool CanSetMemberValue(this MemberInfo member, bool nonPublic, bool canSetReadOnly)
    {
        if (member is PropertyInfo property)
        {
            if (!property.CanWrite)
            {
                return false;
            }

            if (nonPublic)
            {
                return true;
            }

            return property.GetSetMethod(nonPublic) != null;
        }

        if (member is FieldInfo field)
        {
            if (field.IsLiteral)
            {
                return false;
            }

            if (field.IsInitOnly && !canSetReadOnly)
            {
                return false;
            }

            return nonPublic || field.IsPublic;
        }

        return false;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    public static List<MemberInfo> GetFieldsAndProperties(this Type type, BindingFlags bindingFlags) =>
        [
        ..GetFields(type, bindingFlags),
        ..GetProperties(type, bindingFlags)
        ];

    public static TypeNameKey SplitFullyQualifiedTypeName(string fullTypeName)
    {
        var assemblyDelimiterIndex = GetAssemblyDelimiterIndex(fullTypeName);

        if (assemblyDelimiterIndex == null)
        {
            return new(null, fullTypeName);
        }

        var delimiterIndex = assemblyDelimiterIndex.Value;
        var type = fullTypeName.Trim(0, delimiterIndex);
        var assembly = fullTypeName.Trim(delimiterIndex + 1, fullTypeName.Length - delimiterIndex - 1);
        return new(assembly, type);
    }

    static int? GetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
    {
        // we need to get the first comma following all surrounded in brackets because of generic types
        // e.g. System.Collections.Generic.Dictionary`2[[System.String, mscorlib,Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
        var scope = 0;
        for (var i = 0; i < fullyQualifiedTypeName.Length; i++)
        {
            var current = fullyQualifiedTypeName[i];
            switch (current)
            {
                case '[':
                    scope++;
                    break;
                case ']':
                    scope--;
                    break;
                case ',':
                    if (scope == 0)
                    {
                        return i;
                    }

                    break;
            }
        }

        return null;
    }

    public static MemberInfo? GetMemberInfoFromType(
        [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicEvents |
        DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicNestedTypes |
        DynamicallyAccessedMemberTypes.NonPublicProperties |
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicEvents |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.PublicNestedTypes |
        DynamicallyAccessedMemberTypes.PublicProperties)]
        Type targetType,
        MemberInfo member)
    {
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var memberType = member.MemberType;
        if (memberType == MemberTypes.Property)
        {
            var property = (PropertyInfo) member;

            var indexParameters = property.GetIndexParameters();
            var types = new Type[indexParameters.Length];
            for (var index = 0; index < indexParameters.Length; index++)
            {
                types[index] = indexParameters[index].ParameterType;
            }

            return targetType.GetProperty(property.Name, bindingFlags, null, property.PropertyType, types, null);
        }

        return targetType.GetMember(member.Name, memberType, bindingFlags).SingleOrDefault();
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    static List<FieldInfo> GetFields(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.None | DynamicallyAccessedMemberTypes.PublicFields)] Type targetType,
        BindingFlags bindingFlags)
    {
        var fields = new List<FieldInfo>(targetType.GetFields(bindingFlags));
        // Type.GetFields doesn't return inherited private fields
        // manually find private fields from base class
        GetChildPrivateFields(fields, targetType, bindingFlags);

        return fields;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    static void GetChildPrivateFields(List<FieldInfo> initialFields, Type targetType, BindingFlags bindingFlags)
    {
        // fix weirdness with private FieldInfos only being returned for the current Type
        // find base type fields and add them to result
        if ((bindingFlags & BindingFlags.NonPublic) == 0)
        {
            return;
        }

        // modify flags to not search for public fields
        var nonPublicBindingAttr = bindingFlags.RemoveFlag(BindingFlags.Public);

        while ((targetType = targetType.BaseType!) != null)
        {
            // filter out protected fields
            var childPrivateFields = targetType.GetFields(nonPublicBindingAttr)
                .Where(f => f.IsPrivate);

            initialFields.AddRange(childPrivateFields);
        }
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    static List<PropertyInfo> GetProperties(Type targetType, BindingFlags bindingFlags)
    {
        var properties = new List<PropertyInfo>(targetType.GetProperties(bindingFlags));

        // GetProperties on an interface doesn't return properties from its interfaces
        if (targetType.IsInterface)
        {
            foreach (var i in targetType.GetInterfaces())
            {
                properties.AddRange(i.GetProperties(bindingFlags));
            }
        }

        GetChildPrivateProperties(properties, targetType, bindingFlags);

        // a base class private getter/setter will be inaccessible unless the property was gotten from the base class
        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            if (property.DeclaringType != targetType)
            {
                properties[i] = (PropertyInfo) GetMemberInfoFromType(property.DeclaringType!, property)!;
            }
        }

        return properties;
    }

    static BindingFlags RemoveFlag(this BindingFlags source, BindingFlags flag) =>
        (source & flag) == flag
            ? source ^ flag
            : source;

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    static void GetChildPrivateProperties(List<PropertyInfo> initialProperties,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type targetType,
        BindingFlags bindingFlags)
    {
        // fix weirdness with private PropertyInfos only being returned for the current Type
        // find base type properties and add them to result

        // also find base properties that have been hidden by subtype properties with the same name

        while ((targetType = targetType.BaseType!) != null)
        {
            foreach (var property in targetType.GetProperties(bindingFlags))
            {
                if (property.IsVirtual())
                {
                    var subTypePropertyDeclaringType = property.GetBaseDefinition().DeclaringType ??
                                                       property.DeclaringType;

                    var index = initialProperties.FindIndex(
                        _ =>
                            _.Name == property.Name &&
                            _.IsVirtual() &&
                            (_.GetBaseDefinition().DeclaringType ?? _.DeclaringType!).IsAssignableFrom(subTypePropertyDeclaringType));

                    // don't add a virtual property that has an override
                    if (index == -1)
                    {
                        initialProperties.Add(property);
                    }

                    continue;
                }

                if (property.IsPublic())
                {
                    var publicIndex = initialProperties.FindIndex(
                        _ => _.Name == property.Name &&
                             _.DeclaringType == property.DeclaringType);

                    if (publicIndex == -1)
                    {
                        initialProperties.Add(property);
                    }

                    continue;
                }

                // have to test on name rather than reference because instances are different
                // depending on the type that GetProperties was called on
                var nonPublicIndex = initialProperties.FindIndex(_ => _.Name == property.Name);
                if (nonPublicIndex == -1)
                {
                    initialProperties.Add(property);
                    continue;
                }

                var childProperty = initialProperties[nonPublicIndex];
                // don't replace public child with private base
                if (!childProperty.IsPublic())
                {
                    // replace nonpublic properties for a child, but gotten from
                    // the parent with the one from the child
                    // the property gotten from the child will have access to private getter/setter
                    initialProperties[nonPublicIndex] = property;
                }
            }
        }
    }

    public static bool IsMethodOverridden(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type currentType,
        Type methodDeclaringType,
        string method)
    {
        foreach (var inner in currentType
                     .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (inner.Name == method &&
                // check that the method overrides the original on DynamicObjectProxy
                inner.DeclaringType != methodDeclaringType &&
                inner.GetBaseDefinition()
                    .DeclaringType == methodDeclaringType)
            {
                return true;
            }
        }

        return false;
    }

    public static object? GetDefaultValue(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        if (!type.IsValueType)
        {
            return null;
        }

        switch (ConvertUtils.GetTypeCode(type))
        {
            case PrimitiveTypeCode.Boolean:
                return false;
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.SByte:
            case PrimitiveTypeCode.Byte:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
                return 0;
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
                return 0L;
            case PrimitiveTypeCode.Single:
                return 0f;
            case PrimitiveTypeCode.Double:
                return 0.0;
            case PrimitiveTypeCode.Decimal:
                return 0m;
            case PrimitiveTypeCode.DateTime:
                return new DateTime();
            case PrimitiveTypeCode.BigInteger:
                return new BigInteger();
            case PrimitiveTypeCode.Guid:
                return new Guid();
            case PrimitiveTypeCode.DateTimeOffset:
                return new DateTimeOffset();
            case PrimitiveTypeCode.TimeSpan:
                return new TimeSpan();
        }

        if (IsNullable(type))
        {
            return null;
        }

        // possibly use IL initobj for perf here?
        return Activator.CreateInstance(type);
    }
}