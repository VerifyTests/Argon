// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

class BidirectionalDictionary<TFirst, TSecond>(IEqualityComparer<TFirst> firstEqualityComparer,
    IEqualityComparer<TSecond> secondEqualityComparer,
    string duplicateFirstErrorMessage = "Duplicate item already exists for '{0}'.",
    string duplicateSecondErrorMessage = "Duplicate item already exists for '{0}'.")
    where TFirst : notnull
    where TSecond : notnull
{
    readonly IDictionary<TFirst, TSecond> firstToSecond = new Dictionary<TFirst, TSecond>(firstEqualityComparer);
    readonly IDictionary<TSecond, TFirst> secondToFirst = new Dictionary<TSecond, TFirst>(secondEqualityComparer);

    public BidirectionalDictionary()
        : this(EqualityComparer<TFirst>.Default, EqualityComparer<TSecond>.Default)
    {
    }

    public void Set(TFirst first, TSecond second)
    {
        if (firstToSecond.TryGetValue(first, out var existingSecond))
        {
            if (!existingSecond.Equals(second))
            {
                throw new ArgumentException(string.Format(duplicateFirstErrorMessage, first));
            }
        }

        if (secondToFirst.TryGetValue(second, out var existingFirst))
        {
            if (!existingFirst.Equals(first))
            {
                throw new ArgumentException(string.Format(duplicateSecondErrorMessage, second));
            }
        }

        firstToSecond.Add(first, second);
        secondToFirst.Add(second, first);
    }

    public bool TryGetByFirst(TFirst first, [NotNullWhen(true)] out TSecond? second) =>
        firstToSecond.TryGetValue(first, out second);

    public bool TryGetBySecond(TSecond second, [NotNullWhen(true)] out TFirst? first) =>
        secondToFirst.TryGetValue(second, out first);
}