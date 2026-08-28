// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class SerializeContractResolver : TestFixtureBase
{
    #region SerializeContractResolverTypes

    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region SerializeContractResolverUsage

        var person = new Person
        {
            FirstName = "Sarah",
            LastName = "Security"
        };

        var json = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        Console.WriteLine(json);
        // {
        //   "firstName": "Sarah",
        //   "lastName": "Security",
        //   "fullName": "Sarah Security"
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "firstName": "Sarah",
              "lastName": "Security",
              "fullName": "Sarah Security"
            }
            """,
            json);
    }
}