// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class DateTimeUtils
{
    const string IsoDateFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";

    const int DaysPer100Years = 36524;
    const int DaysPer400Years = 146097;
    const int DaysPer4Years = 1461;
    const int DaysPerYear = 365;
    const long TicksPerDay = 864000000000L;
    static readonly int[] DaysToMonth365;
    static readonly int[] DaysToMonth366;

    static DateTimeUtils()
    {
        DaysToMonth365 = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365];
        DaysToMonth366 = [0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366];
    }

    public static TimeSpan GetUtcOffset(this DateTime d) =>
        TimeZoneInfo.Local.GetUtcOffset(d);

    internal static bool TryParseDateTimeOffsetIso(StringReference text, out DateTimeOffset dt)
    {
        var dateTimeParser = new DateTimeParser();
        if (!dateTimeParser.Parse(text.Chars, text.StartIndex, text.Length))
        {
            dt = default;
            return false;
        }

        var d = CreateDateTime(dateTimeParser);

        TimeSpan offset;

        switch (dateTimeParser.Zone)
        {
            case ParserTimeZone.Utc:
                offset = new(0L);
                break;
            case ParserTimeZone.LocalWestOfUtc:
                offset = new(-dateTimeParser.ZoneHour, -dateTimeParser.ZoneMinute, 0);
                break;
            case ParserTimeZone.LocalEastOfUtc:
                offset = new(dateTimeParser.ZoneHour, dateTimeParser.ZoneMinute, 0);
                break;
            default:
                offset = TimeZoneInfo.Local.GetUtcOffset(d);
                break;
        }

        // DateTimeOffset only permits offsets within +/- 14 hours. The ISO parser accepts
        // larger zone hour/minute values, so reject them here instead of throwing from the ctor.
        if (Math.Abs(offset.Ticks) > 14 * TimeSpan.TicksPerHour)
        {
            dt = default;
            return false;
        }

        var ticks = d.Ticks - offset.Ticks;
        if (ticks is < 0 or > 3155378975999999999)
        {
            dt = default;
            return false;
        }

        dt = new(d, offset);
        return true;
    }

    static DateTime CreateDateTime(DateTimeParser parser)
    {
        bool is24Hour;
        if (parser.Hour == 24)
        {
            is24Hour = true;
            parser.Hour = 0;
        }
        else
        {
            is24Hour = false;
        }

        var d = new DateTime(parser.Year, parser.Month, parser.Day, parser.Hour, parser.Minute, parser.Second);
        d = d.AddTicks(parser.Fraction);

        if (is24Hour)
        {
            d = d.AddDays(1);
        }

        return d;
    }

    static readonly int[] fractionScale = [0, 1000000, 100000, 10000, 1000, 100, 10, 1];

    enum IsoZone
    {
        None,
        Utc,
        Offset
    }

    // Parses the ISO 8601 shapes the writer produces: yyyy-MM-ddTHH:mm:ss, an optional fraction of
    // one to seven digits, then nothing, Z, or an offset of +hh:mm or -hh:mm. Anything else is
    // rejected and left to the framework. TryParseExact interprets its format string for every
    // value, which makes it several times slower than reading the fixed positions directly.
    static bool TryParseIso(CharSpan s, out long ticks, out IsoZone zone, out long offsetTicks)
    {
        ticks = 0;
        zone = IsoZone.None;
        offsetTicks = 0;

        if (s.Length < 19 ||
            s[4] != '-' ||
            s[7] != '-' ||
            s[10] != 'T' ||
            s[13] != ':' ||
            s[16] != ':')
        {
            return false;
        }

        if (!TryParse2Digits(s, 0, out var century) ||
            !TryParse2Digits(s, 2, out var yearOfCentury) ||
            !TryParse2Digits(s, 5, out var month) ||
            !TryParse2Digits(s, 8, out var day) ||
            !TryParse2Digits(s, 11, out var hour) ||
            !TryParse2Digits(s, 14, out var minute) ||
            !TryParse2Digits(s, 17, out var second))
        {
            return false;
        }

        var year = century * 100 + yearOfCentury;
        if (year < 1 ||
            month is < 1 or > 12 ||
            day < 1 ||
            hour > 23 ||
            minute > 59 ||
            second > 59 ||
            day > DateTime.DaysInMonth(year, month))
        {
            return false;
        }

        var position = 19;
        var fraction = 0;
        if (position < s.Length &&
            s[position] == '.')
        {
            position++;
            var digits = 0;
            while (position < s.Length &&
                   digits < 7)
            {
                var digit = s[position] - '0';
                if ((uint) digit > 9)
                {
                    break;
                }

                fraction = fraction * 10 + digit;
                digits++;
                position++;
            }

            if (digits == 0)
            {
                return false;
            }

            fraction *= fractionScale[digits];
        }

        if (position != s.Length)
        {
            var sign = s[position];
            if (sign == 'Z')
            {
                if (position + 1 != s.Length)
                {
                    return false;
                }

                zone = IsoZone.Utc;
            }
            else
            {
                if (sign is not ('+' or '-') ||
                    position + 6 != s.Length ||
                    s[position + 3] != ':' ||
                    !TryParse2Digits(s, position + 1, out var offsetHours) ||
                    !TryParse2Digits(s, position + 4, out var offsetMinutes) ||
                    offsetMinutes > 59)
                {
                    return false;
                }

                offsetTicks = (offsetHours * 60L + offsetMinutes) * TimeSpan.TicksPerMinute;
                if (offsetTicks > 14 * TimeSpan.TicksPerHour)
                {
                    return false;
                }

                if (sign == '-')
                {
                    offsetTicks = -offsetTicks;
                }

                zone = IsoZone.Offset;
            }
        }

        ticks = new DateTime(year, month, day, hour, minute, second).Ticks + fraction;
        return true;
    }

    static bool TryParse2Digits(CharSpan s, int start, out int value)
    {
        var digit1 = s[start] - '0';
        var digit2 = s[start + 1] - '0';
        value = digit1 * 10 + digit2;
        return (uint) digit1 <= 9 && (uint) digit2 <= 9;
    }

    // the first and last day of the supported range are left to the framework, which has its own
    // handling for values an offset would push out of range
    static bool IsAwayFromRangeEnds(long ticks) =>
        ticks is >= TimeSpan.TicksPerDay and <= 3155378975999999999 - TimeSpan.TicksPerDay;

    internal static bool TryParseDateTimeIso(CharSpan s, out DateTime dt)
    {
        if (TryParseIso(s, out var ticks, out var zone, out var offsetTicks))
        {
            switch (zone)
            {
                case IsoZone.None:
                    dt = new(ticks, DateTimeKind.Unspecified);
                    return true;
                case IsoZone.Utc:
                    dt = new(ticks, DateTimeKind.Utc);
                    return true;
                default:
                    var utcTicks = ticks - offsetTicks;
                    if (IsAwayFromRangeEnds(ticks) &&
                        IsAwayFromRangeEnds(utcTicks))
                    {
                        dt = new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime();
                        return true;
                    }

                    break;
            }
        }

        dt = default;
        return false;
    }

    internal static bool TryParseDateTimeOffsetIso(CharSpan s, out DateTimeOffset dt)
    {
        if (TryParseIso(s, out var ticks, out var zone, out var offsetTicks) &&
            IsAwayFromRangeEnds(ticks) &&
            IsAwayFromRangeEnds(ticks - offsetTicks))
        {
            switch (zone)
            {
                case IsoZone.None:
                    // no zone means local time, the same as the framework assumes
                    dt = new(new DateTime(ticks, DateTimeKind.Unspecified));
                    return true;
                default:
                    dt = new(ticks, new(offsetTicks));
                    return true;
            }
        }

        dt = default;
        return false;
    }

    internal static bool TryParseDateTime(string s, out DateTime dt)
    {
        if (s.Length > 0)
        {
            if (s.Length is >= 19 and <= 40 && char.IsDigit(s[0]) && s[10] == 'T')
            {
                if (TryParseDateTimeIso(s.AsSpan(), out dt))
                {
                    return true;
                }

                if (DateTime.TryParseExact(s, IsoDateFormat, InvariantCulture, DateTimeStyles.RoundtripKind, out dt))
                {
                    return true;
                }
            }
        }

        dt = default;
        return false;
    }

    internal static bool TryParseDateTimeOffset(string s, out DateTimeOffset dt)
    {
        if (s.Length > 0)
        {
            if (s.Length is >= 19 and <= 40 && char.IsDigit(s[0]) && s[10] == 'T')
            {
                if (TryParseDateTimeOffsetIso(s.AsSpan(), out dt))
                {
                    return true;
                }

                // TryParseExact fully validates and produces the same result the custom ISO
                // parser would; re-parsing (and the ToCharArray copy) was pure overhead.
                if (DateTimeOffset.TryParseExact(s, IsoDateFormat, InvariantCulture, DateTimeStyles.RoundtripKind, out dt))
                {
                    return true;
                }
            }
        }

        dt = default;
        return false;
    }

    #region Write

    internal static void WriteDateTimeString(TextWriter writer, DateTime value)
    {
        Span<char> chars = stackalloc char[64];
        var pos = WriteDateTimeString(chars, 0, value, null, value.Kind);
        writer.Write(chars[..pos]);
    }

    internal static string ToDateTimeString(DateTime value)
    {
        Span<char> chars = stackalloc char[64];
        var pos = WriteDateTimeString(chars, 0, value, null, value.Kind);
        return chars[..pos].ToString();
    }

    internal static int WriteDateTimeString(Span<char> chars, int start, DateTime value, TimeSpan? offset, DateTimeKind kind)
    {
        var pos = WriteDefaultIsoDate(chars, start, value);

        if (kind == DateTimeKind.Local)
        {
            return WriteDateTimeOffset(chars, pos, offset ?? value.GetUtcOffset());
        }

        if (kind == DateTimeKind.Utc)
        {
            chars[pos++] = 'Z';
        }

        return pos;
    }

    static int WriteDefaultIsoDate(Span<char> chars, int start, DateTime dt)
    {
        var length = 19;

        GetDateValues(dt, out var year, out var month, out var day);

        CopyIntToCharArray(chars, start, year, 4);
        chars[start + 4] = '-';
        CopyIntToCharArray(chars, start + 5, month, 2);
        chars[start + 7] = '-';
        CopyIntToCharArray(chars, start + 8, day, 2);
        chars[start + 10] = 'T';
        CopyIntToCharArray(chars, start + 11, dt.Hour, 2);
        chars[start + 13] = ':';
        CopyIntToCharArray(chars, start + 14, dt.Minute, 2);
        chars[start + 16] = ':';
        CopyIntToCharArray(chars, start + 17, dt.Second, 2);

        var fraction = (int) (dt.Ticks % 10000000L);

        if (fraction != 0)
        {
            var digits = 7;
            while (fraction % 10 == 0)
            {
                digits--;
                fraction /= 10;
            }

            chars[start + 19] = '.';
            CopyIntToCharArray(chars, start + 20, fraction, digits);

            length += digits + 1;
        }

        return start + length;
    }

    static void CopyIntToCharArray(Span<char> chars, int start, int value, int digits)
    {
        while (digits-- != 0)
        {
            chars[start + digits] = (char) (value % 10 + 48);
            value /= 10;
        }
    }

    internal static int WriteDateTimeOffset(Span<char> chars, int start, TimeSpan offset)
    {
        chars[start++] = offset.Ticks >= 0L ? '+' : '-';

        var absHours = Math.Abs(offset.Hours);
        CopyIntToCharArray(chars, start, absHours, 2);
        start += 2;

        chars[start++] = ':';

        var absMinutes = Math.Abs(offset.Minutes);
        CopyIntToCharArray(chars, start, absMinutes, 2);
        start += 2;

        return start;
    }

    internal static void WriteDateTimeOffsetString(TextWriter writer, DateTimeOffset value)
    {
        Span<char> chars = stackalloc char[64];
        var pos = WriteDateTimeString(chars, 0, value.DateTime, value.Offset, DateTimeKind.Local);

        writer.Write(chars[..pos]);
    }

    internal static string ToDateTimeOffsetString(DateTimeOffset value)
    {
        Span<char> chars = stackalloc char[64];
        var pos = WriteDateTimeString(chars, 0, value.DateTime, value.Offset, DateTimeKind.Local);
        return chars[..pos].ToString();
    }

    #endregion

    static void GetDateValues(DateTime td, out int year, out int month, out int day)
    {
        var ticks = td.Ticks;
        // n = number of days since 1/1/0001
        var n = (int) (ticks / TicksPerDay);
        // y400 = number of whole 400-year periods since 1/1/0001
        var y400 = n / DaysPer400Years;
        // n = day number within 400-year period
        n -= y400 * DaysPer400Years;
        // y100 = number of whole 100-year periods within 400-year period
        var y100 = n / DaysPer100Years;
        // Last 100-year period has an extra day, so decrement result if 4
        if (y100 == 4)
        {
            y100 = 3;
        }

        // n = day number within 100-year period
        n -= y100 * DaysPer100Years;
        // y4 = number of whole 4-year periods within 100-year period
        var y4 = n / DaysPer4Years;
        // n = day number within 4-year period
        n -= y4 * DaysPer4Years;
        // y1 = number of whole years within 4-year period
        var y1 = n / DaysPerYear;
        // Last year has an extra day, so decrement result if 4
        if (y1 == 4)
        {
            y1 = 3;
        }

        year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;

        // n = day number within year
        n -= y1 * DaysPerYear;

        // Leap year calculation looks different from IsLeapYear since y1, y4,
        // and y100 are relative to year 1, not year 0
        var leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
        var days = leapYear ? DaysToMonth366 : DaysToMonth365;
        // All months have less than 32 days, so n >> 5 is a good conservative
        // estimate for the month
        var m = n >> (5 + 1);
        // m = 1-based month number
        while (n >= days[m])
        {
            m++;
        }

        month = m;

        // Return 1-based day-of-month
        day = n - days[m - 1] + 1;
    }
}