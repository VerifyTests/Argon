# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Argon is a JSON serialization framework for .NET, a hard fork of Newtonsoft.Json. It publishes 6 NuGet packages: Argon, Argon.DataSets, Argon.Xml, Argon.JsonPath, Argon.FSharp, and Argon.InterfaceCallbacks.

## Build & Test Commands

```bash
# Build (all projects)
dotnet build src --configuration Release

# Run all tests
dotnet test src --configuration Release --no-build --no-restore

# Run a single test by name
dotnet test src/ArgonTests --filter "FullyQualifiedName~TestMethodName"

# Run benchmarks
dotnet run --project src/Benchmark.Tests --configuration Release
```

## SDK & Framework Requirements

- Requires .NET SDK 10.0.102 (preview) - see `src/global.json`
- C# language version: `preview` (latest features enabled)
- Target frameworks for Argon core: net462, net472, net48, net6.0, net7.0, net8.0, net9.0, net10.0
- Tests target: net48 (Windows only), net8.0, net9.0, net10.0
- Warnings are treated as errors (`TreatWarningsAsErrors`) with code style enforced in build

## Solution Structure

Solution file: `src/Argon.slnx` (new .slnx format)

| Project | Purpose |
|---|---|
| `Argon` | Core JSON library (serializer, reader/writer, LINQ-to-JSON, contracts, converters) |
| `Argon.JsonPath` | JSONPath query support |
| `Argon.Xml` | JSON-to-XML / XML-to-JSON conversion |
| `Argon.DataSets` | ADO.NET DataSet serialization |
| `Argon.FSharp` | F# discriminated union and type support |
| `Argon.InterfaceCallbacks` | Interface-based serialization callbacks |
| `Argon.NodaTime` | NodaTime integration (not published as NuGet) |
| `ArgonTests` | Main test suite (xUnit v3 + Verify snapshot testing) |
| `Argon.FSharp.Tests` | F# test suite |
| `Benchmark.Tests` | BenchmarkDotNet performance tests |

## Architecture

The core library (`src/Argon/`) is organized into these major areas:

- **Root**: `JsonConvert` (static entry point), `JsonSerializer` (engine), `JsonSerializerSettings`, `JsonReader`/`JsonWriter` (abstract), `JsonTextReader`/`JsonTextWriter` (concrete implementations)
- **Linq/**: JToken DOM — `JObject`, `JArray`, `JProperty`, `JValue`, `JRaw`, `JTokenReader`/`JTokenWriter`
- **Serialization/**: Contract system — `JsonContract` hierarchy (`JsonObjectContract`, `JsonArrayContract`, `JsonDictionaryContract`, etc.), `DefaultContractResolver`, `IContractResolver`
- **Converters/**: Built-in `JsonConverter` implementations (dates, enums, regex, etc.)
- **NamingStrategy/**: `CamelCaseNamingStrategy`, `SnakeCaseNamingStrategy`, `KebabCaseNamingStrategy`
- **Utilities/**: Internal helpers for parsing, reflection, buffering, string operations

## Testing Patterns

- Uses **xUnit v3** with **Verify** (snapshot testing via `Verify.XunitV3`)
- Snapshot files are `.verified.txt` files alongside tests, with target-framework-specific variants (e.g., `TestName.DotNetCore.verified.txt`)
- When tests fail, `.received.txt` files are generated for diff comparison against `.verified.txt`
- Tests namespace is `Tests` (set via `<RootNamespace>Tests</RootNamespace>`)
- `src/ArgonTests/Issues/` contains regression tests for specific bug fixes

## Package Management

Uses central package management via `src/Directory.Packages.props`. All package versions are defined there — individual .csproj files use `<PackageReference>` without version attributes.

## Global Usings

Defined in `src/Directory.Build.props` — all projects automatically import: `System.Globalization`, `Argon`, `System.Runtime.Serialization`, `System.Numerics`, `System.Diagnostics.CodeAnalysis`, and `CultureInfo` as static. A `CharSpan` alias maps to `System.ReadOnlySpan<System.Char>`.
