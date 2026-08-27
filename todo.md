# Performance improvement todo

Findings from a perf review of the core read/write path, serialization, and LINQ-to-JSON / JSONPath (2026-08-27).
Prioritized within each section; high-priority items sit on per-character / per-token / per-property hot paths.

**All high-priority items are implemented.** Medium and low priority items remain open.

## Measured impact

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

### Medium priority (per-value allocations)

- [ ] **Span-based Guid probe in `ReadAsBytes`** — `src/Argon/JsonTextReader.cs:97-98`.
  `TryConvertGuid(stringReference.ToString(), ...)` allocates a 36-char string for every 36-char byte-string, even when it's base64. Add a `TryConvertGuid(CharSpan)` overload in `ConvertUtils` (`Guid.TryParseExact` has span overloads via Polyfill) and pass `stringReference.AsSpan()`.

- [ ] **Stackalloc DateTime write buffers** — `src/Argon/Utilities/DateTimeUtils.cs:136-141, 224-230`.
  `WriteDateTimeString` / `WriteDateTimeOffsetString` allocate `new char[64]` per call (hot for date-keyed dictionaries via `JsonSerializerInternalWriter.cs:1051`). Use `stackalloc char[64]` and span-based helpers.

- [ ] **Fast-path `ToEscapedJavaScriptString` when nothing needs escaping** — `src/Argon/Utilities/JavaScriptUtils.cs:283-300`.
  Always builds via `StringWriter` → `StringBuilder` → `ToString()`. When `FirstCharToEscape` returns -1, build the result directly (`string.Create` on modern TFMs, or `string.Concat` with the delimiters).

- [ ] **Span parameters for `DecimalTryParse` / `Int32TryParse` / `Int64TryParse`** — `src/Argon/JsonReader.cs:760, 786`, `src/Argon/Utilities/ConvertUtils.cs:551, 645, 737`.
  `ReadDecimalString` pays `s.ToCharArray()` on every exponent-form decimal (`"96.014e-05"`), a legitimate parse path. The parsers only index their input — change `char[] chars, int start, int length` to `ReadOnlySpan<char>` and pass spans everywhere; no copies.

- [ ] **`JsonConvert.ToString(char)` allocates a temp array** — `src/Argon/JsonConvert.cs:92-93`.
  `new[]{value}.AsSpan()` heap-allocates per call; use `stackalloc char[1]` or `new ReadOnlySpan<char>(in value)`.

### Low priority

- [ ] **Integer math in `ShiftBufferIfNeeded`** — `src/Argon/JsonTextReader.cs:141`.
  `length - charPos <= length * 0.1` does double conversion/multiply once per string/number token; use `(length - charPos) * 10L <= length`.

- [ ] **(Benchmark first) `ReadNumberIntoBuffer` per-char switch** — `src/Argon/JsonTextReader.cs:1172-1248`.
  28-case switch per digit; `IndexOfAnyExcept` with `SearchValues` of `[0-9a-fA-FxX.+-]` would find the terminator in one call, but numbers are usually short — measure before doing.

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

### Medium priority

- [ ] **Cache transformed dictionary keys in naming strategies** — `src/Argon/NamingStrategy/NamingStrategy.cs:46-54` (+ snake/kebab/camel implementations), reached via `DefaultContractResolver.cs:905-913` from `JsonSerializerInternalWriter.cs:991-994`.
  With `ProcessDictionaryKeys = true`, every dictionary entry pays a case-conversion allocation per serialization call for keys that repeat across calls. Add a bounded `ThreadSafeStore<string, string>` cache (key space is user data — cap growth).

- [ ] **Kill the StringWriter per DateTime dictionary key** — `src/Argon/Serialization/JsonSerializerInternalWriter.cs:1045-1059`.
  `GetDictionaryPropertyName` allocates a `StringWriter` + `StringBuilder` per date key; add direct string-returning overloads in `DateTimeUtils` (pairs with the stackalloc item above).

- [ ] **Cache `$type` name splitting during deserialization** — `src/Argon/Serialization/JsonSerializerInternalReader.cs:646-654`.
  `SplitFullyQualifiedTypeName` allocates substrings per `$type` occurrence; only `BindToType` is cached. Cache `string -> TypeNameKey` (bounded — input is untrusted JSON).

### Low priority

- [ ] **Indexed loop in `CheckForCircularReference` with custom comparer** — `src/Argon/Serialization/JsonSerializerInternalWriter.cs:269-271`.
  `serializeStack.Contains(value, Serializer.EqualityComparer)` is LINQ `Enumerable.Contains` (boxed enumerator per value); replace with a `for` loop.

- [ ] **One-time contract creation: duplicate reflection scan + O(n²) `Contains`** — `src/Argon/Serialization/DefaultContractResolver.cs:101-133, 144`.
  `GetFieldsAndProperties` runs twice and `defaultMembers.Contains(member)` is linear per member. First-use latency only (result is cached); use a `HashSet<MemberInfo>` opportunistically.

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

### Medium priority

- [ ] **Indexed loops in `ClearItems` / `CopyItemsTo` / `ContentsHashCode`** — `src/Argon/Linq/JContainer.cs:315-327, 361, 614-623`.
  `foreach` over interface-typed `children` boxes an enumerator per container; `ContentsHashCode` recurses over whole trees via `JTokenEqualityComparer.GetHashCode`. Use the indexed-loop pattern already used in the copy constructor (lines 26-32).

- [ ] **Special-case `JArray`/`JObject` iteration in JSONPath filters** — `src/Argon.JsonPath/ArrayIndexFilter.cs:12-17`, `src/Argon.JsonPath/QueryFilter.cs:8-16`.
  `foreach (var v in t)` routes through `Children()` → `JEnumerable` over interface-typed lists (boxed enumerator per input token). `ArrayIndexFilter` already pattern-matches `JArray` — bind it and index the backing list.

- [ ] **Skip triple dictionary hash per property in `JTokenWriter.WritePropertyName`** — `src/Argon/Linq/JTokenWriter.cs:120-131`, `src/Argon/Linq/JObject.cs:104`.
  `Remove(name)` + `ValidateToken`'s `Contains(name)` + `AddKey` = three hashes per property on the `FromObject` path. The writer path already flows through `AddAndSkipParentCheck`; let `JObject.InsertItem` skip the duplicate-name `Contains` when that flag is set (the preceding `Remove` guarantees uniqueness).

- [ ] **Iterate `InnerList` in `JObject.CopyTo` (KVP)** — `src/Argon/Linq/JObject.cs:503-510`.
  Boxed enumerator + per-item cast; iterate `properties.InnerList` as `GetEnumerator()` does.

### Low priority

- [ ] **Span-based JSONPath parse for numbers and escape-free strings** — `src/Argon.JsonPath/JPath.cs:563-590, 626-684`.
  `TryParseValue` accumulates digits into a `StringBuilder` before parsing (parse the `expression.AsSpan(start, length)` slice instead); `ReadQuotedString` allocates a `StringBuilder` even with no escapes (defer until first `\`, else `Substring`). Mitigated by the path cache, so parse-time only.

- [ ] **(Awareness only) `JToken.Path` is O(depth × width)** — `src/Argon/Linq/JToken.cs:197-240`.
  Per array ancestor it does a linear `IndexOf(previous)`; building paths for every element of a big array is quadratic. A real fix needs per-child indices (invasive; matches Newtonsoft behavior as-is).

## Incidental changes made while implementing the above

- **Unblocked the test build** — [AssemblyInfo.cs](src/ArgonTests/AssemblyInfo.cs).
  `[assembly: CollectionBehavior(DisableTestParallelization = true)]` became a build error (obsolete as error) after the xUnit v3 4.0.0 bump, so no tests could run at all. Replaced with the current API, `[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]`.

- **New tests for the deferred property name** — [JsonTextWriterTest.cs](src/ArgonTests/JsonTextWriterTest.cs).
  `PathWithSpanPropertyNames` and `SpanPropertyNameInExceptionPath`. The span `WritePropertyName` change is the only one that alters how a value is stored and read back later, and there was no existing coverage pairing the span overload with `Path`. They cover names changing length at one depth, names surviving a push/pop, names needing path escaping, a span sliced out of a larger buffer, and mixing the string and span overloads.

- **Fixed a pre-existing test failure** — [XmlNodeConverterTest.cs](src/ArgonTests/Converters/XmlNodeConverterTest.cs).
  `FloatParseHandlingDecimal` failed on both net10.0 and net48, before and after these changes. It built its input as `(decimal) Math.PI + 1000000000m`, but the `double` → `decimal` conversion returns full precision (`3.1415926535897931159979634685`) rather than the 15 significant digits its hardcoded expectation was written for, so it had become a test of conversion precision rather than of the XML/JSON round trip. Confirmed with a standalone console app that no Argon code was involved in producing the differing value. Now uses the decimal literal `1000000003.14159265358979m` directly, so it tests what it intends to; both the XML assertion and the round-trip assertion pass.

  Full suite is green: **2358/2358 on net10.0, 2337/2337 on net48, 9/9 F#**.

## Already optimal (checked, no action)

- Write-side escape scanning uses `SearchValues` with a ≥16-char threshold; `JsonTextWriter` uses `TryFormat` into pooled buffers; `BoxedPrimitives` covers `JValue(long/bool/double/decimal)`.
- `StringBuffer`/`BufferUtils`/reader `charBuffer` are ArrayPool-backed; `Base64Encoder` has a stackalloc net6+ path; `ConvertUtils.GetTypeCode` uses `FrozenDictionary`.
- Contract property names are interned via `DefaultJsonNameTable`; presence dictionaries have capacity hints; `EnumUtils` caches per (enum, naming strategy) with a struct key.
- `JPropertyKeyedCollection` lookups are dictionary-backed; `ScanFilter` descendants use an allocation-free pointer walk; JPath parses are cached via `JTokenExtensions.ParsePath`.
