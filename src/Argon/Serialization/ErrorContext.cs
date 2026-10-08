// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

class ErrorContext
{
    internal ErrorContext(object? originalObject, Exception exception)
    {
        OriginalObject = originalObject;
        Exception = exception;
    }

    public Exception Exception { get; }

    public object? OriginalObject { get; }

    public bool Handled { get; set; }
}