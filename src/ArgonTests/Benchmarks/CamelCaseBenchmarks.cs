// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using BenchmarkDotNet.Attributes;

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

    [Benchmark]
    public void Run()
    {
        for (var i = 0; i < names.Length; i++)
        {
            CamelCaseNamingStrategy.ToCamelCase(names[i]);
        }
    }
}
