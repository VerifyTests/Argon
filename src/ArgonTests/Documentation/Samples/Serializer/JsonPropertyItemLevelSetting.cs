// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class JsonPropertyItemLevelSetting : TestFixtureBase
{
    #region JsonPropertyItemLevelSettingTypes

    public class Business
    {
        public string Name { get; set; }

        [JsonProperty(ItemIsReference = true)] public IList<Employee> Employees { get; set; }
    }

    public class Employee
    {
        public string Name { get; set; }

        [JsonProperty(IsReference = true)] public Employee Manager { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region JsonPropertyItemLevelSettingUsage

        var manager = new Employee
        {
            Name = "George-Michael"
        };
        var worker = new Employee
        {
            Name = "Maeby",
            Manager = manager
        };

        var business = new Business
        {
            Name = "Acme Ltd.",
            Employees = [manager, worker]
        };

        var json = JsonConvert.SerializeObject(business, Formatting.Indented);

        Console.WriteLine(json);
        // {
        //   "Name": "Acme Ltd.",
        //   "Employees": [
        //     {
        //       "$id": "1",
        //       "Name": "George-Michael",
        //       "Manager": null
        //     },
        //     {
        //       "$id": "2",
        //       "Name": "Maeby",
        //       "Manager": {
        //         "$ref": "1"
        //       }
        //     }
        //   ]
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "Acme Ltd.",
              "Employees": [
                {
                  "$id": "1",
                  "Name": "George-Michael",
                  "Manager": null
                },
                {
                  "$id": "2",
                  "Name": "Maeby",
                  "Manager": {
                    "$ref": "1"
                  }
                }
              ]
            }
            """,
            json);
    }
}