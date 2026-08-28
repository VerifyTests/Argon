// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class JTokenBenchmarks
{
    static readonly JObject JObjectSample = JObject.Parse(BenchmarkConstants.JsonText);
    static readonly string JsonTextSample;
    static readonly string NestedJsonText;

    static JTokenBenchmarks()
    {
        var o = new JObject();
        for (var i = 0; i < 50; i++)
        {
            o[i.ToString()] = i;
        }

        JsonTextSample = o.ToString();

        NestedJsonText = $"{new string('[', 100000)}1{new string(']', 100000)}";
    }

    [Benchmark]
    public void TokenWriteTo()
    {
        var stringWriter = new StringWriter();
        JObjectSample.WriteTo(new JsonTextWriter(stringWriter));
    }

    [Benchmark]
    public JObject JObjectParse() =>
        JObject.Parse(JsonTextSample);

    [Benchmark]
    public JArray JArrayNestedParse() =>
        JArray.Parse(NestedJsonText);

    [Benchmark]
    public JArray JArrayNestedBuild()
    {
        var current = new JArray();
        var root = current;
        for (var j = 0; j < 100000; j++)
        {
            var temp = new JArray();
            current.Add(temp);
            current = temp;
        }

        current.Add(1);

        return root;
    }
}