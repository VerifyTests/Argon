// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class DeserializeObjectCreationHandling : TestFixtureBase
{
    #region DeserializeObjectCreationHandlingTypes

    public class UserViewModel
    {
        public string Name { get; set; }
        public IList<string> Offices { get; } =
        [
            "Auckland",
            "Wellington",
            "Christchurch"
        ];
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region DeserializeObjectCreationHandlingUsage

        var json = """
            {
              'Name': 'James',
              'Offices': [
                'Auckland',
                'Wellington',
                'Christchurch'
              ]
            }
            """;

        var model1 = JsonConvert.DeserializeObject<UserViewModel>(json);

        foreach (var office in model1.Offices)
        {
            Console.WriteLine(office);
        }
        // Auckland
        // Wellington
        // Christchurch
        // Auckland
        // Wellington
        // Christchurch

        var model2 = JsonConvert.DeserializeObject<UserViewModel>(json, new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        });

        foreach (var office in model2.Offices)
        {
            Console.WriteLine(office);
        }

        // Auckland
        // Wellington
        // Christchurch

        #endregion

        Assert.Equal(3, model2.Offices.Count);
    }
}