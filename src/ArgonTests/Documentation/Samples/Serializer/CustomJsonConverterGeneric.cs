// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class CustomJsonConverterGeneric : TestFixtureBase
{
    #region CustomJsonConverterGenericTypes

    public class VersionConverter : JsonConverter<Version>
    {
        public override void WriteJson(JsonWriter writer, Version value, JsonSerializer serializer) =>
            writer.WriteValue(value.ToString());

        public override Version ReadJson(
            JsonReader reader,
            Type type,
            Version existing,
            bool hasExisting,
            JsonSerializer serializer)
        {
            var s = (string) reader.Value;

            return new(s);
        }
    }

    public class NuGetPackage
    {
        public string PackageId { get; set; }
        public Version Version { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region CustomJsonConverterGenericUsage

        var p1 = new NuGetPackage
        {
            PackageId = "Argon",
            Version = new(10, 0, 4)
        };

        var json = JsonConvert.SerializeObject(p1, Formatting.Indented, new VersionConverter());

        Console.WriteLine(json);
        // {
        //   "PackageId": "Argon",
        //   "Version": "10.0.4"
        // }

        var p2 = JsonConvert.DeserializeObject<NuGetPackage>(json, new VersionConverter());

        Console.WriteLine(p2.Version.ToString());
        // 10.0.4

        #endregion

        Assert.Equal("10.0.4", p2.Version.ToString());
    }
}