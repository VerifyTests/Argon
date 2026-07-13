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
}