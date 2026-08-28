// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

[MemoryDiagnoser]
public class SplitFullyQualifiedTypeNameBench
{
    // Mirrors the kind of value passed at DefaultSerializationBinder line ~121:
    // a generic typename with nested assembly-qualified type-argument blocks.
    const string TypeName =
        "System.Collections.Generic.Dictionary`2[" +
        "[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
        "[System.Collections.Generic.List`1[" +
            "[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]" +
        "], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]";

    static readonly (int Start, int Length)[] argSlices = LocateArgSlices(TypeName);

    static (int, int)[] LocateArgSlices(string typeName)
    {
        var openBracketIndex = typeName.IndexOf('[');
        var slices = new List<(int, int)>();
        var scope = 0;
        var argStart = 0;
        var endIndex = typeName.Length - 1;
        for (var i = openBracketIndex + 1; i < endIndex; ++i)
        {
            switch (typeName[i])
            {
                case '[':
                    if (scope == 0)
                    {
                        argStart = i + 1;
                    }

                    ++scope;
                    break;
                case ']':
                    --scope;
                    if (scope == 0)
                    {
                        slices.Add((argStart, i - argStart));
                    }

                    break;
            }
        }

        return slices.ToArray();
    }

    [Benchmark(Baseline = true)]
    public int Old_SubstringPlusStringSplit()
    {
        var sum = 0;
        foreach (var (start, length) in argSlices)
        {
            var arg = TypeName.Substring(start, length);
            var key = OldSplitFullyQualifiedTypeName(arg);
            sum += key.Type.Length + (key.Assembly?.Length ?? 0);
        }

        return sum;
    }

    [Benchmark]
    public int New_AsSpanPlusSpanSplit()
    {
        var sum = 0;
        foreach (var (start, length) in argSlices)
        {
            var arg = TypeName.AsSpan(start, length);
            var key = ReflectionUtils.SplitFullyQualifiedTypeName(arg);
            sum += key.Type.Length + (key.Assembly?.Length ?? 0);
        }

        return sum;
    }

    static TypeNameKey OldSplitFullyQualifiedTypeName(string fullTypeName)
    {
        var assemblyDelimiterIndex = OldGetAssemblyDelimiterIndex(fullTypeName);

        if (assemblyDelimiterIndex == null)
        {
            return new(null, fullTypeName);
        }

        var delimiterIndex = assemblyDelimiterIndex.Value;
        var type = OldTrim(fullTypeName, 0, delimiterIndex);
        var assembly = OldTrim(fullTypeName, delimiterIndex + 1, fullTypeName.Length - delimiterIndex - 1);
        return new(assembly, type);
    }

    static int? OldGetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
    {
        var scope = 0;
        for (var i = 0; i < fullyQualifiedTypeName.Length; i++)
        {
            switch (fullyQualifiedTypeName[i])
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

    static string OldTrim(string s, int start, int length)
    {
        var end = start + length - 1;
        for (; start < end; start++)
        {
            if (!char.IsWhiteSpace(s[start]))
            {
                break;
            }
        }

        for (; end >= start; end--)
        {
            if (!char.IsWhiteSpace(s[end]))
            {
                break;
            }
        }

        return s.Substring(start, end - start + 1);
    }
}
