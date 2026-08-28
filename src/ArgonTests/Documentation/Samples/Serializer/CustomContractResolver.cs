// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class CustomContractResolver : TestFixtureBase
{
    #region CustomContractResolverTypes

    public class DynamicContractResolver(char startingWithChar) : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);

            // only serializer properties that start with the specified character
            properties =
                properties.Where(_ => _.PropertyName.StartsWith(startingWithChar.ToString())).ToList();

            return properties;
        }
    }

    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region CustomContractResolverUsage

        var person = new Person
        {
            FirstName = "Dennis",
            LastName = "Deepwater-Diver"
        };

        var startingWithF = JsonConvert.SerializeObject(person, Formatting.Indented,
            new JsonSerializerSettings {ContractResolver = new DynamicContractResolver('F')});

        Console.WriteLine(startingWithF);
        // {
        //   "FirstName": "Dennis",
        //   "FullName": "Dennis Deepwater-Diver"
        // }

        var startingWithL = JsonConvert.SerializeObject(person, Formatting.Indented,
            new JsonSerializerSettings {ContractResolver = new DynamicContractResolver('L')});

        Console.WriteLine(startingWithL);
        // {
        //   "LastName": "Deepwater-Diver"
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "LastName": "Deepwater-Diver"
            }
            """,
            startingWithL);
    }
}