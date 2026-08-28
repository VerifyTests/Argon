// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class JsonPropertyPropertyLevelSetting : TestFixtureBase
{
    #region JsonPropertyPropertyLevelSettingTypes

    public class Vessel
    {
        public string Name { get; set; }
        public string Class { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LaunchDate { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region JsonPropertyPropertyLevelSettingUsage

        var vessel = new Vessel
        {
            Name = "Red October",
            Class = "Typhoon"
        };

        var json = JsonConvert.SerializeObject(vessel, Formatting.Indented);

        Console.WriteLine(json);
        // {
        //   "Name": "Red October",
        //   "Class": "Typhoon"
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "Red October",
              "Class": "Typhoon"
            }
            """,
            json);
    }
}