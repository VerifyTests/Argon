// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Text;

public class WriterBugFixes : TestFixtureBase
{
    [Fact]
    public void ToString_TimeSpan_does_not_pad_with_nulls()
    {
        Assert.Equal("\"01:00:00\"", JsonConvert.ToString(TimeSpan.FromHours(1)));
        Assert.Equal("\"1.02:03:04\"", JsonConvert.ToString(new TimeSpan(1, 2, 3, 4)));
    }

    [Fact]
    public void ToString_object_TimeSpan_does_not_pad_with_nulls() =>
        Assert.Equal("\"01:00:00\"", JsonConvert.ToString((object) TimeSpan.FromHours(1)));

    [Fact]
    public void WriteValue_StringBuilder_multichunk_writes_single_string_in_array()
    {
        var builder = new StringBuilder();
        builder.Append('a', 10);
        builder.Append('b', 9000);

        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartArray();
            writer.WriteValue(builder);
            writer.WriteEndArray();
        }

        Assert.Equal($"[\"{builder}\"]", stringWriter.ToString());
    }

    [Fact]
    public void WriteValue_StringBuilder_multichunk_writes_single_string_as_property()
    {
        var builder = new StringBuilder();
        builder.Append('x', 10000);

        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("p");
            writer.WriteValue(builder);
            writer.WriteEndObject();
        }

        Assert.Equal($"{{\"p\":\"{builder}\"}}", stringWriter.ToString());
    }

    [Fact]
    public void WriteValue_StringBuilder_null_writes_null()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteValue((StringBuilder) null);
        }

        Assert.Equal("null", stringWriter.ToString());
    }

    [Fact]
    public void WriteValue_char_honors_QuoteChar()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter) {QuoteChar = '\''})
        {
            writer.WriteValue('c');
        }

        Assert.Equal("'c'", stringWriter.ToString());
    }

    [Fact]
    public void WriteValue_char_honors_QuoteValue()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter) {QuoteValue = false})
        {
            writer.WriteValue('c');
        }

        Assert.Equal("c", stringWriter.ToString());
    }

    [Fact]
    public void WriteValue_char_default_is_double_quoted()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteValue('c');
        }

        Assert.Equal("\"c\"", stringWriter.ToString());
    }
}
