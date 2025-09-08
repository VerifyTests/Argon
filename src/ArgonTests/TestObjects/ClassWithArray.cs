// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class ClassWithArray
{
    readonly IList<long> bar = [int.MaxValue];
    string foo;

    [JsonProperty("foo")]
    public string Foo
    {
        get => foo;
        set => foo = value;
    }

    [JsonProperty(PropertyName = "bar")]
    public IList<long> Bar => bar;
}