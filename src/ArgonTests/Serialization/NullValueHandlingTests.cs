// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class NullValueHandlingTests : TestFixtureBase
{
    const string MovieNullValueHandlingIncludeExpectedResult = """
        {
          "Name": "Bad Boys III",
          "Description": "It's no Bad Boys",
          "Classification": null,
          "Studio": null,
          "ReleaseDate": null,
          "ReleaseCountries": null
        }
        """;

    const string MovieNullValueHandlingIgnoreExpectedResult = """
        {
          "Name": "Bad Boys III",
          "Description": "It's no Bad Boys"
        }
        """;

    [Fact]
    public void DeserializeNullIntoDateTime()
    {
        var c = JsonConvert.DeserializeObject<DateTimeTestClass>("{DateTimeField:null}", new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
        Assert.Equal(c.DateTimeField, default);
    }

    [Fact]
    public void DeserializeEmptyStringIntoDateTimeWithEmptyStringDefaultValue()
    {
        var c = JsonConvert.DeserializeObject<DateTimeTestClass>(
            """{DateTimeField:""}""",
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        Assert.Equal(c.DateTimeField, default);
    }

    [Fact]
    public void NullValueHandlingSerialization()
    {
        var s1 = new Store();

        var serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, s1);

        //JsonConvert.ConvertDateTimeToJavaScriptTicks(s1.Established.DateTime)

        Assert.Equal(
            """{"Color":4,"Established":"2010-01-22T01:01:01Z","Width":1.1,"Employees":999,"RoomsPerFloor":[1,2,3,4,5,6,7,8,9],"Open":false,"Symbol":"@","Mottos":["Hello World","öäüÖÄÜ\\'{new Date(12345);}[222]_µ@²³~",null," "],"Cost":100980.1,"Escape":"\r\n\t\f\b?{\\r\\n\"'","product":[{"Name":"Rocket","ExpiryDate":"2000-02-02T23:01:30Z","Price":0.0},{"Name":"Alien","ExpiryDate":"2000-01-01T00:00:00Z","Price":0.0}]}""",
            stringWriter.GetStringBuilder().ToString());

        var s2 = serializer.Deserialize<Store>(new JsonTextReader(new StringReader("{}")));
        Assert.Equal("\r\n\t\f\b?{\\r\\n\"\'", s2.Escape);

        var s3 = serializer.Deserialize<Store>(
            new JsonTextReader(
                new StringReader("""{"Escape":null}""")));
        Assert.Equal("\r\n\t\f\b?{\\r\\n\"\'", s3.Escape);

        // ReSharper disable once UnusedVariable
        var s4 = serializer.Deserialize<Store>(
            new JsonTextReader(
                new StringReader(
                    """{Color:2,Established:'2010-01-22T01:01:01Z',Width:1.1,Employees:999,RoomsPerFloor:[1,2,3,4,5,6,7,8,9],Open:false,Symbol:"@",Mottos:["Hello World","öäüÖÄÜ\\'{new Date(12345);}[222]_µ@²³~",null," "],Cost:100980.1,Escape:"\r\n\t\f\b?{\\r\\n\"'",product:[{Name:'Rocket',ExpiryDate:'2000-02-02T23:01:30Z',Price:0},{Name:'Alien',ExpiryDate:'2000-02-02T23:01:30Z',Price:0.0}]}""")));
        Assert.Equal(s1.Established, s3.Established);
    }

    [Fact]
    public void NullValueHandlingBlogPost()
    {
        var movie = new Movie
        {
            Name = "Bad Boys III",
            Description = "It's no Bad Boys"
        };

        var included = JsonConvert.SerializeObject(movie,
            Formatting.Indented,
            new JsonSerializerSettings());

        // {
        //   "Name": "Bad Boys III",
        //   "Description": "It's no Bad Boys",
        //   "Classification": null,
        //   "Studio": null,
        //   "ReleaseDate": null,
        //   "ReleaseCountries": null
        // }

        var ignored = JsonConvert.SerializeObject(movie,
            Formatting.Indented,
            new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});

        // {
        //   "Name": "Bad Boys III",
        //   "Description": "It's no Bad Boys"
        // }

        XUnitAssert.AreEqualNormalized(MovieNullValueHandlingIncludeExpectedResult, included);

        XUnitAssert.AreEqualNormalized(MovieNullValueHandlingIgnoreExpectedResult, ignored);
    }

    [Fact]
    public void JsonObjectNullValueHandlingIgnore()
    {
        var movie = new MovieWithJsonObjectNullValueHandlingIgnore
        {
            Name = "Bad Boys III",
            Description = "It's no Bad Boys"
        };

        var ignored = JsonConvert.SerializeObject(movie,
            Formatting.Indented,
            new JsonSerializerSettings {NullValueHandling = NullValueHandling.Include});

        // {
        //   "Name": "Bad Boys III",
        //   "Description": "It's no Bad Boys"
        // }

        XUnitAssert.AreEqualNormalized(MovieNullValueHandlingIgnoreExpectedResult, ignored);
    }

    [Fact]
    public void JsonObjectNullValueHandlingInclude()
    {
        var movie = new MovieWithJsonObjectNullValueHandlingInclude
        {
            Name = "Bad Boys III",
            Description = "It's no Bad Boys"
        };

        var included = JsonConvert.SerializeObject(movie,
            Formatting.Indented,
            new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});

        // {
        //   "Name": "Bad Boys III",
        //   "Description": "It's no Bad Boys",
        //   "Classification": null,
        //   "Studio": null,
        //   "ReleaseDate": null,
        //   "ReleaseCountries": null
        // }

        XUnitAssert.AreEqualNormalized(MovieNullValueHandlingIncludeExpectedResult, included);
    }
}