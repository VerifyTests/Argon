// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class SerializeTypeNameHandling : TestFixtureBase
{
    #region SerializeTypeNameHandlingTypes

    public abstract class Business
    {
        public string Name { get; set; }
    }

    public class Hotel : Business
    {
        public int Stars { get; set; }
    }

    public class Stockholder
    {
        public string FullName { get; set; }
        public IList<Business> Businesses { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region SerializeTypeNameHandlingUsage

        var stockholder = new Stockholder
        {
            FullName = "Steve Stockholder",
            Businesses =
                [
                new Hotel
                {
                    Name = "Hudson Hotel",
                    Stars = 4
                }
                ]
        };

        var jsonTypeNameAll = JsonConvert.SerializeObject(stockholder, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });

        Console.WriteLine(jsonTypeNameAll);
        // {
        //   "$type": "Argon.Samples.Stockholder, ArgonTests",
        //   "FullName": "Steve Stockholder",
        //   "Businesses": {
        //     "$type": "System.Collections.Generic.List`1[[Argon.Samples.Business, ArgonTests]], mscorlib",
        //     "$values": [
        //       {
        //         "$type": "Argon.Samples.Hotel, ArgonTests",
        //         "Stars": 4,
        //         "Name": "Hudson Hotel"
        //       }
        //     ]
        //   }
        // }

        var jsonTypeNameAuto = JsonConvert.SerializeObject(stockholder, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        Console.WriteLine(jsonTypeNameAuto);
        // {
        //   "FullName": "Steve Stockholder",
        //   "Businesses": [
        //     {
        //       "$type": "Argon.Samples.Hotel, ArgonTests",
        //       "Stars": 4,
        //       "Name": "Hudson Hotel"
        //     }
        //   ]
        // }

        // for security TypeNameHandling is required when deserializing
        var newStockholder = JsonConvert.DeserializeObject<Stockholder>(jsonTypeNameAuto, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        Console.WriteLine(newStockholder.Businesses[0].GetType().Name);
        // Hotel

        #endregion

        Assert.Equal("Hotel", newStockholder.Businesses[0].GetType().Name);
    }
}