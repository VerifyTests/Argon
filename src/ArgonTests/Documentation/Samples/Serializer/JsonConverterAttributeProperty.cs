// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class JsonConverterAttributeProperty : TestFixtureBase
{
    #region JsonConverterAttributePropertyTypes

    public enum UserStatus
    {
        NotConfirmed,
        Active,
        Deleted
    }

    public class User
    {
        public string UserName { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public UserStatus Status { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region JsonConverterAttributePropertyUsage

        var user = new User
        {
            UserName = @"domain\username",
            Status = UserStatus.Deleted
        };

        var json = JsonConvert.SerializeObject(user, Formatting.Indented);

        Console.WriteLine(json);
        // {
        //   "UserName": "domain\\username",
        //   "Status": "Deleted"
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "UserName": "domain\\username",
              "Status": "Deleted"
            }
            """,
            json);
    }
}