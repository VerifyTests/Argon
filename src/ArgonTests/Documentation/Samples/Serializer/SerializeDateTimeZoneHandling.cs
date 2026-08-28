// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class SerializeDateTimeZoneHandling : TestFixtureBase
{
    #region SerializeDateTimeZoneHandlingTypes

    public class Flight
    {
        public string Destination { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime DepartureDateUtc { get; set; }
        public DateTime DepartureDateLocal { get; set; }
        public TimeSpan Duration { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region SerializeDateTimeZoneHandlingUsage

        var flight = new Flight
        {
            Destination = "Dubai",
            DepartureDate = new(2013, 1, 21, 0, 0, 0, DateTimeKind.Unspecified),
            DepartureDateUtc = new(2013, 1, 21, 0, 0, 0, DateTimeKind.Utc),
            DepartureDateLocal = new(2013, 1, 21, 0, 0, 0, DateTimeKind.Local),
            Duration = TimeSpan.FromHours(5.5)
        };

        var jsonWithRoundtripTimeZone = JsonConvert.SerializeObject(flight, Formatting.Indented, new JsonSerializerSettings());

        Console.WriteLine(jsonWithRoundtripTimeZone);
        // {
        //   "Destination": "Dubai",
        //   "DepartureDate": "2013-01-21T00:00:00",
        //   "DepartureDateUtc": "2013-01-21T00:00:00Z",
        //   "DepartureDateLocal": "2013-01-21T00:00:00+01:00",
        //   "Duration": "05:30:00"
        // }

        #endregion
    }
}