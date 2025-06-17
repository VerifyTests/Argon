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

    static readonly FrozenDictionary<Type, ReadType> readTypeMap = new Dictionary<Type, ReadType>
        {
            [typeof(byte[])] = ReadType.ReadAsBytes,
            [typeof(byte)] = ReadType.ReadAsInt32,
            [typeof(short)] = ReadType.ReadAsInt32,
            [typeof(int)] = ReadType.ReadAsInt32,
            [typeof(decimal)] = ReadType.ReadAsDecimal,
            [typeof(bool)] = ReadType.ReadAsBoolean,
            [typeof(string)] = ReadType.ReadAsString,
            [typeof(DateTime)] = ReadType.ReadAsDateTime,
            [typeof(DateTimeOffset)] = ReadType.ReadAsDateTimeOffset,
            [typeof(float)] = ReadType.ReadAsDouble,
            [typeof(double)] = ReadType.ReadAsDouble,
            [typeof(long)] = ReadType.ReadAsInt64
        }
        .ToFrozenDictionary();
}