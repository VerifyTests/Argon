// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// The default JSON name table implementation.
/// </summary>
public class DefaultJsonNameTable : JsonNameTable
{
    // used to defeat hashtable DoS attack where someone passes in lots of strings that hash to the same hash code
    static readonly int hashCodeRandomizer;

    // dedicated lock: Add mutates the entries field (Grow replaces it), so the field
    // itself cannot be the monitor - locking it would break mutual exclusion across a resize
    readonly object addLock = new();
    int count;
    Entry[] entries;
    int mask = 31;

    static DefaultJsonNameTable() =>
        hashCodeRandomizer = Environment.TickCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultJsonNameTable" /> class.
    /// </summary>
    public DefaultJsonNameTable() =>
        entries = new Entry[mask + 1];

    /// <summary>
    /// Gets a string containing the same characters as the specified range of characters in the given array.
    /// </summary>
    /// <param name="key">The character array containing the name to find.</param>
    /// <param name="start">The zero-based index into the array specifying the first character of the name.</param>
    /// <param name="length">The number of characters in the name.</param>
    /// <returns>A string containing the same characters as the specified range of characters in the given array.</returns>
    public override string? Get(char[] key, int start, int length)
    {
        if (length == 0)
        {
            return string.Empty;
        }

        var hashCode = length + hashCodeRandomizer;
        hashCode += (hashCode << 7) ^ key[start];
        var end = start + length;
        for (var i = start + 1; i < end; i++)
        {
            hashCode += (hashCode << 7) ^ key[i];
        }

        hashCode -= hashCode >> 17;
        hashCode -= hashCode >> 11;
        hashCode -= hashCode >> 5;

        // make sure index is evaluated before accessing _entries, otherwise potential race condition causing IndexOutOfRangeException
        var mask = Volatile.Read(ref this.mask);
        var index = hashCode & mask;

        var entries = this.entries;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        for (var entry = entries[index]; entry != null; entry = entry.Next)
        {
            if (entry.HashCode == hashCode && TextEquals(entry.Value, key, start, length))
            {
                return entry.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds the specified string into name table.
    /// </summary>
    /// <remarks>This method is not thread-safe.</remarks>
    /// <returns>The resolved string.</returns>
    public override string Add(string key)
    {
        lock (addLock)
        {
            return InnerAdd(key);
        }
    }

    string InnerAdd(string key)
    {
        var length = key.Length;
        if (length == 0)
        {
            return string.Empty;
        }

        var hashCode = length + hashCodeRandomizer;
        foreach (var ch in key)
        {
            hashCode += (hashCode << 7) ^ ch;
        }

        hashCode -= hashCode >> 17;
        hashCode -= hashCode >> 11;
        hashCode -= hashCode >> 5;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        for (var entry = entries[hashCode & mask]; entry != null; entry = entry.Next)
        {
            if (entry.HashCode == hashCode && entry.Value.Equals(key, StringComparison.Ordinal))
            {
                return entry.Value;
            }
        }

        return AddEntry(key, hashCode);
    }

    string AddEntry(string str, int hashCode)
    {
        var index = hashCode & mask;
        var entry = new Entry(str, hashCode, entries[index]);
        entries[index] = entry;
        if (count++ == mask)
        {
            Grow();
        }

        return entry.Value;
    }

    void Grow()
    {
        var entries = this.entries;
        var newMask = mask * 2 + 1;
        var newEntries = new Entry[newMask + 1];

        for (var i = 0; i < entries.Length; i++)
        {
            Entry next;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            for (var entry = entries[i]; entry != null; entry = next)
            {
                var index = entry.HashCode & newMask;
                next = entry.Next;
                entry.Next = newEntries[index];
                newEntries[index] = entry;
            }
        }

        this.entries = newEntries;
        Volatile.Write(ref mask, newMask);
    }

    static bool TextEquals(string str1, char[] str2, int str2Start, int str2Length)
    {
        if (str1.Length != str2Length)
        {
            return false;
        }

        for (var i = 0; i < str1.Length; i++)
        {
            if (str1[i] != str2[str2Start + i])
            {
                return false;
            }
        }

        return true;
    }

    class Entry
    {
        internal readonly string Value;
        internal readonly int HashCode;
        internal Entry Next;

        internal Entry(string value, int hashCode, Entry next)
        {
            Value = value;
            HashCode = hashCode;
            Next = next;
        }
    }
}