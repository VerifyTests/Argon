// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class NamingStrategySkipSpecifiedNames : TestFixtureBase
{
    #region NamingStrategySkipSpecifiedNamesTypes

    public class User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [JsonProperty(PropertyName = "UPN")] public string Upn { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region NamingStrategySkipSpecifiedNamesUsage

        var user = new User
        {
            FirstName = "John",
            LastName = "Smith",
            Upn = "john.smith@acme.com"
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                OverrideSpecifiedNames = false
            }
        };

        var json = JsonConvert.SerializeObject(user, new JsonSerializerSettings
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.Indented
        });

        Console.WriteLine(json);
        // {
        //   "firstName": "John",
        //   "lastName": "Smith",
        //   "UPN": "john.smith@acme.com"
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "firstName": "John",
              "lastName": "Smith",
              "UPN": "john.smith@acme.com"
            }
            """,
            json);
    }
}