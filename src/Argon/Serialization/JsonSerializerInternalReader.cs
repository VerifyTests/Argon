// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.


// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable RedundantSuppressNullableWarningExpression

class JsonSerializerInternalReader(JsonSerializer serializer) :
    JsonSerializerInternalBase(serializer)
{
    enum PropertyPresence
    {
        None = 0,
        Null = 1,
        Value = 2
    }

    JsonContract? GetContractSafe(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return GetContract(type);
    }

    JsonContract GetContract(Type type) =>
        Serializer.ResolveContract(type);

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public object? Deserialize(JsonReader reader, Type? type, bool? checkAdditionalContent)
    {
        var contract = GetContractSafe(type);

        try
        {
            var converter = GetConverter(contract, null, null, null);

            if (reader.TokenType == JsonToken.None && !reader.ReadForType(contract, converter != null))
            {
                throw JsonSerializationException.Create(reader, "Expected the input to start with a valid JSON token.");
            }

            object? deserializedValue;

            if (converter is {CanRead: true})
            {
                deserializedValue = DeserializeConvertible(converter, reader, type!, null);
            }
            else
            {
                deserializedValue = CreateValueInternal(reader, type, contract, null, null, null, null);
            }

            if (checkAdditionalContent.GetValueOrDefault())
            {
                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.Comment)
                    {
                        throw JsonSerializationException.Create(reader, "Additional text found in JSON string after finishing deserializing object.");
                    }
                }
            }

            return deserializedValue;
        }
        catch (Exception exception)
        {
            if (IsDeserializeErrorHandled(null, null, reader.Path, exception))
            {
                HandleError(reader, false, 0);
                return null;
            }

            // clear context in case serializer is being used inside a converter
            // if the converter wraps the error then not clearing the context will cause this error:
            // "Current error context error is different to requested error."
            ClearDeserializeErrorContext();
            throw;
        }
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    JsonSerializerProxy GetInternalSerializer() =>
        InternalSerializer ??= new(this);

    static JToken? CreateJToken(JsonReader reader, JsonContract? contract)
    {
        if (contract != null)
        {
            if (contract.UnderlyingType == typeof(JRaw))
            {
                return JRaw.Create(reader);
            }

            if (reader.TokenType == JsonToken.Null &&
                !(contract.UnderlyingType == typeof(JValue) ||
                  contract.UnderlyingType == typeof(JToken)))
            {
                return null;
            }
        }

        using var writer = new JTokenWriter();
        writer.WriteToken(reader);
        var token = writer.Token;

        if (contract == null ||
            token == null ||
            contract.UnderlyingType.IsInstanceOfType(token))
        {
            return token;
        }

        throw JsonSerializationException.Create(reader, $"Deserialized JSON type '{token.GetType().FullName}' is not compatible with expected type '{contract.UnderlyingType.FullName}'.");
    }

    JToken CreateJObject(JsonReader reader)
    {
        // this is needed because we've already read inside the object, looking for metadata properties
        using var writer = new JTokenWriter();
        writer.WriteStartObject();

        do
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = reader.StringValue;
                if (!reader.ReadAndMoveToContent())
                {
                    break;
                }

                if (CheckPropertyName(reader, propertyName))
                {
                    continue;
                }

                writer.WritePropertyName(propertyName);
                writer.WriteToken(reader, true, false);
                continue;
            }

            if (reader.TokenType == JsonToken.Comment)
            {
                // eat
                continue;
            }

            writer.WriteEndObject();
            return writer.Token!;
        } while (reader.Read());

        throw JsonSerializationException.Create(reader, "Unexpected end when deserializing object.");
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    object? CreateValueInternal(JsonReader reader, Type? type, JsonContract? contract, JsonProperty? member, JsonContainerContract? containerContract, JsonProperty? containerMember, object? existingValue)
    {
        if (contract is {ContractType: JsonContractType.Linq})
        {
            return CreateJToken(reader, contract);
        }

        do
        {
            switch (reader.TokenType)
            {
                // populate a typed object or generic dictionary/array
                // depending upon whether an type was supplied
                case JsonToken.StartObject:
                    return CreateObject(reader, type, contract, member, containerContract, containerMember, existingValue);
                case JsonToken.StartArray:
                    return CreateList(reader, type, contract, member, existingValue, null);
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.Boolean:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    return EnsureType(reader, reader.Value, InvariantCulture, contract, type);
                case JsonToken.String:
                    var s = reader.StringValue;

                    // string that needs to be returned as a byte array should be base 64 decoded
                    if (type == typeof(byte[]))
                    {
                        return Convert.FromBase64String(s);
                    }

                    // convert empty string to null automatically for nullable types
                    if (CoerceEmptyStringToNull(type, contract, s))
                    {
                        return null;
                    }

                    return EnsureType(reader, s, InvariantCulture, contract, type);
                case JsonToken.Null:
                case JsonToken.Undefined:
                    if (type == typeof(DBNull))
                    {
                        return DBNull.Value;
                    }

                    return EnsureType(reader, reader.Value, InvariantCulture, contract, type);
                case JsonToken.Raw:
                    return new JRaw((string?) reader.Value);
                case JsonToken.Comment:
                    // ignore
                    break;
                default:
                    throw JsonSerializationException.Create(reader, $"Unexpected token while deserializing object: {reader.TokenType}");
            }
        } while (reader.Read());

        throw JsonSerializationException.Create(reader, "Unexpected end when deserializing object.");
    }

    static bool CoerceEmptyStringToNull(Type? type, JsonContract? contract, string s) =>
        s.Length == 0 &&
        type != null &&
        type != typeof(string) &&
        type != typeof(object) &&
        contract is {IsNullable: true};

    static string GetExpectedDescription(JsonContract contract)
    {
        switch (contract.ContractType)
        {
            case JsonContractType.Object:
            case JsonContractType.Dictionary:
            case JsonContractType.Dynamic:
                return """JSON object (e.g. {"name":"value"})""";
            case JsonContractType.Array:
                return "JSON array (e.g. [1,2,3])";
            case JsonContractType.Primitive:
                return "JSON primitive value (e.g. string, number, boolean, null)";
            case JsonContractType.String:
                return "JSON string value";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    JsonConverter? GetConverter(JsonContract? contract, JsonConverter? memberConverter, JsonContainerContract? containerContract, JsonProperty? containerProperty)
    {
        if (memberConverter != null)
        {
            // member attribute converter
            return memberConverter;
        }

        if (containerProperty?.ItemConverter != null)
        {
            return containerProperty.ItemConverter;
        }

        if (containerContract?.ItemConverter != null)
        {
            return containerContract.ItemConverter;
        }

        if (contract != null)
        {
            if (contract.Converter != null)
            {
                // class attribute converter
                return contract.Converter;
            }

            if (Serializer.GetMatchingConverter(contract.UnderlyingType) is { } matchingConverter)
            {
                // passed in converters
                return matchingConverter;
            }

            if (contract.InternalConverter != null)
            {
                // internally specified converter
                return contract.InternalConverter;
            }
        }

        return null;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    object? CreateObject(JsonReader reader, Type? type, JsonContract? contract, JsonProperty? member, JsonContainerContract? containerContract, JsonProperty? containerMember, object? existingValue)
    {
        string? id;
        var resolvedObjectType = type;

        if (Serializer.MetadataPropertyHandling == MetadataPropertyHandling.Ignore)
        {
            // don't look for metadata properties
            reader.ReadAndAssert();
            id = null;
        }
        else if (Serializer.MetadataPropertyHandling == MetadataPropertyHandling.ReadAhead)
        {
            if (reader is not JTokenReader tokenReader)
            {
                var token = JToken.ReadFrom(reader);
                tokenReader = (JTokenReader) token.CreateReader();
                tokenReader.FloatParseHandling = reader.FloatParseHandling;
                tokenReader.SupportMultipleContent = reader.SupportMultipleContent;

                // start
                tokenReader.ReadAndAssert();

                reader = tokenReader;
            }

            if (ReadMetadataPropertiesToken(tokenReader, ref resolvedObjectType, ref contract, member, containerContract, containerMember, existingValue, out var newValue, out id))
            {
                return newValue;
            }
        }
        else
        {
            reader.ReadAndAssert();
            if (ReadMetadataProperties(reader, ref resolvedObjectType, ref contract, member, containerContract, containerMember, existingValue, out var newValue, out id))
            {
                return newValue;
            }
        }

        if (HasNoDefinedType(contract))
        {
            return CreateJObject(reader);
        }

        MiscellaneousUtils.Assert(resolvedObjectType != null);
        MiscellaneousUtils.Assert(contract != null);

        switch (contract.ContractType)
        {
            case JsonContractType.Object:
            {
                var createdFromNonDefaultCreator = false;
                var objectContract = (JsonObjectContract) contract;
                object targetObject;
                // check that if type name handling is being used that the existing value is compatible with the specified type
                if (existingValue != null &&
                    (resolvedObjectType == type ||
                     resolvedObjectType.IsInstanceOfType(existingValue)))
                {
                    targetObject = existingValue;
                }
                else
                {
                    targetObject = CreateNewObject(reader, objectContract, member, id, out createdFromNonDefaultCreator);
                }

                // don't populate if read from non-default creator because the object has already been read
                if (createdFromNonDefaultCreator)
                {
                    return targetObject;
                }

                return PopulateObject(targetObject, reader, objectContract, member, id);
            }
            case JsonContractType.Primitive:
            {
                var primitiveContract = (JsonPrimitiveContract) contract;
                // if the content is inside $value then read past it
                if (Serializer.MetadataPropertyHandling != MetadataPropertyHandling.Ignore
                    && reader.TokenType == JsonToken.PropertyName
                    && string.Equals((string) reader.GetValue(), JsonTypeReflector.ValuePropertyName, StringComparison.Ordinal))
                {
                    reader.ReadAndAssert();

                    // the token should not be an object because the $type value could have been included in the object
                    // without needing the $value property
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        throw JsonSerializationException.Create(reader, $"Unexpected token when deserializing primitive value: {reader.TokenType}");
                    }

                    var value = CreateValueInternal(reader, resolvedObjectType, primitiveContract, member, null, null, existingValue);

                    reader.ReadAndAssert();
                    return value;
                }

                break;
            }
            case JsonContractType.Dictionary:
            {
                var dictionaryContract = (JsonDictionaryContract) contract;

                if (existingValue == null)
                {
                    var dictionary = CreateNewDictionary(reader, dictionaryContract, out var createdFromNonDefaultCreator);

                    if (createdFromNonDefaultCreator)
                    {
                        if (id != null)
                        {
                            throw JsonSerializationException.Create(reader, $"Cannot preserve reference to readonly dictionary, or dictionary created from a non-default constructor: {contract.UnderlyingType}.");
                        }

                        if (!dictionaryContract.HasParameterizedCreatorInternal)
                        {
                            throw JsonSerializationException.Create(reader, $"Cannot deserialize readonly or fixed size dictionary: {contract.UnderlyingType}.");
                        }
                    }

                    PopulateDictionary(dictionary, reader, dictionaryContract, member, id);

                    if (createdFromNonDefaultCreator)
                    {
                        var creator = (dictionaryContract.OverrideCreator ?? dictionaryContract.ParameterizedCreator)!;
                        return creator(dictionary);
                    }

                    if (dictionary is IWrappedDictionary wrappedDictionary)
                    {
                        return wrappedDictionary.UnderlyingDictionary;
                    }

                    return dictionary;
                }
                else
                {
                    IDictionary dictionary;
                    if (dictionaryContract.ShouldCreateWrapper ||
                        existingValue is not IDictionary value)
                    {
                        dictionary = dictionaryContract.CreateWrapper(existingValue);
                    }
                    else
                    {
                        dictionary = value;
                    }

                    return PopulateDictionary(dictionary, reader, dictionaryContract, member, id);
                }
            }
            case JsonContractType.Dynamic:
                var dynamicContract = (JsonDynamicContract) contract;
                return CreateDynamic(reader, dynamicContract, member, id);
        }

        var message = $$$"""Cannot deserialize the current JSON object (e.g. {{"name":"value"}}) into type '{0}' because the type requires a {1} to deserialize correctly.{{{Environment.NewLine}}}To fix this error either change the JSON to a {1} or change the deserialized type so that it is a normal .NET type (e.g. not a primitive type like integer, not a collection type like an array or List<T>) that can be deserialized from a JSON object. JsonObjectAttribute can also be added to the type to force it to deserialize from a JSON object.{{{Environment.NewLine}}}""";
        message = string.Format(message, resolvedObjectType, GetExpectedDescription(contract));

        throw JsonSerializationException.Create(reader, message);
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    bool ReadMetadataPropertiesToken(JTokenReader reader, ref Type? type, ref JsonContract? contract, JsonProperty? member, JsonContainerContract? containerContract, JsonProperty? containerMember, object? existingValue, out object? newValue, out string? id)
    {
        id = null;
        newValue = null;

        if (reader.TokenType == JsonToken.StartObject)
        {
            var current = (JObject) reader.CurrentToken!;

            var refProperty = current.PropertyOrNull(JsonTypeReflector.RefPropertyName);
            if (refProperty != null)
            {
                var refToken = refProperty.Value;
                if (refToken.Type != JTokenType.String &&
                    refToken.Type != JTokenType.Null)
                {
                    throw JsonSerializationException.Create(refToken, refToken.Path, $"JSON reference {JsonTypeReflector.RefPropertyName} property must have a string or null value.", null);
                }

                var reference = (string?) refProperty;

                if (reference != null)
                {
                    var additionalContent = refProperty.Next ?? refProperty.Previous;
                    if (additionalContent != null)
                    {
                        throw JsonSerializationException.Create(additionalContent, additionalContent.Path, $"Additional content found in JSON reference object. A JSON reference object should only have a {JsonTypeReflector.RefPropertyName} property.", null);
                    }

                    newValue = Serializer.GetReferenceResolver().ResolveReference(this, reference);

                    reader.Skip();
                    return true;
                }
            }

            var typeToken = current[JsonTypeReflector.TypePropertyName];
            if (typeToken != null)
            {
                var qualifiedTypeName = (string?) typeToken;
                var typeTokenReader = typeToken.CreateReader();
                typeTokenReader.ReadAndAssert();
                ResolveTypeName(typeTokenReader, ref type, ref contract, member, containerContract, containerMember, qualifiedTypeName!);

                var valueToken = current[JsonTypeReflector.ValuePropertyName];
                if (valueToken != null)
                {
                    while (true)
                    {
                        reader.ReadAndAssert();
                        if (reader is
                            {
                                TokenType: JsonToken.PropertyName,
                                StringValue: JsonTypeReflector.ValuePropertyName
                            })
                        {
                            return false;
                        }

                        reader.ReadAndAssert();
                        reader.Skip();
                    }
                }
            }

            var idToken = current[JsonTypeReflector.IdPropertyName];
            if (idToken != null)
            {
                id = (string?) idToken;
            }

            var valuesToken = current[JsonTypeReflector.ArrayValuesPropertyName];
            if (valuesToken != null)
            {
                var listReader = valuesToken.CreateReader();
                listReader.ReadAndAssert();
                newValue = CreateList(listReader, type, contract, member, existingValue, id);

                reader.Skip();
                return true;
            }
        }

        reader.ReadAndAssert();
        return false;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    bool ReadMetadataProperties(JsonReader reader, ref Type? type, ref JsonContract? contract, JsonProperty? member, JsonContainerContract? containerContract, JsonProperty? containerMember, object? existingValue, out object? newValue, out string? id)
    {
        id = null;
        newValue = null;

        if (reader.TokenType == JsonToken.PropertyName)
        {
            var propertyName = (string)reader.Value!;

            if (propertyName.Length > 0 &&
                propertyName[0] == '$')
            {
                // read metadata properties
                // $type, $id, $ref, etc
                bool metadataProperty;

                do
                {
                    propertyName = (string) reader.GetValue();

                    if (string.Equals(propertyName, JsonTypeReflector.RefPropertyName, StringComparison.Ordinal))
                    {
                        reader.ReadAndAssert();
                        if (reader.TokenType != JsonToken.String &&
                            reader.TokenType != JsonToken.Null)
                        {
                            throw JsonSerializationException.Create(reader, $"JSON reference {JsonTypeReflector.RefPropertyName} property must have a string or null value.");
                        }

                        var reference = reader.Value?.ToString();

                        reader.ReadAndAssert();

                        if (reference != null)
                        {
                            if (reader.TokenType == JsonToken.PropertyName)
                            {
                                throw JsonSerializationException.Create(reader, $"Additional content found in JSON reference object. A JSON reference object should only have a {JsonTypeReflector.RefPropertyName} property.");
                            }

                            newValue = Serializer.GetReferenceResolver().ResolveReference(this, reference);

                            return true;
                        }

                        metadataProperty = true;
                    }
                    else if (string.Equals(propertyName, JsonTypeReflector.TypePropertyName, StringComparison.Ordinal))
                    {
                        reader.ReadAndAssert();
                        var qualifiedTypeName = (string) reader.GetValue();

                        ResolveTypeName(reader, ref type, ref contract, member, containerContract, containerMember, qualifiedTypeName);

                        reader.ReadAndAssert();

                        metadataProperty = true;
                    }
                    else if (string.Equals(propertyName, JsonTypeReflector.IdPropertyName, StringComparison.Ordinal))
                    {
                        reader.ReadAndAssert();

                        id = reader.Value?.ToString();

                        reader.ReadAndAssert();
                        metadataProperty = true;
                    }
                    else if (string.Equals(propertyName, JsonTypeReflector.ArrayValuesPropertyName, StringComparison.Ordinal))
                    {
                        reader.ReadAndAssert();
                        var list = CreateList(reader, type, contract, member, existingValue, id);
                        reader.ReadAndAssert();
                        newValue = list;
                        return true;
                    }
                    else
                    {
                        metadataProperty = false;
                    }
                } while (metadataProperty &&
                         reader.TokenType == JsonToken.PropertyName);
            }
        }

        return false;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void ResolveTypeName(JsonReader reader, ref Type? type, ref JsonContract? contract, JsonProperty? member, JsonContainerContract? containerContract, JsonProperty? containerMember, string qualifiedTypeName)
    {
        var resolvedTypeNameHandling =
            member?.TypeNameHandling ??
            containerContract?.ItemTypeNameHandling ??
            containerMember?.ItemTypeNameHandling ??
            Serializer.TypeNameHandling ??
            TypeNameHandling.None;

        if (resolvedTypeNameHandling != TypeNameHandling.None)
        {
            var typeNameKey = ReflectionUtils.SplitFullyQualifiedTypeName(qualifiedTypeName);

            var binder = Serializer.SerializationBinder ?? DefaultSerializationBinder.Instance;
            Type specifiedType;
            try
            {
                specifiedType = binder.BindToType(typeNameKey.Assembly, typeNameKey.Type);
            }
            catch (Exception exception)
            {
                throw JsonSerializationException.Create(reader, $"Error resolving type specified in JSON '{qualifiedTypeName}'.", exception);
            }

            if (specifiedType == null)
            {
                throw JsonSerializationException.Create(reader, $"Type specified in JSON '{qualifiedTypeName}' was not resolved.");
            }

            if (type != null &&
                type != typeof(IDynamicMetaObjectProvider) &&
                !type.IsAssignableFrom(specifiedType))
            {
                throw JsonSerializationException.Create(reader, $"Type specified in JSON '{specifiedType.AssemblyQualifiedName}' is not compatible with '{type.AssemblyQualifiedName}'.");
            }

            type = specifiedType;
            contract = GetContract(specifiedType);
        }
    }

    static JsonArrayContract EnsureArrayContract(JsonReader reader, Type type, JsonContract contract)
    {
        if (contract is not JsonArrayContract arrayContract)
        {
            var message = $"Cannot deserialize the current JSON array (e.g. [1,2,3]) into type '{{0}}' because the type requires a {{1}} to deserialize correctly.{Environment.NewLine}To fix this error either change the JSON to a {{1}} or change the deserialized type to an array or a type that implements a collection interface (e.g. ICollection, IList) like List<T> that can be deserialized from a JSON array. JsonArrayAttribute can also be added to the type to force it to deserialize from a JSON array.{Environment.NewLine}";
            message = string.Format(message, type, GetExpectedDescription(contract));

            throw JsonSerializationException.Create(reader, message);
        }

        return arrayContract;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    object? CreateList(JsonReader reader, Type? type, JsonContract? contract, JsonProperty? member, object? existingValue, string? id)
    {
        if (HasNoDefinedType(contract))
        {
            return CreateJToken(reader, contract);
        }

        MiscellaneousUtils.Assert(type != null);
        MiscellaneousUtils.Assert(contract != null);

        var arrayContract = EnsureArrayContract(reader, type, contract);

        if (existingValue == null)
        {
            var list = CreateNewList(reader, arrayContract, out var createdFromNonDefaultCreator);

            if (createdFromNonDefaultCreator)
            {
                if (id != null)
                {
                    throw JsonSerializationException.Create(reader, $"Cannot preserve reference to array or readonly list, or list created from a non-default constructor: {contract.UnderlyingType}.");
                }

                if (arrayContract is {HasParameterizedCreatorInternal: false, IsArray: false})
                {
                    throw JsonSerializationException.Create(reader, $"Cannot deserialize readonly or fixed size list: {contract.UnderlyingType}.");
                }
            }

            if (arrayContract.IsMultidimensionalArray)
            {
                PopulateMultidimensionalArray(list, reader, arrayContract, member, id);
            }
            else
            {
                PopulateList(list, reader, arrayContract, member, id);
            }

            if (createdFromNonDefaultCreator)
            {
                if (arrayContract.IsMultidimensionalArray)
                {
                    list = CollectionUtils.ToMultidimensionalArray(list, arrayContract.CollectionItemType!, contract.CreatedType.GetArrayRank());
                }
                else if (arrayContract.IsArray)
                {
                    var a = Array.CreateInstance(arrayContract.CollectionItemType!, list.Count);
                    list.CopyTo(a, 0);
                    list = a;
                }
                else
                {
                    var creator = (arrayContract.OverrideCreator ?? arrayContract.ParameterizedCreator)!;

                    return creator(list);
                }
            }
            else if (list is IWrappedCollection wrappedCollection)
            {
                return wrappedCollection.UnderlyingCollection;
            }

            return list;
        }

        if (arrayContract.CanDeserialize)
        {
            return PopulateList(!arrayContract.ShouldCreateWrapper && existingValue is IList list ? list : arrayContract.CreateWrapper(existingValue), reader, arrayContract, member, id);
        }

        throw JsonSerializationException.Create(reader, $"Cannot populate list type {contract.CreatedType}.");
    }

    static bool HasNoDefinedType(JsonContract? contract) =>
        contract == null ||
        contract.UnderlyingType == typeof(object) ||
        contract.ContractType == JsonContractType.Linq ||
        contract.UnderlyingType == typeof(IDynamicMetaObjectProvider);

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    static object? EnsureType(JsonReader reader, object? value, CultureInfo culture, JsonContract? contract, Type? targetType)
    {
        if (targetType == null)
        {
            return value;
        }

        MiscellaneousUtils.Assert(contract != null);
        var valueType = value?.GetType();

        // type of value and type of target don't match
        // attempt to convert value's type to target's type
        if (valueType != targetType)
        {
            if (value == null && contract.IsNullable)
            {
                return null;
            }

            try
            {
                if (contract.IsConvertible)
                {
                    var primitiveContract = (JsonPrimitiveContract) contract;

                    if (contract.IsEnum)
                    {
                        if (value is string s)
                        {
                            return EnumUtils.ParseEnum(
                                contract.NonNullableUnderlyingType,
                                null,
                                s,
                                false);
                        }

                        if (ConvertUtils.IsInteger(primitiveContract.TypeCode))
                        {
                            return Enum.ToObject(contract.NonNullableUnderlyingType, value!);
                        }
                    }
                    else if (contract.NonNullableUnderlyingType == typeof(DateTime))
                    {
                        // use DateTimeUtils because Convert.ChangeType does not set DateTime.Kind correctly
                        if (value is string s && DateTimeUtils.TryParseDateTime(s, out var dt))
                        {
                            return dt;
                        }
                    }

                    if (value is BigInteger integer)
                    {
                        return ConvertUtils.FromBigInteger(integer, contract.NonNullableUnderlyingType);
                    }

                    // this won't work when converting to a custom IConvertible
                    return Convert.ChangeType(value, contract.NonNullableUnderlyingType, culture);
                }

                return ConvertUtils.ConvertOrCast(value, contract.NonNullableUnderlyingType);
            }
            catch (Exception exception)
            {
                throw JsonSerializationException.Create(reader, $"Error converting value {MiscellaneousUtils.ToString(value)} to type '{targetType}'.", exception);
            }
        }

        return value;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    bool SetPropertyValue(JsonProperty property, JsonConverter? propertyConverter, JsonContainerContract? containerContract, JsonProperty? containerProperty, JsonReader reader, object target)
    {
        var skipSettingProperty = CalculatePropertyDetails(
            property,
            ref propertyConverter,
            containerContract,
            containerProperty,
            reader,
            target,
            out var useExistingValue,
            out var currentValue,
            out var propertyContract,
            out var gottenCurrentValue,
            out var ignoredValue);

        if (skipSettingProperty)
        {
            return ignoredValue;
        }

        object? value;

        if (propertyConverter is {CanRead: true})
        {
            if (!gottenCurrentValue && property.Readable)
            {
                currentValue = property.ValueProvider!.GetValue(target);
            }

            value = DeserializeConvertible(propertyConverter, reader, property.PropertyType!, currentValue);
        }
        else
        {
            value = CreateValueInternal(reader, property.PropertyType, propertyContract, property, containerContract, containerProperty, useExistingValue ? currentValue : null);
        }

        // the value wasn't set be JSON was populated onto the existing value
        if ((useExistingValue && value == currentValue) ||
            !ShouldSetPropertyValue(property, containerContract as JsonObjectContract, value))
        {
            return useExistingValue;
        }

        // always set the value if useExistingValue is false,
        // otherwise also set it if CreateValue returns a new value compared to the currentValue
        // this could happen because of a JsonConverter against the type
        property.ValueProvider!.SetValue(target, value);

        return true;
    }

    bool CalculatePropertyDetails(
        JsonProperty property,
        ref JsonConverter? propertyConverter,
        JsonContainerContract? containerContract,
        JsonProperty? containerProperty,
        JsonReader reader,
        object target,
        out bool useExistingValue,
        out object? currentValue,
        out JsonContract? propertyContract,
        out bool gottenCurrentValue,
        out bool ignoredValue)
    {
        currentValue = null;
        useExistingValue = false;
        propertyContract = null;
        gottenCurrentValue = false;
        ignoredValue = false;

        if (property.Ignored)
        {
            return true;
        }

        var tokenType = reader.TokenType;

        property.PropertyContract ??= GetContractSafe(property.PropertyType);

        var objectCreationHandling =
            property.ObjectCreationHandling.GetValueOrDefault(Serializer.ObjectCreationHandling);

        if (objectCreationHandling != ObjectCreationHandling.Replace &&
            (tokenType is
                 JsonToken.StartArray or
                 JsonToken.StartObject ||
             propertyConverter != null) &&
            property.Readable &&
            property.PropertyContract?.ContractType != JsonContractType.Linq)
        {
            currentValue = property.ValueProvider!.GetValue(target);
            gottenCurrentValue = true;

            if (currentValue != null)
            {
                propertyContract = GetContract(currentValue.GetType());

                useExistingValue = propertyContract is
                {
                    IsReadOnlyOrFixedSize: false,
                    UnderlyingType.IsValueType: false
                };
            }
        }

        if (!property.Writable && !useExistingValue)
        {
            return true;
        }

        // test tokenType here because null might not be convertible to some types, e.g. ignoring null when applied to DateTime
        if (tokenType == JsonToken.Null &&
            ResolvedNullValueHandling(containerContract as JsonObjectContract, property) == NullValueHandling.Ignore)
        {
            ignoredValue = true;
            return true;
        }

        // test tokenType here because default value might not be convertible to actual type, e.g. default of "" for DateTime
        var handling = property.DefaultValueHandling.GetValueOrDefault(Serializer.DefaultValueHandling);
        if (HasFlag(handling, DefaultValueHandling.Ignore) &&
            !HasFlag(handling, DefaultValueHandling.Populate) &&
            JsonTokenUtils.IsPrimitiveToken(tokenType) &&
            MiscellaneousUtils.ValueEquals(reader.Value, property.GetResolvedDefaultValue()))
        {
            ignoredValue = true;
            return true;
        }

        if (currentValue == null)
        {
            propertyContract = property.PropertyContract;
        }
        else
        {
            propertyContract = GetContract(currentValue.GetType());

            if (propertyContract != property.PropertyContract)
            {
                propertyConverter = GetConverter(propertyContract, property.Converter, containerContract, containerProperty);
            }
        }

        return false;
    }

    void AddReference(JsonReader reader, string id, object value)
    {
        try
        {
            Serializer.GetReferenceResolver().AddReference(this, id, value);
        }
        catch (Exception exception)
        {
            throw JsonSerializationException.Create(reader, $"Error reading object reference '{id}'.", exception);
        }
    }

    bool ShouldSetPropertyValue(JsonProperty property, JsonObjectContract? contract, object? value)
    {
        if (value == null &&
            ResolvedNullValueHandling(contract, property) == NullValueHandling.Ignore)
        {
            return false;
        }

        var handling = property.DefaultValueHandling.GetValueOrDefault(Serializer.DefaultValueHandling);
        if (HasFlag(handling, DefaultValueHandling.Ignore) &&
            !HasFlag(handling, DefaultValueHandling.Populate) &&
            MiscellaneousUtils.ValueEquals(value, property.GetResolvedDefaultValue()))
        {
            return false;
        }

        return property.Writable;
    }

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    IList CreateNewList(JsonReader reader, JsonArrayContract contract, out bool createdFromNonDefaultCreator)
    {
        // some types like non-generic IEnumerable can be serialized but not deserialized
        if (!contract.CanDeserialize)
        {
            throw JsonSerializationException.Create(reader, $"Cannot create and populate list type {contract.CreatedType}.");
        }

        if (contract.OverrideCreator != null)
        {
            if (contract.HasParameterizedCreator)
            {
                createdFromNonDefaultCreator = true;
                return contract.CreateTemporaryCollection();
            }

            var list = contract.OverrideCreator();

            if (contract.ShouldCreateWrapper)
            {
                list = contract.CreateWrapper(list);
            }

            createdFromNonDefaultCreator = false;
            return (IList) list;
        }

        if (contract.IsReadOnlyOrFixedSize)
        {
            createdFromNonDefaultCreator = true;
            var list = contract.CreateTemporaryCollection();

            if (contract.ShouldCreateWrapper)
            {
                list = contract.CreateWrapper(list);
            }

            return list;
        }

        if (contract.DefaultCreator != null &&
            (!contract.DefaultCreatorNonPublic ||
             Serializer.ConstructorHandling.GetValueOrDefault() == ConstructorHandling.AllowNonPublicDefaultConstructor))
        {
            var list = contract.DefaultCreator();

            if (contract.ShouldCreateWrapper)
            {
                list = contract.CreateWrapper(list);
            }

            createdFromNonDefaultCreator = false;
            return (IList) list;
        }

        if (contract.HasParameterizedCreatorInternal)
        {
            createdFromNonDefaultCreator = true;
            return contract.CreateTemporaryCollection();
        }

        if (contract.IsInstantiable)
        {
            throw JsonSerializationException.Create(reader, $"Unable to find a constructor to use for type {contract.UnderlyingType}.");
        }

        throw JsonSerializationException.Create(reader, $"Could not create an instance of type {contract.UnderlyingType}. Type is an interface or abstract class and cannot be instantiated.");
    }

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    IDictionary CreateNewDictionary(JsonReader reader, JsonDictionaryContract contract, out bool createdFromNonDefaultCreator)
    {
        if (contract.OverrideCreator != null)
        {
            if (contract.HasParameterizedCreator)
            {
                createdFromNonDefaultCreator = true;
                return contract.CreateTemporaryDictionary();
            }

            createdFromNonDefaultCreator = false;
            return (IDictionary) contract.OverrideCreator();
        }

        if (contract.IsReadOnlyOrFixedSize)
        {
            createdFromNonDefaultCreator = true;
            return contract.CreateTemporaryDictionary();
        }

        if (contract.DefaultCreator != null &&
            (!contract.DefaultCreatorNonPublic ||
             Serializer.ConstructorHandling.GetValueOrDefault() == ConstructorHandling.AllowNonPublicDefaultConstructor))
        {
            var dictionary = contract.DefaultCreator();

            if (contract.ShouldCreateWrapper)
            {
                dictionary = contract.CreateWrapper(dictionary);
            }

            createdFromNonDefaultCreator = false;
            return (IDictionary) dictionary;
        }

        if (contract.HasParameterizedCreatorInternal)
        {
            createdFromNonDefaultCreator = true;
            return contract.CreateTemporaryDictionary();
        }

        if (contract.IsInstantiable)
        {
            throw JsonSerializationException.Create(reader, $"Unable to find a default constructor to use for type {contract.UnderlyingType}.");
        }

        throw JsonSerializationException.Create(reader, $"Could not create an instance of type {contract.UnderlyingType}. Type is an interface or abstract class and cannot be instantiated.");
    }

    void OnDeserializing(JsonReader reader, object value) =>
        Serializer.Deserializing?.Invoke(reader, value);

    void OnDeserialized(JsonReader reader, object value) =>
        Serializer.Deserialized?.Invoke(reader, value);

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    object PopulateDictionary(IDictionary dictionary, JsonReader reader, JsonDictionaryContract contract, JsonProperty? containerProperty, string? id)
    {
        var underlyingDictionary = dictionary is IWrappedDictionary wrappedDictionary ? wrappedDictionary.UnderlyingDictionary : dictionary;

        if (id != null)
        {
            AddReference(reader, id, underlyingDictionary);
        }

        OnDeserializing(reader, underlyingDictionary);

        var initialDepth = reader.Depth;

        contract.KeyContract ??= GetContractSafe(contract.DictionaryKeyType);

        contract.ItemContract ??= GetContractSafe(contract.DictionaryValueType);

        var dictionaryValueConverter = contract.ItemConverter ?? GetConverter(contract.ItemContract, null, contract, containerProperty);
        var keyTypeCode = contract.KeyContract is JsonPrimitiveContract keyContract ? keyContract.TypeCode : PrimitiveTypeCode.Empty;

        var finished = false;
        do
        {
            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    var keyValue = reader.GetValue();
                    if (CheckPropertyName(reader, keyValue.ToString()!))
                    {
                        continue;
                    }

                    try
                    {
                        try
                        {
                            // this is for correctly reading ISO and MS formatted dictionary keys
                            switch (keyTypeCode)
                            {
                                case PrimitiveTypeCode.DateTime:
                                case PrimitiveTypeCode.DateTimeNullable:
                                {
                                    if (DateTimeUtils.TryParseDateTime(keyValue.ToString()!, out var dt))
                                    {
                                        keyValue = dt;
                                    }
                                    else
                                    {
                                        keyValue = EnsureType(reader, keyValue, InvariantCulture, contract.KeyContract, contract.DictionaryKeyType)!;
                                    }

                                    break;
                                }
                                case PrimitiveTypeCode.DateTimeOffset:
                                case PrimitiveTypeCode.DateTimeOffsetNullable:
                                {
                                    if (DateTimeUtils.TryParseDateTimeOffset(keyValue.ToString()!, out var dt))
                                    {
                                        keyValue = dt;
                                    }
                                    else
                                    {
                                        keyValue = EnsureType(reader, keyValue, InvariantCulture, contract.KeyContract, contract.DictionaryKeyType)!;
                                    }

                                    break;
                                }
                                default:
                                    if (contract.KeyContract is {IsEnum: true})
                                    {
                                        keyValue = EnumUtils.ParseEnum(contract.KeyContract.NonNullableUnderlyingType, (Serializer.ContractResolver as DefaultContractResolver)?.NamingStrategy, keyValue.ToString()!, false);
                                    }
                                    else
                                    {
                                        keyValue = EnsureType(reader, keyValue, InvariantCulture, contract.KeyContract, contract.DictionaryKeyType)!;
                                    }

                                    break;
                            }
                        }
                        catch (Exception exception)
                        {
                            throw JsonSerializationException.Create(reader, $"Could not convert string '{reader.Value}' to dictionary key type '{contract.DictionaryKeyType}'. Create a TypeConverter to convert from the string to the key type object.", exception);
                        }

                        if (!reader.ReadForType(contract.ItemContract, dictionaryValueConverter != null))
                        {
                            throw JsonSerializationException.Create(reader, "Unexpected end when deserializing object.");
                        }

                        object? itemValue;
                        if (dictionaryValueConverter is {CanRead: true})
                        {
                            itemValue = DeserializeConvertible(dictionaryValueConverter, reader, contract.DictionaryValueType!, null);
                        }
                        else
                        {
                            itemValue = CreateValueInternal(reader, contract.DictionaryValueType, contract.ItemContract, null, contract, containerProperty, null);
                        }

                        dictionary[keyValue] = itemValue;
                    }
                    catch (Exception exception)
                    {
                        if (IsDeserializeErrorHandled(underlyingDictionary, keyValue, reader.Path, exception))
                        {
                            HandleError(reader, true, initialDepth);
                        }
                        else
                        {
                            throw;
                        }
                    }

                    break;
                case JsonToken.Comment:
                    break;
                case JsonToken.EndObject:
                    finished = true;
                    break;
                default:
                    throw JsonSerializationException.Create(reader, $"Unexpected token when deserializing object: {reader.TokenType}");
            }
        } while (!finished && reader.Read());

        if (!finished)
        {
            ThrowUnexpectedEndException(reader, underlyingDictionary, "Unexpected end when deserializing object.");
        }

        OnDeserialized(reader, underlyingDictionary);
        return underlyingDictionary;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void PopulateMultidimensionalArray(IList list, JsonReader reader, JsonArrayContract contract, JsonProperty? containerProperty, string? id)
    {
        var rank = contract.UnderlyingType.GetArrayRank();

        if (id != null)
        {
            AddReference(reader, id, list);
        }

        OnDeserializing(reader, list);

        var collectionItemContract = GetContractSafe(contract.CollectionItemType);
        var collectionItemConverter = GetConverter(collectionItemContract, null, contract, containerProperty);

        int? previousErrorIndex = null;
        var listStack = new Stack<IList>();
        listStack.Push(list);
        var currentList = list;

        var finished = false;
        do
        {
            var initialDepth = reader.Depth;

            if (listStack.Count == rank)
            {
                try
                {
                    if (reader.ReadForType(collectionItemContract, collectionItemConverter != null))
                    {
                        switch (reader.TokenType)
                        {
                            case JsonToken.EndArray:
                                listStack.Pop();
                                currentList = listStack.Peek();
                                previousErrorIndex = null;
                                break;
                            case JsonToken.Comment:
                                break;
                            default:
                                object? value;

                                if (collectionItemConverter is {CanRead: true})
                                {
                                    value = DeserializeConvertible(collectionItemConverter, reader, contract.CollectionItemType!, null);
                                }
                                else
                                {
                                    value = CreateValueInternal(reader, contract.CollectionItemType, collectionItemContract, null, contract, containerProperty, null);
                                }

                                currentList.Add(value);
                                break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception exception)
                {
                    var errorPosition = reader.GetPosition(initialDepth);

                    if (IsDeserializeErrorHandled(list, errorPosition.Position, reader.Path, exception))
                    {
                        HandleError(reader, true, initialDepth + 1);

                        if (previousErrorIndex != null &&
                            previousErrorIndex == errorPosition.Position)
                        {
                            // reader index has not moved since previous error handling
                            // break out of reading array to prevent infinite loop
                            throw JsonSerializationException.Create(reader, "Infinite loop detected from error handling.", exception);
                        }

                        previousErrorIndex = errorPosition.Position;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                if (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.StartArray:
                            IList newList = new List<object>();
                            currentList.Add(newList);
                            listStack.Push(newList);
                            currentList = newList;
                            break;
                        case JsonToken.EndArray:
                            listStack.Pop();

                            if (listStack.Count > 0)
                            {
                                currentList = listStack.Peek();
                            }
                            else
                            {
                                finished = true;
                            }

                            break;
                        case JsonToken.Comment:
                            break;
                        default:
                            throw JsonSerializationException.Create(reader, $"Unexpected token when deserializing multidimensional array: {reader.TokenType}");
                    }
                }
                else
                {
                    break;
                }
            }
        } while (!finished);

        if (!finished)
        {
            ThrowUnexpectedEndException(reader, list, "Unexpected end when deserializing array.");
        }

        OnDeserialized(reader, list);
    }

    void ThrowUnexpectedEndException(JsonReader reader, object? currentObject, string message)
    {
        try
        {
            throw JsonSerializationException.Create(reader, message);
        }
        catch (Exception exception)
        {
            if (IsDeserializeErrorHandled(currentObject, null, reader.Path, exception))
            {
                HandleError(reader, false, 0);
            }
            else
            {
                throw;
            }
        }
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    object PopulateList(IList list, JsonReader reader, JsonArrayContract contract, JsonProperty? containerProperty, string? id)
    {
#pragma warning disable CS8600, CS8602, CS8603, CS8604
        var underlyingList = list is IWrappedCollection wrappedCollection ? wrappedCollection.UnderlyingCollection : list;

        if (id != null)
        {
            AddReference(reader, id, underlyingList);
        }

        // can't populate an existing array
        if (list.IsFixedSize)
        {
            reader.Skip();
            return underlyingList;
        }

        OnDeserializing(reader, underlyingList);

        var initialDepth = reader.Depth;

        contract.ItemContract ??= GetContractSafe(contract.CollectionItemType);

        var collectionItemConverter = GetConverter(contract.ItemContract, null, contract, containerProperty);

        int? previousErrorIndex = null;

        var finished = false;
        do
        {
            try
            {
                if (reader.ReadForType(contract.ItemContract, collectionItemConverter != null))
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.EndArray:
                            finished = true;
                            break;
                        case JsonToken.Comment:
                            break;
                        default:
                            object? value;

                            if (collectionItemConverter is {CanRead: true})
                            {
                                value = DeserializeConvertible(collectionItemConverter, reader, contract.CollectionItemType, null);
                            }
                            else
                            {
                                value = CreateValueInternal(reader, contract.CollectionItemType, contract.ItemContract, null, contract, containerProperty, null);
                            }

                            list.Add(value);
                            break;
                    }
                }
                else
                {
                    break;
                }
            }
            catch (Exception exception)
            {
                var errorPosition = reader.GetPosition(initialDepth);

                if (IsDeserializeErrorHandled(underlyingList, errorPosition.Position, reader.Path, exception))
                {
                    HandleError(reader, true, initialDepth + 1);

                    if (previousErrorIndex != null &&
                        previousErrorIndex == errorPosition.Position)
                    {
                        // reader index has not moved since previous error handling
                        // break out of reading array to prevent infinite loop
                        throw JsonSerializationException.Create(reader, "Infinite loop detected from error handling.", exception);
                    }

                    previousErrorIndex = errorPosition.Position;
                }
                else
                {
                    throw;
                }
            }
        } while (!finished);

        if (!finished)
        {
            ThrowUnexpectedEndException(reader, underlyingList, "Unexpected end when deserializing array.");
        }

        OnDeserialized(reader, underlyingList);
        return underlyingList;
#pragma warning restore CS8600, CS8602, CS8603, CS8604
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    object CreateDynamic(JsonReader reader, JsonDynamicContract contract, JsonProperty? member, string? id)
    {
        IDynamicMetaObjectProvider newObject;

        if (!contract.IsInstantiable)
        {
            throw JsonSerializationException.Create(reader, $"Could not create an instance of type {contract.UnderlyingType}. Type is an interface or abstract class and cannot be instantiated.");
        }

        if (contract.DefaultCreator != null &&
            (!contract.DefaultCreatorNonPublic ||
             Serializer.ConstructorHandling.GetValueOrDefault() == ConstructorHandling.AllowNonPublicDefaultConstructor))
        {
            newObject = (IDynamicMetaObjectProvider) contract.DefaultCreator();
        }
        else
        {
            throw JsonSerializationException.Create(reader, $"Unable to find a default constructor to use for type {contract.UnderlyingType}.");
        }

        if (id != null)
        {
            AddReference(reader, id, newObject);
        }

        OnDeserializing(reader, newObject);

        var initialDepth = reader.Depth;

        var finished = false;
        do
        {
            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    var memberName = (string) reader.GetValue();

                    try
                    {
                        if (!reader.Read())
                        {
                            throw JsonSerializationException.Create(reader, $"Unexpected end when setting {memberName}'s value.");
                        }

                        // first attempt to find a settable property, otherwise fall back to a dynamic set without type
                        var property = contract.Properties.GetClosestMatchProperty(memberName);

                        if (property is {Writable: true, Ignored: false})
                        {
                            property.PropertyContract ??= GetContractSafe(property.PropertyType);

                            var propertyConverter = GetConverter(property.PropertyContract, property.Converter, null, null);

                            if (!SetPropertyValue(property, propertyConverter, null, member, reader, newObject))
                            {
                                reader.Skip();
                            }
                        }
                        else
                        {
                            var t = JsonTokenUtils.IsPrimitiveToken(reader.TokenType) ? reader.ValueType! : typeof(IDynamicMetaObjectProvider);

                            var dynamicMemberContract = GetContractSafe(t);
                            var dynamicMemberConverter = GetConverter(dynamicMemberContract, null, null, member);

                            object? value;
                            if (dynamicMemberConverter is {CanRead: true})
                            {
                                value = DeserializeConvertible(dynamicMemberConverter, reader, t, null);
                            }
                            else
                            {
                                value = CreateValueInternal(reader, t, dynamicMemberContract, null, null, member, null);
                            }

                            JsonDynamicContract.TrySetMember(newObject, memberName, value);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (IsDeserializeErrorHandled(newObject, memberName, reader.Path, exception))
                        {
                            HandleError(reader, true, initialDepth);
                        }
                        else
                        {
                            throw;
                        }
                    }

                    break;
                case JsonToken.EndObject:
                    finished = true;
                    break;
                default:
                    throw JsonSerializationException.Create(reader, $"Unexpected token when deserializing object: {reader.TokenType}");
            }
        } while (!finished && reader.Read());

        if (!finished)
        {
            ThrowUnexpectedEndException(reader, newObject, "Unexpected end when deserializing object.");
        }

        OnDeserialized(reader, newObject);

        return newObject;
    }

    class CreatorPropertyContext
    {
        public JsonProperty? Property;
        public JsonProperty? ConstructorProperty;
        public PropertyPresence? Presence;
        public object? Value;
        public bool Used;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    object CreateObjectUsingCreatorWithParameters(JsonReader reader, JsonObjectContract contract, JsonProperty? containerProperty, ObjectConstructor creator, string? id)
    {
        // only need to keep a track of properties' presence if they are required or a value should be defaulted if missing
        var trackPresence = contract.HasRequiredOrDefaultValueProperties ||
                            HasFlag(Serializer.DefaultValueHandling, DefaultValueHandling.Populate);

        var propertyContexts = ResolvePropertyAndCreatorValues(contract, containerProperty, reader, contract.UnderlyingType);
        if (trackPresence)
        {
            foreach (var property in contract.Properties)
            {
                if (!property.Ignored)
                {
                    if (propertyContexts.All(_ => _.Property != property))
                    {
                        propertyContexts.Add(
                            new()
                            {
                                Property = property,
                                Presence = PropertyPresence.None
                            });
                    }
                }
            }
        }

        var creatorParameterValues = new object?[contract.CreatorParameters.Count];

        foreach (var context in propertyContexts)
        {
            // set presence of read values
            if (trackPresence)
            {
                if (context is {Property: not null, Presence: null})
                {
                    var v = context.Value;
                    PropertyPresence propertyPresence;
                    if (v == null)
                    {
                        propertyPresence = PropertyPresence.Null;
                    }
                    else if (v is string s)
                    {
                        propertyPresence = CoerceEmptyStringToNull(context.Property.PropertyType, context.Property.PropertyContract, s)
                            ? PropertyPresence.Null
                            : PropertyPresence.Value;
                    }
                    else
                    {
                        propertyPresence = PropertyPresence.Value;
                    }

                    context.Presence = propertyPresence;
                }
            }

            var constructorProperty = context.ConstructorProperty;
            if (constructorProperty == null && context.Property != null)
            {
                constructorProperty = contract.CreatorParameters.ForgivingCaseSensitiveFind(context.Property.UnderlyingName!);
            }

            if (constructorProperty is {Ignored: false})
            {
                // handle giving default values to creator parameters
                // this needs to happen before the call to creator
                if (trackPresence)
                {
                    if (context.Presence is PropertyPresence.None or PropertyPresence.Null)
                    {
                        constructorProperty.PropertyContract ??= GetContractSafe(constructorProperty.PropertyType);

                        if (HasFlag(constructorProperty.DefaultValueHandling.GetValueOrDefault(Serializer.DefaultValueHandling), DefaultValueHandling.Populate))
                        {
                            context.Value = EnsureType(
                                reader,
                                constructorProperty.GetResolvedDefaultValue(),
                                InvariantCulture,
                                constructorProperty.PropertyContract!,
                                constructorProperty.PropertyType);
                        }
                    }
                }

                var i = contract.CreatorParameters.IndexOf(constructorProperty);
                creatorParameterValues[i] = context.Value;

                context.Used = true;
            }
        }

        var createdObject = creator(creatorParameterValues);

        if (id != null)
        {
            AddReference(reader, id, createdObject);
        }

        OnDeserializing(reader, createdObject);

        // go through unused values and set the newly created object's properties
        foreach (var context in propertyContexts)
        {
            if (context.Used ||
                context.Property == null ||
                context.Property.Ignored ||
                context.Presence == PropertyPresence.None)
            {
                continue;
            }

            var property = context.Property;
            var value = context.Value;

            if (ShouldSetPropertyValue(property, contract, value))
            {
                property.ValueProvider!.SetValue(createdObject, value);
                context.Used = true;
            }
            else if (!property.Writable &&
                     value != null)
            {
                // handle readonly collection/dictionary properties
                var propertyContract = Serializer.ResolveContract(property.PropertyType!);

                if (propertyContract.ContractType == JsonContractType.Array)
                {
                    var propertyArrayContract = (JsonArrayContract) propertyContract;

                    if (propertyArrayContract is
                        {
                            CanDeserialize: true,
                            IsReadOnlyOrFixedSize: false
                        })
                    {
                        var createdObjectCollection = property.ValueProvider!.GetValue(createdObject);
                        if (createdObjectCollection != null)
                        {
                            propertyArrayContract = (JsonArrayContract) GetContract(createdObjectCollection.GetType());

                            var createdObjectCollectionWrapper = propertyArrayContract.ShouldCreateWrapper ? propertyArrayContract.CreateWrapper(createdObjectCollection) : (IList) createdObjectCollection;

                            // Don't attempt to populate array/read-only list
                            if (!createdObjectCollectionWrapper.IsFixedSize)
                            {
                                var newValues = propertyArrayContract.ShouldCreateWrapper ? propertyArrayContract.CreateWrapper(value) : (IList) value;

                                foreach (var newValue in newValues)
                                {
                                    createdObjectCollectionWrapper.Add(newValue);
                                }
                            }
                        }
                    }
                }
                else if (propertyContract.ContractType == JsonContractType.Dictionary)
                {
                    var dictionaryContract = (JsonDictionaryContract) propertyContract;

                    if (!dictionaryContract.IsReadOnlyOrFixedSize)
                    {
                        var createdObjectDictionary = property.ValueProvider!.GetValue(createdObject);
                        if (createdObjectDictionary != null)
                        {
                            var targetDictionary = dictionaryContract.ShouldCreateWrapper ? dictionaryContract.CreateWrapper(createdObjectDictionary) : (IDictionary) createdObjectDictionary;
                            var newValues = dictionaryContract.ShouldCreateWrapper ? dictionaryContract.CreateWrapper(value) : (IDictionary) value;

                            // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                            var e = newValues.GetEnumerator();
                            try
                            {
                                while (e.MoveNext())
                                {
                                    var entry = e.Entry;
                                    targetDictionary[entry.Key] = entry.Value;
                                }
                            }
                            finally
                            {
                                (e as IDisposable)?.Dispose();
                            }
                        }
                    }
                }

                context.Used = true;
            }
        }

        if (trackPresence)
        {
            foreach (var context in propertyContexts)
            {
                if (context.Property == null)
                {
                    continue;
                }

                EndProcessProperty(
                    createdObject,
                    reader,
                    contract,
                    reader.Depth,
                    context.Property,
                    context.Presence.GetValueOrDefault(),
                    !context.Used);
            }
        }

        OnDeserialized(reader, createdObject);
        return createdObject;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    object? DeserializeConvertible(JsonConverter converter, JsonReader reader, Type type, object? existingValue) =>
        converter.ReadJson(reader, type, existingValue, GetInternalSerializer());

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    List<CreatorPropertyContext> ResolvePropertyAndCreatorValues(JsonObjectContract contract, JsonProperty? containerProperty, JsonReader reader, Type type)
    {
        var propertyValues = new List<CreatorPropertyContext>();
        var exit = false;
        do
        {
            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    var memberName = (string) reader.GetValue();

                    var creatorPropertyContext = new CreatorPropertyContext
                    {
                        ConstructorProperty = contract.CreatorParameters.GetClosestMatchProperty(memberName),
                        Property = contract.Properties.GetClosestMatchProperty(memberName)
                    };
                    propertyValues.Add(creatorPropertyContext);

                    var property = creatorPropertyContext.ConstructorProperty ?? creatorPropertyContext.Property;
                    if (property != null)
                    {
                        if (!property.Ignored)
                        {
                            property.PropertyContract ??= GetContractSafe(property.PropertyType);

                            var propertyConverter = GetConverter(property.PropertyContract, property.Converter, contract, containerProperty);

                            if (!reader.ReadForType(property.PropertyContract, propertyConverter != null))
                            {
                                throw JsonSerializationException.Create(reader, $"Unexpected end when setting {memberName}'s value.");
                            }

                            if (propertyConverter is {CanRead: true})
                            {
                                creatorPropertyContext.Value = DeserializeConvertible(propertyConverter, reader, property.PropertyType!, null);
                            }
                            else
                            {
                                creatorPropertyContext.Value = CreateValueInternal(reader, property.PropertyType, property.PropertyContract, property, contract, containerProperty, null);
                            }

                            continue;
                        }

                        if (!reader.Read())
                        {
                            throw JsonSerializationException.Create(reader, $"Unexpected end when setting {memberName}'s value.");
                        }
                    }
                    else
                    {
                        if (!reader.Read())
                        {
                            throw JsonSerializationException.Create(reader, $"Unexpected end when setting {memberName}'s value.");
                        }

                        if ((contract.MissingMemberHandling ?? Serializer.MissingMemberHandling) == MissingMemberHandling.Error)
                        {
                            throw JsonSerializationException.Create(reader, $"Could not find member '{memberName}' on object of type '{type.Name}'");
                        }
                    }

                    reader.Skip();

                    break;
                case JsonToken.Comment:
                    break;
                case JsonToken.EndObject:
                    exit = true;
                    break;
                default:
                    throw JsonSerializationException.Create(reader, $"Unexpected token when deserializing object: {reader.TokenType}");
            }
        } while (!exit && reader.Read());

        if (!exit)
        {
            ThrowUnexpectedEndException(reader, null, "Unexpected end when deserializing object.");
        }

        return propertyValues;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public object CreateNewObject(JsonReader reader, JsonObjectContract objectContract, JsonProperty? containerMember, string? id, out bool createdFromNonDefaultCreator)
    {
        object? newObject = null;

        if (objectContract.OverrideCreator != null)
        {
            if (objectContract.CreatorParameters.Count > 0)
            {
                createdFromNonDefaultCreator = true;
                return CreateObjectUsingCreatorWithParameters(reader, objectContract, containerMember, objectContract.OverrideCreator, id);
            }

            newObject = objectContract.OverrideCreator();
        }
        else if (objectContract.DefaultCreator != null &&
                 (!objectContract.DefaultCreatorNonPublic ||
                  Serializer.ConstructorHandling.GetValueOrDefault() == ConstructorHandling.AllowNonPublicDefaultConstructor || objectContract.ParameterizedCreator == null))
        {
            // use the default constructor if it is...
            // public
            // non-public and the user has change constructor handling settings
            // non-public and there is no other creator
            newObject = objectContract.DefaultCreator();
        }
        else if (objectContract.ParameterizedCreator != null)
        {
            createdFromNonDefaultCreator = true;
            return CreateObjectUsingCreatorWithParameters(reader, objectContract, containerMember, objectContract.ParameterizedCreator, id);
        }

        if (newObject == null)
        {
            if (objectContract.IsInstantiable)
            {
                throw JsonSerializationException.Create(reader, $"Unable to find a constructor to use for type {objectContract.UnderlyingType}. A class should either have a default constructor, one constructor with arguments or a constructor marked with the JsonConstructor attribute.");
            }

            throw JsonSerializationException.Create(reader, $"Could not create an instance of type {objectContract.UnderlyingType}. Type is an interface or abstract class and cannot be instantiated.");
        }

        createdFromNonDefaultCreator = false;
        return newObject;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    object PopulateObject(object newObject, JsonReader reader, JsonObjectContract contract, JsonProperty? member, string? id)
    {
        OnDeserializing(reader, newObject);

        // only need to keep a track of properties' presence if they are required or a value should be defaulted if missing
        var propertiesPresence = contract.HasRequiredOrDefaultValueProperties ||
                                 HasFlag(Serializer.DefaultValueHandling, DefaultValueHandling.Populate)
            ? contract.Properties.ToDictionary(_ => _, _ => PropertyPresence.None)
            : null;

        if (id != null)
        {
            AddReference(reader, id, newObject);
        }

        var initialDepth = reader.Depth;

        var finished = false;
        do
        {
            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                {
                    var propertyName = (string) reader.GetValue();

                    if (CheckPropertyName(reader, propertyName))
                    {
                        continue;
                    }

                    try
                    {
                        // attempt exact case match first
                        // then try match ignoring case
                        var property = contract.Properties.GetClosestMatchProperty(propertyName);

                        if (property == null)
                        {
                            if ((contract.MissingMemberHandling ?? Serializer.MissingMemberHandling) == MissingMemberHandling.Error)
                            {
                                throw JsonSerializationException.Create(reader, $"Could not find member '{propertyName}' on object of type '{contract.UnderlyingType.Name}'");
                            }

                            if (!reader.Read())
                            {
                                break;
                            }

                            reader.Skip();
                            continue;
                        }

                        if (property.Ignored)
                        {
                            if (!reader.Read())
                            {
                                break;
                            }

                            SetPropertyPresence(reader, property, propertiesPresence);
                            reader.Skip();
                        }
                        else
                        {
                            property.PropertyContract ??= GetContractSafe(property.PropertyType);

                            var propertyConverter = GetConverter(property.PropertyContract, property.Converter, contract, member);

                            if (!reader.ReadForType(property.PropertyContract, propertyConverter != null))
                            {
                                throw JsonSerializationException.Create(reader, $"Unexpected end when setting {propertyName}'s value.");
                            }

                            SetPropertyPresence(reader, property, propertiesPresence);

                            // set extension data if property is ignored or readonly
                            if (!SetPropertyValue(property, propertyConverter, contract, member, reader, newObject))
                            {
                                reader.Skip();
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (IsDeserializeErrorHandled(newObject, propertyName, reader.Path, exception))
                        {
                            HandleError(reader, true, initialDepth);
                        }
                        else
                        {
                            throw;
                        }
                    }

                    break;
                }
                case JsonToken.EndObject:
                    finished = true;
                    break;
                case JsonToken.Comment:
                    // ignore
                    break;
                default:
                    throw JsonSerializationException.Create(reader, $"Unexpected token when deserializing object: {reader.TokenType}");
            }
        } while (!finished && reader.Read());

        if (!finished)
        {
            ThrowUnexpectedEndException(reader, newObject, "Unexpected end when deserializing object.");
        }

        if (propertiesPresence != null)
        {
            foreach (var (property, presence) in propertiesPresence)
            {
                EndProcessProperty(newObject, reader, contract, initialDepth, property, presence, true);
            }
        }

        OnDeserialized(reader, newObject);
        return newObject;
    }

    bool CheckPropertyName(JsonReader reader, string memberName)
    {
        if (Serializer.MetadataPropertyHandling == MetadataPropertyHandling.ReadAhead)
        {
            switch (memberName)
            {
                case JsonTypeReflector.IdPropertyName:
                case JsonTypeReflector.RefPropertyName:
                case JsonTypeReflector.TypePropertyName:
                case JsonTypeReflector.ArrayValuesPropertyName:
                    reader.Skip();
                    return true;
            }
        }

        return false;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void EndProcessProperty(object newObject, JsonReader reader, JsonObjectContract contract, int initialDepth, JsonProperty property, PropertyPresence presence, bool setDefaultValue)
    {
        if (presence is not (PropertyPresence.None or PropertyPresence.Null))
        {
            return;
        }

        try
        {
            var resolvedRequired = property.Ignored ? Required.Default : property.required ?? contract.ItemRequired ?? Required.Default;

            switch (presence)
            {
                case PropertyPresence.None:
                    if (resolvedRequired is
                        Required.AllowNull or
                        Required.Always)
                    {
                        throw JsonSerializationException.Create(reader, $"Required property '{property.PropertyName}' not found in JSON.");
                    }

                    if (setDefaultValue && !property.Ignored)
                    {
                        property.PropertyContract ??= GetContractSafe(property.PropertyType);

                        if (HasFlag(property.DefaultValueHandling.GetValueOrDefault(Serializer.DefaultValueHandling), DefaultValueHandling.Populate) && property.Writable)
                        {
                            property.ValueProvider!.SetValue(newObject, EnsureType(reader, property.GetResolvedDefaultValue(), InvariantCulture, property.PropertyContract!, property.PropertyType));
                        }
                    }

                    break;
                case PropertyPresence.Null:
                    if (resolvedRequired == Required.Always)
                    {
                        throw JsonSerializationException.Create(reader, $"Required property '{property.PropertyName}' expects a value but got null.");
                    }

                    if (resolvedRequired == Required.DisallowNull)
                    {
                        throw JsonSerializationException.Create(reader, $"Required property '{property.PropertyName}' expects a non-null value.");
                    }

                    break;
            }
        }
        catch (Exception exception)
        {
            if (IsDeserializeErrorHandled(newObject, property.PropertyName, reader.Path, exception))
            {
                HandleError(reader, true, initialDepth);
            }
            else
            {
                throw;
            }
        }
    }

    static void SetPropertyPresence(JsonReader reader, JsonProperty property, Dictionary<JsonProperty, PropertyPresence>? requiredProperties)
    {
        if (requiredProperties == null)
        {
            return;
        }

        PropertyPresence propertyPresence;
        switch (reader.TokenType)
        {
            case JsonToken.String:
                if (CoerceEmptyStringToNull(property.PropertyType, property.PropertyContract, (string) reader.GetValue()))
                {
                    propertyPresence = PropertyPresence.Null;
                }
                else
                {
                    propertyPresence = PropertyPresence.Value;
                }

                break;
            case JsonToken.Null:
            case JsonToken.Undefined:
                propertyPresence = PropertyPresence.Null;
                break;
            default:
                propertyPresence = PropertyPresence.Value;
                break;
        }

        requiredProperties[property] = propertyPresence;
    }

    void HandleError(JsonReader reader, bool readPastError, int initialDepth)
    {
        ClearDeserializeErrorContext();

        if (!readPastError)
        {
            return;
        }

        reader.Skip();

        while (reader.Depth > initialDepth)
        {
            if (!reader.Read())
            {
                break;
            }
        }
    }

    ErrorContext? currentDeserializeErrorContext;

    protected void ClearDeserializeErrorContext()
    {
        if (currentDeserializeErrorContext == null)
        {
            throw new InvalidOperationException("Could not clear error context. Error context is already null.");
        }

        currentDeserializeErrorContext = null;
    }

    protected bool IsDeserializeErrorHandled(object? currentObject, object? member, string path, Exception exception)
    {
        if (currentDeserializeErrorContext == null)
        {
            currentDeserializeErrorContext = new(currentObject, exception);
        }
        else if (currentDeserializeErrorContext.Exception != exception)
        {
            throw new InvalidOperationException("Current error context error is different to requested error.");
        }

        if (!currentDeserializeErrorContext.Handled)
        {
            Serializer.DeserializeError?
                .Invoke(
                    currentObject,
                    currentDeserializeErrorContext.OriginalObject,
                    path,
                    member,
                    exception,
                    () => currentDeserializeErrorContext.Handled = true);
        }

        return currentDeserializeErrorContext.Handled;
    }
}