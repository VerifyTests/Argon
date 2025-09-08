// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class ListErrorObject
{
    public string Member { get; set; }

    public string ThrowError
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            throw new("ListErrorObject.ThrowError get error!");
        }
        set
        {
            if (value != null && value.StartsWith("Handle"))
            {
                field = value;
                return;
            }

            throw new("ListErrorObject.ThrowError set error!");
        }
    }

    public string Member2 { get; set; }
}