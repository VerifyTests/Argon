// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;
using Staff = TestObjects.Employee;

public class DefaultSettings : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        string json;

        try
        {
            #region DefaultSettingsUsage

            // settings will automatically be used by JsonConvert.SerializeObject/DeserializeObject
            JsonConvert.DefaultSettings = () => new()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var s = new Staff
            {
                FirstName = "Eric",
                LastName = "Example",
                BirthDate = new(1980, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                Department = "IT",
                JobTitle = "Web Dude"
            };

            json = JsonConvert.SerializeObject(s);
            // {
            //   "firstName": "Eric",
            //   "lastName": "Example",
            //   "birthDate": "1980-04-20T00:00:00Z",
            //   "department": "IT",
            //   "jobTitle": "Web Dude"
            // }

            #endregion
        }
        finally
        {
            JsonConvert.DefaultSettings = null;
        }

        XUnitAssert.AreEqualNormalized(
            """
            {
              "firstName": "Eric",
              "lastName": "Example",
              "birthDate": "1980-04-20T00:00:00Z",
              "department": "IT",
              "jobTitle": "Web Dude"
            }
            """,
            json);
    }
}