// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class NullValueHandlingIgnore : TestFixtureBase
{
    #region NullValueHandlingIgnoreTypes

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
        #region NullValueHandlingIgnoreUsage

        var person = new Person
        {
            Name = "Nigal Newborn",
            Age = 1
        };

        var jsonIncludeNullValues = JsonConvert.SerializeObject(person, Formatting.Indented);

        Console.WriteLine(jsonIncludeNullValues);
        // {
        //   "Name": "Nigal Newborn",
        //   "Age": 1,
        //   "Partner": null,
        //   "Salary": null
        // }

        var jsonIgnoreNullValues = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        Console.WriteLine(jsonIgnoreNullValues);
        // {
        //   "Name": "Nigal Newborn",
        //   "Age": 1
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "Nigal Newborn",
              "Age": 1
            }
            """,
            jsonIgnoreNullValues);
    }
}