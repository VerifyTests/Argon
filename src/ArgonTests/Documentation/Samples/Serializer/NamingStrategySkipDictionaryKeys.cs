// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class NamingStrategySkipDictionaryKeys : TestFixtureBase
{
    #region NamingStrategySkipDictionaryKeysTypes

    public class DailyHighScores
    {
        public DateTime Date { get; set; }
        public string Game { get; set; }
        public Dictionary<string, int> UserPoints { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region NamingStrategySkipDictionaryKeysUsage

        var dailyHighScores = new DailyHighScores
        {
            Date = new(2016, 6, 27, 0, 0, 0, DateTimeKind.Utc),
            Game = "Donkey Kong",
            UserPoints = new()
            {
                ["JamesNK"] = 9001,
                ["JoC"] = 1337,
                ["JessicaN"] = 1000
            }
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = false
            }
        };

        var json = JsonConvert.SerializeObject(dailyHighScores, new JsonSerializerSettings
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.Indented
        });

        Console.WriteLine(json);
        // {
        //   "date": "2016-06-27T00:00:00Z",
        //   "game": "Donkey Kong",
        //   "userPoints": {
        //     "JamesNK": 9001,
        //     "JoC": 1337,
        //     "JessicaN": 1000
        //   }
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "date": "2016-06-27T00:00:00Z",
              "game": "Donkey Kong",
              "userPoints": {
                "JamesNK": 9001,
                "JoC": 1337,
                "JessicaN": 1000
              }
            }
            """,
            json);
    }
}