// Copyright 2017 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

namespace Argon.NodaTime;

/// <summary>
/// Helper static methods for argument/state validation. (Just the subset used within this library.)
/// </summary>
static class Preconditions
{
    internal static void CheckArgument(bool expression, string parameter, string message)
    {
        if (!expression)
        {
            throw new ArgumentException(message, parameter);
        }
    }

    internal static void CheckData<T>(bool expression, string messageFormat, T messageArg)
    {
        if (!expression)
        {
            var message = string.Format(messageFormat, messageArg);
            throw new InvalidNodaDataException(message);
        }
    }
}