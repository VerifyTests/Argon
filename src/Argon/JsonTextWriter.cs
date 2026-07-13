// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Represents a writer that provides a fast, non-cached, forward-only way of generating JSON data.
/// </summary>
public class JsonTextWriter : JsonWriter
{
    const int indentCharBufferSize = 12;
    TextWriter writer;
    const char indentChar = ' ';
    const int indentation = 2;
    char quoteChar = '"';
    bool[]? charEscapeFlags;
    char[]? writeBuffer;
    char[] indentChars;
    string newLine;

    /// <summary>
    /// Gets or sets which character to use to quote attribute values.
    /// </summary>
    public char QuoteChar
    {
        get => quoteChar;
        set
        {
            if (value != '"' &&
                value != '\'')
            {
                throw new ArgumentException(@"Invalid JavaScript string quote character. Valid quote characters are ' and "".");
            }

            quoteChar = value;
            UpdateCharEscapeFlags();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether object names will be surrounded with quotes.
    /// </summary>
    public bool QuoteName { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether object values will be surrounded with quotes.
    /// </summary>
    public bool QuoteValue { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonTextWriter" /> class using the specified <see cref="TextWriter" />.
    /// </summary>
    public JsonTextWriter(TextWriter textWriter)
    {
        writer = textWriter;
        newLine = writer.NewLine;

        UpdateCharEscapeFlags();

        indentChars = new char[newLine.Length + indentCharBufferSize];
        new Span<char>(indentChars).Fill(indentChar);
        newLine.CopyTo(0, indentChars, 0, newLine.Length);
    }

    /// <summary>
    /// Flushes whatever is in the buffer to the underlying <see cref="TextWriter" /> and also flushes the underlying <see cref="TextWriter" />.
    /// </summary>
    public override void Flush() =>
        writer.Flush();

    /// <summary>
    /// Closes this writer.
    /// If <see cref="JsonWriter.CloseOutput" /> is set to <c>true</c>, the underlying <see cref="TextWriter" /> is also closed.
    /// If <see cref="JsonWriter.AutoCompleteOnClose" /> is set to <c>true</c>, the JSON is auto-completed.
    /// </summary>
    public override void Close()
    {
        base.Close();

        CloseBufferAndWriter();
    }

    void CloseBufferAndWriter()
    {
        if (writeBuffer != null)
        {
            BufferUtils.ReturnBuffer(writeBuffer);
            writeBuffer = null;
        }

        if (CloseOutput)
        {
            writer.Close();
        }
    }

    /// <summary>
    /// Writes the beginning of a JSON object.
    /// </summary>
    public override void WriteStartObject()
    {
        InternalWriteStart(JsonToken.StartObject, JsonContainerType.Object);

        writer.Write('{');
    }

    /// <summary>
    /// Writes the beginning of a JSON array.
    /// </summary>
    public override void WriteStartArray()
    {
        InternalWriteStart(JsonToken.StartArray, JsonContainerType.Array);

        writer.Write('[');
    }

    /// <summary>
    /// Writes the specified end token.
    /// </summary>
    protected override void WriteEnd(JsonToken token)
    {
        switch (token)
        {
            case JsonToken.EndObject:
                writer.Write('}');
                break;
            case JsonToken.EndArray:
                writer.Write(']');
                break;
            default:
                throw JsonWriterException.Create(this, $"Invalid JsonToken: {token}");
        }
    }

    /// <summary>
    /// Writes the property name of a name/value pair on a JSON object.
    /// </summary>
    public override void WritePropertyName(string name)
    {
        // keep the original string: routing through the span overload would
        // re-materialize it in InternalWritePropertyName for position tracking
        InternalWritePropertyName(name);

        WriteEscapedString(name.AsSpan(), QuoteName);

        writer.Write(':');
    }

    /// <summary>
    /// Writes the property name of a name/value pair on a JSON object.
    /// </summary>
    public override void WritePropertyName(CharSpan name)
    {
        InternalWritePropertyName(name);

        WriteEscapedString(name, QuoteName);

        writer.Write(':');
    }

    /// <summary>
    /// Writes the property name of a name/value pair on a JSON object.
    /// </summary>
    /// <param name="escape">A flag to indicate whether the text should be escaped when it is written as a JSON property name.</param>
    public override void WritePropertyName(string name, bool escape)
    {
        InternalWritePropertyName(name);

        if (escape)
        {
            WriteEscapedString(name.AsSpan(), QuoteName);
        }
        else
        {
            if (QuoteName)
            {
                writer.Write(quoteChar);
            }

            writer.Write(name);

            if (QuoteName)
            {
                writer.Write(quoteChar);
            }
        }

        writer.Write(':');
    }

    /// <summary>
    /// Writes the property name of a name/value pair on a JSON object.
    /// </summary>
    /// <param name="escape">A flag to indicate whether the text should be escaped when it is written as a JSON property name.</param>
    public override void WritePropertyName(CharSpan name, bool escape)
    {
        InternalWritePropertyName(name);

        if (escape)
        {
            WriteEscapedString(name, QuoteName);
        }
        else
        {
            if (QuoteName)
            {
                writer.Write(quoteChar);
            }

            writer.Write(name);

            if (QuoteName)
            {
                writer.Write(quoteChar);
            }
        }

        writer.Write(':');
    }

    protected override void OnEscapeHandlingChanged() =>
        UpdateCharEscapeFlags();

    void UpdateCharEscapeFlags() =>
        charEscapeFlags = JavaScriptUtils.GetCharEscapeFlags(EscapeHandling, quoteChar);

    /// <summary>
    /// Writes indent characters.
    /// </summary>
    protected override void WriteIndent()
    {
        // levels of indentation multiplied by the indent count
        var currentIndentCount = Top * indentation;

        writer.Write(indentChars, 0, newLine.Length + Math.Min(currentIndentCount, indentCharBufferSize));

        while ((currentIndentCount -= indentCharBufferSize) > 0)
        {
            writer.Write(indentChars, newLine.Length, Math.Min(currentIndentCount, indentCharBufferSize));
        }
    }

    /// <summary>
    /// Writes the JSON value delimiter.
    /// </summary>
    protected override void WriteValueDelimiter() =>
        writer.Write(',');

    /// <summary>
    /// Writes an indent space.
    /// </summary>
    protected override void WriteIndentSpace() =>
        writer.Write(' ');

    void WriteValueInternal(string value) =>
        writer.Write(value);

    #region WriteValue methods

    /// <summary>
    /// Writes a <see cref="Object" /> value.
    /// An error will raised if the value cannot be written as a single JSON token.
    /// </summary>
    public override void WriteValue(object? value)
    {
        if (value is BigInteger bigInteger)
        {
            InternalWriteValue(JsonToken.Integer);
            WriteValueInternal(bigInteger.ToString(InvariantCulture));
        }
        else
        {
            base.WriteValue(value);
        }
    }

    /// <summary>
    /// Writes a null value.
    /// </summary>
    public override void WriteNull()
    {
        InternalWriteValue(JsonToken.Null);
        WriteValueInternal(JsonConvert.Null);
    }

    /// <summary>
    /// Writes an undefined value.
    /// </summary>
    public override void WriteUndefined()
    {
        InternalWriteValue(JsonToken.Undefined);
        WriteValueInternal(JsonConvert.Undefined);
    }

    /// <summary>
    /// Writes raw JSON.
    /// </summary>
    public override void WriteRaw(char? json)
    {
        if (json == null)
        {
            return;
        }

        writer.Write(json.Value);
    }

    /// <summary>
    /// Writes raw JSON.
    /// </summary>
    public override void WriteRaw(string? json) =>
        writer.Write(json);

    /// <summary>
    /// Writes raw JSON.
    /// </summary>
    public override void WriteRaw(StringBuilder? json)
    {
        if (json == null)
        {
            return;
        }

#if NETCOREAPP3_0_OR_GREATER
        foreach (var chunk in json.GetChunks())
        {
            writer.Write(chunk.Span);
        }
#else
        writer.Write(json.ToString());
#endif
    }

    /// <summary>
    /// Writes raw JSON.
    /// </summary>
    public override void WriteRaw(CharSpan json) =>
        writer.Write(json);

    /// <summary>
    /// Writes a <see cref="String" /> value.
    /// </summary>
    public override void WriteValue(string? value)
    {
        InternalWriteValue(JsonToken.String);

        if (value == null)
        {
            WriteValueInternal(JsonConvert.Null);
        }
        else
        {
            WriteEscapedString(value.AsSpan(), QuoteValue);
        }
    }

    /// <summary>
    /// Writes a <see cref="String" /> value.
    /// </summary>
    public override void WriteValue(CharSpan value)
    {
        InternalWriteValue(JsonToken.String);

        WriteEscapedString(value, QuoteValue);
    }

    void WriteEscapedString(CharSpan value, bool quote)
    {
        EnsureBuffer();
        JavaScriptUtils.WriteEscapedJavaScriptString(writer, value, quoteChar, quote, charEscapeFlags!, EscapeHandling, ref writeBuffer);
    }

    /// <summary>
    /// Writes a <see cref="Int32" /> value.
    /// </summary>
    public override void WriteValue(int value)
    {
        InternalWriteValue(JsonToken.Integer);
        WriteIntegerValue(value);
    }

    /// <summary>
    /// Writes a <see cref="UInt32" /> value.
    /// </summary>
    public override void WriteValue(uint value)
    {
        InternalWriteValue(JsonToken.Integer);
        WriteIntegerValue(value);
    }

    /// <summary>
    /// Writes a <see cref="Int64" /> value.
    /// </summary>
    public override void WriteValue(long value)
    {
        InternalWriteValue(JsonToken.Integer);
        WriteIntegerValue(value);
    }

    /// <summary>
    /// Writes a <see cref="UInt64" /> value.
    /// </summary>
    public override void WriteValue(ulong value)
    {
        InternalWriteValue(JsonToken.Integer);
        WriteIntegerValue(value, false);
    }

    /// <summary>
    /// Writes a <see cref="Single" /> value.
    /// </summary>
    public override void WriteValue(float value)
    {
        InternalWriteValue(JsonToken.Float);
        WriteFloatValue(value, false);
    }

    /// <summary>
    /// Writes a <see cref="Nullable{T}" /> of <see cref="Single" /> value.
    /// </summary>
    public override void WriteValue(float? value)
    {
        if (value == null)
        {
            WriteNull();
        }
        else
        {
            InternalWriteValue(JsonToken.Float);
            WriteFloatValue(value.GetValueOrDefault(), true);
        }
    }

    /// <summary>
    /// Writes a <see cref="Double" /> value.
    /// </summary>
    public override void WriteValue(double value)
    {
        InternalWriteValue(JsonToken.Float);
        WriteDoubleValue(value, false);
    }

    /// <summary>
    /// Writes a <see cref="Nullable{T}" /> of <see cref="Double" /> value.
    /// </summary>
    public override void WriteValue(double? value)
    {
        if (value == null)
        {
            WriteNull();
        }
        else
        {
            InternalWriteValue(JsonToken.Float);
            WriteDoubleValue(value.GetValueOrDefault(), true);
        }
    }

    /// <summary>
    /// Writes a <see cref="Boolean" /> value.
    /// </summary>
    public override void WriteValue(bool value)
    {
        InternalWriteValue(JsonToken.Boolean);
        WriteValueInternal(JsonConvert.ToString(value));
    }

    /// <summary>
    /// Writes a <see cref="Int16" /> value.
    /// </summary>
    public override void WriteValue(short value)
    {
        InternalWriteValue(JsonToken.Integer);
        WriteIntegerValue(value);
    }

    /// <summary>
    /// Writes a <see cref="UInt16" /> value.
    /// </summary>
    public override void WriteValue(ushort value)
    {
        InternalWriteValue(JsonToken.Integer);
        WriteIntegerValue(value);
    }

    /// <summary>
    /// Writes a <see cref="Char" /> value.
    /// </summary>
    public override void WriteValue(char value)
    {
        InternalWriteValue(JsonToken.String);
        WriteEscapedString([value], QuoteValue);
    }

    /// <summary>
    /// Writes a <see cref="Byte" /> value.
    /// </summary>
    public override void WriteValue(byte value)
    {
        InternalWriteValue(JsonToken.Integer);
        WriteIntegerValue(value);
    }

    /// <summary>
    /// Writes a <see cref="SByte" /> value.
    /// </summary>
    public override void WriteValue(sbyte value)
    {
        InternalWriteValue(JsonToken.Integer);
        WriteIntegerValue(value);
    }

    /// <summary>
    /// Writes a <see cref="Decimal" /> value.
    /// </summary>
    public override void WriteValue(decimal value)
    {
        InternalWriteValue(JsonToken.Float);

        EnsureBuffer();
        var buffer = writeBuffer!;
        // reserve two chars so the decimal place can always be appended in-buffer
        if (value.TryFormat(buffer.AsSpan(0, buffer.Length - 2), out var written, default, InvariantCulture))
        {
            if (buffer.AsSpan(0, written).IndexOf('.') == -1)
            {
                buffer[written++] = '.';
                buffer[written++] = '0';
            }

            writer.Write(buffer, 0, written);
        }
        else
        {
            WriteValueInternal(JsonConvert.ToString(value));
        }
    }

    /// <summary>
    /// Writes a <see cref="DateTime" /> value.
    /// </summary>
    public override void WriteValue(DateTime value)
    {
        InternalWriteValue(JsonToken.Date);

        var length = WriteValueToBuffer(value);

        writer.Write(writeBuffer!, 0, length);
    }

    int WriteValueToBuffer(DateTime value)
    {
        EnsureBuffer();
        MiscellaneousUtils.Assert(writeBuffer != null);

        var pos = 0;
        if (QuoteValue)
        {
            writeBuffer[pos++] = quoteChar;
        }

        pos = DateTimeUtils.WriteDateTimeString(writeBuffer, pos, value, null, value.Kind);
        if (QuoteValue)
        {
            writeBuffer[pos++] = quoteChar;
        }

        return pos;
    }

    /// <summary>
    /// Writes a <see cref="Byte" />[] value.
    /// </summary>
    public override void WriteValue(byte[]? value)
    {
        if (value == null)
        {
            WriteNull();
        }
        else
        {
            InternalWriteValue(JsonToken.Bytes);
            if (QuoteValue)
            {
                writer.Write(quoteChar);
            }

            writer.WriteBase64(value);
            if (QuoteValue)
            {
                writer.Write(quoteChar);
            }
        }
    }

    /// <summary>
    /// Writes a <see cref="DateTimeOffset" /> value.
    /// </summary>
    public override void WriteValue(DateTimeOffset value)
    {
        InternalWriteValue(JsonToken.Date);

        var length = WriteValueToBuffer(value);

        writer.Write(writeBuffer!, 0, length);
    }

    int WriteValueToBuffer(DateTimeOffset value)
    {
        EnsureBuffer();
        MiscellaneousUtils.Assert(writeBuffer != null);

        var pos = 0;
        if (QuoteValue)
        {
            writeBuffer[pos++] = quoteChar;
        }

        pos = DateTimeUtils.WriteDateTimeString(writeBuffer, pos, value.DateTime, value.Offset, DateTimeKind.Local);
        if (QuoteValue)
        {
            writeBuffer[pos++] = quoteChar;
        }

        return pos;
    }

    /// <summary>
    /// Writes a <see cref="Guid" /> value.
    /// </summary>
    public override void WriteValue(Guid value)
    {
        InternalWriteValue(JsonToken.String);

        // 36 chars for the "D" format plus the quotes
        var buffer = EnsureBuffer(38, 0);
        var pos = 0;
        if (QuoteValue)
        {
            buffer[pos++] = quoteChar;
        }

        value.TryFormat(buffer.AsSpan(pos), out var written);
        pos += written;

        if (QuoteValue)
        {
            buffer[pos++] = quoteChar;
        }

        writer.Write(buffer, 0, pos);
    }

    /// <summary>
    /// Writes a <see cref="TimeSpan" /> value.
    /// </summary>
    public override void WriteValue(TimeSpan value)
    {
        InternalWriteValue(JsonToken.String);

        // constant "c" format is at most 26 chars plus the quotes
        EnsureBuffer();
        var buffer = writeBuffer!;
        var pos = 0;
        if (QuoteValue)
        {
            buffer[pos++] = quoteChar;
        }

        value.TryFormat(buffer.AsSpan(pos), out var written, "c");
        pos += written;

        if (QuoteValue)
        {
            buffer[pos++] = quoteChar;
        }

        writer.Write(buffer, 0, pos);
    }

    /// <summary>
    /// Writes a <see cref="Uri" /> value.
    /// </summary>
    public override void WriteValue(Uri? value)
    {
        if (value == null)
        {
            WriteNull();
        }
        else
        {
            InternalWriteValue(JsonToken.String);
            WriteEscapedString(value.OriginalString.AsSpan(), QuoteValue);
        }
    }

    #endregion

    /// <summary>
    /// Writes a comment <c>/*...*/</c> containing the specified text.
    /// </summary>
    public override void WriteComment(string? text)
    {
        InternalWriteComment();

        writer.Write("/*");
        writer.Write(text);
        writer.Write("*/");
    }

    /// <summary>
    /// Writes a comment <c>/*...*/</c> containing the specified text.
    /// </summary>
    public override void WriteComment(CharSpan text)
    {
        InternalWriteComment();

        writer.Write("/*");
        writer.Write(text);
        writer.Write("*/");
    }

    /// <summary>
    /// Writes the given white space.
    /// </summary>
    public override void WriteWhitespace(string ws)
    {
        InternalWriteWhitespace(ws);

        writer.Write(ws);
    }

    /// <summary>
    /// Writes the given white space.
    /// </summary>
    public override void WriteWhitespace(CharSpan ws)
    {
        InternalWriteWhitespace(ws);

        writer.Write(ws);
    }

    void EnsureBuffer() =>
        // maximum buffer sized used when writing iso date
        writeBuffer ??= BufferUtils.RentBuffer(35);

    void WriteDoubleValue(double value, bool nullable)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            WriteValueInternal(JsonConvert.ToString(value, FloatFormatHandling, QuoteChar, nullable, FloatFormat));
            return;
        }

        EnsureBuffer();
        var buffer = writeBuffer!;
        // reserve two chars so the decimal place can always be appended in-buffer;
        // a custom FloatPrecision format that overflows falls back to the string path
        if (value.TryFormat(buffer.AsSpan(0, buffer.Length - 2), out var written, FloatFormat, InvariantCulture))
        {
            written = EnsureDecimalPlace(buffer, written);
            writer.Write(buffer, 0, written);
        }
        else
        {
            WriteValueInternal(JsonConvert.ToString(value, FloatFormatHandling, QuoteChar, nullable, FloatFormat));
        }
    }

    void WriteFloatValue(float value, bool nullable)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            WriteValueInternal(JsonConvert.ToString(value, FloatFormatHandling, QuoteChar, nullable, FloatFormat));
            return;
        }

        EnsureBuffer();
        var buffer = writeBuffer!;
        if (value.TryFormat(buffer.AsSpan(0, buffer.Length - 2), out var written, FloatFormat, InvariantCulture))
        {
            written = EnsureDecimalPlace(buffer, written);
            writer.Write(buffer, 0, written);
        }
        else
        {
            WriteValueInternal(JsonConvert.ToString(value, FloatFormatHandling, QuoteChar, nullable, FloatFormat));
        }
    }

    static int EnsureDecimalPlace(char[] buffer, int written)
    {
        if (buffer.AsSpan(0, written).IndexOfAny('.', 'E', 'e') == -1)
        {
            buffer[written++] = '.';
            buffer[written++] = '0';
        }

        return written;
    }

    internal char[] EnsureBuffer(int length, int copyTo)
    {
        if (length < 35)
        {
            length = 35;
        }

        var buffer = writeBuffer;
        if (buffer == null)
        {
            return writeBuffer = BufferUtils.RentBuffer(length);
        }

        if (buffer.Length >= length)
        {
            return buffer;
        }

        var newBuffer = BufferUtils.RentBuffer(length);
        if (copyTo != 0)
        {
            Array.Copy(buffer, newBuffer, copyTo);
        }

        BufferUtils.ReturnBuffer(buffer);
        writeBuffer = newBuffer;
        return newBuffer;
    }

    void WriteIntegerValue(long value)
    {
        if (value is >= 0 and <= 9)
        {
            writer.Write((char) ('0' + value));
        }
        else
        {
            var negative = value < 0;
            WriteIntegerValue(negative ? (ulong) -value : (ulong) value, negative);
        }
    }

    void WriteIntegerValue(ulong value, bool negative)
    {
        if (!negative & (value <= 9))
        {
            writer.Write((char) ('0' + value));
        }
        else
        {
            var length = WriteNumberToBuffer(value, negative);
            writer.Write(writeBuffer!, 0, length);
        }
    }

    int WriteNumberToBuffer(ulong value, bool negative)
    {
        if (value <= uint.MaxValue)
        {
            // avoid the 64 bit division if possible
            return WriteNumberToBuffer((uint) value, negative);
        }

        EnsureBuffer();
        MiscellaneousUtils.Assert(writeBuffer != null);

        var totalLength = MathUtils.IntLength(value);

        if (negative)
        {
            totalLength++;
            writeBuffer[0] = '-';
        }

        var index = totalLength;

        do
        {
            var quotient = value / 10;
            var digit = value - quotient * 10;
            writeBuffer[--index] = (char) ('0' + digit);
            value = quotient;
        } while (value != 0);

        return totalLength;
    }

    void WriteIntegerValue(int value)
    {
        if (value is >= 0 and <= 9)
        {
            writer.Write((char) ('0' + value));
        }
        else
        {
            var negative = value < 0;
            WriteIntegerValue(negative ? (uint) -value : (uint) value, negative);
        }
    }

    void WriteIntegerValue(uint value, bool negative)
    {
        if (!negative & (value <= 9))
        {
            writer.Write((char) ('0' + value));
        }
        else
        {
            var length = WriteNumberToBuffer(value, negative);
            writer.Write(writeBuffer!, 0, length);
        }
    }

    int WriteNumberToBuffer(uint value, bool negative)
    {
        EnsureBuffer();
        MiscellaneousUtils.Assert(writeBuffer != null);

        var totalLength = MathUtils.IntLength(value);

        if (negative)
        {
            totalLength++;
            writeBuffer[0] = '-';
        }

        var index = totalLength;

        do
        {
            var quotient = value / 10;
            var digit = value - quotient * 10;
            writeBuffer[--index] = (char) ('0' + digit);
            value = quotient;
        } while (value != 0);

        return totalLength;
    }
}