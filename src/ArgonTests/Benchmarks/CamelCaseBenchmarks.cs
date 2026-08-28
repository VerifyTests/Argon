// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

[MemoryDiagnoser]
public class CamelCaseBenchmarks
{
    static readonly string[] names =
    [
        "Id",
        "Name",
        "FirstName",
        "LastName",
        "EmailAddress",
        "PhoneNumber",
        "DateOfBirth",
        "IsActive",
        "CreatedAt",
        "UpdatedAt",
        "URL",
        "HTTPStatusCode",
        "XMLDocument",
        "SomeVeryLongPropertyNameThatExercisesTheLoop",
        "ABC"
    ];

    static readonly CamelCasePropertyNamesContractResolver resolver = new();

    [Benchmark]
    public void Run()
    {
        for (var i = 0; i < names.Length; i++)
        {
            CamelCaseNamingStrategy.ToCamelCase(names[i]);
        }
    }

    // ResolveContract runs once per value serialized/deserialized. It allocated a Tuple key on
    // every probe (including cache hits) before the struct-key fix.
    [Benchmark]
    public JsonContract ResolveContract()
    {
        JsonContract contract = null;
        for (var i = 0; i < 100; i++)
        {
            contract = resolver.ResolveContract(typeof(ResolveTarget));
        }

        return contract;
    }

    public class ResolveTarget
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
