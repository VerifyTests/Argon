// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using BenchmarkDotNet.Running;

public class Program
{
    public static void Main(string[] args)
    {
        var attribute = (AssemblyFileVersionAttribute) typeof(JsonConvert).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))!;
        Console.WriteLine($"Json.NET Version: {attribute.Version}");

        var switcher = new BenchmarkSwitcher(
        [
            typeof(WriteEscapedJavaScriptString),
            typeof(SerializeJTokenList),
            typeof(CamelCaseBenchmarks),
            typeof(ReadQuotedNumbers),
            typeof(WriteBase64Benchmark),
            typeof(SplitFullyQualifiedTypeNameBench),
            typeof(PropertyOrderBenchmark),
            typeof(DictionaryWrapperKeysBenchmark),
            typeof(SerializerEngineBenchmarks),
            typeof(ReaderBenchmarks),
            typeof(WriterBenchmarks),
            typeof(LinqBenchmarks),
            typeof(CreatorDeserializeBenchmark),
            typeof(SatelliteBenchmarks)
        ]);
        if (args.Length == 0)
        {
            switcher.Run(["*"]);
        }
        else
        {
            switcher.Run(args);
        }
    }
}