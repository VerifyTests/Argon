// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class FooRequired
{
    [JsonProperty(Required = Required.Always)]
    public List<string> Bars { get; }

    public FooRequired(IEnumerable<string> bars)
    {
        Bars = [];
        if (bars != null)
        {
            Bars.AddRange(bars);
        }
    }
}