// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class JValueConvertBenchmarks
{
    static readonly JValue StringJValue = [with("String!")];

    [Benchmark]
    public string JTokenToObjectFast() =>
        (string) StringJValue.ToObject(typeof(string));

    [Benchmark]
    public string JTokenToObjectWithSerializer() =>
        (string) StringJValue.ToObject(typeof(string), new());

    [Benchmark]
    public string JTokenToObjectConvert() =>
        StringJValue.Value<string>();

    [Benchmark]
    public string JTokenToObjectCast() =>
        (string) StringJValue;
}
