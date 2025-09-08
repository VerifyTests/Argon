// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class NonDefaultConstructorWithReadOnlyCollectionProperty
{
    public string Title { get; set; }
    public IList<string> Categories { get; }

    public NonDefaultConstructorWithReadOnlyCollectionProperty(string title)
    {
        Title = title;
        Categories = [];
    }
}