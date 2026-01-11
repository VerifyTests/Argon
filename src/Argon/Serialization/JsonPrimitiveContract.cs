// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Contract details for a <see cref="Type" /> used by the <see cref="JsonSerializer" />.
/// </summary>
public class JsonPrimitiveContract : JsonContract
{
    internal PrimitiveTypeCode TypeCode { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPrimitiveContract" /> class.
    /// </summary>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    public JsonPrimitiveContract(Type underlyingType)
        : base(underlyingType)
    {
        ContractType = JsonContractType.Primitive;

        TypeCode = ConvertUtils.GetTypeCode(underlyingType);
        IsReadOnlyOrFixedSize = true;

        if (readTypeMap.TryGetValue(NonNullableUnderlyingType, out var readType))
        {
            InternalReadType = readType;
        }
    }

    static readonly FrozenDictionary<Type, ReadType> readTypeMap =
        new KeyValuePair<Type, ReadType>[]
        {
            new(typeof(byte[]), ReadType.ReadAsBytes),
            new(typeof(byte), ReadType.ReadAsInt32),
            new(typeof(short), ReadType.ReadAsInt32),
            new(typeof(int), ReadType.ReadAsInt32),
            new(typeof(decimal), ReadType.ReadAsDecimal),
            new(typeof(bool), ReadType.ReadAsBoolean),
            new(typeof(string), ReadType.ReadAsString),
            new(typeof(DateTime), ReadType.ReadAsDateTime),
            new(typeof(DateTimeOffset), ReadType.ReadAsDateTimeOffset),
            new(typeof(float), ReadType.ReadAsDouble),
            new(typeof(double), ReadType.ReadAsDouble),
            new(typeof(long), ReadType.ReadAsInt64),
        }.ToFrozenDictionary();
}