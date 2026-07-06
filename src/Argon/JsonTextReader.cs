// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Represents a reader that provides fast, non-cached, forward-only access to JSON text data.
/// </summary>
public class JsonTextReader : JsonReader, IJsonLineInfo
{
    const char unicodeReplacementChar = '\uFFFD';
    const int maximumJavascriptIntegerCharacterLength = 380;
#if RELEASE
    const int LargeBufferLength = int.MaxValue / 2;
#else
    internal int LargeBufferLength { get; set; } = int.MaxValue / 2;
#endif

    readonly TextReader reader;
    int charsUsed;
    int lineStartPos;
    int lineNumber;
    bool isEndOfFile;
    StringBuffer stringBuffer;
    StringReference stringReference;
    char[] charBuffer;
    int charPos;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonTextReader" /> class with the specified <see cref="TextReader" />.
    /// </summary>
    public JsonTextReader(TextReader reader, int bufferSize = 1024)
    {
        this.reader = reader;
        lineNumber = 1;

        charBuffer = BufferUtils.RentBuffer(bufferSize);
        charBuffer[0] = '\0';
    }

    /// <summary>
    /// Gets or sets the reader's property name table.
    /// </summary>
    public JsonNameTable? PropertyNameTable { get; set; }

    void EnsureBufferNotEmpty()
    {
        if (stringBuffer.IsEmpty)
        {
            stringBuffer = new(1024);
        }
    }

    void SetNewLine(bool hasNextChar)
    {
        if (hasNextChar && charBuffer[charPos] == StringUtils.LineFeed)
        {
            charPos++;
        }

        OnNewLine(charPos);
    }

    void OnNewLine(int pos)
    {
        lineNumber++;
        lineStartPos = pos;
    }

    void ParseString(char quote, ReadType readType)
    {
        charPos++;

        ShiftBufferIfNeeded();
        ReadStringIntoBuffer(quote);
        ParseReadString(quote, readType);
    }

    void ParseReadString(char quote, ReadType readType)
    {
        SetPostValueState(true);

        switch (readType)
        {
            case ReadType.ReadAsBytes:
                byte[] data;
                if (stringReference.Length == 0)
                {
                    data = [];
                }
                else if (stringReference.Length == 36 &&
                         ConvertUtils.TryConvertGuid(stringReference.ToString(), out var g))
                {
                    data = g.ToByteArray();
                }
                else
                {
                    data = Convert.FromBase64CharArray(stringReference.Chars, stringReference.StartIndex, stringReference.Length);
                }

                SetToken(data);
                break;
            case ReadType.ReadAsString:
                var text = stringReference.ToString();

                SetToken(text);
                quoteChar = quote;
                break;
            case ReadType.ReadAsInt32:
            case ReadType.ReadAsDecimal:
            case ReadType.ReadAsDouble:
            case ReadType.ReadAsBoolean:
                // caller will convert result
                break;
            default:
                SetToken(stringReference.ToString());
                quoteChar = quote;
                break;
        }
    }

    static void BlockCopyChars(char[] src, int srcOffset, char[] dst, int dstOffset, int count)
    {
        const int charByteCount = 2;

        Buffer.BlockCopy(src, srcOffset * charByteCount, dst, dstOffset * charByteCount, count * charByteCount);
    }

    void ShiftBufferIfNeeded()
    {
        // once in the last 10% of the buffer, or buffer is already very large then
        // shift the remaining content to the start to avoid unnecessarily increasing
        // the buffer size when reading numbers/strings
        var length = charBuffer.Length;
        if (length - charPos <= length * 0.1 || length >= LargeBufferLength)
        {
            var count = charsUsed - charPos;
            if (count > 0)
            {
                BlockCopyChars(charBuffer, charPos, charBuffer, 0, count);
            }

            lineStartPos -= charPos;
            charPos = 0;
            charsUsed = count;
            charBuffer[charsUsed] = '\0';
        }
    }

    int ReadData(bool append) =>
        ReadData(append, 0);

    void PrepareBufferForReadData(bool append, int charsRequired)
    {
        // char buffer is full
        if (charsUsed + charsRequired >= charBuffer.Length - 1)
        {
            if (append)
            {
                var doubledArrayLength = charBuffer.Length * 2;

                // copy to new array either double the size of the current or big enough to fit required content
                var newArrayLength = Math.Max(
                    doubledArrayLength < 0 ? int.MaxValue : doubledArrayLength, // handle overflow
                    charsUsed + charsRequired + 1);

                // increase the size of the buffer
                var dst = BufferUtils.RentBuffer(newArrayLength);

                BlockCopyChars(charBuffer, 0, dst, 0, charBuffer.Length);

                BufferUtils.ReturnBuffer(charBuffer);

                charBuffer = dst;
            }
            else
            {
                var remainingCharCount = charsUsed - charPos;

                if (remainingCharCount + charsRequired + 1 >= charBuffer.Length)
                {
                    // the remaining count plus the required is bigger than the current buffer size
                    var dst = BufferUtils.RentBuffer(remainingCharCount + charsRequired + 1);

                    if (remainingCharCount > 0)
                    {
                        BlockCopyChars(charBuffer, charPos, dst, 0, remainingCharCount);
                    }

                    BufferUtils.ReturnBuffer(charBuffer);

                    charBuffer = dst;
                }
                else
                {
                    // copy any remaining data to the beginning of the buffer if needed and reset positions
                    if (remainingCharCount > 0)
                    {
                        BlockCopyChars(charBuffer, charPos, charBuffer, 0, remainingCharCount);
                    }
                }

                lineStartPos -= charPos;
                charPos = 0;
                charsUsed = remainingCharCount;
            }
        }
    }

    int ReadData(bool append, int charsRequired)
    {
        if (isEndOfFile)
        {
            return 0;
        }

        PrepareBufferForReadData(append, charsRequired);

        var attemptCharReadCount = charBuffer.Length - charsUsed - 1;

        var charsRead = reader.Read(charBuffer, charsUsed, attemptCharReadCount);

        charsUsed += charsRead;

        if (charsRead == 0)
        {
            isEndOfFile = true;
        }

        charBuffer[charsUsed] = '\0';
        return charsRead;
    }

    bool EnsureChars(int relativePosition, bool append) =>
        charPos + relativePosition < charsUsed ||
        ReadChars(relativePosition, append);

    bool ReadChars(int relativePosition, bool append)
    {
        if (isEndOfFile)
        {
            return false;
        }

        var charsRequired = charPos + relativePosition - charsUsed + 1;

        var totalCharsRead = 0;

        // it is possible that the TextReader doesn't return all data at once
        // repeat read until the required text is returned or the reader is out of content
        do
        {
            var charsRead = ReadData(append, charsRequired - totalCharsRead);

            // no more content
            if (charsRead == 0)
            {
                break;
            }

            totalCharsRead += charsRead;
        } while (totalCharsRead < charsRequired);

        return totalCharsRead >= charsRequired;
    }

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="TextReader" />.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the next token was read successfully; <c>false</c> if there are no more tokens to read.
    /// </returns>
    public override bool Read()
    {
        while (true)
        {
            switch (currentState)
            {
                case State.Start:
                case State.Property:
                case State.Array:
                case State.ArrayStart:
                    return ParseValue();
                case State.Object:
                case State.ObjectStart:
                    return ParseObject();
                case State.PostValue:
                    // returns true if it hits
                    // end of object or array
                    if (ParsePostValue(false))
                    {
                        return true;
                    }

                    break;
                case State.Finished:
                    if (EnsureChars(0, false))
                    {
                        EatWhitespace();
                        if (isEndOfFile)
                        {
                            SetToken(JsonToken.None);
                            return false;
                        }

                        if (charBuffer[charPos] == '/')
                        {
                            ParseComment(true);
                            return true;
                        }

                        throw JsonReaderException.Create(this, $"Additional text encountered after finished reading JSON content: {charBuffer[charPos]}.");
                    }

                    SetToken(JsonToken.None);
                    return false;
                default:
                    throw JsonReaderException.Create(this, $"Unexpected state: {CurrentState}.");
            }
        }
    }

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="TextReader" /> as a <see cref="Nullable{T}" /> of <see cref="Int32" />.
    /// </summary>
    /// <returns>A <see cref="Nullable{T}" /> of <see cref="Int32" />. This method will return <c>null</c> at the end of an array.</returns>
    public override int? ReadAsInt32() =>
        (int?) ReadNumberValue(ReadType.ReadAsInt32);

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="TextReader" /> as a <see cref="Nullable{T}" /> of <see cref="DateTime" />.
    /// </summary>
    /// <returns>A <see cref="Nullable{T}" /> of <see cref="DateTime" />. This method will return <c>null</c> at the end of an array.</returns>
    public override DateTime? ReadAsDateTime() =>
        (DateTime?) ReadStringValue(ReadType.ReadAsDateTime);

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="TextReader" /> as a <see cref="String" />.
    /// </summary>
    /// <returns>A <see cref="String" />. This method will return <c>null</c> at the end of an array.</returns>
    public override string? ReadAsString() =>
        (string?) ReadStringValue(ReadType.ReadAsString);

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="TextReader" /> as a <see cref="Byte" />[].
    /// </summary>
    /// <returns>A <see cref="Byte" />[] or <c>null</c> if the next JSON token is null. This method will return <c>null</c> at the end of an array.</returns>
    public override byte[]? ReadAsBytes()
    {
        var isWrapped = false;

        switch (currentState)
        {
            case State.PostValue:
                if (ParsePostValue(true))
                {
                    return null;
                }

                goto case State.Start;
            case State.Start:
            case State.Property:
            case State.Array:
            case State.ArrayStart:
                while (true)
                {
                    var currentChar = charBuffer[charPos];

                    switch (currentChar)
                    {
                        case '\0':
                            if (ReadNullChar())
                            {
                                SetNoneToken();
                                return null;
                            }

                            break;
                        case '"':
                        case '\'':
                            ParseString(currentChar, ReadType.ReadAsBytes);
                            var data = (byte[]?) Value;
                            if (isWrapped)
                            {
                                ReaderReadAndAssert();
                                if (TokenType != JsonToken.EndObject)
                                {
                                    throw JsonReaderException.Create(this, $"Error reading bytes. Unexpected token: {TokenType}.");
                                }

                                SetToken(data);
                            }

                            return data;
                        case '{':
                            charPos++;
                            SetToken(JsonToken.StartObject);
                            ReadIntoWrappedTypeObject();
                            isWrapped = true;
                            break;
                        case '[':
                            charPos++;
                            SetToken(JsonToken.StartArray);
                            return ReadArrayIntoByteArray();
                        case 'n':
                            HandleNull();
                            return null;
                        case '/':
                            ParseComment(false);
                            break;
                        case ',':
                            ProcessValueComma();
                            break;
                        case ']':
                            charPos++;
                            if (currentState is State.Array or State.ArrayStart or State.PostValue)
                            {
                                SetToken(JsonToken.EndArray);
                                return null;
                            }

                            throw CreateUnexpectedCharacterException(currentChar);
                        case StringUtils.CarriageReturn:
                            ProcessCarriageReturn(false);
                            break;
                        case StringUtils.LineFeed:
                            ProcessLineFeed();
                            break;
                        case ' ':
                        case StringUtils.Tab:
                            // eat
                            charPos++;
                            break;
                        default:
                            charPos++;

                            if (!char.IsWhiteSpace(currentChar))
                            {
                                throw CreateUnexpectedCharacterException(currentChar);
                            }

                            // eat
                            break;
                    }
                }
            case State.Finished:
                ReadFinished();
                return null;
            default:
                throw JsonReaderException.Create(this, $"Unexpected state: {CurrentState}.");
        }
    }

    object? ReadStringValue(ReadType readType)
    {
        switch (currentState)
        {
            case State.PostValue:
                if (ParsePostValue(true))
                {
                    return null;
                }

                goto case State.Start;
            case State.Start:
            case State.Property:
            case State.Array:
            case State.ArrayStart:
                while (true)
                {
                    var currentChar = charBuffer[charPos];

                    switch (currentChar)
                    {
                        case '\0':
                            if (ReadNullChar())
                            {
                                SetNoneToken();
                                return null;
                            }

                            break;
                        case '"':
                        case '\'':
                            ParseString(currentChar, readType);
                            return FinishReadQuotedStringValue(readType);
                        case '-':
                            if (EnsureChars(1, true) && charBuffer[charPos + 1] == 'I')
                            {
                                return ParseNumberNegativeInfinity(readType);
                            }

                            ParseNumber(readType);
                            return Value;
                        case '.':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            if (readType != ReadType.ReadAsString)
                            {
                                charPos++;
                                throw CreateUnexpectedCharacterException(currentChar);
                            }

                            ParseNumber(ReadType.ReadAsString);
                            return Value;
                        case 't':
                        case 'f':
                            if (readType != ReadType.ReadAsString)
                            {
                                charPos++;
                                throw CreateUnexpectedCharacterException(currentChar);
                            }

                            var expected = currentChar == 't' ? JsonConvert.True : JsonConvert.False;
                            if (!MatchValueWithTrailingSeparator(expected))
                            {
                                throw CreateUnexpectedCharacterException(charBuffer[charPos]);
                            }

                            SetToken(JsonToken.String, expected);
                            return expected;
                        case 'I':
                            return ParseNumberPositiveInfinity(readType);
                        case 'N':
                            return ParseNumberNaN(readType);
                        case 'n':
                            HandleNull();
                            return null;
                        case '/':
                            ParseComment(false);
                            break;
                        case ',':
                            ProcessValueComma();
                            break;
                        case ']':
                            charPos++;
                            if (currentState is
                                State.Array or
                                State.ArrayStart or
                                State.PostValue)
                            {
                                SetToken(JsonToken.EndArray);
                                return null;
                            }

                            throw CreateUnexpectedCharacterException(currentChar);
                        case StringUtils.CarriageReturn:
                            ProcessCarriageReturn(false);
                            break;
                        case StringUtils.LineFeed:
                            ProcessLineFeed();
                            break;
                        case ' ':
                        case StringUtils.Tab:
                            // eat
                            charPos++;
                            break;
                        default:
                            charPos++;

                            if (!char.IsWhiteSpace(currentChar))
                            {
                                throw CreateUnexpectedCharacterException(currentChar);
                            }

                            // eat
                            break;
                    }
                }
            case State.Finished:
                ReadFinished();
                return null;
            default:
                throw JsonReaderException.Create(this, $"Unexpected state: {CurrentState}.");
        }
    }

    object? FinishReadQuotedStringValue(ReadType readType)
    {
        switch (readType)
        {
            case ReadType.ReadAsBytes:
            case ReadType.ReadAsString:
                return Value;
            case ReadType.ReadAsDateTime:
                if (Value is DateTime time)
                {
                    return time;
                }

                return ReadDateTimeString((string?) Value);
            case ReadType.ReadAsDateTimeOffset:
                if (Value is DateTimeOffset offset)
                {
                    return offset;
                }

                return ReadDateTimeOffsetString((string?) Value);
            default:
                throw new ArgumentOutOfRangeException(nameof(readType));
        }
    }

    JsonReaderException CreateUnexpectedCharacterException(char c) =>
        JsonReaderException.Create(this, $"Unexpected character encountered while parsing value: {c}.");

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="TextReader" /> as a <see cref="Nullable{T}" /> of <see cref="Boolean" />.
    /// </summary>
    /// <returns>A <see cref="Nullable{T}" /> of <see cref="Boolean" />. This method will return <c>null</c> at the end of an array.</returns>
    public override bool? ReadAsBoolean()
    {
        switch (currentState)
        {
            case State.PostValue:
                if (ParsePostValue(true))
                {
                    return null;
                }

                goto case State.Start;
            case State.Start:
            case State.Property:
            case State.Array:
            case State.ArrayStart:
                while (true)
                {
                    var currentChar = charBuffer[charPos];

                    switch (currentChar)
                    {
                        case '\0':
                            if (ReadNullChar())
                            {
                                SetNoneToken();
                                return null;
                            }

                            break;
                        case '"':
                        case '\'':
                            // ReadAsBoolean skips the intermediate string token; the span is re-parsed below
                            ParseString(currentChar, ReadType.ReadAsBoolean);
                            return ReadBooleanString(stringReference.AsSpan());
                        case 'n':
                            HandleNull();
                            return null;
                        case '-':
                        case '.':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            ParseNumber(ReadType.Read);
                            bool b;
                            if (Value is BigInteger integer)
                            {
                                b = integer != 0;
                            }
                            else
                            {
                                b = Convert.ToBoolean(Value, InvariantCulture);
                            }

                            SetToken(b);
                            return b;
                        case 't':
                        case 'f':
                            var isTrue = currentChar == 't';
                            var expected = isTrue ? JsonConvert.True : JsonConvert.False;

                            if (!MatchValueWithTrailingSeparator(expected))
                            {
                                throw CreateUnexpectedCharacterException(charBuffer[charPos]);
                            }

                            SetToken(JsonToken.Boolean, BoxedPrimitives.Get(isTrue));
                            return isTrue;
                        case '/':
                            ParseComment(false);
                            break;
                        case ',':
                            ProcessValueComma();
                            break;
                        case ']':
                            charPos++;
                            if (currentState is State.Array or State.ArrayStart or State.PostValue)
                            {
                                SetToken(JsonToken.EndArray);
                                return null;
                            }

                            throw CreateUnexpectedCharacterException(currentChar);
                        case StringUtils.CarriageReturn:
                            ProcessCarriageReturn(false);
                            break;
                        case StringUtils.LineFeed:
                            ProcessLineFeed();
                            break;
                        case ' ':
                        case StringUtils.Tab:
                            // eat
                            charPos++;
                            break;
                        default:
                            charPos++;

                            if (!char.IsWhiteSpace(currentChar))
                            {
                                throw CreateUnexpectedCharacterException(currentChar);
                            }

                            // eat
                            break;
                    }
                }
            case State.Finished:
                ReadFinished();
                return null;
            default:
                throw JsonReaderException.Create(this, $"Unexpected state: {CurrentState}.");
        }
    }

    void ProcessValueComma()
    {
        charPos++;

        if (currentState != State.PostValue)
        {
            SetToken(JsonToken.Undefined);
            var ex = CreateUnexpectedCharacterException(',');
            // so the comma will be parsed again
            charPos--;

            throw ex;
        }

        SetStateBasedOnCurrent();
    }

    object? ReadNumberValue(ReadType readType)
    {
        switch (currentState)
        {
            case State.PostValue:
                if (ParsePostValue(true))
                {
                    return null;
                }

                goto case State.Start;
            case State.Start:
            case State.Property:
            case State.Array:
            case State.ArrayStart:
                while (true)
                {
                    var currentChar = charBuffer[charPos];

                    switch (currentChar)
                    {
                        case '\0':
                            if (ReadNullChar())
                            {
                                SetNoneToken();
                                return null;
                            }

                            break;
                        case '"':
                        case '\'':
                            ParseString(currentChar, readType);
                            return FinishReadQuotedNumber(readType);
                        case 'n':
                            HandleNull();
                            return null;
                        case 'N':
                            return ParseNumberNaN(readType);
                        case 'I':
                            return ParseNumberPositiveInfinity(readType);
                        case '-':
                            if (EnsureChars(1, true) && charBuffer[charPos + 1] == 'I')
                            {
                                return ParseNumberNegativeInfinity(readType);
                            }

                            ParseNumber(readType);
                            return Value;
                        case '.':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            ParseNumber(readType);
                            return Value;
                        case '/':
                            ParseComment(false);
                            break;
                        case ',':
                            ProcessValueComma();
                            break;
                        case ']':
                            charPos++;
                            if (currentState is State.Array or State.ArrayStart or State.PostValue)
                            {
                                SetToken(JsonToken.EndArray);
                                return null;
                            }

                            throw CreateUnexpectedCharacterException(currentChar);
                        case StringUtils.CarriageReturn:
                            ProcessCarriageReturn(false);
                            break;
                        case StringUtils.LineFeed:
                            ProcessLineFeed();
                            break;
                        case ' ':
                        case StringUtils.Tab:
                            // eat
                            charPos++;
                            break;
                        default:
                            charPos++;

                            if (!char.IsWhiteSpace(currentChar))
                            {
                                throw CreateUnexpectedCharacterException(currentChar);
                            }

                            // eat
                            break;
                    }
                }
            case State.Finished:
                ReadFinished();
                return null;
            default:
                throw JsonReaderException.Create(this, $"Unexpected state: {CurrentState}.");
        }
    }

    object? FinishReadQuotedNumber(ReadType readType)
    {
        switch (readType)
        {
            case ReadType.ReadAsInt32:
                return ReadInt32String(stringReference.AsSpan());
            case ReadType.ReadAsDecimal:
                return ReadDecimalString(stringReference.AsSpan());
            case ReadType.ReadAsDouble:
                return ReadDoubleString(stringReference.AsSpan());
            default:
                throw new ArgumentOutOfRangeException(nameof(readType));
        }
    }

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="TextReader" /> as a <see cref="Nullable{T}" /> of <see cref="DateTimeOffset" />.
    /// </summary>
    /// <returns>A <see cref="Nullable{T}" /> of <see cref="DateTimeOffset" />. This method will return <c>null</c> at the end of an array.</returns>
    public override DateTimeOffset? ReadAsDateTimeOffset() =>
        (DateTimeOffset?) ReadStringValue(ReadType.ReadAsDateTimeOffset);

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="TextReader" /> as a <see cref="Nullable{T}" /> of <see cref="Decimal" />.
    /// </summary>
    /// <returns>A <see cref="Nullable{T}" /> of <see cref="Decimal" />. This method will return <c>null</c> at the end of an array.</returns>
    public override decimal? ReadAsDecimal() =>
        (decimal?) ReadNumberValue(ReadType.ReadAsDecimal);

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="TextReader" /> as a <see cref="Nullable{T}" /> of <see cref="Double" />.
    /// </summary>
    /// <returns>A <see cref="Nullable{T}" /> of <see cref="Double" />. This method will return <c>null</c> at the end of an array.</returns>
    public override double? ReadAsDouble() =>
        (double?) ReadNumberValue(ReadType.ReadAsDouble);

    void HandleNull()
    {
        if (EnsureChars(1, true))
        {
            var next = charBuffer[charPos + 1];

            if (next == 'u')
            {
                ParseNull();
                return;
            }

            charPos += 2;
            throw CreateUnexpectedCharacterException(charBuffer[charPos - 1]);
        }

        charPos = charsUsed;
        throw CreateUnexpectedEndException();
    }

    void ReadFinished()
    {
        if (EnsureChars(0, false))
        {
            EatWhitespace();
            if (isEndOfFile)
            {
                return;
            }

            if (charBuffer[charPos] == '/')
            {
                ParseComment(false);
            }
            else
            {
                throw JsonReaderException.Create(this, $"Additional text encountered after finished reading JSON content: {charBuffer[charPos]}.");
            }
        }

        SetToken(JsonToken.None);
    }

    bool ReadNullChar()
    {
        if (charsUsed == charPos)
        {
            if (ReadData(false) == 0)
            {
                isEndOfFile = true;
                return true;
            }
        }
        else
        {
            charPos++;
        }

        return false;
    }

    void ReadStringIntoBuffer(char quote)
    {
        var charPos = this.charPos;
        var initialPosition = this.charPos;
        var lastWritePosition = this.charPos;
        stringBuffer.Position = 0;

        while (true)
        {
            switch (charBuffer[charPos++])
            {
                case '\0':
                    if (charsUsed == charPos - 1)
                    {
                        charPos--;

                        if (ReadData(true) == 0)
                        {
                            this.charPos = charPos;
                            throw JsonReaderException.Create(this, $"Unterminated string. Expected delimiter: {quote}.");
                        }
                    }

                    break;
                case '\\':
                    this.charPos = charPos;
                    if (!EnsureChars(0, true))
                    {
                        throw JsonReaderException.Create(this, $"Unterminated string. Expected delimiter: {quote}.");
                    }

                    // start of escape sequence
                    var escapeStartPos = charPos - 1;

                    var currentChar = charBuffer[charPos];
                    charPos++;

                    char writeChar;

                    switch (currentChar)
                    {
                        case 'b':
                            writeChar = '\b';
                            break;
                        case 't':
                            writeChar = '\t';
                            break;
                        case 'n':
                            writeChar = '\n';
                            break;
                        case 'f':
                            writeChar = '\f';
                            break;
                        case 'r':
                            writeChar = '\r';
                            break;
                        case '\\':
                            writeChar = '\\';
                            break;
                        case '"':
                        case '\'':
                        case '/':
                            writeChar = currentChar;
                            break;
                        case 'u':
                            this.charPos = charPos;
                            writeChar = ParseUnicode();

                            if (char.IsLowSurrogate(writeChar))
                            {
                                // low surrogate with no preceding high surrogate; this char is replaced
                                writeChar = unicodeReplacementChar;
                            }
                            else if (char.IsHighSurrogate(writeChar))
                            {
                                bool anotherHighSurrogate;

                                // loop for handling situations where there are multiple consecutive high surrogates
                                do
                                {
                                    anotherHighSurrogate = false;

                                    // potential start of a surrogate pair
                                    if (EnsureChars(2, true) && charBuffer[this.charPos] == '\\' && charBuffer[this.charPos + 1] == 'u')
                                    {
                                        var highSurrogate = writeChar;

                                        this.charPos += 2;
                                        writeChar = ParseUnicode();

                                        if (char.IsLowSurrogate(writeChar))
                                        {
                                            // a valid surrogate pair!
                                        }
                                        else if (char.IsHighSurrogate(writeChar))
                                        {
                                            // another high surrogate; replace current and start check over
                                            highSurrogate = unicodeReplacementChar;
                                            anotherHighSurrogate = true;
                                        }
                                        else
                                        {
                                            // high surrogate not followed by low surrogate; original char is replaced
                                            highSurrogate = unicodeReplacementChar;
                                        }

                                        EnsureBufferNotEmpty();

                                        WriteCharToBuffer(highSurrogate, lastWritePosition, escapeStartPos);
                                        lastWritePosition = this.charPos;
                                    }
                                    else
                                    {
                                        // there are not enough remaining chars for the low surrogate or is not follow by unicode sequence
                                        // replace high surrogate and continue on as usual
                                        writeChar = unicodeReplacementChar;
                                    }
                                } while (anotherHighSurrogate);
                            }

                            charPos = this.charPos;
                            break;
                        default:
                            this.charPos = charPos;
                            throw JsonReaderException.Create(this, $"Bad JSON escape sequence: \\{currentChar}.");
                    }

                    EnsureBufferNotEmpty();
                    WriteCharToBuffer(writeChar, lastWritePosition, escapeStartPos);

                    lastWritePosition = charPos;
                    break;
                case StringUtils.CarriageReturn:
                    this.charPos = charPos - 1;
                    ProcessCarriageReturn(true);
                    charPos = this.charPos;
                    break;
                case StringUtils.LineFeed:
                    this.charPos = charPos - 1;
                    ProcessLineFeed();
                    charPos = this.charPos;
                    break;
                case '"':
                case '\'':
                    if (charBuffer[charPos - 1] == quote)
                    {
                        FinishReadStringIntoBuffer(charPos - 1, initialPosition, lastWritePosition);
                        return;
                    }

                    break;
            }
        }
    }

    void FinishReadStringIntoBuffer(int charPos, int initialPosition, int lastWritePosition)
    {
        if (initialPosition == lastWritePosition)
        {
            stringReference = new(charBuffer, initialPosition, charPos - initialPosition);
        }
        else
        {
            EnsureBufferNotEmpty();

            if (charPos > lastWritePosition)
            {
                stringBuffer.Append(charBuffer, lastWritePosition, charPos - lastWritePosition);
            }

            stringReference = new(stringBuffer.InternalBuffer!, 0, stringBuffer.Position);
        }

        this.charPos = charPos + 1;
    }

    void WriteCharToBuffer(char writeChar, int lastWritePosition, int writeToPosition)
    {
        if (writeToPosition > lastWritePosition)
        {
            stringBuffer.Append(charBuffer, lastWritePosition, writeToPosition - lastWritePosition);
        }

        stringBuffer.Append(writeChar);
    }

    char ConvertUnicode(bool enoughChars)
    {
        if (enoughChars)
        {
            if (ConvertUtils.TryHexTextToInt(charBuffer, charPos, charPos + 4, out var value))
            {
                var hexChar = Convert.ToChar(value);
                charPos += 4;
                return hexChar;
            }

            throw JsonReaderException.Create(this, $@"Invalid Unicode escape sequence: \u{new string(charBuffer, charPos, 4)}.");
        }

        throw JsonReaderException.Create(this, "Unexpected end while parsing Unicode escape sequence.");
    }

    char ParseUnicode() =>
        ConvertUnicode(EnsureChars(4, true));

    void ReadNumberIntoBuffer()
    {
        var charPos = this.charPos;

        while (true)
        {
            var currentChar = charBuffer[charPos];
            if (currentChar == '\0')
            {
                this.charPos = charPos;

                if (charsUsed == charPos)
                {
                    if (ReadData(true) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else if (ReadNumberCharIntoBuffer(currentChar, charPos))
            {
                return;
            }
            else
            {
                charPos++;
            }
        }
    }

    bool ReadNumberCharIntoBuffer(char currentChar, int charPos)
    {
        switch (currentChar)
        {
            case '-':
            case '+':
            case 'a':
            case 'A':
            case 'b':
            case 'B':
            case 'c':
            case 'C':
            case 'd':
            case 'D':
            case 'e':
            case 'E':
            case 'f':
            case 'F':
            case 'x':
            case 'X':
            case '.':
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                return false;
            default:
                this.charPos = charPos;

                if (char.IsWhiteSpace(currentChar) || currentChar is ',' or '}' or ']' or ')' or '/')
                {
                    return true;
                }

                throw JsonReaderException.Create(this, $"Unexpected character encountered while parsing number: {currentChar}.");
        }
    }

    void ClearRecentString()
    {
        stringBuffer.Position = 0;
        stringReference = new();
    }

    bool ParsePostValue(bool ignoreComments)
    {
        while (true)
        {
            var currentChar = charBuffer[charPos];

            switch (currentChar)
            {
                case '\0':
                    if (charsUsed == charPos)
                    {
                        if (ReadData(false) == 0)
                        {
                            currentState = State.Finished;
                            return false;
                        }
                    }
                    else
                    {
                        charPos++;
                    }

                    break;
                case '}':
                    charPos++;
                    SetToken(JsonToken.EndObject);
                    return true;
                case ']':
                    charPos++;
                    SetToken(JsonToken.EndArray);
                    return true;
                case '/':
                    ParseComment(!ignoreComments);
                    if (!ignoreComments)
                    {
                        return true;
                    }

                    break;
                case ',':
                    charPos++;

                    // finished parsing
                    SetStateBasedOnCurrent();
                    return false;
                case ' ':
                case StringUtils.Tab:
                    // eat
                    charPos++;
                    break;
                case StringUtils.CarriageReturn:
                    ProcessCarriageReturn(false);
                    break;
                case StringUtils.LineFeed:
                    ProcessLineFeed();
                    break;
                default:
                    if (char.IsWhiteSpace(currentChar))
                    {
                        // eat
                        charPos++;
                    }
                    else
                    {
                        // handle multiple content without comma delimiter
                        if (SupportMultipleContent && Depth == 0)
                        {
                            SetStateBasedOnCurrent();
                            return false;
                        }

                        throw JsonReaderException.Create(this, $"After parsing a value an unexpected character was encountered: {currentChar}.");
                    }

                    break;
            }
        }
    }

    bool ParseObject()
    {
        while (true)
        {
            var currentChar = charBuffer[charPos];

            switch (currentChar)
            {
                case '\0':
                    if (charsUsed == charPos)
                    {
                        if (ReadData(false) == 0)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        charPos++;
                    }

                    break;
                case '}':
                    SetToken(JsonToken.EndObject);
                    charPos++;
                    return true;
                case '/':
                    ParseComment(true);
                    return true;
                case StringUtils.CarriageReturn:
                    ProcessCarriageReturn(false);
                    break;
                case StringUtils.LineFeed:
                    ProcessLineFeed();
                    break;
                case ' ':
                case StringUtils.Tab:
                    // eat
                    charPos++;
                    break;
                default:
                    if (char.IsWhiteSpace(currentChar))
                    {
                        // eat
                        charPos++;
                    }
                    else
                    {
                        return ParseProperty();
                    }

                    break;
            }
        }
    }

    bool ParseProperty()
    {
        var firstChar = charBuffer[charPos];
        char quoteChar;

        if (firstChar is '"' or '\'')
        {
            charPos++;
            quoteChar = firstChar;
            ShiftBufferIfNeeded();
            ReadStringIntoBuffer(quoteChar);
        }
        else if (ValidIdentifierChar(firstChar))
        {
            quoteChar = '\0';
            ShiftBufferIfNeeded();
            ParseUnquotedProperty();
        }
        else
        {
            throw JsonReaderException.Create(this, $"Invalid property identifier character: {charBuffer[charPos]}.");
        }

        var propertyName = PropertyNameTable?.Get(stringReference.Chars, stringReference.StartIndex, stringReference.Length) ??
                           // no match in name table
                           stringReference.ToString();

        EatWhitespace();

        if (charBuffer[charPos] != ':')
        {
            throw JsonReaderException.Create(this, $"Invalid character after parsing property name. Expected ':' but got: {charBuffer[charPos]}.");
        }

        charPos++;

        SetPropertyToken(propertyName);
        this.quoteChar = quoteChar;
        ClearRecentString();

        return true;
    }

    static bool ValidIdentifierChar(char value) =>
        char.IsLetterOrDigit(value) || value is '_' or '$';

    void ParseUnquotedProperty()
    {
        var initialPosition = charPos;

        // parse unquoted property name until whitespace or colon
        while (true)
        {
            var currentChar = charBuffer[charPos];
            if (currentChar == '\0')
            {
                if (charsUsed == charPos)
                {
                    if (ReadData(true) == 0)
                    {
                        throw JsonReaderException.Create(this, "Unexpected end while parsing unquoted property name.");
                    }

                    continue;
                }

                stringReference = new(charBuffer, initialPosition, charPos - initialPosition);
                return;
            }

            if (ReadUnquotedPropertyReportIfDone(currentChar, initialPosition))
            {
                return;
            }
        }
    }

    bool ReadUnquotedPropertyReportIfDone(char currentChar, int initialPosition)
    {
        if (ValidIdentifierChar(currentChar))
        {
            charPos++;
            return false;
        }

        if (char.IsWhiteSpace(currentChar) || currentChar == ':')
        {
            stringReference = new(charBuffer, initialPosition, charPos - initialPosition);
            return true;
        }

        throw JsonReaderException.Create(this, $"Invalid JavaScript property identifier character: {currentChar}.");
    }

    bool ParseValue()
    {
        while (true)
        {
            var currentChar = charBuffer[charPos];

            switch (currentChar)
            {
                case '\0':
                    if (charsUsed == charPos)
                    {
                        if (ReadData(false) == 0)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        charPos++;
                    }

                    break;
                case '"':
                case '\'':
                    ParseString(currentChar, ReadType.Read);
                    return true;
                case 't':
                    ParseTrue();
                    return true;
                case 'f':
                    ParseFalse();
                    return true;
                case 'n':
                    if (EnsureChars(1, true))
                    {
                        var next = charBuffer[charPos + 1];

                        if (next == 'u')
                        {
                            ParseNull();
                        }
                        else
                        {
                            throw CreateUnexpectedCharacterException(charBuffer[charPos]);
                        }
                    }
                    else
                    {
                        charPos++;
                        throw CreateUnexpectedEndException();
                    }

                    return true;
                case 'N':
                    ParseNumberNaN(ReadType.Read);
                    return true;
                case 'I':
                    ParseNumberPositiveInfinity(ReadType.Read);
                    return true;
                case '-':
                    if (EnsureChars(1, true) && charBuffer[charPos + 1] == 'I')
                    {
                        ParseNumberNegativeInfinity(ReadType.Read);
                    }
                    else
                    {
                        ParseNumber(ReadType.Read);
                    }

                    return true;
                case '/':
                    ParseComment(true);
                    return true;
                case 'u':
                    ParseUndefined();
                    return true;
                case '{':
                    charPos++;
                    SetToken(JsonToken.StartObject);
                    return true;
                case '[':
                    charPos++;
                    SetToken(JsonToken.StartArray);
                    return true;
                case ']':
                    charPos++;
                    SetToken(JsonToken.EndArray);
                    return true;
                case ',':
                    // don't increment position, the next call to read will handle comma
                    // this is done to handle multiple empty comma values
                    SetToken(JsonToken.Undefined);
                    return true;
                case StringUtils.CarriageReturn:
                    ProcessCarriageReturn(false);
                    break;
                case StringUtils.LineFeed:
                    ProcessLineFeed();
                    break;
                case ' ':
                case StringUtils.Tab:
                    // eat
                    charPos++;
                    break;
                default:
                    if (char.IsWhiteSpace(currentChar))
                    {
                        // eat
                        charPos++;
                        break;
                    }

                    if (char.IsNumber(currentChar) || currentChar is '-' or '.')
                    {
                        ParseNumber(ReadType.Read);
                        return true;
                    }

                    throw CreateUnexpectedCharacterException(currentChar);
            }
        }
    }

    void ProcessLineFeed()
    {
        charPos++;
        OnNewLine(charPos);
    }

    void ProcessCarriageReturn(bool append)
    {
        charPos++;

        SetNewLine(EnsureChars(1, append));
    }

    void EatWhitespace()
    {
        while (true)
        {
            var currentChar = charBuffer[charPos];

            switch (currentChar)
            {
                case '\0':
                    if (charsUsed == charPos)
                    {
                        if (ReadData(false) == 0)
                        {
                            return;
                        }
                    }
                    else
                    {
                        charPos++;
                    }

                    break;
                case StringUtils.CarriageReturn:
                    ProcessCarriageReturn(false);
                    break;
                case StringUtils.LineFeed:
                    ProcessLineFeed();
                    break;
                default:
                    if (currentChar == ' ' || char.IsWhiteSpace(currentChar))
                    {
                        charPos++;
                    }
                    else
                    {
                        return;
                    }

                    break;
            }
        }
    }

    void ParseNumber(ReadType readType)
    {
        ShiftBufferIfNeeded();

        var firstChar = charBuffer[charPos];
        var initialPosition = charPos;

        ReadNumberIntoBuffer();

        ParseReadNumber(readType, firstChar, initialPosition);
    }

    void ParseReadNumber(ReadType readType, char firstChar, int initialPosition)
    {
        // set state to PostValue now so that if there is an error parsing the number then the reader can continue
        SetPostValueState(true);

        stringReference = new(charBuffer, initialPosition, charPos - initialPosition);

        object numberValue;
        JsonToken numberType;

        var singleDigit = char.IsDigit(firstChar) && stringReference.Length == 1;
        switch (readType)
        {
            case ReadType.ReadAsString:
            {
                var number = stringReference.ToString();

                if (!double.TryParse(number, NumberStyles.Float, InvariantCulture, out _))
                {
                    throw ThrowReaderError($"Input string '{stringReference}' is not a valid number.");
                }

                numberType = JsonToken.String;
                numberValue = number;
            }
                break;
            case ReadType.ReadAsInt32:
            {
                if (singleDigit)
                {
                    // digit char values start at 48
                    numberValue = BoxedPrimitives.Get(firstChar - 48);
                }
                else
                {
                    var parseResult = ConvertUtils.Int32TryParse(stringReference.Chars, stringReference.StartIndex, stringReference.Length, out var value);
                    if (parseResult == ParseResult.Success)
                    {
                        numberValue = BoxedPrimitives.Get(value);
                    }
                    else if (parseResult == ParseResult.Overflow)
                    {
                        throw ThrowReaderError($"JSON integer {stringReference} is too large or small for an Int32.");
                    }
                    else
                    {
                        throw ThrowReaderError($"Input string '{stringReference}' is not a valid integer.");
                    }
                }

                numberType = JsonToken.Integer;
            }
                break;
            case ReadType.ReadAsDecimal:
            {
                if (singleDigit)
                {
                    // digit char values start at 48
                    numberValue = BoxedPrimitives.Get((decimal) firstChar - 48);
                }
                else
                {
                    var parseResult = ConvertUtils.DecimalTryParse(stringReference.Chars, stringReference.StartIndex, stringReference.Length, out var value);
                    if (parseResult == ParseResult.Success)
                    {
                        numberValue = BoxedPrimitives.Get(value);
                    }
                    else
                    {
                        throw ThrowReaderError($"Input string '{stringReference}' is not a valid decimal.");
                    }
                }

                numberType = JsonToken.Float;
            }
                break;
            case ReadType.ReadAsDouble:
            {
                if (singleDigit)
                {
                    // digit char values start at 48
                    numberValue = BoxedPrimitives.Get((double) firstChar - 48);
                }
                else
                {
                    if (double.TryParse(stringReference.AsSpan(), NumberStyles.Float, InvariantCulture, out var value))
                    {
                        numberValue = BoxedPrimitives.Get(value);
                    }
                    else
                    {
                        throw ThrowReaderError($"Input string '{stringReference}' is not a valid double.");
                    }
                }

                numberType = JsonToken.Float;
            }
                break;
            case ReadType.Read:
            case ReadType.ReadAsInt64:
            {
                if (singleDigit)
                {
                    // digit char values start at 48
                    numberValue = BoxedPrimitives.Get((long) firstChar - 48);
                    numberType = JsonToken.Integer;
                }
                else
                {
                    var parseResult = ConvertUtils.Int64TryParse(stringReference.Chars, stringReference.StartIndex, stringReference.Length, out var value);
                    if (parseResult == ParseResult.Success)
                    {
                        numberValue = BoxedPrimitives.Get(value);
                        numberType = JsonToken.Integer;
                    }
                    else if (parseResult == ParseResult.Overflow)
                    {
                        var number = stringReference.ToString();

                        if (number.Length > maximumJavascriptIntegerCharacterLength)
                        {
                            throw ThrowReaderError($"JSON integer {stringReference} is too large to parse.");
                        }

                        numberValue = BigInteger.Parse(number, InvariantCulture);
                        numberType = JsonToken.Integer;
                    }
                    else
                    {
                        if (FloatParseHandling == FloatParseHandling.Decimal)
                        {
                            parseResult = ConvertUtils.DecimalTryParse(stringReference.Chars, stringReference.StartIndex, stringReference.Length, out var d);
                            if (parseResult == ParseResult.Success)
                            {
                                numberValue = BoxedPrimitives.Get(d);
                            }
                            else
                            {
                                throw ThrowReaderError($"Input string '{stringReference}' is not a valid decimal.");
                            }
                        }
                        else
                        {
                            if (double.TryParse(stringReference.AsSpan(), NumberStyles.Float, InvariantCulture, out var d))
                            {
                                numberValue = BoxedPrimitives.Get(d);
                            }
                            else
                            {
                                throw ThrowReaderError($"Input string '{stringReference}' is not a valid number.");
                            }
                        }

                        numberType = JsonToken.Float;
                    }
                }
            }
                break;
            default:
                throw JsonReaderException.Create(this, "Cannot read number value as type.");
        }

        ClearRecentString();

        // index has already been updated
        SetToken(numberType, numberValue, false);
    }

    JsonReaderException ThrowReaderError(string message, Exception? ex = null)
    {
        SetUndefinedToken();
        return JsonReaderException.Create(this, message, ex);
    }

    void ParseComment(bool setToken)
    {
        // should have already parsed / character before reaching this method
        charPos++;

        if (!EnsureChars(1, false))
        {
            throw JsonReaderException.Create(this, "Unexpected end while parsing comment.");
        }

        bool singleLineComment;

        if (charBuffer[charPos] == '*')
        {
            singleLineComment = false;
        }
        else if (charBuffer[charPos] == '/')
        {
            singleLineComment = true;
        }
        else
        {
            throw JsonReaderException.Create(this, $"Error parsing comment. Expected: *, got {charBuffer[charPos]}.");
        }

        charPos++;

        var initialPosition = charPos;

        while (true)
        {
            switch (charBuffer[charPos])
            {
                case '\0':
                    if (charsUsed == charPos)
                    {
                        if (ReadData(true) == 0)
                        {
                            if (!singleLineComment)
                            {
                                throw JsonReaderException.Create(this, "Unexpected end while parsing comment.");
                            }

                            EndComment(setToken, initialPosition, charPos);
                            return;
                        }
                    }
                    else
                    {
                        charPos++;
                    }

                    break;
                case '*':
                    charPos++;

                    if (!singleLineComment)
                    {
                        if (EnsureChars(0, true))
                        {
                            if (charBuffer[charPos] == '/')
                            {
                                EndComment(setToken, initialPosition, charPos - 1);

                                charPos++;
                                return;
                            }
                        }
                    }

                    break;
                case StringUtils.CarriageReturn:
                    if (singleLineComment)
                    {
                        EndComment(setToken, initialPosition, charPos);
                        return;
                    }

                    ProcessCarriageReturn(true);
                    break;
                case StringUtils.LineFeed:
                    if (singleLineComment)
                    {
                        EndComment(setToken, initialPosition, charPos);
                        return;
                    }

                    ProcessLineFeed();
                    break;
                default:
                    charPos++;
                    break;
            }
        }
    }

    void EndComment(bool setToken, int initialPosition, int endPosition)
    {
        if (setToken)
        {
            SetToken(JsonToken.Comment, new string(charBuffer, initialPosition, endPosition - initialPosition));
        }
    }

    bool MatchValue(string value) =>
        MatchValue(EnsureChars(value.Length - 1, true), value);

    bool MatchValue(bool enoughChars, string value)
    {
        if (!enoughChars)
        {
            charPos = charsUsed;
            throw CreateUnexpectedEndException();
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (charBuffer[charPos + i] != value[i])
            {
                charPos += i;
                return false;
            }
        }

        charPos += value.Length;

        return true;
    }

    bool MatchValueWithTrailingSeparator(string value)
    {
        // will match value and then move to the next character, checking that it is a separator character
        var match = MatchValue(value);

        if (!match)
        {
            return false;
        }

        if (!EnsureChars(0, false))
        {
            return true;
        }

        //TODO
        return IsSeparator(charBuffer[charPos]) || charBuffer[charPos] == '\0';
    }

    bool IsSeparator(char c)
    {
        switch (c)
        {
            case '}':
            case ']':
            case ',':
                return true;
            case '/':
                // check next character to see if start of a comment
                if (!EnsureChars(1, false))
                {
                    return false;
                }

                var nextChart = charBuffer[charPos + 1];

                return nextChart is '*' or '/';
            case ' ':
            case StringUtils.Tab:
            case StringUtils.LineFeed:
            case StringUtils.CarriageReturn:
                return true;
            default:
                if (char.IsWhiteSpace(c))
                {
                    return true;
                }

                break;
        }

        return false;
    }

    void ParseTrue()
    {
        // check characters equal 'true'
        // and that it is followed by either a separator character
        // or the text ends
        if (MatchValueWithTrailingSeparator(JsonConvert.True))
        {
            SetToken(JsonToken.Boolean, BoxedPrimitives.BooleanTrue);
        }
        else
        {
            throw JsonReaderException.Create(this, "Error parsing boolean value.");
        }
    }

    void ParseNull()
    {
        if (MatchValueWithTrailingSeparator(JsonConvert.Null))
        {
            SetToken(JsonToken.Null);
        }
        else
        {
            throw JsonReaderException.Create(this, "Error parsing null value.");
        }
    }

    void ParseUndefined()
    {
        if (MatchValueWithTrailingSeparator(JsonConvert.Undefined))
        {
            SetToken(JsonToken.Undefined);
        }
        else
        {
            throw JsonReaderException.Create(this, "Error parsing undefined value.");
        }
    }

    void ParseFalse()
    {
        if (MatchValueWithTrailingSeparator(JsonConvert.False))
        {
            SetToken(JsonToken.Boolean, BoxedPrimitives.BooleanFalse);
        }
        else
        {
            throw JsonReaderException.Create(this, "Error parsing boolean value.");
        }
    }

    object ParseNumberNegativeInfinity(ReadType readType) =>
        ParseNumberNegativeInfinity(readType, MatchValueWithTrailingSeparator(JsonConvert.NegativeInfinity));

    object ParseNumberNegativeInfinity(ReadType readType, bool matched)
    {
        if (matched)
        {
            switch (readType)
            {
                case ReadType.Read:
                case ReadType.ReadAsDouble:
                    if (FloatParseHandling == FloatParseHandling.Double)
                    {
                        SetToken(JsonToken.Float, BoxedPrimitives.DoubleNegativeInfinity);
                        return double.NegativeInfinity;
                    }

                    break;
                case ReadType.ReadAsString:
                    SetToken(JsonToken.String, JsonConvert.NegativeInfinity);
                    return JsonConvert.NegativeInfinity;
            }

            throw JsonReaderException.Create(this, "Cannot read -Infinity value.");
        }

        throw JsonReaderException.Create(this, "Error parsing -Infinity value.");
    }

    object ParseNumberPositiveInfinity(ReadType readType) =>
        ParseNumberPositiveInfinity(readType, MatchValueWithTrailingSeparator(JsonConvert.PositiveInfinity));

    object ParseNumberPositiveInfinity(ReadType readType, bool matched)
    {
        if (matched)
        {
            switch (readType)
            {
                case ReadType.Read:
                case ReadType.ReadAsDouble:
                    if (FloatParseHandling == FloatParseHandling.Double)
                    {
                        SetToken(JsonToken.Float, BoxedPrimitives.DoublePositiveInfinity);
                        return double.PositiveInfinity;
                    }

                    break;
                case ReadType.ReadAsString:
                    SetToken(JsonToken.String, JsonConvert.PositiveInfinity);
                    return JsonConvert.PositiveInfinity;
            }

            throw JsonReaderException.Create(this, "Cannot read Infinity value.");
        }

        throw JsonReaderException.Create(this, "Error parsing Infinity value.");
    }

    object ParseNumberNaN(ReadType readType) =>
        ParseNumberNaN(readType, MatchValueWithTrailingSeparator(JsonConvert.NaN));

    object ParseNumberNaN(ReadType readType, bool matched)
    {
        if (matched)
        {
            switch (readType)
            {
                case ReadType.Read:
                case ReadType.ReadAsDouble:
                    if (FloatParseHandling == FloatParseHandling.Double)
                    {
                        SetToken(JsonToken.Float, BoxedPrimitives.DoubleNaN);
                        return double.NaN;
                    }

                    break;
                case ReadType.ReadAsString:
                    SetToken(JsonToken.String, JsonConvert.NaN);
                    return JsonConvert.NaN;
            }

            throw JsonReaderException.Create(this, "Cannot read NaN value.");
        }

        throw JsonReaderException.Create(this, "Error parsing NaN value.");
    }

    /// <summary>
    /// Changes the reader's state to <see cref="JsonReader.State.Closed" />.
    /// If <see cref="JsonReader.CloseInput" /> is set to <c>true</c>, the underlying <see cref="TextReader" /> is also closed.
    /// </summary>
    public override void Close()
    {
        if (CurrentState == State.Closed)
        {
            return;
        }

        base.Close();

        BufferUtils.ReturnBuffer(charBuffer);
        charBuffer = [];

        if (CloseInput)
        {
            reader.Close();
        }

        stringBuffer.Clear();
    }

    /// <summary>
    /// Gets a value indicating whether the class can return line information.
    /// </summary>
    /// <returns>
    /// <c>true</c> if <see cref="JsonTextReader.LineNumber" /> and <see cref="JsonTextReader.LinePosition" /> can be provided; otherwise, <c>false</c>.
    /// </returns>
    public bool HasLineInfo() =>
        true;

    /// <summary>
    /// Gets the current line number.
    /// </summary>
    public int LineNumber
    {
        get
        {
            if (CurrentState == State.Start && LinePosition == 0 && TokenType != JsonToken.Comment)
            {
                return 0;
            }

            return lineNumber;
        }
    }

    /// <summary>
    /// Gets the current line position.
    /// </summary>
    public int LinePosition => charPos - lineStartPos;
}