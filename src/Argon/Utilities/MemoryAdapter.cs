// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#if NET6_0_OR_GREATER

/// <summary>
/// Reads and writes the contents of a <see cref="Memory{T}" /> or <see cref="ReadOnlyMemory{T}" />,
/// neither of which is enumerable.
/// </summary>
abstract class MemoryAdapter
{
    protected bool IsReadOnly { get; private set; }

    internal static bool IsMemoryType(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(Memory<>) ||
               definition == typeof(ReadOnlyMemory<>);
    }

    internal static bool IsByteMemoryType(Type type) =>
        type == typeof(Memory<byte>) ||
        type == typeof(ReadOnlyMemory<byte>);

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    internal static MemoryAdapter Create(Type type)
    {
        var adapter = (MemoryAdapter) Activator.CreateInstance(typeof(TypedMemoryAdapter<>).MakeGenericType(type.GetGenericArguments()))!;
        adapter.IsReadOnly = type.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>);
        return adapter;
    }

    internal abstract IEnumerable GetEnumerable(object value);

    internal abstract object FromList(IList values);

    class TypedMemoryAdapter<T> :
        MemoryAdapter
    {
        internal override IEnumerable GetEnumerable(object value)
        {
            if (value is Memory<T> memory)
            {
                return Enumerate(memory);
            }

            return Enumerate((ReadOnlyMemory<T>) value);
        }

        static IEnumerable Enumerate(ReadOnlyMemory<T> memory)
        {
            for (var index = 0; index < memory.Length; index++)
            {
                yield return memory.Span[index];
            }
        }

        internal override object FromList(IList values)
        {
            var array = new T[values.Count];
            values.CopyTo(array, 0);
            if (IsReadOnly)
            {
                return new ReadOnlyMemory<T>(array);
            }

            return new Memory<T>(array);
        }
    }
}

#endif
