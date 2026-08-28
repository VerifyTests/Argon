// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class DefaultValueHandlingIgnore : TestFixtureBase
{
    #region DefaultValueHandlingIgnoreTypes

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Person Partner { get; set; }
        public decimal? Salary { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region DefaultValueHandlingIgnoreUsage

        var person = new Person();

        var jsonIncludeDefaultValues = JsonConvert.SerializeObject(person, Formatting.Indented);

        Console.WriteLine(jsonIncludeDefaultValues);
        // {
        //   "Name": null,
        //   "Age": 0,
        //   "Partner": null,
        //   "Salary": null
        // }

        var jsonIgnoreDefaultValues = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore
        });

        Console.WriteLine(jsonIgnoreDefaultValues);
        // {}

        #endregion

        Assert.Equal("{}", jsonIgnoreDefaultValues);
    }
}