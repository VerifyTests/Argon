// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class VersionClass
{
    public VersionClass(string version1, string version2)
    {
        StringProperty1 = "StringProperty1";
        Version1 = new(version1);
        Version2 = new(version2);
        StringProperty2 = "StringProperty2";
    }

    public VersionClass()
    {
    }

    public string StringProperty1 { get; set; }
    public Version Version1 { get; set; }
    public Version Version2 { get; set; }
    public string StringProperty2 { get; set; }
}

public class VersionConverterTests : TestFixtureBase
{
    [Fact]
    public void ReportsNonStringTokenAsJsonException() =>
        // was a raw InvalidCastException with no path/line info
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Version>("123"));

    internal static class VersionHelperClass
    {
        internal static void SerializeVersionClass(string version1, string version2)
        {
            var versionClass = new VersionClass(version1, version2);

            var json = JsonConvert.SerializeObject(versionClass, Formatting.Indented);

            var expectedJson = $$"""
                                 {
                                   "StringProperty1": "StringProperty1",
                                   "Version1": "{{version1}}",
                                   "Version2": "{{version2}}",
                                   "StringProperty2": "StringProperty2"
                                 }
                                 """;

            XUnitAssert.AreEqualNormalized(expectedJson, json);
        }

        internal static void DeserializeVersionClass(string version1, string version2)
        {
            var json = $$"""{"StringProperty1": "StringProperty1", "Version1": "{{version1}}", "Version2": "{{version2}}", "StringProperty2": "StringProperty2"}""";
            var expectedVersion1 = new Version(version1);
            var expectedVersion2 = new Version(version2);

            var versionClass = JsonConvert.DeserializeObject<VersionClass>(json);

            Assert.Equal("StringProperty1", versionClass.StringProperty1);
            Assert.Equal(expectedVersion1, versionClass.Version1);
            Assert.Equal(expectedVersion2, versionClass.Version2);
            Assert.Equal("StringProperty2", versionClass.StringProperty2);
        }
    }

    [Fact]
    public void SerializeVersionClass()
    {
        VersionHelperClass.SerializeVersionClass("1.0.0.0", "2.0.0.0");
        VersionHelperClass.SerializeVersionClass("1.2.0.0", "2.3.0.0");
        VersionHelperClass.SerializeVersionClass("1.2.3.0", "2.3.4.0");
        VersionHelperClass.SerializeVersionClass("1.2.3.4", "2.3.4.5");

        VersionHelperClass.SerializeVersionClass("1.2", "2.3");
        VersionHelperClass.SerializeVersionClass("1.2.3", "2.3.4");
        VersionHelperClass.SerializeVersionClass("1.2.3.4", "2.3.4.5");
    }

    [Fact]
    public void DeserializeVersionClass()
    {
        VersionHelperClass.DeserializeVersionClass("1.0.0.0", "2.0.0.0");
        VersionHelperClass.DeserializeVersionClass("1.2.0.0", "2.3.0.0");
        VersionHelperClass.DeserializeVersionClass("1.2.3.0", "2.3.4.0");
        VersionHelperClass.DeserializeVersionClass("1.2.3.4", "2.3.4.5");

        VersionHelperClass.DeserializeVersionClass("1.2", "2.3");
        VersionHelperClass.DeserializeVersionClass("1.2.3", "2.3.4");
        VersionHelperClass.DeserializeVersionClass("1.2.3.4", "2.3.4.5");
    }

    [Fact]
    public void RoundtripImplicitConverter()
    {
        var version = new Version(1, 0, 0, 0);
        var reportJSON = JsonConvert.SerializeObject(version);

        //Test
        var report2 = JsonConvert.DeserializeObject<Version>(reportJSON);
        var reportJSON2 = JsonConvert.SerializeObject(report2);

        Assert.Equal(reportJSON, reportJSON2);
    }
}