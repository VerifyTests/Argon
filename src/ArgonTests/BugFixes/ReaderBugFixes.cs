// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class ReaderBugFixes : TestFixtureBase
{
    [Fact]
    public void Close_releases_buffer_and_is_idempotent()
    {
        var reader = new JsonTextReader(new StringReader("123"));
        reader.Read();
        reader.Close();

        // After close the pooled buffer must be released so a second close cannot
        // return the same array to ArrayPool.Shared a second time.
        var field = typeof(JsonTextReader).GetField("charBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Empty((char[]) field.GetValue(reader));

        // second close must be a no-op (no double-return, no throw)
        reader.Close();
        Assert.Empty((char[]) field.GetValue(reader));
    }

    [Fact]
    public void Dispose_after_explicit_close_does_not_double_return()
    {
        var reader = new JsonTextReader(new StringReader("123"));
        reader.Read();
        reader.Close();

        // Disposing after an explicit Close must not return the buffer again.
        ((IDisposable) reader).Dispose();

        var field = typeof(JsonTextReader).GetField("charBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Empty((char[]) field.GetValue(reader));
    }

    [Fact]
    public void ReadAsInt32_number_error_sets_Undefined_token()
    {
        var reader = new JsonTextReader(new StringReader("[123456789012345]"));
        reader.Read(); // StartArray

        Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());

        // The error-recovery token must be Undefined, distinguishable from a real JSON null.
        Assert.Equal(JsonToken.Undefined, reader.TokenType);
    }

    [Fact]
    public void ReadAsInt32_invalid_integer_sets_Undefined_token()
    {
        var reader = new JsonTextReader(new StringReader("[1.5]"));
        reader.Read(); // StartArray

        Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());

        Assert.Equal(JsonToken.Undefined, reader.TokenType);
    }
}
