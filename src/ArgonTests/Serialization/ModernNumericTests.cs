// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#if NET7_0_OR_GREATER

public class ModernNumericTests : TestFixtureBase
{
    public class Holder
    {
        public Int128 Signed { get; set; }
        public UInt128 Unsigned { get; set; }
        public Half Half { get; set; }
        public Int128? NullableSigned { get; set; }
        public UInt128? NullableUnsigned { get; set; }
        public Half? NullableHalf { get; set; }
    }

    [Fact]
    public void RoundTripAsNumbers()
    {
        var holder = new Holder
        {
            Signed = Int128.MinValue,
            Unsigned = UInt128.MaxValue,
            Half = (Half) 0.1f,
            NullableSigned = 5,
            NullableUnsigned = 7,
            NullableHalf = (Half) (-1.5f)
        };

        var json = JsonConvert.SerializeObject(holder);

        Assert.Equal(
            """{"Signed":-170141183460469231731687303715884105728,"Unsigned":340282366920938463463374607431768211455,"Half":0.1,"NullableSigned":5,"NullableUnsigned":7,"NullableHalf":-1.5}""",
            json);

        var result = JsonConvert.DeserializeObject<Holder>(json);

        Assert.Equal(holder.Signed, result.Signed);
        Assert.Equal(holder.Unsigned, result.Unsigned);
        Assert.Equal(holder.Half, result.Half);
        Assert.Equal(holder.NullableSigned, result.NullableSigned);
        Assert.Equal(holder.NullableUnsigned, result.NullableUnsigned);
        Assert.Equal(holder.NullableHalf, result.NullableHalf);
    }

    [Fact]
    public void NullableDefaultsRoundTrip()
    {
        var json = JsonConvert.SerializeObject(new Holder());

        Assert.Equal("""{"Signed":0,"Unsigned":0,"Half":0.0,"NullableSigned":null,"NullableUnsigned":null,"NullableHalf":null}""", json);

        var result = JsonConvert.DeserializeObject<Holder>(json);

        Assert.Null(result.NullableSigned);
        Assert.Null(result.NullableUnsigned);
        Assert.Null(result.NullableHalf);
    }

    [Fact]
    public void RootValues()
    {
        Assert.Equal("170141183460469231731687303715884105727", JsonConvert.SerializeObject(Int128.MaxValue));
        Assert.Equal(Int128.MaxValue, JsonConvert.DeserializeObject<Int128>("170141183460469231731687303715884105727"));
        Assert.Equal((UInt128) 5, JsonConvert.DeserializeObject<UInt128>("5"));
        Assert.Equal("1.5", JsonConvert.SerializeObject((Half) 1.5f));
        Assert.Equal((Half) 1.5f, JsonConvert.DeserializeObject<Half>("1.5"));
        Assert.Equal((Half) 2, JsonConvert.DeserializeObject<Half>("2"));
        Assert.Equal("[1,2]", JsonConvert.SerializeObject(new List<Int128> {1, 2}));
        Assert.Equal(new List<Int128> {1, 2}, JsonConvert.DeserializeObject<List<Int128>>("[1,2]"));
    }

    // values used to be written as strings, which have to stay readable
    [Fact]
    public void ReadFromStrings()
    {
        var result = JsonConvert.DeserializeObject<Holder>("""{"Signed":"-5","Unsigned":"340282366920938463463374607431768211455","Half":"1.5"}""");

        Assert.Equal((Int128) (-5), result.Signed);
        Assert.Equal(UInt128.MaxValue, result.Unsigned);
        Assert.Equal((Half) 1.5f, result.Half);
    }

    [Fact]
    public void OutOfRangeThrows()
    {
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<UInt128>("-1"));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Int128>("170141183460469231731687303715884105728"));
    }

    [Fact]
    public void HalfSpecialValues()
    {
        Assert.Equal(
            """
            "NaN"
            """,
            JsonConvert.SerializeObject(Half.NaN));
        Assert.Equal(
            """
            "-Infinity"
            """,
            JsonConvert.SerializeObject(Half.NegativeInfinity));
        Assert.True(Half.IsNaN(JsonConvert.DeserializeObject<Half>("\"NaN\"")));
        Assert.Equal(Half.PositiveInfinity, JsonConvert.DeserializeObject<Half>("\"Infinity\""));

        var settings = new JsonSerializerSettings
        {
            FloatFormatHandling = FloatFormatHandling.Symbol
        };
        Assert.Equal("NaN", JsonConvert.SerializeObject(Half.NaN, settings));
    }

    [Fact]
    public void JsonConvertToString()
    {
        Assert.Equal("170141183460469231731687303715884105727", JsonConvert.ToString((object) Int128.MaxValue));
        Assert.Equal("340282366920938463463374607431768211455", JsonConvert.ToString((object) UInt128.MaxValue));
        Assert.Equal("1.5", JsonConvert.ToString((object) (Half) 1.5f));
        Assert.Equal("2.0", JsonConvert.ToString((object) (Half) 2));
    }

    [Fact]
    public void JValues()
    {
        var signed = new JValue((object) (Int128) 5);
        var unsigned = new JValue((object) UInt128.MaxValue);
        var half = new JValue((object) (Half) 1.5f);

        Assert.Equal(JTokenType.Integer, signed.Type);
        Assert.Equal(JTokenType.Integer, unsigned.Type);
        Assert.Equal(JTokenType.Float, half.Type);

        Assert.Equal("5", signed.ToString(Argon.Formatting.None));
        Assert.Equal("340282366920938463463374607431768211455", unsigned.ToString(Argon.Formatting.None));
        Assert.Equal("1.5", half.ToString(Argon.Formatting.None));

        Assert.True(signed.Equals(new JValue(5)));
        Assert.Equal(new JValue(5).GetHashCode(), signed.GetHashCode());
        Assert.True(unsigned.Equals(new JValue((BigInteger) UInt128.MaxValue)));
        Assert.Equal(new JValue((BigInteger) UInt128.MaxValue).GetHashCode(), unsigned.GetHashCode());
        Assert.True(half.Equals(new JValue(1.5d)));
        Assert.Equal(new JValue(1.5d).GetHashCode(), half.GetHashCode());
        Assert.True(signed.CompareTo(new JValue(6)) < 0);

        Assert.Equal(5, (int) signed);
        Assert.Equal(1.5d, (double) half);
        Assert.Equal((Int128) 5, signed.Value<Int128>());
        Assert.Equal((Int128) 6, new JValue(6).Value<Int128>());
        Assert.Equal((Half) 1.5f, new JValue(1.5d).Value<Half>());
        Assert.Equal(5L, signed.ToObject<long>());
        Assert.Equal(UInt128.MaxValue, unsigned.ToObject<UInt128>());
    }

    [Fact]
    public void JTokenRoundTrip()
    {
        var holder = new Holder
        {
            Signed = Int128.MaxValue,
            Unsigned = 7,
            Half = (Half) 1.5f
        };

        var token = JObject.FromObject(holder);

        Assert.IsType<Int128>(((JValue) token["Signed"]).Value);
        Assert.IsType<Half>(((JValue) token["Half"]).Value);
        Assert.Equal(
            """{"Signed":170141183460469231731687303715884105727,"Unsigned":7,"Half":1.5,"NullableSigned":null,"NullableUnsigned":null,"NullableHalf":null}""",
            token.ToString(Argon.Formatting.None));

        var result = token.ToObject<Holder>();

        Assert.Equal(holder.Signed, result.Signed);
        Assert.Equal(holder.Unsigned, result.Unsigned);
        Assert.Equal(holder.Half, result.Half);
    }
}

#endif
