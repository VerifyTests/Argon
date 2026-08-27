# Performance improvement todo

Findings from a perf review of the core read/write path, serialization, and LINQ-to-JSON / JSONPath (2026-08-27).
Prioritized within each section; high-priority items sit on per-character / per-token / per-property hot paths.

**All high, medium and low priority items are resolved.** One was explicitly "benchmark first";
it was measured and turned down — see [Measured and turned down](#measured-and-turned-down).

## Measured impact — high priority

BenchmarkDotNet `--job short`, .NET 10.0.11, AMD Ryzen 9 5900X. Baseline is a clean worktree at the
commit these changes sit on, running the identical benchmark code. Benchmarks live in
[HotPathBenchmarks.cs](src/ArgonTests/Benchmarks/HotPathBenchmarks.cs), plus the pre-existing
`ReaderBenchmarks`, `WriterBenchmarks` and `JsonPathRegexBenchmark`.

| Benchmark | Before | After | Time | Allocation |
|---|---|---|---|---|
| `ReadStringHeavy` | 5.727 µs | 1.611 µs | **3.6× faster** | unchanged |
| `SerializeWithTypeNames` | 121.2 µs / 265 KB | 36.21 µs / 89.9 KB | **3.3× faster** | **−66%** |
| `SerializeWithConverters` | 70.65 µs | 32.56 µs | **2.2× faster** | +0.5 KB (memo) |
| `GetInternedNames` | 2.081 µs | 1.083 µs | **1.9× faster** | none either way |
| `DeserializeWithConverters` | 86.20 µs | 47.20 µs | **1.8× faster** | +0.5 KB (memo) |
| `WriteSpanPropertyNames` | 1.208 µs / 3.46 KB | 693.5 ns / 2.51 KB | **1.7× faster** | **−27%** |
| `DeserializeWideRecord` | 2.544 µs | 1.628 µs | **1.6× faster** | unchanged |
| `EnumerateProperties` | 301.8 ns / 88 B | 227.2 ns / 64 B | **1.3× faster** | **−27%** |
| `PopulateExistingValues` | 1.727 µs | 1.317 µs | **1.3× faster** | +0.05 KB |
| `IndexArray` | 2.107 µs | 1.998 µs | 1.05× faster | none either way |
| `IntArrayFromObject` | 27.5 µs / 69.1 KB | 29.6 µs / 57.1 KB | within noise | **−17%** |

Notes on the two rows that are not a clean win:

- The converter memo adds ~0.5 KB per serialize/deserialize call for the `Dictionary<Type, JsonConverter?>`
  itself. That buys roughly a halving of wall-clock whenever converters are registered, and the
  dictionary is never allocated at all when the converter list is empty.
- `IntArrayFromObject` time moved within the measurement error of both runs (the confidence intervals
  overlap); the 17% allocation drop from routing `WriteValue(int)` through `BoxedPrimitives` is the real
  and repeatable part.

## Measured impact — medium and low priority

Benchmarks in [PerValueBenchmarks.cs](src/ArgonTests/Benchmarks/PerValueBenchmarks.cs), same baseline
worktree and machine, `--job medium`.

**Read the timings with the caveat below.** During this measurement session the machine alternated
between two performance modes roughly 2× apart: the *same* baseline binary measured 54.5 µs and
90.6 µs for `DeserializeWithTypeNames` in back-to-back runs, and a current-tree run measured 2.15 µs
then 1.00 µs for `FromObjectWide`. Runs were therefore ordered ABBA (current, baseline, baseline,
current) to expose the drift, and only differences that hold *within* a mode are reported as wins.
Allocation numbers are deterministic and are unaffected by any of this.

| Benchmark | Allocated before → after | Time |
|---|---|---|
| `SerializeDateKeys` | 60.30 KB → 25.15 KB (**−58%**) | 14.75 µs → 6.66 µs (**~1.6–2.2× faster**) |
| `SerializeDateTimeOffsetKeys` | 62.85 KB → 27.70 KB (**−56%**) | 15.94 µs → 7.79 µs (**~1.4–2.0× faster**) |
| `ToStringNoEscapes` | 35.08 KB → 11.72 KB (**−67%**) | 4.31 µs → 1.79 µs (**~1.4–2.4× faster**) |
| `ReadBase64Strings` | 29.92 KB → 11.17 KB (**−63%**) | 10.68 µs → 7.94 µs (best of each; modes did not line up) |
| `ReadExponentDecimals` | 22.11 KB → 12.73 KB (**−42%**) | within noise |
| `DeserializeWithTypeNames` | 83.02 KB → 55.28 KB (**−33%**) | ~4% faster in both modes (90.6→87.0, 54.5→52.0) |
| `SerializeCamelCaseKeys` | 22.59 KB → 16.34 KB (**−28%**) | within noise in the one mode measured on both |
| `WildcardIndexFilter` | 448 B → 416 B | ~1% in mode; one boxed enumerator per input token gone |
| `QueryFilter` | 29176 B → 29136 B | not separable from noise |
| `ParseAndQuery` | 43712 B → 43672 B | not separable from noise |
| `FromObjectWide` | unchanged (4.84 KB) | not separable from noise (one of three name hashes per property gone) |

The three ranges quoted as "× faster" are the cases where the *slowest* current-tree run still beat
the *fastest* baseline run, so they hold regardless of which mode each run landed in. The rows marked
"not separable" are changes that remove work but not allocation (a boxed enumerator, a dictionary
hash), and this machine could not resolve them today; they are kept because the mechanism is not in
doubt, not because a number was produced. Worth re-running on a quiet machine.

## Core reader / writer

### High priority — done

- [x] **Vectorize `ReadStringIntoBuffer`** — [JsonTextReader.cs](src/Argon/JsonTextReader.cs).
  The hottest loop in the reader scanned every string char-by-char through a scalar `switch`, but typical strings contain none of the six interesting chars (`\0 \\ \r \n " '`). Added `SkipToNextStringDelimiter`, which uses a static `SearchValues<char>` to jump straight to the next char the switch actually acts on; the `'\0'` terminator kept at `charsUsed` doubles as the scan's stop sentinel, so the "need more data" path is unchanged. Guarded to net8+, with the original scalar walk on older TFMs. **3.6× faster** on `ReadStringHeavy`.

- [x] **Stop allocating a string per property in span-based `WritePropertyName`** — [JsonWriter.cs](src/Argon/JsonWriter.cs), [JsonPosition.cs](src/Argon/JsonPosition.cs).
  `InternalWritePropertyName(name.ToString())` materialized the span on every call, so the span overload allocated exactly as much as the string one. `JsonPosition` now also holds `NameChars`/`NameLength`, and the span overload copies into a buffer owned by the position and reused by every property at that depth. Path building reads whichever of the two is set. **1.7× faster, 27% less allocation.**

- [x] **Vectorize `DefaultJsonNameTable.TextEquals`** — [DefaultJsonNameTable.cs](src/Argon/DefaultJsonNameTable.cs).
  Replaced the manual char loop with `str1.AsSpan().SequenceEqual(str2.AsSpan(str2Start, str2Length))`, which also short circuits the length mismatch. **1.9× faster** on `GetInternedNames`.

- [x] **Keep the escape writer vectorized after the first escape** — [JavaScriptUtils.cs](src/Argon/Utilities/JavaScriptUtils.cs).
  `WriteEscapedJavaScriptNonNullString` used the vectorized `FirstCharToEscape` only to find the first escapable char and then walked the rest of the string one char at a time. It now re-runs that scan on the remaining slice after each escape, so the clean runs between escapes are skipped rather than stepped over.

### Medium priority — done

- [x] **Span-based Guid probe in `ReadAsBytes`** — [JsonTextReader.cs](src/Argon/JsonTextReader.cs), [ConvertUtils.cs](src/Argon/Utilities/ConvertUtils.cs).
  Added a `TryConvertGuid(CharSpan)` overload and pass `stringReference.AsSpan()`, so a 36-char base64 string no longer allocates a string just to be rejected as a Guid. **63% less allocation** on `ReadBase64Strings`.

- [x] **Stackalloc DateTime write buffers** — [DateTimeUtils.cs](src/Argon/Utilities/DateTimeUtils.cs).
  `WriteDateTimeString` / `WriteDateTimeOffsetString` format into a `stackalloc char[64]`, and the helpers they call (`WriteDefaultIsoDate`, `WriteDateTimeOffset`, `CopyIntToCharArray`) take `Span<char>`. `char[]` callers such as `JsonTextWriter`'s pooled write buffer convert implicitly, so nothing else changed. Paired with the dictionary key item below: **58% less allocation, ~1.6–2.2× faster** on `SerializeDateKeys`.

- [x] **Fast-path `ToEscapedJavaScriptString` when nothing needs escaping** — [JavaScriptUtils.cs](src/Argon/Utilities/JavaScriptUtils.cs).
  When the vectorized `FirstCharToEscape` scan comes back -1 the result is copied straight out: the span itself when there are no delimiters, otherwise through a pooled buffer. The `StringWriter` + `StringBuilder` are only built when there is something to escape. **67% less allocation, ~1.4–2.4× faster** on `ToStringNoEscapes`.

- [x] **Span parameters for `DecimalTryParse` / `Int32TryParse` / `Int64TryParse`** — [ConvertUtils.cs](src/Argon/Utilities/ConvertUtils.cs), [JsonReader.cs](src/Argon/JsonReader.cs).
  The three parsers take `CharSpan` instead of `char[] chars, int start, int length` (the `start`/`length` pair is kept, since `JsonTextReader` passes a slice of its shared buffer). `ReadDecimalString` no longer copies through `ToCharArray()`/`ToArray()` for exponent-form decimals. **42% less allocation** on `ReadExponentDecimals`.

- [x] **`JsonConvert.ToString(char)` allocates a temp array** — [JsonConvert.cs](src/Argon/JsonConvert.cs).
  Now a `stackalloc char[1]`.

### Low priority — done

- [x] **Integer math in `ShiftBufferIfNeeded`** — [JsonTextReader.cs](src/Argon/JsonTextReader.cs).
  `(length - charPos) * 10L <= length`, so the once-per-token check no longer converts to double. The `10L` keeps it correct for buffers past 214M chars.

## Serialization

### High priority — done

- [x] **Memoize the per-value converter scan** — [JsonSerializerInternalBase.cs](src/Argon/Serialization/JsonSerializerInternalBase.cs), used from [JsonSerializerInternalWriter.cs](src/Argon/Serialization/JsonSerializerInternalWriter.cs) and [JsonSerializerInternalReader.cs](src/Argon/Serialization/JsonSerializerInternalReader.cs).
  `GetMatchingConverter` walked the converter list calling virtual `CanConvert(type)` for every value serialized and every property, item and dictionary entry deserialized. Now memoized in a `Dictionary<Type, JsonConverter?>` on the internal base, which is instantiated fresh per serialize/deserialize call, so converters registered between calls are still picked up. Not cacheable on the contract, since contracts are shared across serializers with different converter lists. **2.2× faster serializing, 1.8× deserializing** with converters registered.

- [x] **Cache formatted `$type` names** — [JsonSerializerInternalWriter.cs](src/Argon/Serialization/JsonSerializerInternalWriter.cs).
  Every `$type` concatenated the type and assembly names and re-parsed the result through `RemoveAssemblyDetails`, allocating a `StringBuilder` and a string per object. Now cached per run, keyed on type alone — the binder and format handling are fixed for the duration of a serialization. **3.3× faster, 66% less allocation.**

- [x] **Drop duplicate contract resolution in `CalculatePropertyDetails`** — [JsonSerializerInternalReader.cs](src/Argon/Serialization/JsonSerializerInternalReader.cs).
  `GetContract(currentValue.GetType())` ran twice with the same argument per populated property. `currentValue` is only ever non-null when the earlier block ran, and that block already resolved the same contract, so the second resolution is gone. **1.3× faster** on `PopulateExistingValues`.

- [x] **Fix O(n²) creator-parameter index lookup** — [JsonObjectContract.cs](src/Argon/Serialization/JsonObjectContract.cs).
  `CreatorParameters.IndexOf(constructorProperty)` was a linear scan per matched parameter, making every object built through a parameterized constructor quadratic in its parameter count. Added `IndexOfCreatorParameter`, backed by a `Dictionary<JsonProperty, int>` built on first use (the resolver populates `CreatorParameters` after construction) and rebuilt if that collection later changes. **1.6× faster** deserializing a 16-parameter record.

- [x] **Single-lookup `SetPropertyPresence`** — [JsonSerializerInternalReader.cs](src/Argon/Serialization/JsonSerializerInternalReader.cs).
  `ContainsKey` followed by an indexer set hashed the key twice per property. Uses `CollectionsMarshal.GetValueRefOrNullRef` on net6+, with the original two-lookup form kept under `#if` for net4x.

### Medium priority — done

- [x] **Cache transformed dictionary keys in naming strategies** — [NamingStrategy.cs](src/Argon/NamingStrategy/NamingStrategy.cs).
  `GetDictionaryKey` memoizes resolved keys in a `ConcurrentDictionary<string, string>` on the strategy instance, created lazily and only when `ProcessDictionaryKeys` is set. Dictionary keys are user data, so the cache stops accepting new entries at 512 — past that keys are still resolved, just not remembered. Caching assumes `ResolvePropertyName` is a pure function of the name, which holds for every strategy in Argon; a strategy that resolves a name from outside state can opt out by overriding the new `CacheDictionaryKeys` to false. **28% less allocation** on `SerializeCamelCaseKeys`.

- [x] **Kill the StringWriter per DateTime dictionary key** — [JsonSerializerInternalWriter.cs](src/Argon/Serialization/JsonSerializerInternalWriter.cs), [DateTimeUtils.cs](src/Argon/Utilities/DateTimeUtils.cs).
  `GetDictionaryPropertyName` calls new `ToDateTimeString` / `ToDateTimeOffsetString` overloads that format into a stack buffer and return the string, instead of writing into a `StringWriter` over a `StringBuilder` and calling `ToString` on it.

- [x] **Cache `$type` name splitting during deserialization** — [JsonSerializerInternalReader.cs](src/Argon/Serialization/JsonSerializerInternalReader.cs).
  `SplitTypeName` memoizes `string -> TypeNameKey` on the reader, which is created per deserialize call. Capped at 128 entries since `$type` values come from untrusted JSON; a polymorphic payload repeats a handful of type names, so the cap costs nothing in practice. **33% less allocation** on `DeserializeWithTypeNames`.

### Low priority — done

- [x] **Indexed loop in `CheckForCircularReference` with custom comparer** — [JsonSerializerInternalWriter.cs](src/Argon/Serialization/JsonSerializerInternalWriter.cs).
  Extracted `SerializeStackContains`, which walks the list by index when a custom `EqualityComparer` is set rather than going through `Enumerable.Contains` and its boxed enumerator. The no-comparer path still uses `List<T>.Contains`, which was already an indexed scan.

- [x] **O(n²) `Contains` during one-time contract creation** — [DefaultContractResolver.cs](src/Argon/Serialization/DefaultContractResolver.cs).
  `defaultMembers` is a `HashSet<MemberInfo>`, so the `ShouldSerialize` probe per member is a hash rather than a scan. The other half of that item — the two `GetFieldsAndProperties` calls — was left alone deliberately: the calls pass different `BindingFlags` (public-instance vs public-and-non-public-instance), so collapsing them means re-implementing the binding flag semantics by hand for a first-use-only cost.

## LINQ-to-JSON / JSONPath

### High priority — done

- [x] **Replace LINQ `Cast<JProperty>()` in `JObject.Properties()`** — [JObject.cs](src/Argon/Linq/JObject.cs).
  Now an iterator over `properties.InnerList` (the pattern `GetEnumerator()` already used), so there is no LINQ wrapper enumerable and no boxed interface enumerator. `CopyTo` got the same treatment. **1.3× faster, 27% less allocation.**

- [x] **Route `JTokenWriter.WriteValue(int)` through `BoxedPrimitives`** — [JTokenWriter.cs](src/Argon/Linq/JTokenWriter.cs).
  `int` is the most common CLR type for a JSON number and was the only numeric overload still boxing at the call site. Deliberately not applied to `short`/`ushort`/`byte`/`sbyte`/`uint`: those widen to `int`, which would change the CLR type stored in `JValue.Value` and so is a behaviour change, not just a perf one. **17% less allocation** on `IntArrayFromObject`.

- [x] **Store a `Regex` instance in JSONPath `=~` expressions** — [BooleanQueryExpression.cs](src/Argon.JsonPath/BooleanQueryExpression.cs).
  Evaluation went through static `Regex.IsMatch` per candidate token — a process-wide cache probe each time, degrading to a full pattern re-parse once more than `Regex.CacheSize` patterns are in play. The constructed `Regex` is now cached with the timeout it was built for, in a single reference field so a concurrently evaluated cached `JPath` cannot observe a regex paired with the wrong timeout. Built lazily so an invalid pattern still surfaces during evaluation rather than at parse time.

- [x] **Override `GetItem` in `JArray`** — [JArray.cs](src/Argon/Linq/JArray.cs).
  Indexes the backing list directly instead of going through the virtual `ChildrenTokens` property and an `IList<JToken>` interface dispatch, mirroring what `IndexOfItem` already did.

### Medium priority — done

- [x] **Indexed loops in `ClearItems` / `CopyItemsTo` / `ContentsHashCode`** — [JContainer.cs](src/Argon/Linq/JContainer.cs).
  All three index `ChildrenTokens` rather than `foreach`ing over it, matching the copy constructor, so none of them boxes an enumerator any more.

- [x] **Special-case `JArray`/`JObject` iteration in JSONPath filters** — [ArrayIndexFilter.cs](src/Argon.JsonPath/ArrayIndexFilter.cs), [QueryFilter.cs](src/Argon.JsonPath/QueryFilter.cs).
  `ArrayIndexFilter` binds the `JArray` it already pattern-matches and indexes it. `QueryFilter` walks `First`/`Next` for any `JContainer` — `ChildrenTokens` is `protected`, so it is not reachable from the JsonPath assembly, and the sibling links are the same walk `ScanFilter` uses. Non-containers are skipped, which is what enumerating them produced anyway. One boxed enumerator per input token gone.

- [x] **Skip triple dictionary hash per property in `JTokenWriter.WritePropertyName`** — [JContainer.cs](src/Argon/Linq/JContainer.cs), [JObject.cs](src/Argon/Linq/JObject.cs).
  `ValidateToken` takes a `skipDuplicateNameCheck` flag, set from `InsertItem`'s `skipParentCheck`. That flag has exactly one source — `AddAndSkipParentCheck`, called only by `JTokenWriter.AddParent`, and both `WritePropertyName` overloads remove any property of that name immediately before — so the duplicate name check is provably redundant there. The type check still runs, and every other path (including `JObject.Load`, which is where a duplicate name in a document is caught) is unchanged.

- [x] **Iterate `InnerList` in `JObject.CopyTo` (KVP)** — no action needed.
  Stale finding: `CopyTo` already iterates `properties.InnerList`, which is typed `List<JToken>`, so the `foreach` uses the struct enumerator and boxes nothing.

### Low priority

- [x] **Span-based JSONPath parse for numbers and escape-free strings** — [JPath.cs](src/Argon.JsonPath/JPath.cs).
  `TryParseValue` parses the `expression.AsSpan(start, length)` slice for numbers, and `ReadQuotedString` returns a `Substring` when the string holds no escapes, only building a `StringBuilder` from the first backslash on (copying the run between escapes in one `Append`). The span number parsers need Polyfill, which this project does not reference, so `TryParseInt64`/`TryParseDouble` fall back to a string on net4x.

- [ ] **(Awareness only) `JToken.Path` is O(depth × width)** — `src/Argon/Linq/JToken.cs:197-240`.
  Per array ancestor it does a linear `IndexOf(previous)`; building paths for every element of a big array is quadratic. A real fix needs per-child indices (invasive; matches Newtonsoft behavior as-is).

## Measured and turned down

- [x] **(Benchmark first) `ReadNumberIntoBuffer` per-char switch** — measured, not taken.
  The idea was to replace the 28-case switch per digit with `IndexOfAnyExcept` over a
  `SearchValues` of `[0-9a-fA-FxX.+-]`, finding the terminator in one call. It was implemented
  behind a temporary toggle so both scans could be compared in the same process, and it passed the
  full suite on every framework, so the implementation was sound. It is not worth taking:

  | Digits per number | Scalar switch | Vectorized | |
  |---|---|---|---|
  | 1 | 11.5–12.0 µs | 14.6 µs | **25% slower** |
  | 3 | 31.4–31.6 µs | 32.9 µs | **4% slower** |
  | 8 | 44.6–44.9 µs | 38.4–40.2 µs | 10% faster |
  | 18 | 71.2–72.1 µs | 49.9–54.8 µs | 27% faster |

  Reading 500 numbers, `--job medium --launchCount 3`, from the two runs that agreed with each
  other. The crossover sits between 3 and 8 digits: the vectorized scan has setup cost to earn back,
  and a short number never gives it the chance. The premise in the original finding — that numbers
  are usually short — is what decides it, since ids, counts and small quantities are most of the
  numbers in real JSON, and those get slower.

  A hybrid (scalar for the first 8 chars, vectorized for whatever is left) should in principle take
  the win without the loss, and also passed the full suite, but it could not be measured to a
  conclusion: across five runs the *same* code measured 1.17× to 1.78× apart run to run, which is
  wider than the effect. That is the thing to try first if this is ever revisited on a quiet
  machine. The experiment itself is reverted; `NumberScanBenchmark` in
  [PerValueBenchmarks.cs](src/ArgonTests/Benchmarks/PerValueBenchmarks.cs) stays as the cost profile
  of number reading by length.

## Incidental changes made while implementing the above

- **Unblocked the test build** — [AssemblyInfo.cs](src/ArgonTests/AssemblyInfo.cs).
  `[assembly: CollectionBehavior(DisableTestParallelization = true)]` became a build error (obsolete as error) after the xUnit v3 4.0.0 bump, so no tests could run at all. Replaced with the current API, `[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]`.

- **New tests for the deferred property name** — [JsonTextWriterTest.cs](src/ArgonTests/JsonTextWriterTest.cs).
  `PathWithSpanPropertyNames` and `SpanPropertyNameInExceptionPath`. The span `WritePropertyName` change is the only one that alters how a value is stored and read back later, and there was no existing coverage pairing the span overload with `Path`. They cover names changing length at one depth, names surviving a push/pop, names needing path escaping, a span sliced out of a larger buffer, and mixing the string and span overloads.

- **Fixed a pre-existing test failure** — [XmlNodeConverterTest.cs](src/ArgonTests/Converters/XmlNodeConverterTest.cs).
  `FloatParseHandlingDecimal` failed on both net10.0 and net48, before and after these changes. It built its input as `(decimal) Math.PI + 1000000000m`, but the `double` → `decimal` conversion returns full precision (`3.1415926535897931159979634685`) rather than the 15 significant digits its hardcoded expectation was written for, so it had become a test of conversion precision rather than of the XML/JSON round trip. Confirmed with a standalone console app that no Argon code was involved in producing the differing value. Now uses the decimal literal `1000000003.14159265358979m` directly, so it tests what it intends to; both the XML assertion and the round-trip assertion pass.

- **New tests for the JSONPath quoted string parse** — [JPathParseTests.cs](src/ArgonTests/Linq/JsonPath/JPathParseTests.cs).
  `SinglePropertyAndFilterWithEscapesAroundText`, `SinglePropertyAndFilterWithEscapeAtEnd` and `SinglePropertyAndFilterWithEmptyString`. Deferring the `StringBuilder` until the first escape means the parser now tracks how much of the expression has been copied in, and the existing tests only covered a single escape (`'h\\i'`) or none at all. These cover text between two escapes, an escape as the final character, and the empty string.

- **New benchmarks** — [PerValueBenchmarks.cs](src/ArgonTests/Benchmarks/PerValueBenchmarks.cs), registered in [Program.cs](src/Benchmark.Tests/Program.cs).
  One per medium/low priority item, in the same shape as `HotPathBenchmarks`.

- **Fixed the 13 net11.0 failures** — see below. They were pre-existing (they fail identically on a
  clean worktree at the parent commit) and are unrelated to the perf work, but they were the only thing
  keeping the suite from being green everywhere.

  Full suite: **2361/2361 net10.0, 2360/2360 net11.0, net9.0 and net8.0, 2340/2340 net48, 9/9 F#** —
  11790 tests, 0 failures.

## The net11.0 decimal failures

.NET 11 makes `double`/`float` → `decimal` conversion correctly rounded instead of truncating to 15
(double) or 7 (float) significant digits — [dotnet/runtime#130566](https://github.com/dotnet/runtime/pull/130566),
merged for 11.0-preview7, breaking change documented in dotnet/docs#55743. `Convert.ToDecimal(Math.PI)`
returns `3.1415926535897931159979634685` there and `3.14159265358979` before. The `(decimal)` cast was
already exact on every runtime; only the `Convert` path changed.

Argon was not changed. It uses `Convert.ToDecimal` for `JToken`'s decimal conversion operator, for
`JValue.Compare`, and for dynamic arithmetic where either operand is a decimal, and following the
platform is the right behaviour — reintroducing the old truncation inside Argon would mean deliberately
re-adding an inaccuracy the BCL just removed, and would diverge from what a `(decimal)` cast in the
caller's own code does. The 13 failures were all in the tests:

- **Test data that encoded the old truncation** — `SerializationEventTests` built its input with
  `Convert.ToDecimal(Math.PI)`, the DataTable/DataSet tests assigned the double `64.0021` to a decimal
  typed column, and `FloatTests.FloatParseHandling` asserted against `Convert.ToDecimal(1E-06)`. All now
  use decimal literals, so the test says what it means and does not change with the runtime. This is the
  same fix the runtime team applied to their own two affected tests in that PR.
- **Documentation samples** — the three copies of the `SelectToken` sample sum prices read from JSON as
  doubles, so the total now carries the binary expansion of `99.95`. They assert on the rounded total.
- **Dynamic arithmetic and comparison** — `JValueAddition`'s decimal assertions compare to 10 decimal
  places (xUnit's precision overload), which is far more precision than those expressions are testing.
  `JValueEquals` has the two genuinely runtime-dependent assertions under `#if NET11_0_OR_GREATER`: a
  `JValue` holding the decimal `1.1` no longer compares equal to the double `1.1`, because the double
  now converts to `1.100000000000000088817841970`. That is a real, if narrow, behaviour change for
  anyone comparing a decimal token against a double on .NET 11.

## Already optimal (checked, no action)

- Write-side escape scanning uses `SearchValues` with a ≥16-char threshold; `JsonTextWriter` uses `TryFormat` into pooled buffers; `BoxedPrimitives` covers `JValue(long/bool/double/decimal)`.
- `StringBuffer`/`BufferUtils`/reader `charBuffer` are ArrayPool-backed; `Base64Encoder` has a stackalloc net6+ path; `ConvertUtils.GetTypeCode` uses `FrozenDictionary`.
- Contract property names are interned via `DefaultJsonNameTable`; presence dictionaries have capacity hints; `EnumUtils` caches per (enum, naming strategy) with a struct key.
- `JPropertyKeyedCollection` lookups are dictionary-backed; `ScanFilter` descendants use an allocation-free pointer walk; JPath parses are cached via `JTokenExtensions.ParsePath`.
