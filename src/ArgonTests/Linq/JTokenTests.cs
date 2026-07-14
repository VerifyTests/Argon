// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestCase = Xunit.InlineDataAttribute;
// ReSharper disable UnusedVariable

public class JTokenTests : TestFixtureBase
{
    [Fact]
    public void DeepClonePreservesLineInfoOnContainers()
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

    [Fact]
    public void DeepEqualsObjectOrder()
    {
        var ob1 = """{"key1":"1","key2":"2"}""";
        var ob2 = """{"key2":"2","key1":"1"}""";

        var j1 = JObject.Parse(ob1);
        var j2 = JObject.Parse(ob2);
        Assert.True(j1.DeepEquals(j2));
    }

    [Fact]
    public void ReadFrom()
    {
        var o = (JObject) JToken.ReadFrom(new JsonTextReader(new StringReader("{'pie':true}")));
        Assert.True((bool) o["pie"]);

        var a = (JArray) JToken.ReadFrom(new JsonTextReader(new StringReader("[1,2,3]")));
        Assert.Equal(1, (int) a[0]);
        Assert.Equal(2, (int) a[1]);
        Assert.Equal(3, (int) a[2]);

        var reader = new JsonTextReader(new StringReader("{'pie':true}"));
        reader.Read();
        reader.Read();

        var p = (JProperty) JToken.ReadFrom(reader);
        Assert.Equal("pie", p.Name);
        Assert.True((bool) p.Value);

        var v = (JValue) JToken.ReadFrom(
            new JsonTextReader(
                new StringReader(
                    """
                    "stringvalue"
                    """)));
        Assert.Equal("stringvalue", (string) v);

        v = (JValue) JToken.ReadFrom(new JsonTextReader(new StringReader("1")));
        Assert.Equal(1, (int) v);

        v = (JValue) JToken.ReadFrom(new JsonTextReader(new StringReader("1.1")));
        Assert.Equal(1.1, (double) v);
    }

    [Fact]
    public void Load()
    {
        var o = (JObject) JToken.Load(new JsonTextReader(new StringReader("{'pie':true}")));
        Assert.True((bool) o["pie"]);
    }

    [Fact]
    public void Parse()
    {
        var o = (JObject) JToken.Parse("{'pie':true}");
        Assert.True((bool) o["pie"]);
    }

    [Fact]
    public void Next()
    {
        var a =
            new JArray(
                5,
                6,
                new JArray(7, 8),
                new JArray(9, 10)
            );

        var next = a[0].Next;
        Assert.Equal(6, (int) next);

        next = next.Next;
        Assert.True(JToken.DeepEquals(new JArray(7, 8), next));

        next = next.Next;
        Assert.True(JToken.DeepEquals(new JArray(9, 10), next));

        next = next.Next;
        Assert.Null(next);
    }

    [Fact]
    public void Previous()
    {
        var a =
            new JArray(
                5,
                6,
                new JArray(7, 8),
                new JArray(9, 10)
            );

        var previous = a[3].Previous;
        Assert.True(JToken.DeepEquals(new JArray(7, 8), previous));

        previous = previous.Previous;
        Assert.Equal(6, (int) previous);

        previous = previous.Previous;
        Assert.Equal(5, (int) previous);

        previous = previous.Previous;
        Assert.Null(previous);
    }

    [Fact]
    public void Children()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        Assert.Equal(4, a.Count);
        Assert.Equal(3, a.Children<JArray>().Count());
    }

    [Fact]
    public void BeforeAfter()
    {
        var a =
            new JArray(
                5,
                new JArray(1, 2, 3),
                new JArray(1, 2, 3),
                new JArray(1, 2, 3)
            );

        Assert.Equal(5, (int) a[1].Previous);
        Assert.Equal(2, a[2].BeforeSelf().Count());
    }

    [Fact]
    public void BeforeSelf_NoParent_ReturnEmpty()
    {
        var o = new JObject();

        var result = o.BeforeSelf().ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void BeforeSelf_OnlyChild_ReturnEmpty()
    {
        var a = new JArray();
        var o = new JObject();
        a.Add(o);

        var result = o.BeforeSelf().ToList();
        Assert.Empty(result);
    }

#nullable enable
    [Fact]
    public void Casting()
    {
        Assert.Equal(1L, (long) new JValue(1));
        Assert.Equal(2L, (long) new JArray(1, 2, 3)[1]);

        Assert.Equal(new(2000, 12, 20), (DateTime) new JValue(new DateTime(2000, 12, 20)));
        Assert.Equal(new(2000, 12, 20, 0, 0, 0, TimeSpan.Zero), (DateTimeOffset) new JValue(new DateTime(2000, 12, 20, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(new(2000, 12, 20, 23, 50, 10, TimeSpan.Zero), (DateTimeOffset) new JValue(new DateTimeOffset(2000, 12, 20, 23, 50, 10, TimeSpan.Zero)));
        Assert.Null((DateTimeOffset?) new JValue((DateTimeOffset?) null));
        Assert.Null((DateTimeOffset?) (JValue?) null);
        Assert.True((bool) new JValue(true));
        Assert.True((bool?) new JValue(true));
        Assert.Null((bool?) (JValue?) null);
        Assert.Null((bool?) JValue.CreateNull());
        Assert.Equal(10, (long) new JValue(10));
        Assert.Null((long?) new JValue((long?) null));
        Assert.Null((long?) (JValue?) null);
        Assert.Null((int?) new JValue((int?) null));
        Assert.Null((int?) (JValue?) null);
        Assert.Null((DateTime?) new JValue((DateTime?) null));
        Assert.Null((DateTime?) (JValue?) null);
        Assert.Null((short?) new JValue((short?) null));
        Assert.Null((short?) (JValue?) null);
        Assert.Null((float?) new JValue((float?) null));
        Assert.Null((float?) (JValue?) null);
        Assert.Null((double?) new JValue((double?) null));
        Assert.Null((double?) (JValue?) null);
        Assert.Null((decimal?) new JValue((decimal?) null));
        Assert.Null((decimal?) (JValue?) null);
        Assert.Null((uint?) new JValue((uint?) null));
        Assert.Null((uint?) (JValue?) null);
        Assert.Null((sbyte?) new JValue((sbyte?) null));
        Assert.Null((sbyte?) (JValue?) null);
        Assert.Null((byte?) new JValue((byte?) null));
        Assert.Null((byte?) (JValue?) null);
        Assert.Null((ulong?) new JValue((ulong?) null));
        Assert.Null((ulong?) (JValue?) null);
        Assert.Null((uint?) new JValue((uint?) null));
        Assert.Null((uint?) (JValue?) null);
        Assert.Equal(11.1f, (float) new JValue(11.1));
        Assert.Equal(float.MinValue, (float) new JValue(float.MinValue));
        Assert.Equal(1.1, (double) new JValue(1.1));
        Assert.Equal(uint.MaxValue, (uint) new JValue(uint.MaxValue));
        Assert.Equal(ulong.MaxValue, (ulong) new JValue(ulong.MaxValue));
        Assert.Equal(ulong.MaxValue, (ulong) new JProperty("Test", new JValue(ulong.MaxValue)));
        Assert.Null((string?) new JValue((string?) null));
        Assert.Equal(5m, (decimal) new JValue(5L));
        Assert.Equal(5m, (decimal?) new JValue(5L));
        Assert.Equal(5f, (float) new JValue(5L));
        Assert.Equal(5f, (float) new JValue(5m));
        Assert.Equal(5f, (float?) new JValue(5m));
        Assert.Equal(5, (byte) new JValue(5));
        Assert.Equal(sbyte.MinValue, (sbyte?) new JValue(sbyte.MinValue));
        Assert.Equal(sbyte.MinValue, (sbyte) new JValue(sbyte.MinValue));

        Assert.Null((sbyte?) JValue.CreateNull());

        Assert.Equal("1", (string?) new JValue(1));
        Assert.Equal("1", (string?) new JValue(1.0));
        Assert.Equal("1.0", (string?) new JValue(1.0m));
        Assert.Equal("True", (string?) new JValue(true));
        Assert.Null((string?) JValue.CreateNull());
        Assert.Null((string?) (JValue?) null);
        Assert.Equal("12/12/2000 12:12:12", (string?) new JValue(new DateTime(2000, 12, 12, 12, 12, 12, DateTimeKind.Utc)));
        Assert.Equal("12/12/2000 12:12:12 +00:00", (string?) new JValue(new DateTimeOffset(2000, 12, 12, 12, 12, 12, TimeSpan.Zero)));
        Assert.True((bool) new JValue(1));
        Assert.True((bool) new JValue(1.0));
        Assert.True((bool) new JValue("true"));
        Assert.True((bool) new JValue(true));
        Assert.True((bool) new JValue(2));
        Assert.False( (bool) new JValue(0));
        Assert.Equal(1, (int) new JValue(1));
        Assert.Equal(1, (int) new JValue(1.0));
        Assert.Equal(1, (int) new JValue("1"));
        Assert.Equal(1, (int) new JValue(true));
        Assert.Equal(1m, (decimal) new JValue(1));
        Assert.Equal(1m, (decimal) new JValue(1.0));
        Assert.Equal(1m, (decimal) new JValue("1"));
        Assert.Equal(1m, (decimal) new JValue(true));
        Assert.Equal(TimeSpan.FromMinutes(1), (TimeSpan) new JValue(TimeSpan.FromMinutes(1)));
        Assert.Equal("00:01:00", (string?) new JValue(TimeSpan.FromMinutes(1)));
        Assert.Equal(TimeSpan.FromMinutes(1), (TimeSpan) new JValue("00:01:00"));
        Assert.Equal("46efe013-b56a-4e83-99e4-4dce7678a5bc", (string?) new JValue(new Guid("46EFE013-B56A-4E83-99E4-4DCE7678A5BC")));
        Assert.Equal("http://www.google.com/", (string?) new JValue(new Uri("http://www.google.com")));
        Assert.Equal(new("46EFE013-B56A-4E83-99E4-4DCE7678A5BC"), (Guid) new JValue("46EFE013-B56A-4E83-99E4-4DCE7678A5BC"));
        Assert.Equal(new("46EFE013-B56A-4E83-99E4-4DCE7678A5BC"), (Guid) new JValue(new Guid("46EFE013-B56A-4E83-99E4-4DCE7678A5BC")));
        Assert.Equal(new("http://www.google.com"), (Uri?) new JValue("http://www.google.com"));
        Assert.Equal(new("http://www.google.com"), (Uri?) new JValue(new Uri("http://www.google.com")));
        Assert.Null((Uri?) JValue.CreateNull());
        Assert.Equal(Convert.ToBase64String("hi"u8.ToArray()), (string?) new JValue("hi"u8.ToArray()));
        Assert.Equal("hi"u8.ToArray(), (byte[]?) new JValue(Convert.ToBase64String("hi"u8.ToArray())));
        Assert.Equal(new("46EFE013-B56A-4E83-99E4-4DCE7678A5BC"), (Guid) new JValue(new Guid("46EFE013-B56A-4E83-99E4-4DCE7678A5BC").ToByteArray()));
        Assert.Equal(new Guid("46EFE013-B56A-4E83-99E4-4DCE7678A5BC"), (Guid?) new JValue(new Guid("46EFE013-B56A-4E83-99E4-4DCE7678A5BC").ToByteArray()));
        Assert.Equal((sbyte?) 1, (sbyte?) new JValue((short?) 1));

        Assert.Null((Uri?) (JValue?) null);
        Assert.Null((int?) (JValue?) null);
        Assert.Null((uint?) (JValue?) null);
        Assert.Null((Guid?) (JValue?) null);
        Assert.Null((TimeSpan?) (JValue?) null);
        Assert.Null((byte[]?) (JValue?) null);
        Assert.Null((bool?) (JValue?) null);
        Assert.Null((char?) (JValue?) null);
        Assert.Null((DateTime?) (JValue?) null);
        Assert.Null((DateTimeOffset?) (JValue?) null);
        Assert.Null((short?) (JValue?) null);
        Assert.Null((ushort?) (JValue?) null);
        Assert.Null((byte?) (JValue?) null);
        Assert.Null((byte?) (JValue?) null);
        Assert.Null((sbyte?) (JValue?) null);
        Assert.Null((sbyte?) (JValue?) null);
        Assert.Null((long?) (JValue?) null);
        Assert.Null((ulong?) (JValue?) null);
        Assert.Null((double?) (JValue?) null);
        Assert.Null((float?) (JValue?) null);

        var data = Array.Empty<byte>();
        Assert.Equal(data, (byte[]?) new JValue(data));

        Assert.Equal(5, (int) new JValue(StringComparison.OrdinalIgnoreCase));

        var bigIntegerText = "1234567899999999999999999999999999999999999999999999999999999999999990";

        Assert.Equal(BigInteger.Parse(bigIntegerText), new JValue(BigInteger.Parse(bigIntegerText)).Value);

        Assert.Equal(BigInteger.Parse(bigIntegerText), new JValue(bigIntegerText).ToObject<BigInteger>());
        Assert.Equal(new(long.MaxValue), new JValue(long.MaxValue).ToObject<BigInteger>());
        Assert.Equal(new(4.5d), new JValue(4.5d).ToObject<BigInteger>());
        Assert.Equal(new(4.5f), new JValue(4.5f).ToObject<BigInteger>());
        Assert.Equal(new(byte.MaxValue), new JValue(byte.MaxValue).ToObject<BigInteger>());
        Assert.Equal(new(123), new JValue(123).ToObject<BigInteger>());
        Assert.Equal(new BigInteger(123), new JValue(123).ToObject<BigInteger?>());
        Assert.Null(JValue.CreateNull().ToObject<BigInteger?>());

        var intData = BigInteger.Parse(bigIntegerText).ToByteArray();
        Assert.Equal(BigInteger.Parse(bigIntegerText), new JValue(intData).ToObject<BigInteger>());

        Assert.Equal(4.0d, (double) new JValue(new BigInteger(4.5d)));
        Assert.True((bool) new JValue(new BigInteger(1)));
        Assert.Equal(long.MaxValue, (long) new JValue(new BigInteger(long.MaxValue)));
        Assert.Equal(long.MaxValue, (long) new JValue(new BigInteger([255, 255, 255, 255, 255, 255, 255, 127])));
        Assert.Equal("9223372036854775807", (string?) new JValue(new BigInteger(long.MaxValue)));

        intData = (byte[]?) new JValue(new BigInteger(long.MaxValue));
        Assert.Equal([255, 255, 255, 255, 255, 255, 255, 127], intData);
    }
#nullable disable

    [Fact]
    public void FailedCasting()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var i = (DateTime) new JValue(true);
        });
        Assert.Equal("Can not convert Boolean to DateTime.", exception.Message);
        var exception1 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (DateTime) new JValue(1);
        });
        Assert.Equal("Can not convert Integer to DateTime.", exception1.Message);
        var exception2 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (DateTime) new JValue(1.1);
        });
        Assert.Equal("Can not convert Float to DateTime.", exception2.Message);
        var exception3 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (DateTime) new JValue(1.1m);
        });
        Assert.Equal("Can not convert Float to DateTime.", exception3.Message);
        var exception4 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (DateTime) new JValue(TimeSpan.Zero);
        });
        Assert.Equal("Can not convert TimeSpan to DateTime.", exception4.Message);
        var exception5 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (DateTime) new JValue(new Uri("http://www.google.com"));
        });
        Assert.Equal("Can not convert Uri to DateTime.", exception5.Message);
        var exception6 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (DateTime) JValue.CreateNull();
        });
        Assert.Equal("Can not convert Null to DateTime.", exception6.Message);
        var exception7 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (DateTime) new JValue(Guid.NewGuid());
        });
        Assert.Equal("Can not convert Guid to DateTime.", exception7.Message);

        var exception8 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Uri) new JValue(true);
        });
        Assert.Equal("Can not convert Boolean to Uri.", exception8.Message);
        var exception9 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Uri) new JValue(1);
        });
        Assert.Equal("Can not convert Integer to Uri.", exception9.Message);
        var exception10 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Uri) new JValue(1.1);
        });
        Assert.Equal("Can not convert Float to Uri.", exception10.Message);
        var exception11 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Uri) new JValue(1.1m);
        });
        Assert.Equal("Can not convert Float to Uri.", exception11.Message);
        var exception12 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Uri) new JValue(TimeSpan.Zero);
        });
        Assert.Equal("Can not convert TimeSpan to Uri.", exception12.Message);
        var exception13 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Uri) new JValue(Guid.NewGuid());
        });
        Assert.Equal("Can not convert Guid to Uri.", exception13.Message);
        var exception14 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Uri) new JValue(DateTime.Now);
        });
        Assert.Equal("Can not convert Date to Uri.", exception14.Message);
        var exception15 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Uri) new JValue(DateTimeOffset.Now);
        });
        Assert.Equal("Can not convert Date to Uri.", exception15.Message);

        var exception16 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (TimeSpan) new JValue(true);
        });
        Assert.Equal("Can not convert Boolean to TimeSpan.", exception16.Message);
        var exception17 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (TimeSpan) new JValue(1);
        });
        Assert.Equal("Can not convert Integer to TimeSpan.", exception17.Message);
        var exception18 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (TimeSpan) new JValue(1.1);
        });
        Assert.Equal("Can not convert Float to TimeSpan.", exception18.Message);
        var exception19 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (TimeSpan) new JValue(1.1m);
        });
        Assert.Equal("Can not convert Float to TimeSpan.", exception19.Message);
        var exception20 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (TimeSpan) JValue.CreateNull();
        });
        Assert.Equal("Can not convert Null to TimeSpan.", exception20.Message);
        var exception21 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (TimeSpan) new JValue(Guid.NewGuid());
        });
        Assert.Equal("Can not convert Guid to TimeSpan.", exception21.Message);
        var exception22 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (TimeSpan) new JValue(DateTime.Now);
        });
        Assert.Equal("Can not convert Date to TimeSpan.", exception22.Message);
        var exception23 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (TimeSpan) new JValue(DateTimeOffset.Now);
        });
        Assert.Equal("Can not convert Date to TimeSpan.", exception23.Message);
        var exception24 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (TimeSpan) new JValue(new Uri("http://www.google.com"));
        });
        Assert.Equal("Can not convert Uri to TimeSpan.", exception24.Message);

        var exception25 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Guid) new JValue(true);
        });
        Assert.Equal("Can not convert Boolean to Guid.", exception25.Message);
        var exception26 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Guid) new JValue(1);
        });
        Assert.Equal("Can not convert Integer to Guid.", exception26.Message);
        var exception27 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Guid) new JValue(1.1);
        });
        Assert.Equal("Can not convert Float to Guid.", exception27.Message);
        var exception28 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Guid) new JValue(1.1m);
        });
        Assert.Equal("Can not convert Float to Guid.", exception28.Message);
        var exception29 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Guid) JValue.CreateNull();
        });
        Assert.Equal("Can not convert Null to Guid.", exception29.Message);
        var exception30 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Guid) new JValue(DateTime.Now);
        });
        Assert.Equal("Can not convert Date to Guid.", exception30.Message);
        var exception31 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Guid) new JValue(DateTimeOffset.Now);
        });
        Assert.Equal("Can not convert Date to Guid.", exception31.Message);
        var exception32 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Guid) new JValue(TimeSpan.FromMinutes(1));
        });
        Assert.Equal("Can not convert TimeSpan to Guid.", exception32.Message);
        var exception33 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Guid) new JValue(new Uri("http://www.google.com"));
        });
        Assert.Equal("Can not convert Uri to Guid.", exception33.Message);

        var exception34 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (DateTimeOffset) new JValue(true);
        });
        Assert.Equal("Can not convert Boolean to DateTimeOffset.", exception34.Message);
        var exception35 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (Uri) new JValue(true);
        });
        Assert.Equal("Can not convert Boolean to Uri.", exception35.Message);

        var exception36 = Assert.Throws<ArgumentException>(() =>
        {
            var i = new JValue(new Uri("http://www.google.com")).ToObject<BigInteger>();
        });
        Assert.Equal("Can not convert Uri to BigInteger.", exception36.Message);
        var exception37 = Assert.Throws<ArgumentException>(() =>
        {
            var i = JValue.CreateNull().ToObject<BigInteger>();
        });
        Assert.Equal("Can not convert Null to BigInteger.", exception37.Message);
        var exception38 = Assert.Throws<ArgumentException>(() =>
        {
            var i = new JValue(Guid.NewGuid()).ToObject<BigInteger>();
        });
        Assert.Equal("Can not convert Guid to BigInteger.", exception38.Message);
        var exception39 = Assert.Throws<ArgumentException>(() =>
        {
            var i = new JValue(Guid.NewGuid()).ToObject<BigInteger?>();
        });
        Assert.Equal("Can not convert Guid to BigInteger.", exception39.Message);

        var exception40 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (sbyte?) new JValue(DateTime.Now);
        });
        Assert.Equal("Can not convert Date to SByte.", exception40.Message);
        var exception41 = Assert.Throws<ArgumentException>(() =>
        {
            var i = (sbyte) new JValue(DateTime.Now);
        });
        Assert.Equal("Can not convert Date to SByte.", exception41.Message);

        var jValue = new JValue("Ordinal1");
        var exception42 = Assert.Throws<ArgumentException>(() =>
        {
            var i = jValue.ToObject<StringComparison>();
        });
        Assert.Equal("Could not convert 'Ordinal1' to StringComparison.", exception42.Message);
        var exception43 = Assert.Throws<ArgumentException>(() =>
        {
            var i = jValue.ToObject<StringComparison?>();
        });
        Assert.Equal("Could not convert 'Ordinal1' to StringComparison.", exception43.Message);
    }

    [Fact]
    public void ToObject()
    {
        Assert.Equal((BigInteger) 1, new JValue(1).ToObject(typeof(BigInteger)));
        Assert.Equal((BigInteger) 1, new JValue(1).ToObject(typeof(BigInteger?)));
        Assert.Null(JValue.CreateNull().ToObject(typeof(BigInteger?)));
        Assert.Equal((ushort) 1, new JValue(1).ToObject(typeof(ushort)));
        Assert.Equal((ushort) 1, new JValue(1).ToObject(typeof(ushort?)));
        Assert.Equal((uint) 1L, new JValue(1).ToObject(typeof(uint)));
        Assert.Equal((uint) 1L, new JValue(1).ToObject(typeof(uint?)));
        Assert.Equal((ulong) 1L, new JValue(1).ToObject(typeof(ulong)));
        Assert.Equal((ulong) 1L, new JValue(1).ToObject(typeof(ulong?)));
        Assert.Equal((sbyte) 1L, new JValue(1).ToObject(typeof(sbyte)));
        Assert.Equal((sbyte) 1L, new JValue(1).ToObject(typeof(sbyte?)));
        Assert.Null(JValue.CreateNull().ToObject(typeof(sbyte?)));
        Assert.Equal((byte) 1L, new JValue(1).ToObject(typeof(byte)));
        Assert.Equal((byte) 1L, new JValue(1).ToObject(typeof(byte?)));
        Assert.Equal((short) 1L, new JValue(1).ToObject(typeof(short)));
        Assert.Equal((short) 1L, new JValue(1).ToObject(typeof(short?)));
        Assert.Equal(1, new JValue(1).ToObject(typeof(int)));
        Assert.Equal(1, new JValue(1).ToObject(typeof(int?)));
        Assert.Equal(1L, new JValue(1).ToObject(typeof(long)));
        Assert.Equal(1L, new JValue(1).ToObject(typeof(long?)));
        Assert.Equal((float) 1, new JValue(1.0).ToObject(typeof(float)));
        Assert.Equal((float) 1, new JValue(1.0).ToObject(typeof(float?)));
        Assert.Equal((double) 1, new JValue(1.0).ToObject(typeof(double)));
        Assert.Equal((double) 1, new JValue(1.0).ToObject(typeof(double?)));
        Assert.Equal(1m, new JValue(1).ToObject(typeof(decimal)));
        Assert.Equal(1m, new JValue(1).ToObject(typeof(decimal?)));
        Assert.True((bool) new JValue(true).ToObject(typeof(bool)));
        Assert.True((bool) new JValue(true).ToObject(typeof(bool?)));
        Assert.Equal('b', new JValue('b').ToObject(typeof(char)));
        Assert.Equal('b', new JValue('b').ToObject(typeof(char?)));
        Assert.Equal(TimeSpan.MaxValue, new JValue(TimeSpan.MaxValue).ToObject(typeof(TimeSpan)));
        Assert.Equal(TimeSpan.MaxValue, new JValue(TimeSpan.MaxValue).ToObject(typeof(TimeSpan?)));
        Assert.Equal(DateTime.MaxValue, new JValue(DateTime.MaxValue).ToObject(typeof(DateTime)));
        Assert.Equal(DateTime.MaxValue, new JValue(DateTime.MaxValue).ToObject(typeof(DateTime?)));
        Assert.Equal(DateTimeOffset.MaxValue, new JValue(DateTimeOffset.MaxValue).ToObject(typeof(DateTimeOffset)));
        Assert.Equal(DateTimeOffset.MaxValue, new JValue(DateTimeOffset.MaxValue).ToObject(typeof(DateTimeOffset?)));
        Assert.Equal("b", new JValue("b").ToObject(typeof(string)));
        Assert.Equal(new Guid("A34B2080-B5F0-488E-834D-45D44ECB9E5C"), new JValue(new Guid("A34B2080-B5F0-488E-834D-45D44ECB9E5C")).ToObject(typeof(Guid)));
        Assert.Equal(new Guid("A34B2080-B5F0-488E-834D-45D44ECB9E5C"), new JValue(new Guid("A34B2080-B5F0-488E-834D-45D44ECB9E5C")).ToObject(typeof(Guid?)));
        Assert.Equal(new Uri("http://www.google.com/"), new JValue(new Uri("http://www.google.com/")).ToObject(typeof(Uri)));
        Assert.Equal(StringComparison.Ordinal, new JValue("Ordinal").ToObject(typeof(StringComparison)));
        Assert.Equal(StringComparison.Ordinal, new JValue("Ordinal").ToObject(typeof(StringComparison?)));
        Assert.Null(JValue.CreateNull().ToObject(typeof(StringComparison?)));
    }

#nullable enable
    [Fact]
    public void ImplicitCastingTo()
    {
        Assert.True(JToken.DeepEquals(new JValue(new DateTime(2000, 12, 20)), (JValue) new DateTime(2000, 12, 20)));
        Assert.True(JToken.DeepEquals(new JValue(new DateTimeOffset(2000, 12, 20, 23, 50, 10, TimeSpan.Zero)), (JValue) new DateTimeOffset(2000, 12, 20, 23, 50, 10, TimeSpan.Zero)));
        Assert.True(JToken.DeepEquals(new JValue((DateTimeOffset?) null), (JValue) (DateTimeOffset?) null));

        // had to remove implicit casting to avoid user reference to System.Numerics.dll
        Assert.True(JToken.DeepEquals(new JValue(new BigInteger(1)), new JValue(new BigInteger(1))));
        Assert.True(JToken.DeepEquals(new JValue((BigInteger?) null), new JValue((BigInteger?) null)));
        Assert.True(JToken.DeepEquals(new JValue(true), (JValue) true));
        Assert.True(JToken.DeepEquals(new JValue(true), (JValue) true));
        Assert.True(JToken.DeepEquals(new JValue(true), (JValue) (bool?) true));
        Assert.True(JToken.DeepEquals(new JValue((bool?) null), (JValue) (bool?) null));
        Assert.True(JToken.DeepEquals(new JValue(10), (JValue) 10));
        Assert.True(JToken.DeepEquals(new JValue((long?) null), (JValue) (long?) null));
        Assert.True(JToken.DeepEquals(new JValue((DateTime?) null), (JValue) (DateTime?) null));
        Assert.True(JToken.DeepEquals(new JValue(long.MaxValue), (JValue) long.MaxValue));
        Assert.True(JToken.DeepEquals(new JValue((int?) null), (JValue) (int?) null));
        Assert.True(JToken.DeepEquals(new JValue((short?) null), (JValue) (short?) null));
        Assert.True(JToken.DeepEquals(new JValue((double?) null), (JValue) (double?) null));
        Assert.True(JToken.DeepEquals(new JValue((uint?) null), (JValue) (uint?) null));
        Assert.True(JToken.DeepEquals(new JValue((decimal?) null), (JValue) (decimal?) null));
        Assert.True(JToken.DeepEquals(new JValue((ulong?) null), (JValue) (ulong?) null));
        Assert.True(JToken.DeepEquals(new JValue((sbyte?) null), (JValue) (sbyte?) null));
        Assert.True(JToken.DeepEquals(new JValue(1), (JValue) (sbyte) 1));
        Assert.True(JToken.DeepEquals(new JValue((byte?) null), (JValue) (byte?) null));
        Assert.True(JToken.DeepEquals(new JValue(1), (JValue) (byte) 1));
        Assert.True(JToken.DeepEquals(new JValue((ushort?) null), (JValue) (ushort?) null));
        Assert.True(JToken.DeepEquals(new JValue(short.MaxValue), (JValue) short.MaxValue));
        Assert.True(JToken.DeepEquals(new JValue(ushort.MaxValue), (JValue) ushort.MaxValue));
        Assert.True(JToken.DeepEquals(new JValue(11.1f), (JValue) 11.1f));
        Assert.True(JToken.DeepEquals(new JValue(float.MinValue), (JValue) float.MinValue));
        Assert.True(JToken.DeepEquals(new JValue(double.MinValue), (JValue) double.MinValue));
        Assert.True(JToken.DeepEquals(new JValue(uint.MaxValue), (JValue) uint.MaxValue));
        Assert.True(JToken.DeepEquals(new JValue(ulong.MaxValue), (JValue) ulong.MaxValue));
        Assert.True(JToken.DeepEquals(new JValue(ulong.MinValue), (JValue) ulong.MinValue));
        Assert.True(JToken.DeepEquals(new JValue((string?) null), (JValue) (string?) null));
        Assert.True(JToken.DeepEquals(new JValue((DateTime?) null), (JValue) (DateTime?) null));
        Assert.True(JToken.DeepEquals(new JValue(decimal.MaxValue), (JValue) decimal.MaxValue));
        Assert.True(JToken.DeepEquals(new JValue(decimal.MaxValue), (JValue) (decimal?) decimal.MaxValue));
        Assert.True(JToken.DeepEquals(new JValue(decimal.MinValue), (JValue) decimal.MinValue));
        Assert.True(JToken.DeepEquals(new JValue(float.MaxValue), (JValue) (float?) float.MaxValue));
        Assert.True(JToken.DeepEquals(new JValue(double.MaxValue), (JValue) (double?) double.MaxValue));
        Assert.True(JToken.DeepEquals(JValue.CreateNull(), (JValue) (double?) null));

        Assert.False(JToken.DeepEquals(new JValue(true), (JValue) (bool?) null));
        Assert.False(JToken.DeepEquals(JValue.CreateNull(), null));

        var emptyData = Array.Empty<byte>();
        Assert.True(JToken.DeepEquals(new JValue(emptyData), (JValue) emptyData));
        Assert.False(JToken.DeepEquals(new JValue(emptyData), (JValue) new byte[1]));
        Assert.True(JToken.DeepEquals(new JValue("Hi"u8.ToArray()), (JValue) "Hi"u8.ToArray()));

        Assert.True(JToken.DeepEquals(new JValue(TimeSpan.FromMinutes(1)), (JValue) TimeSpan.FromMinutes(1)));
        Assert.True(JToken.DeepEquals(JValue.CreateNull(), (JValue) (TimeSpan?) null));
        Assert.True(JToken.DeepEquals(new JValue(TimeSpan.FromMinutes(1)), (JValue) (TimeSpan?) TimeSpan.FromMinutes(1)));
        Assert.True(JToken.DeepEquals(new JValue(new Guid("46EFE013-B56A-4E83-99E4-4DCE7678A5BC")), (JValue) new Guid("46EFE013-B56A-4E83-99E4-4DCE7678A5BC")));
        Assert.True(JToken.DeepEquals(new JValue(new Uri("http://www.google.com")), (JValue) new Uri("http://www.google.com")));
        Assert.True(JToken.DeepEquals(JValue.CreateNull(), (JValue) (Uri?) null));
        Assert.True(JToken.DeepEquals(JValue.CreateNull(), (JValue) (Guid?) null));
    }
#nullable disable

    [Fact]
    public void Root()
    {
        var a =
            new JArray(
                5,
                6,
                new JArray(7, 8),
                new JArray(9, 10)
            );

        Assert.Equal(a, a.Root);
        Assert.Equal(a, a[0].Root);
        Assert.Equal(a, ((JArray) a[2])[0].Root);
    }

    [Fact]
    public void Remove()
    {
        var a =
            new JArray(
                5,
                6,
                new JArray(7, 8),
                new JArray(9, 10)
            );

        a[0].Remove();

        Assert.Equal(6, (int) a[0]);

        a[1].Remove();

        Assert.Equal(6, (int) a[0]);
        Assert.True(JToken.DeepEquals(new JArray(9, 10), a[1]));
        Assert.Equal(2, a.Count);

        var token = a[1];
        token.Remove();
        Assert.Equal(6, (int) a[0]);
        Assert.Null(token.Next);
        Assert.Null(token.Previous);
        Assert.Null(token.Parent);

        token = a[0];
        token.Remove();
        Assert.Empty(a);

        Assert.Null(token.Next);
        Assert.Null(token.Previous);
        Assert.Null(token.Parent);
    }

    [Fact]
    public void AfterSelf()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var token = a[1];
        var afterTokens = token.AfterSelf().ToList();

        Assert.Equal(2, afterTokens.Count);
        Assert.True(JToken.DeepEquals(new JArray(1, 2), afterTokens[0]));
        Assert.True(JToken.DeepEquals(new JArray(1, 2, 3), afterTokens[1]));
    }

    [Fact]
    public void BeforeSelf()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var token = a[2];
        var beforeTokens = token.BeforeSelf().ToList();

        Assert.Equal(2, beforeTokens.Count);
        Assert.True(JToken.DeepEquals(new JValue(5), beforeTokens[0]));
        Assert.True(JToken.DeepEquals(new JArray(1), beforeTokens[1]));
    }

    [Fact]
    public void HasValues()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        Assert.True(a.HasValues);
    }

    [Fact]
    public void Ancestors()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var token = a[1][0];
        var ancestors = token.Ancestors().ToList();
        Assert.Equal(2, ancestors.Count);
        Assert.Equal(a[1], ancestors[0]);
        Assert.Equal(a, ancestors[1]);
    }

    [Fact]
    public void AncestorsAndSelf()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var token = a[1][0];
        var ancestors = token.AncestorsAndSelf().ToList();
        Assert.Equal(3, ancestors.Count);
        Assert.Equal(token, ancestors[0]);
        Assert.Equal(a[1], ancestors[1]);
        Assert.Equal(a, ancestors[2]);
    }

    [Fact]
    public void AncestorsAndSelf_Many()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var o = new JObject
        {
            {"prop1", "value1"}
        };

        var t1 = a[1][0];
        var t2 = o["prop1"];

        var source = new List<JToken> {t1, t2};

        var ancestors = source.AncestorsAndSelf().ToList();
        Assert.Equal(6, ancestors.Count);
        Assert.Equal(t1, ancestors[0]);
        Assert.Equal(a[1], ancestors[1]);
        Assert.Equal(a, ancestors[2]);
        Assert.Equal(t2, ancestors[3]);
        Assert.Equal(o.Property("prop1"), ancestors[4]);
        Assert.Equal(o, ancestors[5]);
    }

    [Fact]
    public void Ancestors_Many()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var o = new JObject
        {
            {"prop1", "value1"}
        };

        var t1 = a[1][0];
        var t2 = o["prop1"];

        var source = new List<JToken> {t1, t2};

        var ancestors = source.Ancestors().ToList();
        Assert.Equal(4, ancestors.Count);
        Assert.Equal(a[1], ancestors[0]);
        Assert.Equal(a, ancestors[1]);
        Assert.Equal(o.Property("prop1"), ancestors[2]);
        Assert.Equal(o, ancestors[3]);
    }

    [Fact]
    public void Descendants()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var descendants = a.Descendants().ToList();
        Assert.Equal(10, descendants.Count);
        Assert.Equal(5, (int) descendants[0]);
        Assert.True(JToken.DeepEquals(new JArray(1, 2, 3), descendants[^4]));
        Assert.Equal(1, (int) descendants[^3]);
        Assert.Equal(2, (int) descendants[^2]);
        Assert.Equal(3, (int) descendants[^1]);
    }

    [Fact]
    public void Descendants_Many()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var o = new JObject
        {
            {"prop1", "value1"}
        };

        var source = new List<JContainer> {a, o};

        var descendants = source.Descendants().ToList();
        Assert.Equal(12, descendants.Count);
        Assert.Equal(5, (int) descendants[0]);
        Assert.True(JToken.DeepEquals(new JArray(1, 2, 3), descendants[^6]));
        Assert.Equal(1, (int) descendants[^5]);
        Assert.Equal(2, (int) descendants[^4]);
        Assert.Equal(3, (int) descendants[^3]);
        Assert.Equal(o.Property("prop1"), descendants[^2]);
        Assert.Equal(o["prop1"], descendants[^1]);
    }

    [Fact]
    public void DescendantsAndSelf()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var descendantsAndSelf = a.DescendantsAndSelf().ToList();
        Assert.Equal(11, descendantsAndSelf.Count);
        Assert.Equal(a, descendantsAndSelf[0]);
        Assert.Equal(5, (int) descendantsAndSelf[1]);
        Assert.True(JToken.DeepEquals(new JArray(1, 2, 3), descendantsAndSelf[^4]));
        Assert.Equal(1, (int) descendantsAndSelf[^3]);
        Assert.Equal(2, (int) descendantsAndSelf[^2]);
        Assert.Equal(3, (int) descendantsAndSelf[^1]);
    }

    [Fact]
    public void DescendantsAndSelf_Many()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var o = new JObject
        {
            {"prop1", "value1"}
        };

        var source = new List<JContainer> {a, o};

        var descendantsAndSelf = source.DescendantsAndSelf().ToList();
        Assert.Equal(14, descendantsAndSelf.Count);
        Assert.Equal(a, descendantsAndSelf[0]);
        Assert.Equal(5, (int) descendantsAndSelf[1]);
        Assert.True(JToken.DeepEquals(new JArray(1, 2, 3), descendantsAndSelf[^7]));
        Assert.Equal(1, (int) descendantsAndSelf[^6]);
        Assert.Equal(2, (int) descendantsAndSelf[^5]);
        Assert.Equal(3, (int) descendantsAndSelf[^4]);
        Assert.Equal(o, descendantsAndSelf[^3]);
        Assert.Equal(o.Property("prop1"), descendantsAndSelf[^2]);
        Assert.Equal(o["prop1"], descendantsAndSelf[^1]);
    }

    [Fact]
    public void CreateWriter()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var writer = a.CreateWriter();
        Assert.NotNull(writer);
        Assert.Equal(4, a.Count);

        writer.WriteValue("String");
        Assert.Equal(5, a.Count);
        Assert.Equal("String", (string) a[4]);

        writer.WriteStartObject();
        writer.WritePropertyName("Property");
        writer.WriteValue("PropertyValue");
        writer.WriteEnd();

        Assert.Equal(6, a.Count);
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("Property", "PropertyValue")), a[5]));
    }

    [Fact]
    public void AddFirst()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        a.AddFirst("First");

        Assert.Equal("First", (string) a[0]);
        Assert.Equal(a, a[0].Parent);
        Assert.Equal(a[1], a[0].Next);
        Assert.Equal(5, a.Count);

        a.AddFirst("NewFirst");
        Assert.Equal("NewFirst", (string) a[0]);
        Assert.Equal(a, a[0].Parent);
        Assert.Equal(a[1], a[0].Next);
        Assert.Equal(6, a.Count);

        Assert.Equal(a[0], a[0].Next.Previous);
    }

    [Fact]
    public void RemoveAll()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        var first = a.First;
        Assert.Equal(5, (int) first);

        a.RemoveAll();
        Assert.Empty(a);

        Assert.Null(first.Parent);
        Assert.Null(first.Next);
    }

    [Fact]
    public void AddPropertyToArray()
    {
        var property = new JProperty("PropertyName");
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var a = new JArray
            {
                property
            };
        });
        Assert.Equal("Can not add Argon.JProperty to Argon.JArray.", exception.Message);
    }

    [Fact]
    public void AddValueToObject()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var o = new JObject {5};
        });
        Assert.Equal("Can not add Argon.JValue to Argon.JObject.", exception.Message);
    }

    [Fact]
    public void Replace()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        a[0].Replace(new JValue(int.MaxValue));
        Assert.Equal(int.MaxValue, (int) a[0]);
        Assert.Equal(4, a.Count);

        a[1][0].Replace(new JValue("Test"));
        Assert.Equal("Test", (string) a[1][0]);

        a[2].Replace(new JValue(int.MaxValue));
        Assert.Equal(int.MaxValue, (int) a[2]);
        Assert.Equal(4, a.Count);

        Assert.True(JToken.DeepEquals(new JArray(int.MaxValue, new JArray("Test"), int.MaxValue, new JArray(1, 2, 3)), a));
    }

    [Fact]
    public void ToStringWithConverters()
    {
        var a =
            new JArray(
                new JValue(new DateTime(2009, 2, 15, 0, 0, 0, DateTimeKind.Utc))
            );

        var json = a.ToString(Formatting.Indented, new IsoDateTimeConverter());

        XUnitAssert.AreEqualNormalized(
            """
            [
              "2009-02-15T00:00:00Z"
            ]
            """,
            json);

        json = JsonConvert.SerializeObject(a, new IsoDateTimeConverter());

        Assert.Equal("""["2009-02-15T00:00:00Z"]""", json);
    }

    [Fact]
    public void ToStringWithNoIndenting()
    {
        var a =
            new JArray(
                new JValue(new DateTime(2009, 2, 15, 0, 0, 0, DateTimeKind.Utc))
            );

        var json = a.ToString(Formatting.None, new IsoDateTimeConverter());

        Assert.Equal("""["2009-02-15T00:00:00Z"]""", json);
    }

    [Fact]
    public void AddAfterSelf()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        a[1].AddAfterSelf("pie");

        Assert.Equal(5, (int) a[0]);
        Assert.Single(a[1]);
        Assert.Equal("pie", (string) a[2]);
        Assert.Equal(5, a.Count);

        a[4].AddAfterSelf("lastpie");

        Assert.Equal("lastpie", (string) a[5]);
        Assert.Equal("lastpie", (string) a.Last);
    }

    [Fact]
    public void AddBeforeSelf()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3)
            );

        a[1].AddBeforeSelf("pie");

        Assert.Equal(5, (int) a[0]);
        Assert.Equal("pie", (string) a[1]);
        Assert.Equal(a, a[1].Parent);
        Assert.Equal(a[2], a[1].Next);
        Assert.Equal(5, a.Count);

        a[0].AddBeforeSelf("firstpie");

        Assert.Equal("firstpie", (string) a[0]);
        Assert.Equal(5, (int) a[1]);
        Assert.Equal("pie", (string) a[2]);
        Assert.Equal(a, a[0].Parent);
        Assert.Equal(a[1], a[0].Next);
        Assert.Equal(6, a.Count);

        a.Last.AddBeforeSelf("secondlastpie");

        Assert.Equal("secondlastpie", (string) a[5]);
        Assert.Equal(7, a.Count);
    }

    [Fact]
    public void DeepClone()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3),
                new JObject(
                    new JProperty("First", new JValue("Hi"u8.ToArray())),
                    new JProperty("Second", 1),
                    new JProperty("Third", null),
                    new JProperty("Fifth", double.PositiveInfinity),
                    new JProperty("Sixth", double.NaN)
                )
            );

        var a2 = (JArray) a.DeepClone();

        XUnitAssert.AreEqualNormalized(
            """
            [
              5,
              [
                1
              ],
              [
                1,
                2
              ],
              [
                1,
                2,
                3
              ],
              {
                "First": "SGk=",
                "Second": 1,
                "Third": null,
                "Fifth": "Infinity",
                "Sixth": "NaN"
              }
            ]
            """,
            a2.ToString(Formatting.Indented));

        Assert.True(a.DeepEquals(a2));
    }

    [Fact]
    public void Clone()
    {
        var a =
            new JArray(
                5,
                new JArray(1),
                new JArray(1, 2),
                new JArray(1, 2, 3),
                new JObject(
                    new JProperty("First", new JValue("Hi"u8.ToArray())),
                    new JProperty("Second", 1),
                    new JProperty("Third", null),
                    new JProperty("Fifth", double.PositiveInfinity),
                    new JProperty("Sixth", double.NaN)
                )
            );

        ICloneable c = a;

        var a2 = (JArray) c.Clone();

        Assert.True(a.DeepEquals(a2));
    }

    [Fact]
    public void DoubleDeepEquals()
    {
        var a =
            new JArray(
                double.NaN,
                double.PositiveInfinity,
                double.NegativeInfinity
            );

        var a2 = (JArray) a.DeepClone();

        Assert.True(a.DeepEquals(a2));

        var d = 1 + 0.1 + 0.1 + 0.1;

        var v1 = new JValue(d);
        var v2 = new JValue(1.3);

        Assert.True(v1.DeepEquals(v2));
    }

    [Fact]
    public void ParseAdditionalContent()
    {
        var json = """
                   [
                       "Small",
                       "Medium",
                       "Large"
                   ],
                   """;

        var exception = Assert.Throws<JsonReaderException>(() => JToken.Parse(json));
        Assert.Equal("Additional text encountered after finished reading JSON content: ,. Path '', line 5, position 1.", exception.Message);
    }

    [Fact]
    public void Path()
    {
        var o =
            new JObject(
                new JProperty("Test1", new JArray(1, 2, 3)),
                new JProperty("Test2", "Test2Value"),
                new JProperty("Test3", new JObject(new JProperty("Test1", new JArray(1, new JObject(new JProperty("Test1", 1)), 3))))
            );

        var token = o.SelectToken("Test1[0]");
        Assert.Equal("Test1[0]", token.Path);

        token = o.SelectToken("Test2");
        Assert.Equal("Test2", token.Path);

        token = o.SelectToken("");
        Assert.Equal("", token.Path);

        token = token.DeepClone();
        Assert.Equal("", token.Path);

        token = o.SelectToken("Test3.Test1[1].Test1");
        Assert.Equal("Test3.Test1[1].Test1", token.Path);

        var a = new JArray(1);
        Assert.Equal("", a.Path);

        Assert.Equal("[0]", a[0].Path);
    }

    [Fact]
    public void Parse_NoComments()
    {
        var json = "{'prop':[1,2/*comment*/,3]}";

        var o = JToken.Parse(json, new()
        {
            CommentHandling = CommentHandling.Ignore
        });

        Assert.Equal(3, o["prop"].Count());
        Assert.Equal(1, (int) o["prop"][0]);
        Assert.Equal(2, (int) o["prop"][1]);
        Assert.Equal(3, (int) o["prop"][2]);
    }

    [Fact]
    public void Parse_ExcessiveContentJustComments()
    {
        var json = """
                   {'prop':[1,2,3]}/*comment*/
                   //Another comment.
                   """;

        var o = JToken.Parse(json);

        Assert.Equal(3, o["prop"].Count());
        Assert.Equal(1, (int) o["prop"][0]);
        Assert.Equal(2, (int) o["prop"][1]);
        Assert.Equal(3, (int) o["prop"][2]);
    }

    [Fact]
    public void Parse_ExcessiveContent()
    {
        var json = """
                   {'prop':[1,2,3]}/*comment*/
                   //Another comment.
                   {}
                   """;

        var exception = Assert.Throws<JsonReaderException>(() => JToken.Parse(json));
        Assert.Equal("Additional text encountered after finished reading JSON content: {. Path '', line 3, position 0.", exception.Message);
    }

    [Theory]
    [TestCase("test customer", "['test customer']")]
    [TestCase("test customer's", "['test customer\\'s']")]
    [TestCase("testcustomer's", "['testcustomer\\'s']")]
    [TestCase("testcustomer", "testcustomer")]
    [TestCase("test.customer", "['test.customer']")]
    [TestCase("test\rcustomer", "['test\\rcustomer']")]
    [TestCase("test\ncustomer", "['test\\ncustomer']")]
    [TestCase("test\tcustomer", "['test\\tcustomer']")]
    [TestCase("test\bcustomer", "['test\\bcustomer']")]
    [TestCase("test\fcustomer", "['test\\fcustomer']")]
    [TestCase("test/customer", "['test/customer']")]
    [TestCase("test\\customer", "['test\\\\customer']")]
    [TestCase("\"test\"customer", "['\"test\"customer']")]
    public void PathEscapingTest(string name, string expectedPath)
    {
        var v = new JValue("12345");
        var o = new JObject
        {
            [name] = v
        };

        var path = v.Path;

        Assert.Equal(expectedPath, path);

        var token = o.SelectToken(path);
        Assert.Equal(v, token);
    }
}