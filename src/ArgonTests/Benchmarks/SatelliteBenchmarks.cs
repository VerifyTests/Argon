// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Data;
using BenchmarkDotNet.Attributes;
using Microsoft.FSharp.Collections;

[MemoryDiagnoser]
public class SatelliteBenchmarks
{
    DataTable table;
    JsonSerializerSettings dataTableSettings;
    string fsharpListJson;
    JsonConverter[] fsharpConverters;
    JsonSerializerSettings fsharpTaxSettings;
    List<SatellitePoco> pocoList;

    [GlobalSetup]
    public void Setup()
    {
        table = new();
        for (var c = 0; c < 10; c++)
        {
            table.Columns.Add($"ColumnName{c}", typeof(int));
        }

        for (var r = 0; r < 500; r++)
        {
            table.Rows.Add(Enumerable.Range(0, 10).Cast<object>().ToArray());
        }

        dataTableSettings = new()
        {
            ContractResolver = new DefaultContractResolver {NamingStrategy = new CamelCaseNamingStrategy()}
        };
        dataTableSettings.Converters.Add(new Argon.DataSets.DataTableConverter());

        fsharpListJson = "[" + string.Join(",", Enumerable.Range(0, 50)) + "]";
        fsharpConverters = FSharpConverters.Instances;
        fsharpTaxSettings = new();
        foreach (var converter in fsharpConverters)
        {
            fsharpTaxSettings.Converters.Add(converter);
        }

        pocoList = Enumerable.Range(0, 20).Select(i => new SatellitePoco
        {
            Name = $"name{i}",
            Count = i
        }).ToList();
    }

    [Benchmark]
    public string SerializeDataTable() =>
        JsonConvert.SerializeObject(table, dataTableSettings);

    [Benchmark]
    public FSharpList<int> DeserializeFSharpList() =>
        JsonConvert.DeserializeObject<FSharpList<int>>(fsharpListJson, fsharpConverters);

    [Benchmark]
    public string FSharpMapRoundtrip()
    {
        var map = JsonConvert.DeserializeObject<FSharpMap<string, int>>("""{"a":1,"b":2,"c":3}""", fsharpConverters);
        return JsonConvert.SerializeObject(map, fsharpConverters);
    }

    [Benchmark]
    public string SerializePocoWithFSharpConvertersRegistered() =>
        JsonConvert.SerializeObject(pocoList, fsharpTaxSettings);

    public class SatellitePoco
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
