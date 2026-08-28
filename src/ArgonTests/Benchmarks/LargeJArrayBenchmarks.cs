// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class LargeJArrayBenchmarks
{
    JArray largeJArraySample;

    [GlobalSetup]
    public void SetupData()
    {
        largeJArraySample = [];
        for (var i = 0; i < 100000; i++)
        {
            largeJArraySample.Add(i);
        }
    }

    [Benchmark]
    public string JTokenPathFirstItem()
    {
        var first = largeJArraySample.First;

        return first.Path;
    }

    [Benchmark]
    public string JTokenPathLastItem()
    {
        var last = largeJArraySample.Last;

        return last.Path;
    }

    [Benchmark]
    public void AddPerformance()
    {
        largeJArraySample.Add(1);
        largeJArraySample.RemoveAt(largeJArraySample.Count - 1);
    }
}