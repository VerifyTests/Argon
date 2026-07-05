// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class UtilitiesBugFixes : TestFixtureBase
{
    // Bug: ReflectionUtils.GetDefaultValue(typeof(char)) returned a boxed int 0 instead of
    // '\0', so DefaultValueHandling.Ignore never omitted a default char member.

    [Fact]
    public void DefaultValueHandling_Ignore_omits_default_char()
    {
        var json = JsonConvert.SerializeObject(
            new HasChar(),
            new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});

        Assert.Equal("{}", json);
    }

    [Fact]
    public void DefaultValueHandling_Ignore_keeps_non_default_char()
    {
        var json = JsonConvert.SerializeObject(
            new HasChar {C = 'a'},
            new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});

        Assert.Equal("""{"C":"a"}""", json);
    }

    public class HasChar
    {
        public char C { get; set; }
    }

    // Bug: BoxedPrimitives.Get(decimal) returned a cached positive-zero box for -0.0m, so
    // the negative-zero sign was dropped at scale 0/1 but kept at higher scales.

    [Fact]
    public void Negative_zero_decimal_preserves_sign_across_scales()
    {
        Assert.True(HasNegativeSignBit(ReadDecimal("-0.0")));
        Assert.True(HasNegativeSignBit(ReadDecimal("-0.00")));
        Assert.False(HasNegativeSignBit(ReadDecimal("0.0")));

        // ToString is unaffected (decimal never renders a negative-zero sign).
        Assert.Equal("0.0", ReadDecimal("-0.0").ToString(InvariantCulture));
    }

    static bool HasNegativeSignBit(decimal value) =>
        (decimal.GetBits(value)[3] & int.MinValue) != 0;

    static decimal ReadDecimal(string json)
    {
        var reader = new JsonTextReader(new StringReader(json))
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.Float)
            {
                return (decimal) reader.Value;
            }
        }

        throw new InvalidOperationException("no float token");
    }

    // Bug: the ISO date parser accepts zone offsets up to +/-99:99, then the DateTimeOffset
    // constructor threw instead of the parse returning false.

    [Fact]
    public void TryParseDateTimeOffsetIso_rejects_out_of_range_offset()
    {
        Assert.False(DateTimeUtils.TryParseDateTimeOffsetIso(Ref("2000-01-01T00:00:00+15:00"), out _));
        Assert.False(DateTimeUtils.TryParseDateTimeOffsetIso(Ref("2000-01-01T00:00:00-15:00"), out _));
        Assert.False(DateTimeUtils.TryParseDateTimeOffsetIso(Ref("2000-01-01T00:00:00+13:99"), out _));

        // +/-14:00 is the maximum offset DateTimeOffset allows.
        Assert.True(DateTimeUtils.TryParseDateTimeOffsetIso(Ref("2000-01-01T00:00:00+14:00"), out _));
    }

    static StringReference Ref(string s) =>
        new(s.ToCharArray(), 0, s.Length);
}
