// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class NamingStrategySnakeCase : TestFixtureBase
{
    #region NamingStrategySnakeCaseTypes

    public class User
    {
        public string UserName { get; set; }
        public bool Enabled { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region NamingStrategySnakeCaseUsage

        var user1 = new User
        {
            UserName = "jamesn",
            Enabled = true
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };

        var json = JsonConvert.SerializeObject(user1, new JsonSerializerSettings
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.Indented
        });

        Console.WriteLine(json);
        // {
        //   "user_name": "jamesn",
        //   "enabled": true
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "user_name": "jamesn",
              "enabled": true
            }
            """,
            json);
    }
}