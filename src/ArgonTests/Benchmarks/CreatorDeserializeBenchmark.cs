// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class CreatorDeserializeBenchmark
{
    const string creatorJson = """{"a":1,"b":2,"c":3,"extra1":4,"extra2":5,"extra3":6,"extra4":7,"extra5":8}""";
    string requiredJson;

    [GlobalSetup]
    public void Setup() =>
        requiredJson = JsonConvert.SerializeObject(new RequiredHolder());

    [Benchmark]
    public CreatorRecord DeserializeThroughCreator() =>
        JsonConvert.DeserializeObject<CreatorRecord>(creatorJson);

    [Benchmark]
    public RequiredHolder DeserializeWithRequiredProperty() =>
        JsonConvert.DeserializeObject<RequiredHolder>(requiredJson);

    public class CreatorRecord
    {
        public CreatorRecord(int a, int b)
        {
            A = a;
            B = b;
        }

        public int A { get; }
        public int B { get; }
        public int C { get; set; }
        public int Extra1 { get; set; }
        public int Extra2 { get; set; }
        public int Extra3 { get; set; }
        public int Extra4 { get; set; }
        public int Extra5 { get; set; }
    }

    public class RequiredHolder
    {
        [JsonRequired]
        public int A { get; set; } = 1;

        public int B { get; set; } = 2;
        public int C { get; set; } = 3;
        public int D { get; set; } = 4;
        public int E { get; set; } = 5;
        public int F { get; set; } = 6;
        public int G { get; set; } = 7;
        public int H { get; set; } = 8;
    }
}
