// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable RedundantSuppressNullableWarningExpression

class JsonSerializerInternalWriter(JsonSerializer serializer) :
    JsonSerializerInternalBase(serializer)
{
    Type? rootType;
    int rootLevel;
    readonly List<object> serializeStack = [];

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public void Serialize(JsonWriter jsonWriter, object? value, Type? type)
    {
        rootType = type;
        rootLevel = serializeStack.Count + 1;

        var contract = GetContractSafe(value);

        try
        {
            if (ShouldWriteReference(value, null, contract, null, null))
            {
                WriteReference(jsonWriter, value);
            }
            else
            {
                SerializeValue(jsonWriter, value, contract, null, null, null);
            }
        }
        catch (Exception exception)
        {
            if (IsSerializeErrorHandled(null, null, jsonWriter.Path, exception))
            {
                HandleError(jsonWriter, 0);
            }
            else
            {
                // clear context in case serializer is being used inside a converter
                // if the converter wraps the error then not clearing the context will cause this error:
                // "Current error context error is different to requested error."
                ClearSerializeErrorContext();
                throw;
            }
        }
        finally
        {
            // clear root contract to ensure that if level was > 1 then it won't
            // accidentally be used for non root values
            rootType = null;
        }
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    JsonSerializerProxy GetInternalSerializer() =>
        InternalSerializer ??= new(this);

    JsonContract? GetContractSafe(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return GetContract(value);
    }

    JsonContract GetContract(object value) =>
        Serializer.ResolveContract(value.GetType());

    void SerializePrimitive(JsonWriter writer, object value, JsonPrimitiveContract contract, JsonProperty? member, JsonContainerContract? containerContract, JsonProperty? containerProperty)
    {
        if (contract.TypeCode != PrimitiveTypeCode.Bytes)
        {
            JsonWriter.WriteValue(writer, contract.TypeCode, value);
            return;
        }

        var bytes = (byte[]) value;
        // if type name handling is enabled then wrap the base64 byte string in an object with the type name
        var includeTypeDetails = ShouldWriteType(TypeNameHandling.Objects, contract, member, containerContract, containerProperty);
        if (!includeTypeDetails)
        {
            writer.WriteValue(bytes);
            return;
        }

        writer.WriteStartObject();
        WriteTypeProperty(writer, contract.CreatedType);
        writer.WritePropertyName(JsonTypeReflector.ValuePropertyName, false);
        writer.WriteValue(bytes);
        writer.WriteEndObject();
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void SerializeValue(JsonWriter writer, object? value, JsonContract? valueContract, JsonProperty? member, JsonContainerContract? containerContract, JsonProperty? containerProperty)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        MiscellaneousUtils.Assert(valueContract != null);

        var converter =
            member?.Converter ??
            containerProperty?.ItemConverter ??
            containerContract?.ItemConverter ??
            valueContract.Converter ??
            Serializer.GetMatchingConverter(valueContract.UnderlyingType) ??
            valueContract.InternalConverter;

        if (converter is {CanWrite: true})
        {
            SerializeConvertible(writer, converter, value, valueContract, containerContract, containerProperty);
            return;
        }

        switch (valueContract.ContractType)
        {
            case JsonContractType.Object:
                SerializeObject(writer, value, (JsonObjectContract) valueContract, member, containerContract, containerProperty);
                break;
            case JsonContractType.Array:
                var arrayContract = (JsonArrayContract) valueContract;
                if (arrayContract.IsMultidimensionalArray)
                {
                    SerializeMultidimensionalArray(writer, (Array) value, arrayContract, member, containerContract, containerProperty);
                }
                else
                {
                    SerializeList(writer, (IEnumerable) value, arrayContract, member, containerContract, containerProperty);
                }

                break;
            case JsonContractType.Primitive:
                SerializePrimitive(writer, value, (JsonPrimitiveContract) valueContract, member, containerContract, containerProperty);
                break;
            case JsonContractType.String:
                SerializeString(writer, value, (JsonStringContract) valueContract);
                break;
            case JsonContractType.Dictionary:
                var dictionaryContract = (JsonDictionaryContract) valueContract;
                SerializeDictionary(writer, value as IDictionary ?? dictionaryContract.CreateWrapper(value), dictionaryContract, member, containerContract, containerProperty);
                break;
            case JsonContractType.Dynamic:
                SerializeDynamic(writer, (IDynamicMetaObjectProvider) value, (JsonDynamicContract) valueContract, member, containerContract, containerProperty);
                break;
            case JsonContractType.Linq:
                var token = (JToken) value;
                OnSerializing(writer, token);
                token.WriteTo(writer, Serializer.Converters.ToArray());
                OnSerialized(writer, token);
                break;
        }
    }

    static bool? ResolveIsReference(JsonContract contract, JsonProperty? property, JsonContainerContract? collectionContract, JsonProperty? containerProperty)
    {
        bool? isReference = null;

        // value could be coming from a dictionary or array and not have a property
        if (property != null)
        {
            isReference = property.IsReference;
        }

        if (isReference == null && containerProperty != null)
        {
            isReference = containerProperty.ItemIsReference;
        }

        if (isReference == null && collectionContract != null)
        {
            isReference = collectionContract.ItemIsReference;
        }

        return isReference ?? contract.IsReference;
    }

    bool ShouldWriteReference([NotNullWhen(true)] object? value, JsonProperty? property, JsonContract? valueContract, JsonContainerContract? collectionContract, JsonProperty? containerProperty)
    {
        if (value == null)
        {
            return false;
        }

        MiscellaneousUtils.Assert(valueContract != null);
        if (valueContract.ContractType is JsonContractType.Primitive or JsonContractType.String)
        {
            return false;
        }

        var isReference = ResolveIsReference(valueContract, property, collectionContract, containerProperty);

        if (isReference == null)
        {
            if (valueContract.ContractType == JsonContractType.Array)
            {
                isReference = HasFlag(Serializer.PreserveReferencesHandling, PreserveReferencesHandling.Arrays);
            }
            else
            {
                isReference = HasFlag(Serializer.PreserveReferencesHandling, PreserveReferencesHandling.Objects);
            }
        }

        if (!isReference.GetValueOrDefault())
        {
            return false;
        }

        return Serializer.GetReferenceResolver().IsReferenced(this, value);
    }

    bool ShouldWriteProperty(object? memberValue, JsonObjectContract? containerContract, JsonProperty property)
    {
        if (memberValue == null && ResolvedNullValueHandling(containerContract, property) == NullValueHandling.Ignore)
        {
            return false;
        }

        return !HasFlag(property.DefaultValueHandling.GetValueOrDefault(Serializer.DefaultValueHandling), DefaultValueHandling.Ignore) ||
               !MiscellaneousUtils.ValueEquals(memberValue, property.GetResolvedDefaultValue());
    }

    bool CheckForCircularReference(JsonWriter writer, object? value, JsonProperty? property, JsonContract? contract, JsonContainerContract? containerContract, JsonProperty? containerProperty)
    {
        if (value == null)
        {
            return true;
        }

        MiscellaneousUtils.Assert(contract != null);

        if (contract.ContractType is
            JsonContractType.Primitive or
            JsonContractType.String)
        {
            return true;
        }

        ReferenceLoopHandling? referenceLoopHandling = null;

        if (property != null)
        {
            referenceLoopHandling = property.ReferenceLoopHandling;
        }

        if (referenceLoopHandling == null && containerProperty != null)
        {
            referenceLoopHandling = containerProperty.ItemReferenceLoopHandling;
        }

        if (referenceLoopHandling == null && containerContract != null)
        {
            referenceLoopHandling = containerContract.ItemReferenceLoopHandling;
        }

        var exists = Serializer.EqualityComparer == null
            ? serializeStack.Contains(value)
            : serializeStack.Contains(value, Serializer.EqualityComparer);

        if (!exists)
        {
            return true;
        }

        switch (referenceLoopHandling.GetValueOrDefault(Serializer.ReferenceLoopHandling))
        {
            case ReferenceLoopHandling.Error:
            {
                var message = "Self referencing loop detected";
                if (property != null)
                {
                    message += $" for property '{property.PropertyName}'";
                }

                message += $" with type '{value.GetType()}'.";
                throw JsonSerializationException.Create(null, writer.ContainerPath, message, null);
            }
            case ReferenceLoopHandling.Ignore:
                return false;
            case ReferenceLoopHandling.Serialize:
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void WriteReference(JsonWriter writer, object value)
    {
        var reference = GetReference(writer, value);
        writer.WriteStartObject();
        writer.WritePropertyName(JsonTypeReflector.RefPropertyName, false);
        writer.WriteValue(reference);
        writer.WriteEndObject();
    }

    string GetReference(JsonWriter writer, object value)
    {
        try
        {
            return Serializer.GetReferenceResolver().GetReference(this, value);
        }
        catch (Exception exception)
        {
            throw JsonSerializationException.Create(null, writer.ContainerPath, $"Error writing object reference for '{value.GetType()}'.", exception);
        }
    }

    [RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
    static bool TryConvertToString(object value, Type type, [NotNullWhen(true)] out string? s)
    {
#if NET6_0_OR_GREATER
        if (value is Date date)
        {
            s = date.ToString("yyyy'-'MM'-'dd", InvariantCulture);
            return true;
        }
        if (value is Time time)
        {
            s = time.ToString("HH':'mm':'ss.FFFFFFF", InvariantCulture);
            return true;
        }
#endif
        if (JsonTypeReflector.TryGetStringConverter(type, out var converter))
        {
            s = converter.ConvertToInvariantString(value)!;
            return true;
        }

        if (value is Guid guid)
        {
            s = guid.ToString();
            return true;
        }

        if (value is TimeSpan timeSpan)
        {
            s = timeSpan.ToString();
            return true;
        }

        if (value is Type t)
        {
            s = t.AssemblyQualifiedName!;
            return true;
        }

        s = null;
        return false;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    void SerializeString(JsonWriter writer, object value, JsonStringContract contract)
    {
        OnSerializing(writer, value);

        TryConvertToString(value, contract.UnderlyingType, out var s);
        writer.WriteValue(s);

        OnSerialized(writer, value);
    }

    void OnSerializing(JsonWriter writer, object value) =>
        Serializer.Serializing?.Invoke(writer, value);

    void OnSerialized(JsonWriter writer, object value) =>
        Serializer.Serialized?.Invoke(writer, value);

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void SerializeObject(JsonWriter writer, object value, JsonObjectContract contract, JsonProperty? member, JsonContainerContract? collectionContract, JsonProperty? containerProperty)
    {
        OnSerializing(writer, value);

        serializeStack.Add(value);

        WriteObjectStart(writer, value, contract, member, collectionContract, containerProperty);

        var initialDepth = writer.Top;

        foreach (var property in contract.Properties)
        {
            try
            {
                if (!CalculatePropertyValues(writer, value, contract, member, property, out var memberContract, out var memberValue))
                {
                    continue;
                }

                property.WritePropertyName(writer);
                SerializeValue(writer, memberValue, memberContract, property, contract, member);
            }
            catch (Exception exception)
            {
                if (IsSerializeErrorHandled(value, property.PropertyName, writer.ContainerPath, exception))
                {
                    HandleError(writer, initialDepth);
                }
                else
                {
                    throw;
                }
            }
        }

        writer.WriteEndObject();

        serializeStack.RemoveAt(serializeStack.Count - 1);

        OnSerialized(writer, value);
    }

    bool CalculatePropertyValues(JsonWriter writer, object value, JsonContainerContract contract, JsonProperty? member, JsonProperty property, [NotNullWhen(true)] out JsonContract? memberContract, out object? memberValue)
    {
        if (property is {Ignored: false, Readable: true})
        {
            property.PropertyContract ??= Serializer.ResolveContract(property.PropertyType!);

            memberValue = property.ValueProvider!.GetValue(value);
            memberContract = property.PropertyContract.IsSealed ? property.PropertyContract : GetContractSafe(memberValue);

            if (ShouldWriteProperty(memberValue, contract as JsonObjectContract, property))
            {
                if (ShouldWriteReference(memberValue, property, memberContract, contract, member))
                {
                    property.WritePropertyName(writer);
                    WriteReference(writer, memberValue);
                    return false;
                }

                if (!CheckForCircularReference(writer, memberValue, property, memberContract, contract, member))
                {
                    return false;
                }

                if (memberValue == null)
                {
                    var objectContract = contract as JsonObjectContract;
                    var resolvedRequired = property.required ?? objectContract?.ItemRequired ?? Required.Default;
                    if (resolvedRequired == Required.Always)
                    {
                        throw JsonSerializationException.Create(null, writer.ContainerPath, $"Cannot write a null value for property '{property.PropertyName}'. Property requires a value.", null);
                    }

                    if (resolvedRequired == Required.DisallowNull)
                    {
                        throw JsonSerializationException.Create(null, writer.ContainerPath, $"Cannot write a null value for property '{property.PropertyName}'. Property requires a non-null value.", null);
                    }
                }

#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
                return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
            }
        }

        memberContract = null;
        memberValue = null;
        return false;
    }

    void WriteObjectStart(JsonWriter writer, object value, JsonContract contract, JsonProperty? member, JsonContainerContract? collectionContract, JsonProperty? containerProperty)
    {
        writer.WriteStartObject();

        var isReference = ResolveIsReference(contract, member, collectionContract, containerProperty) ?? HasFlag(Serializer.PreserveReferencesHandling, PreserveReferencesHandling.Objects);
        // don't make readonly fields that aren't creator parameters the referenced value because they can't be deserialized to
        if (isReference && (member == null || member.Writable || HasCreatorParameter(collectionContract, member)))
        {
            WriteReferenceIdProperty(writer, value);
        }

        if (ShouldWriteType(TypeNameHandling.Objects, contract, member, collectionContract, containerProperty))
        {
            WriteTypeProperty(writer, contract.UnderlyingType);
        }
    }

    static bool HasCreatorParameter(JsonContainerContract? contract, JsonProperty property)
    {
        if (contract is JsonObjectContract objectContract)
        {
            return objectContract.CreatorParameters.Contains(property.PropertyName!);
        }

        return false;
    }

    void WriteReferenceIdProperty(JsonWriter writer, object value)
    {
        var reference = GetReference(writer, value);
        writer.WritePropertyName(JsonTypeReflector.IdPropertyName, false);
        writer.WriteValue(reference);
    }

    void WriteTypeProperty(JsonWriter writer, Type type)
    {
        var typeName = type.GetTypeName(Serializer.TypeNameAssemblyFormatHandling, Serializer.SerializationBinder);
        writer.WritePropertyName(JsonTypeReflector.TypePropertyName, false);
        writer.WriteValue(typeName);
    }

    static bool HasFlag(PreserveReferencesHandling? value, PreserveReferencesHandling flag) =>
        value != null &&
        (value & flag) == flag;

    static bool HasFlag(TypeNameHandling? value, TypeNameHandling flag) =>
        value != null &&
        (value & flag) == flag;


    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void SerializeConvertible(JsonWriter writer, JsonConverter converter, object value, JsonContract contract, JsonContainerContract? collectionContract, JsonProperty? containerProperty)
    {
        if (ShouldWriteReference(value, null, contract, collectionContract, containerProperty))
        {
            WriteReference(writer, value);
            return;
        }

        if (!CheckForCircularReference(writer, value, null, contract, collectionContract, containerProperty))
        {
            return;
        }

        serializeStack.Add(value);

        converter.WriteJson(writer, value, GetInternalSerializer());

        serializeStack.RemoveAt(serializeStack.Count - 1);
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void SerializeList(JsonWriter writer, IEnumerable values, JsonArrayContract contract, JsonProperty? member, JsonContainerContract? collectionContract, JsonProperty? containerProperty)
    {
        var underlyingList = values is IWrappedCollection wrappedCollection ? wrappedCollection.UnderlyingCollection : values;

        OnSerializing(writer, underlyingList);

        serializeStack.Add(underlyingList);

        var hasWrittenMetadataObject = WriteStartArray(writer, underlyingList, contract, member, collectionContract, containerProperty);

        writer.WriteStartArray();

        var initialDepth = writer.Top;

        var index = 0;
        // note that an error in the IEnumerable won't be caught
        foreach (var value in contract.InterceptSerializeItems(values))
        {
            SerializeArrayItem(writer, contract, member, value, underlyingList, initialDepth, ref index);
        }

        writer.WriteEndArray();

        if (hasWrittenMetadataObject)
        {
            writer.WriteEndObject();
        }

        serializeStack.RemoveAt(serializeStack.Count - 1);

        OnSerialized(writer, underlyingList);
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    private void SerializeArrayItem(JsonWriter writer, JsonArrayContract contract, JsonProperty? member, object? value, object underlyingList, int initialDepth, ref int index)
    {
        try
        {
            var interceptResult = contract.InterceptSerializeItem(value);
            if (interceptResult.ShouldIgnore)
            {
                return;
            }

            if (interceptResult.ShouldReplace)
            {
                writer.WriteValue(interceptResult.Replacement);
                return;
            }

            var valueContract = GetContractSafe(value);

            if (ShouldWriteReference(value, null, valueContract, contract, member))
            {
                WriteReference(writer, value);
            }
            else
            {
                if (CheckForCircularReference(writer, value, null, valueContract, contract, member))
                {
                    SerializeValue(writer, value, valueContract, null, contract, member);
                }
            }
        }
        catch (Exception exception)
        {
            if (IsSerializeErrorHandled(underlyingList, index, writer.ContainerPath, exception))
            {
                HandleError(writer, initialDepth);
            }
            else
            {
                throw;
            }
        }
        finally
        {
            index++;
        }
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void SerializeMultidimensionalArray(JsonWriter writer, Array values, JsonArrayContract contract, JsonProperty? member, JsonContainerContract? collectionContract, JsonProperty? containerProperty)
    {
        OnSerializing(writer, values);

        serializeStack.Add(values);

        var hasWrittenMetadataObject = WriteStartArray(writer, values, contract, member, collectionContract, containerProperty);

        SerializeMultidimensionalArray(writer, values, contract, member, writer.Top, []);

        if (hasWrittenMetadataObject)
        {
            writer.WriteEndObject();
        }

        serializeStack.RemoveAt(serializeStack.Count - 1);

        OnSerialized(writer, values);
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void SerializeMultidimensionalArray(JsonWriter writer, Array values, JsonArrayContract contract, JsonProperty? member, int initialDepth, int[] indices)
    {
        var dimension = indices.Length;
        var newIndices = new int[dimension + 1];
        for (var i = 0; i < dimension; i++)
        {
            newIndices[i] = indices[i];
        }

        writer.WriteStartArray();

        for (var i = values.GetLowerBound(dimension); i <= values.GetUpperBound(dimension); i++)
        {
            newIndices[dimension] = i;
            var isTopLevel = newIndices.Length == values.Rank;

            if (isTopLevel)
            {
                var value = values.GetValue(newIndices)!;

                try
                {
                    var valueContract = GetContractSafe(value);

                    if (ShouldWriteReference(value, null, valueContract, contract, member))
                    {
                        WriteReference(writer, value);
                    }
                    else
                    {
                        if (CheckForCircularReference(writer, value, null, valueContract, contract, member))
                        {
                            SerializeValue(writer, value, valueContract, null, contract, member);
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (IsSerializeErrorHandled(values, i, writer.ContainerPath, exception))
                    {
                        HandleError(writer, initialDepth + 1);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                SerializeMultidimensionalArray(writer, values, contract, member, initialDepth + 1, newIndices);
            }
        }

        writer.WriteEndArray();
    }

    bool WriteStartArray(JsonWriter writer, object values, JsonArrayContract contract, JsonProperty? member, JsonContainerContract? containerContract, JsonProperty? containerProperty)
    {
        var isReference = ResolveIsReference(contract, member, containerContract, containerProperty) ?? HasFlag(Serializer.PreserveReferencesHandling, PreserveReferencesHandling.Arrays);
        // don't make readonly fields that aren't creator parameters the referenced value because they can't be deserialized to
        isReference = isReference && (member == null || member.Writable || HasCreatorParameter(containerContract, member));

        var includeTypeDetails = ShouldWriteType(TypeNameHandling.Arrays, contract, member, containerContract, containerProperty);
        var writeMetadataObject = isReference || includeTypeDetails;

        if (writeMetadataObject)
        {
            writer.WriteStartObject();

            if (isReference)
            {
                WriteReferenceIdProperty(writer, values);
            }

            if (includeTypeDetails)
            {
                WriteTypeProperty(writer, values.GetType());
            }

            writer.WritePropertyName(JsonTypeReflector.ArrayValuesPropertyName, false);
        }

        contract.ItemContract ??= Serializer.ResolveContract(contract.CollectionItemType ?? typeof(object));

        return writeMetadataObject;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void SerializeDynamic(JsonWriter writer, IDynamicMetaObjectProvider value, JsonDynamicContract contract, JsonProperty? member, JsonContainerContract? collectionContract, JsonProperty? containerProperty)
    {
        OnSerializing(writer, value);
        serializeStack.Add(value);

        WriteObjectStart(writer, value, contract, member, collectionContract, containerProperty);

        var initialDepth = writer.Top;

        foreach (var property in contract.Properties)
        {
            // only write non-dynamic properties that have an explicit attribute
            if (property.HasMemberAttribute)
            {
                try
                {
                    if (!CalculatePropertyValues(writer, value, contract, member, property, out var memberContract, out var memberValue))
                    {
                        continue;
                    }

                    property.WritePropertyName(writer);
                    SerializeValue(writer, memberValue, memberContract, property, contract, member);
                }
                catch (Exception exception)
                {
                    if (IsSerializeErrorHandled(value, property.PropertyName, writer.ContainerPath, exception))
                    {
                        HandleError(writer, initialDepth);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        foreach (var memberName in value.GetDynamicMemberNames())
        {
            if (JsonDynamicContract.TryGetMember(value, memberName, out var memberValue))
            {
                try
                {
                    var valueContract = GetContractSafe(memberValue);

                    if (!ShouldWriteDynamicProperty(memberValue))
                    {
                        continue;
                    }

                    if (CheckForCircularReference(writer, memberValue, null, valueContract, contract, member))
                    {
                        var resolvedPropertyName = contract.PropertyNameResolver == null
                            ? memberName
                            : contract.PropertyNameResolver(writer, memberName);

                        writer.WritePropertyName(resolvedPropertyName);
                        SerializeValue(writer, memberValue, valueContract, null, contract, member);
                    }
                }
                catch (Exception exception)
                {
                    if (IsSerializeErrorHandled(value, memberName, writer.ContainerPath, exception))
                    {
                        HandleError(writer, initialDepth);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        writer.WriteEndObject();

        serializeStack.RemoveAt(serializeStack.Count - 1);
        OnSerialized(writer, value);
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    bool ShouldWriteDynamicProperty(object? memberValue)
    {
        if (Serializer.NullValueHandling == NullValueHandling.Ignore &&
            memberValue == null)
        {
            return false;
        }

        return !HasFlag(Serializer.DefaultValueHandling, DefaultValueHandling.Ignore) ||
               (memberValue != null && !MiscellaneousUtils.ValueEquals(memberValue, ReflectionUtils.GetDefaultValue(memberValue.GetType())));
    }

    bool ShouldWriteType(TypeNameHandling typeNameHandlingFlag, JsonContract contract, JsonProperty? member, JsonContainerContract? containerContract, JsonProperty? containerProperty)
    {
        var resolvedTypeNameHandling =
            member?.TypeNameHandling ??
            containerProperty?.ItemTypeNameHandling ??
            containerContract?.ItemTypeNameHandling ??
            Serializer.TypeNameHandling;

        if (HasFlag(resolvedTypeNameHandling, typeNameHandlingFlag))
        {
            return true;
        }

        // instance type and the property's type's contract default type are different (no need to put the type in JSON because the type will be created by default)
        if (HasFlag(resolvedTypeNameHandling, TypeNameHandling.Auto))
        {
            if (member != null)
            {
                if (contract.NonNullableUnderlyingType != member.PropertyContract!.CreatedType)
                {
                    return true;
                }
            }
            else if (containerContract != null)
            {
                if (containerContract.ItemContract == null || contract.NonNullableUnderlyingType != containerContract.ItemContract.CreatedType)
                {
                    return true;
                }
            }
            else if (rootType != null && serializeStack.Count == rootLevel)
            {
                var rootContract = Serializer.ResolveContract(rootType);

                if (contract.NonNullableUnderlyingType != rootContract.CreatedType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void SerializeDictionary(JsonWriter writer, IDictionary values, JsonDictionaryContract contract, JsonProperty? member, JsonContainerContract? collectionContract, JsonProperty? containerProperty)
    {
#pragma warning disable CS8600, CS8602, CS8604
        var underlying = values is IWrappedDictionary wrappedDictionary ? wrappedDictionary.UnderlyingDictionary : values;

        OnSerializing(writer, underlying);
        serializeStack.Add(underlying);

        WriteObjectStart(writer, underlying, contract, member, collectionContract, containerProperty);

        contract.ItemContract ??= Serializer.ContractResolver.ResolveContract(contract.DictionaryValueType ?? typeof(object));

        var keyContract = contract.KeyContract ??= Serializer.ContractResolver.ResolveContract(contract.DictionaryKeyType ?? typeof(object));

        static IEnumerable<DictionaryEntry> Items(IDictionary values)
        {
            foreach (DictionaryEntry entry in values)
            {
                yield return entry;
            }
        }

        if (contract is {OrderByKey: true, IsSortable: true})
        {
            if (contract.DictionaryKeyType == typeof(string))
            {
                foreach (var entry in Items(values).OrderBy(_ => ((string) _.Key, StringComparer.OrdinalIgnoreCase)))
                {
                    SerializeDictionaryItem(writer, contract, member, entry.Key, entry.Value, keyContract, underlying);
                }
            }
            else
            {
                foreach (var entry in Items(values).OrderBy(_ => _.Key))
                {
                    SerializeDictionaryItem(writer, contract, member, entry.Key, entry.Value, keyContract, underlying);
                }
            }
        }
        else
        {
            foreach (DictionaryEntry entry in values)
            {
                SerializeDictionaryItem(writer, contract, member, entry.Key, entry.Value, keyContract, underlying);
            }
        }

        writer.WriteEndObject();

        serializeStack.RemoveAt(serializeStack.Count - 1);

        OnSerialized(writer, underlying);
#pragma warning restore CS8600, CS8602, CS8604
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    void SerializeDictionaryItem(JsonWriter writer, JsonDictionaryContract contract, JsonProperty? member, object key, object? value, JsonContract keyContract, object underlyingDictionary)
    {
        var initialDepth = writer.Top;
        var interceptResult = contract.InterceptSerializeItem(writer, key, value);
        if (interceptResult.ShouldIgnore)
        {
            return;
        }

        if (interceptResult.ShouldReplaceAndValue)
        {
            writer.WritePropertyName(interceptResult.ReplacementKey);
            writer.WriteValue(interceptResult.ReplacementValue);
            return;
        }

        string propertyName;
        var escape = false;
        if (interceptResult.ShouldReplaceKey)
        {
            propertyName = interceptResult.ReplacementKey;
        }
        else
        {
            propertyName = GetDictionaryPropertyName(key, keyContract, out escape);

            if (contract.DictionaryKeyResolver != null)
            {
                propertyName = contract.DictionaryKeyResolver(writer, propertyName, key);
            }
        }

        if (interceptResult.ShouldReplaceValue)
        {
            writer.WritePropertyName(propertyName, escape);
            writer.WriteValue(interceptResult.ReplacementValue);
            return;
        }

        try
        {
            var valueContract = GetContractSafe(value);

            if (ShouldWriteReference(value, null, valueContract, contract, member))
            {
                writer.WritePropertyName(propertyName, escape);
                WriteReference(writer, value);
                return;
            }

            if (!CheckForCircularReference(writer, value, null, valueContract, contract, member))
            {
                return;
            }

            writer.WritePropertyName(propertyName, escape);

            SerializeValue(writer, value, valueContract, null, contract, member);
        }
        catch (Exception exception)
        {
            if (IsSerializeErrorHandled(underlyingDictionary, propertyName, writer.ContainerPath, exception))
            {
                HandleError(writer, initialDepth);
            }
            else
            {
                throw;
            }
        }
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    static string GetDictionaryPropertyName(object key, JsonContract contract, out bool escape)
    {
        if (contract.ContractType == JsonContractType.Primitive)
        {
            var primitiveContract = (JsonPrimitiveContract) contract;
            switch (primitiveContract.TypeCode)
            {
                case PrimitiveTypeCode.DateTime:
                {
                    var dt = (DateTime) key;

                    escape = false;
                    var writer = new StringWriter(InvariantCulture);
                    DateTimeUtils.WriteDateTimeString(writer, dt);
                    return writer.ToString();
                }
                case PrimitiveTypeCode.DateTimeOffset:
                {
                    escape = false;
                    var writer = new StringWriter(InvariantCulture);
                    DateTimeUtils.WriteDateTimeOffsetString(writer, (DateTimeOffset) key);
                    return writer.ToString();
                }
                case PrimitiveTypeCode.Double:
                {
                    var d = (double) key;

                    escape = false;
                    return d.ToString("R", InvariantCulture);
                }
                case PrimitiveTypeCode.Single:
                {
                    var f = (float) key;

                    escape = false;
                    return f.ToString("R", InvariantCulture);
                }
                default:
                {
                    escape = true;

                    if (primitiveContract.IsEnum &&
                        EnumUtils.TryToString(primitiveContract.NonNullableUnderlyingType, key, null, out var enumName))
                    {
                        return enumName;
                    }

                    return Convert.ToString(key, InvariantCulture)!;
                }
            }
        }

        if (TryConvertToString(key, key.GetType(), out var propertyName))
        {
            escape = true;
            return propertyName;
        }

        escape = true;
        return key.ToString()!;
    }

    void HandleError(JsonWriter writer, int initialDepth)
    {
        ClearSerializeErrorContext();

        if (writer.WriteState == WriteState.Property)
        {
            writer.WriteNull();
        }

        while (writer.Top > initialDepth)
        {
            writer.WriteEnd();
        }
    }

    ErrorContext? currentSerializeErrorContext;

    protected void ClearSerializeErrorContext()
    {
        if (currentSerializeErrorContext == null)
        {
            throw new InvalidOperationException("Could not clear error context. Error context is already null.");
        }

        currentSerializeErrorContext = null;
    }

    protected bool IsSerializeErrorHandled(object? currentObject, object? member, string path, Exception exception)
    {
        if (currentSerializeErrorContext == null)
        {
            currentSerializeErrorContext = new(currentObject, exception);
        }
        else if (currentSerializeErrorContext.Exception != exception)
        {
            throw new InvalidOperationException("Current error context error is different to requested error.");
        }

        if (!currentSerializeErrorContext.Handled)
        {
            Serializer.SerializeError?
                .Invoke(
                    currentObject,
                    currentSerializeErrorContext.OriginalObject,
                    path,
                    member,
                    exception,
                    () => currentSerializeErrorContext.Handled = true);
        }

        return currentSerializeErrorContext.Handled;
    }
}