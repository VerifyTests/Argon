// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class SerializeObject : TestFixtureBase
{
    #region SerializeObjectTypes

    public class Account
    {
        public string Email { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; }
        public IList<string> Roles { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region SerializeObjectUsage

        var account = new Account
        {
            Email = "james@example.com",
            Active = true,
            CreatedDate = new(2013, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            Roles =
            [
                "User",
                "Admin"
            ]
        };

        var json = JsonConvert.SerializeObject(account, Formatting.Indented);
        // {
        //   "Email": "james@example.com",
        //   "Active": true,
        //   "CreatedDate": "2013-01-20T00:00:00Z",
        //   "Roles": [
        //     "User",
        //     "Admin"
        //   ]
        // }

        Console.WriteLine(json);

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Email": "james@example.com",
              "Active": true,
              "CreatedDate": "2013-01-20T00:00:00Z",
              "Roles": [
                "User",
                "Admin"
              ]
            }
            """,
            json);
    }
}