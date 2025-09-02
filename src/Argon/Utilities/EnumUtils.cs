// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class EnumUtils
{
    const char EnumSeparatorChar = ',';
    const string EnumSeparatorString = ", ";

    static readonly ThreadSafeStore<Tuple<Type, NamingStrategy?>, EnumInfo> ValuesAndNamesPerEnum = new(InitializeValuesAndNames);

    [UnconditionalSuppressMessage("TrimAnalysis", "IL2075", Justification = "Enum fields are not trimmed")]
    static EnumInfo InitializeValuesAndNames(Tuple<Type, NamingStrategy?> key)
    {
        var enumType = key.Item1;
        var names = Enum.GetNames(enumType);
        var resolvedNames = new string[names.Length];
        var values = new ulong[names.Length];

        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            var f = enumType.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
            values[i] = ToUInt64(f.GetValue(null)!);

            var specifiedName = f.GetCustomAttributes(typeof(EnumMemberAttribute), true)
                .Cast<EnumMemberAttribute>()
                .Select(_ => _.Value)
                .SingleOrDefault();
            var hasSpecifiedName = specifiedName != null;
            var resolvedName = specifiedName ?? name;

            if (Array.IndexOf(resolvedNames, resolvedName, 0, i) != -1)
            {
                throw new InvalidOperationException($"Enum name '{resolvedName}' already exists on enum '{enumType.Name}'.");
            }

            resolvedNames[i] = key.Item2 == null
                ? resolvedName
                : key.Item2.GetPropertyName(resolvedName, hasSpecifiedName);
        }

        var isFlags = enumType.IsDefined(typeof(FlagsAttribute), false);

        return new(isFlags, values, names, resolvedNames);
    }

    public static bool TryToString(Type enumType, object value, NamingStrategy? namingStrategy, [NotNullWhen(true)] out string? name)
    {
        var enumInfo = ValuesAndNamesPerEnum.Get(new(enumType, namingStrategy));
        var v = ToUInt64(value);

        if (enumInfo.IsFlags)
        {
            name = InternalFlagsFormat(enumInfo, v);
            return name != null;
        }

        var index = Array.BinarySearch(enumInfo.Values, v);
        if (index >= 0)
        {
            name = enumInfo.ResolvedNames[index];
            return true;
        }

        // is number value
        name = null;
        return false;
    }

    static string? InternalFlagsFormat(EnumInfo entry, ulong result)
    {
        var resolvedNames = entry.ResolvedNames;
        var values = entry.Values;

        var index = values.Length - 1;
        var stringBuilder = new StringBuilder();
        var firstTime = true;
        var saveResult = result;

        // We will not optimize this code further to keep it maintainable. There are some boundary checks that can be applied
        // to minimize the comparisons required. This code works the same for the best/worst case. In general the number of
        // items in an enum are sufficiently small and not worth the optimization.
        while (index >= 0)
        {
            var value = values[index];
            if (index == 0 && value == 0)
            {
                break;
            }

            if ((result & value) == value)
            {
                result -= value;
                if (!firstTime)
                {
                    stringBuilder.Insert(0, EnumSeparatorString);
                }

                var resolvedName = resolvedNames[index];
                stringBuilder.Insert(0, resolvedName);
                firstTime = false;
            }

            index--;
        }

        if (result != 0)
        {
            // We were unable to represent this number as a bitwise or of valid flags
            return null; // return null so the caller knows to .ToString() the input
        }

        if (saveResult == 0)
        {
            // For the cases when we have zero
            if (values.Length > 0 && values[0] == 0)
            {
                return resolvedNames[0]; // Zero was one of the enum values.
            }

            return null;
        }

        return stringBuilder.ToString(); // Return the string representation
    }

    static ulong ToUInt64(object value)
    {
        var typeCode = ConvertUtils.GetTypeCode(value.GetType(), out _);

        switch (typeCode)
        {
            case PrimitiveTypeCode.SByte:
                return (ulong) (sbyte) value;
            case PrimitiveTypeCode.Byte:
                return (byte) value;
            case PrimitiveTypeCode.Boolean:
                // direct cast from bool to byte is not allowed
                return Convert.ToByte((bool) value);
            case PrimitiveTypeCode.Int16:
                return (ulong) (short) value;
            case PrimitiveTypeCode.UInt16:
                return (ushort) value;
            case PrimitiveTypeCode.Char:
                return (char) value;
            case PrimitiveTypeCode.UInt32:
                return (uint) value;
            case PrimitiveTypeCode.Int32:
                return (ulong) (int) value;
            case PrimitiveTypeCode.UInt64:
                return (ulong) value;
            case PrimitiveTypeCode.Int64:
                return (ulong) (long) value;
            // All unsigned types will be directly cast
            default:
                throw new InvalidOperationException("Unknown enum type.");
        }
    }

    public static object ParseEnum(Type enumType, NamingStrategy? namingStrategy, string value, bool disallowNumber)
    {
        if (!enumType.IsEnum)
        {
            throw new ArgumentException("Type provided must be an Enum.", nameof(enumType));
        }

        var entry = ValuesAndNamesPerEnum.Get(new(enumType, namingStrategy));
        var enumNames = entry.Names;
        var resolvedNames = entry.ResolvedNames;
        var enumValues = entry.Values;

        // first check if the entire text (including commas) matches a resolved name
        var matchingIndex = FindIndexByName(resolvedNames, value, 0, value.Length, StringComparison.Ordinal);
        if (matchingIndex != null)
        {
            return Enum.ToObject(enumType, enumValues[matchingIndex.Value]);
        }

        var firstNonWhitespaceIndex = -1;
        for (var i = 0; i < value.Length; i++)
        {
            if (!char.IsWhiteSpace(value[i]))
            {
                firstNonWhitespaceIndex = i;
                break;
            }
        }

        if (firstNonWhitespaceIndex == -1)
        {
            throw new ArgumentException("Must specify valid information for parsing in the string.");
        }

        // check whether string is a number and parse as a number value
        var firstNonWhitespaceChar = value[firstNonWhitespaceIndex];
        if (char.IsDigit(firstNonWhitespaceChar) || firstNonWhitespaceChar is '-' or '+')
        {
            var underlyingType = Enum.GetUnderlyingType(enumType);

            value = value.Trim();
            object? temp = null;

            try
            {
                temp = Convert.ChangeType(value, underlyingType, InvariantCulture);
            }
            catch (FormatException)
            {
                // We need to Parse this as a String instead. There are cases
                // when you tlbimp enums that can have values of the form "3D".
                // Don't fix this code.
            }

            if (temp != null)
            {
                if (disallowNumber)
                {
                    throw new FormatException($"Integer string '{value}' is not allowed.");
                }

                return Enum.ToObject(enumType, temp);
            }
        }

        ulong result = 0;

        var valueIndex = firstNonWhitespaceIndex;
        while (valueIndex <= value.Length) // '=' is to handle invalid case of an ending comma
        {
            // Find the next separator, if there is one, otherwise the end of the string.
            var endIndex = value.IndexOf(EnumSeparatorChar, valueIndex);
            if (endIndex == -1)
            {
                endIndex = value.Length;
            }

            // Shift the starting and ending indices to eliminate whitespace
            var endIndexNoWhitespace = endIndex;
            while (valueIndex < endIndex && char.IsWhiteSpace(value[valueIndex]))
            {
                valueIndex++;
            }

            while (endIndexNoWhitespace > valueIndex && char.IsWhiteSpace(value[endIndexNoWhitespace - 1]))
            {
                endIndexNoWhitespace--;
            }

            var valueSubstringLength = endIndexNoWhitespace - valueIndex;

            // match with case sensitivity
            matchingIndex = MatchName(value, enumNames, resolvedNames, valueIndex, valueSubstringLength, StringComparison.Ordinal) ??
                            // if no match found, attempt case insensitive search
                            MatchName(value, enumNames, resolvedNames, valueIndex, valueSubstringLength, StringComparison.OrdinalIgnoreCase);

            if (matchingIndex == null)
            {
                // still can't find a match
                // before we throw an error, check whether the entire string has a case insensitive match against resolve names
                matchingIndex = FindIndexByName(resolvedNames, value, 0, value.Length, StringComparison.OrdinalIgnoreCase);
                if (matchingIndex != null)
                {
                    return Enum.ToObject(enumType, enumValues[matchingIndex.Value]);
                }

                // no match so error
                throw new ArgumentException($"Requested value '{value}' was not found.");
            }

            result |= enumValues[matchingIndex.Value];

            // Move our pointer to the ending index to go again.
            valueIndex = endIndex + 1;
        }

        return Enum.ToObject(enumType, result);
    }

    static int? MatchName(string value, string[] enumNames, string[] resolvedNames, int valueIndex, int valueSubstringLength, StringComparison comparison) =>
        FindIndexByName(resolvedNames, value, valueIndex, valueSubstringLength, comparison) ??
        FindIndexByName(enumNames, value, valueIndex, valueSubstringLength, comparison);

    static int? FindIndexByName(string[] enumNames, string value, int valueIndex, int valueSubstringLength, StringComparison comparison)
    {
        for (var i = 0; i < enumNames.Length; i++)
        {
            var enumName = enumNames[i];
            if (enumName.Length == valueSubstringLength &&
                string.Compare(enumName, 0, value, valueIndex, valueSubstringLength, comparison) == 0)
            {
                return i;
            }
        }

        return null;
    }
}