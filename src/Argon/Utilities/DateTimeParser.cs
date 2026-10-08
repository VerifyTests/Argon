// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

struct DateTimeParser
{
    public int Year;
    public int Month;
    public int Day;
    public int Hour;
    public int Minute;
    public int Second;
    public int Fraction;
    public int ZoneHour;
    public int ZoneMinute;
    public ParserTimeZone Zone;

    char[] text;
    int end;

    static readonly int[] Power10 = [-1, 10, 100, 1000, 10000, 100000, 1000000];

    static readonly int Lzyyyy = "yyyy".Length;
    static readonly int Lzyyyy_ = "yyyy-".Length;
    static readonly int Lzyyyy_MM = "yyyy-MM".Length;
    static readonly int Lzyyyy_MM_ = "yyyy-MM-".Length;
    static readonly int Lzyyyy_MM_dd = "yyyy-MM-dd".Length;
    static readonly int Lzyyyy_MM_ddT = "yyyy-MM-ddT".Length;
    static readonly int LzHH = "HH".Length;
    static readonly int LzHH_ = "HH:".Length;
    static readonly int LzHH_mm = "HH:mm".Length;
    static readonly int LzHH_mm_ = "HH:mm:".Length;
    static readonly int LzHH_mm_ss = "HH:mm:ss".Length;
    static readonly int Lz_ = "-".Length;
    static readonly int Lz_zz = "-zz".Length;

    const short MaxFractionDigits = 7;

    public bool Parse(char[] text, int startIndex, int length)
    {
        this.text = text;
        end = startIndex + length;

        return ParseDate(startIndex) &&
               ParseChar(Lzyyyy_MM_dd + startIndex, 'T') &&
               ParseTimeAndZoneAndWhitespace(Lzyyyy_MM_ddT + startIndex);
    }

    bool ParseDate(int start) =>
        Parse4Digit(start, out Year) &&
        1 <= Year &&
        ParseChar(start + Lzyyyy, '-') &&
        Parse2Digit(start + Lzyyyy_, out Month) &&
        Month is >= 1 and <= 12 &&
        ParseChar(start + Lzyyyy_MM, '-') &&
        Parse2Digit(start + Lzyyyy_MM_, out Day) &&
        1 <= Day &&
        Day <= DateTime.DaysInMonth(Year, Month);

    bool ParseTimeAndZoneAndWhitespace(int start) =>
        ParseTime(ref start) && ParseZone(start);

    bool ParseTime(ref int start)
    {
        if (!(Parse2Digit(start, out Hour) &&
              Hour <= 24 &&
              ParseChar(start + LzHH, ':') &&
              Parse2Digit(start + LzHH_, out Minute) &&
              Minute < 60 &&
              ParseChar(start + LzHH_mm, ':') &&
              Parse2Digit(start + LzHH_mm_, out Second) &&
              Second < 60 &&
              // hour can be 24 if minute/second is zero)
              (Hour != 24 || (Minute == 0 && Second == 0))))
        {
            return false;
        }

        start += LzHH_mm_ss;
        if (ParseChar(start, '.'))
        {
            Fraction = 0;
            var numberOfDigits = 0;

            while (++start < end && numberOfDigits < MaxFractionDigits)
            {
                var digit = text[start] - '0';
                if (digit is < 0 or > 9)
                {
                    break;
                }

                Fraction = Fraction * 10 + digit;

                numberOfDigits++;
            }

            if (numberOfDigits < MaxFractionDigits)
            {
                if (numberOfDigits == 0)
                {
                    return false;
                }

                Fraction *= Power10[MaxFractionDigits - numberOfDigits];
            }

            if (Hour == 24 && Fraction != 0)
            {
                return false;
            }
        }

        return true;
    }

    bool ParseZone(int start)
    {
        if (start < end)
        {
            var ch = text[start];
            if (ch is 'Z' or 'z')
            {
                Zone = ParserTimeZone.Utc;
                start++;
            }
            else
            {
                if (start + 2 < end
                    && Parse2Digit(start + Lz_, out ZoneHour)
                    && ZoneHour <= 99)
                {
                    switch (ch)
                    {
                        case '-':
                            Zone = ParserTimeZone.LocalWestOfUtc;
                            start += Lz_zz;
                            break;

                        case '+':
                            Zone = ParserTimeZone.LocalEastOfUtc;
                            start += Lz_zz;
                            break;
                    }
                }

                if (start < end)
                {
                    if (ParseChar(start, ':'))
                    {
                        start += 1;

                        if (start + 1 < end
                            && Parse2Digit(start, out ZoneMinute)
                            && ZoneMinute <= 99)
                        {
                            start += 2;
                        }
                    }
                    else
                    {
                        if (start + 1 < end
                            && Parse2Digit(start, out ZoneMinute)
                            && ZoneMinute <= 99)
                        {
                            start += 2;
                        }
                    }
                }
            }
        }

        return start == end;
    }

    bool Parse4Digit(int start, out int num)
    {
        if (start + 3 < end)
        {
            var digit1 = text[start] - '0';
            var digit2 = text[start + 1] - '0';
            var digit3 = text[start + 2] - '0';
            var digit4 = text[start + 3] - '0';
            if (digit1 is >= 0 and < 10 && digit2 is >= 0 and < 10 && digit3 is >= 0 and < 10 && digit4 is >= 0 and < 10)
            {
                num = ((digit1 * 10 + digit2) * 10 + digit3) * 10 + digit4;
                return true;
            }
        }

        num = 0;
        return false;
    }

    bool Parse2Digit(int start, out int num)
    {
        if (start + 1 < end)
        {
            var digit1 = text[start] - '0';
            var digit2 = text[start + 1] - '0';
            if (digit1 is >= 0 and < 10 && digit2 is >= 0 and < 10)
            {
                num = digit1 * 10 + digit2;
                return true;
            }
        }

        num = 0;
        return false;
    }

    bool ParseChar(int start, char ch) =>
        start < end && text[start] == ch;
}