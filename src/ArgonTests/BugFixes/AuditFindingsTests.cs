// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

// Regression tests for the issues found during the deep-dive audit.
public class AuditFindingsTests : TestFixtureBase
{
    #region DefaultJsonNameTable lock

    [Fact]
    public void NameTable_resolves_all_names_after_growth()
    {
        var table = new DefaultJsonNameTable();

        // more than the initial 32 buckets, forcing at least one Grow()
        var names = Enumerable.Range(0, 300)
            .Select(_ => $"property{_}")
            .ToList();

        foreach (var name in names)
        {
            Assert.Equal(name, table.Add(name));
        }

        foreach (var name in names)
        {
            var key = name.ToCharArray();
            Assert.Equal(name, table.Get(key, 0, key.Length));
        }
    }

    [Fact]
    public void NameTable_is_thread_safe_under_concurrent_add()
    {
        var table = new DefaultJsonNameTable();
        var names = Enumerable.Range(0, 400)
            .Select(_ => $"name{_}")
            .ToArray();

        // Add locks a dedicated object; before the fix it locked the entries array that Grow()
        // replaces, so concurrent adders spanning a resize could corrupt the shared table.
        Parallel.For(0, 32, _ =>
        {
            foreach (var name in names)
            {
                Assert.Equal(name, table.Add(name));
            }
        });

        foreach (var name in names)
        {
            var key = name.ToCharArray();
            Assert.Equal(name, table.Get(key, 0, key.Length));
        }
    }

    #endregion

    #region JTokenWriter span overloads

    [Fact]
    public void JTokenWriter_writes_span_property_name_and_value()
    {
        using var writer = new JTokenWriter();
        writer.WriteStartObject();
        writer.WritePropertyName("name".AsSpan());
        writer.WriteValue("value".AsSpan());
        writer.WriteEndObject();

        var token = (JObject) writer.Token!;
        Assert.Single(token.Properties());
        Assert.Equal("value", (string) token["name"]!);
    }

    [Fact]
    public void JTokenWriter_writes_span_value_into_array()
    {
        using var writer = new JTokenWriter();
        writer.WriteStartArray();
        writer.WriteValue("a".AsSpan());
        writer.WriteValue("b".AsSpan());
        writer.WriteEndArray();

        var array = (JArray) writer.Token!;
        Assert.Equal(2, array.Count);
        Assert.Equal("a", (string) array[0]!);
        Assert.Equal("b", (string) array[1]!);
    }

    #endregion

    #region JTokenWriter null byte[] / Uri

    [Fact]
    public void JTokenWriter_writes_null_byte_array_as_single_null()
    {
        using var writer = new JTokenWriter();
        writer.WriteStartArray();
        writer.WriteValue((byte[]) null);
        writer.WriteEndArray();

        var array = (JArray) writer.Token!;
        Assert.Single(array);
        Assert.Equal(JTokenType.Null, array[0].Type);
    }

    [Fact]
    public void JTokenWriter_writes_null_uri_in_object_without_throwing()
    {
        using var writer = new JTokenWriter();
        writer.WriteStartObject();
        writer.WritePropertyName("uri");
        writer.WriteValue((Uri) null);
        writer.WriteEndObject();

        var token = (JObject) writer.Token!;
        Assert.Equal(JTokenType.Null, token["uri"]!.Type);
    }

    #endregion

    #region JsonTextWriter span comment / whitespace

    [Fact]
    public void JsonTextWriter_writes_span_comment()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartArray();
            writer.WriteComment("hello".AsSpan());
            writer.WriteEndArray();
        }

        Assert.Contains("/*hello*/", stringWriter.ToString());
    }

    [Fact]
    public void JsonTextWriter_writes_span_whitespace()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteWhitespace("   ".AsSpan());
            writer.WriteValue(1);
        }

        Assert.Equal("   1", stringWriter.ToString());
    }

    #endregion

    #region JContainer.DeepClone line info

    [Fact]
    public void DeepClone_preserves_line_info_on_containers()
    {
        var settings = new JsonLoadSettings {LineInfoHandling = LineInfoHandling.Load};
        var original = JObject.Parse("{\r\n  \"a\": 1\r\n}", settings);

        var originalLineInfo = (IJsonLineInfo) original;
        Assert.True(originalLineInfo.HasLineInfo());

        var clone = (JObject) original.DeepClone();
        var cloneLineInfo = (IJsonLineInfo) clone;

        // before the fix the container clone copied line info from itself (a no-op), so it was lost
        Assert.True(cloneLineInfo.HasLineInfo());
        Assert.Equal(originalLineInfo.LineNumber, cloneLineInfo.LineNumber);
        Assert.Equal(originalLineInfo.LinePosition, cloneLineInfo.LinePosition);
    }

    #endregion

    #region JValue char/string hash consistency

    [Fact]
    public void JValue_char_and_string_hash_consistently_in_equality_comparer()
    {
        var charValue = new JValue('a');
        var stringValue = new JValue("a");

        Assert.True(charValue.Equals(stringValue));

        var comparer = JToken.EqualityComparer;
        Assert.Equal(comparer.GetHashCode(charValue), comparer.GetHashCode(stringValue));

        var set = new HashSet<JToken>(comparer) {charValue};
        Assert.Contains(stringValue, set);
    }

    #endregion

    #region JsonTextReader buffer size guard

    [Fact]
    public void JsonTextReader_rejects_non_positive_buffer_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new JsonTextReader(new StringReader("1"), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JsonTextReader(new StringReader("1"), -5));
    }

    #endregion

    #region MemberSerialization.Fields inherited private fields

    [Fact]
    public void Fields_serialization_includes_base_class_private_fields()
    {
        var model = new FieldsModel(baseValue: 7, derivedValue: 9);

        var json = JsonConvert.SerializeObject(model);
        // Type.GetFields does not return inherited private fields, so before the fix baseField was dropped
        Assert.Contains("baseField", json);

        var roundTripped = JsonConvert.DeserializeObject<FieldsModel>(json)!;
        Assert.Equal(7, roundTripped.GetBaseValue());
        Assert.Equal(9, roundTripped.DerivedValue);
    }

    #endregion

    #region IImmutableQueue contract created type

    [Fact]
    public void IImmutableQueue_contract_uses_the_concrete_created_type()
    {
        var resolver = new DefaultContractResolver();
        var contract = (JsonArrayContract) resolver.ResolveContract(typeof(IImmutableQueue<int>));

        // was the (non-instantiable) IImmutableQueue<> interface, unlike every sibling immutable
        // interface which maps to its concrete type
        Assert.Equal(typeof(ImmutableQueue<int>), contract.CreatedType);

        var model = new ImmutableQueueHolder {Queue = ImmutableQueue.Create(1, 2, 3)};
        var json = JsonConvert.SerializeObject(model);
        var roundTripped = JsonConvert.DeserializeObject<ImmutableQueueHolder>(json)!;
        Assert.Equal(new[] {1, 2, 3}, roundTripped.Queue.ToArray());
    }

    #endregion

    #region Converter error handling

    [Fact]
    public void VersionConverter_reports_non_string_token_as_json_exception() =>
        // was a raw InvalidCastException with no path/line info
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Version>("123"));

    [Fact]
    public void FileInfo_converter_throws_for_non_string_token_instead_of_dropping_to_null() =>
        // before the fix a non-string, non-null token silently deserialized to null
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<FileInfo>("123"));

    [Fact]
    public void FileInfo_converter_still_maps_null_token_to_null()
    {
        var result = JsonConvert.DeserializeObject<FileInfoHolder>("""{"File":null}""");
        Assert.Null(result.File);
    }

    #endregion

    #region Built-in converters exposed read-only

    [Fact]
    public void BuiltInConverters_are_exposed_as_read_only_list()
    {
        var propertyType = typeof(DefaultContractResolver)
            .GetProperty(nameof(DefaultContractResolver.Converters))!
            .PropertyType;

        Assert.Equal(typeof(IReadOnlyList<JsonConverter>), propertyType);
        Assert.NotEmpty(DefaultContractResolver.Converters);
    }

    #endregion

    #region Immutable struct with multiple constructors

    [Fact]
    public void Immutable_struct_with_multiple_constructors_round_trips()
    {
        var json = JsonConvert.SerializeObject(new MultiConstructorImmutable(42));
        var result = JsonConvert.DeserializeObject<MultiConstructorImmutable>(json);
        Assert.Equal(42, result.Value);
    }

    #endregion

    #region JsonPath && / || precedence

    [Fact]
    public void JsonPath_and_binds_tighter_than_or()
    {
        var array = JArray.Parse("""[{"b":1,"c":1},{"a":1,"b":1},{"c":1}]""");

        // '&&' binds tighter than '||': "(a && b) || c" matches all three items
        var result = array.SelectTokens("$[?(@.a && @.b || @.c)]").ToList();
        Assert.Equal(3, result.Count);

        // the logically identical "c || (a && b)" must select the same set regardless of order
        var reordered = array.SelectTokens("$[?(@.c || @.a && @.b)]").ToList();
        Assert.Equal(3, reordered.Count);
    }

    #endregion

    [JsonObject(MemberSerialization.Fields)]
    public class FieldsModel : FieldsModelBase
    {
        public int DerivedValue;

        public FieldsModel()
        {
        }

        public FieldsModel(int baseValue, int derivedValue)
        {
            SetBaseValue(baseValue);
            DerivedValue = derivedValue;
        }
    }

    public class FieldsModelBase
    {
        int baseField;

        protected void SetBaseValue(int value) =>
            baseField = value;

        public int GetBaseValue() =>
            baseField;
    }

    public class ImmutableQueueHolder
    {
        public IImmutableQueue<int> Queue { get; set; } = ImmutableQueue<int>.Empty;
    }

    public class FileInfoHolder
    {
        public FileInfo File { get; set; }
    }

    public readonly struct MultiConstructorImmutable
    {
        // declared before the matching constructor: before the fix a non-matching first
        // constructor aborted the whole search
        public MultiConstructorImmutable(string ignored) =>
            Value = -1;

        public MultiConstructorImmutable(int value) =>
            Value = value;

        public int Value { get; }
    }
}
