// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class ReferenceLoopHandlingIgnore : TestFixtureBase
{
    #region ReferenceLoopHandlingIgnoreTypes

    public class Employee
    {
        public string Name { get; set; }
        public Employee Manager { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region ReferenceLoopHandlingIgnoreUsage

        var joe = new Employee {Name = "Joe User"};
        var mike = new Employee {Name = "Mike Manager"};
        joe.Manager = mike;
        mike.Manager = mike;

        var json = JsonConvert.SerializeObject(joe, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        Console.WriteLine(json);
        // {
        //   "Name": "Joe User",
        //   "Manager": {
        //     "Name": "Mike Manager"
        //   }
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "Joe User",
              "Manager": {
                "Name": "Mike Manager"
              }
            }
            """,
            json);
    }
}