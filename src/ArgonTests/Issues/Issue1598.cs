// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class Issue1598 : TestFixtureBase
{
    [Fact]
    public void Test()
    {
        var activities = new Activities
        {
            List =
            [
                new()
                {
                    Name = "An activity"
                }
            ]
        };

        var json = JsonConvert.SerializeObject(activities, Formatting.Indented);
        // note that this has been reverted back in 11.0.2 because it is causing compat issues
        // https://github.com/JamesNK/Newtonsoft.Json/issues/1627
        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "Name": "An activity"
              }
            ]
            """,
            json);
    }

    [Fact]
    public void Test_SubClass()
    {
        var activities = new ActivitiesSubClass
        {
            List =
            [
                new()
                {
                    Name = "An activity"
                }
            ]
        };

        var json = JsonConvert.SerializeObject(activities, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "Name": "An activity"
              }
            ]
            """,
            json);
    }

    public class Activity
    {
        public string Name { get; set; }
    }

    public class ActivitiesSubClass : Activities;

    [DataContract]
    public class Activities : IEnumerable<Activity>
    {
        [DataMember]
        public List<Activity> List { get; set; }

        public IEnumerator<Activity> GetEnumerator() =>
            List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}