// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class JsonPropertyOrder : TestFixtureBase
{
    #region JsonPropertyOrderTypes

    public class Account
    {
        public string EmailAddress { get; set; }

        // appear last
        [JsonProperty(Order = 1)] public bool Deleted { get; set; }

        [JsonProperty(Order = 2)] public DateTime DeletedDate { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        // appear first
        [JsonProperty(Order = -2)] public string FullName { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region JsonPropertyOrderUsage

        var account = new Account
        {
            FullName = "Aaron Account",
            EmailAddress = "aaron@example.com",
            Deleted = true,
            DeletedDate = new(2013, 1, 25),
            UpdatedDate = new(2013, 1, 25),
            CreatedDate = new(2010, 10, 1)
        };

        var json = JsonConvert.SerializeObject(account, Formatting.Indented);

        Console.WriteLine(json);
        // {
        //   "FullName": "Aaron Account",
        //   "EmailAddress": "aaron@example.com",
        //   "CreatedDate": "2010-10-01T00:00:00",
        //   "UpdatedDate": "2013-01-25T00:00:00",
        //   "Deleted": true,
        //   "DeletedDate": "2013-01-25T00:00:00"
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "FullName": "Aaron Account",
              "EmailAddress": "aaron@example.com",
              "CreatedDate": "2010-10-01T00:00:00",
              "UpdatedDate": "2013-01-25T00:00:00",
              "Deleted": true,
              "DeletedDate": "2013-01-25T00:00:00"
            }
            """,
            json);
    }
}