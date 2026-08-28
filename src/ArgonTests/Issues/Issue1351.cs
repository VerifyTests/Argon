// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class Issue1351 : TestFixtureBase
{
    public class Color
    {
        public Color()
        {
        }

        public Color(uint colorCode)
        {
            A = (byte)((colorCode & 0xff000000) >> 24);
            R = (byte)((colorCode & 0x00ff0000) >> 16);
            G = (byte)((colorCode & 0x0000ff00) >> 8);
            B = (byte)(colorCode & 0x000000ff);
        }

        public byte A { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    public static class Colors
    {
        public static Color White = new(0xFFFFFFFF);
    }

    [DataContract]
    public class TestClass
    {
        [DataMember]
        public Color Color { get; set; } = Colors.White;
    }

    [Fact]
    public void Test()
    {
        var t = new List<TestClass>
        {
            new()
            {
                Color = new()
                {
                    A = 1,
                    G = 1,
                    B = 1,
                    R = 1
                }
            },
            new()
            {
                Color = new()
                {
                    A = 2,
                    G = 2,
                    B = 2,
                    R = 2
                }
            }
        };
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            Formatting = Formatting.Indented
        };

        var json = JsonConvert.SerializeObject(t, settings);

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<List<TestClass>>(json, settings));
        Assert.Equal("Error reading object reference '4'. Path '[1].Color.A', line 16, position 10.", exception.Message);
        Assert.Equal("A different Id has already been assigned for value 'Issue1351+Color'. This error may be caused by an object being reused multiple times during deserialization and can be fixed with the setting ObjectCreationHandling.Replace.", exception.InnerException.Message);
    }

    [Fact]
    public void Test_Replace()
    {
        var t = new List<TestClass>
        {
            new()
            {
                Color = new()
                {
                    A = 1,
                    G = 1,
                    B = 1,
                    R = 1
                }
            },
            new()
            {
                Color = new()
                {
                    A = 2,
                    G = 2,
                    B = 2,
                    R = 2
                }
            }
        };
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            Formatting = Formatting.Indented,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };
        var json = JsonConvert.SerializeObject(t, settings);

        var obj = JsonConvert.DeserializeObject<List<TestClass>>(json, settings);

        var o1 = obj[0];
        Assert.Equal(1, o1.Color.A);

        var o2 = obj[1];
        Assert.Equal(2, o2.Color.A);
    }
}