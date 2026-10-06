// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

class EnumInfo(bool isFlags, PrimitiveTypeCode typeCode, ulong[] values, string[] names, string[] resolvedNames)
{
    public readonly bool IsFlags = isFlags;

    // the enum's underlying type code, cached so it is not re-derived reflectively per value written
    public readonly PrimitiveTypeCode TypeCode = typeCode;
    public readonly ulong[] Values = values;
    public readonly string[] Names = names;
    public readonly string[] ResolvedNames = resolvedNames;

    object?[]? boxedValues;

    // Boxing an enum value goes through reflection, and a payload repeats the same few values.
    // The boxes are immutable, so a race to create one only costs a duplicate.
    public object GetBoxedValue(Type enumType, int index)
    {
        var boxes = boxedValues ??= new object?[Values.Length];
        return boxes[index] ??= Enum.ToObject(enumType, Values[index]);
    }
}