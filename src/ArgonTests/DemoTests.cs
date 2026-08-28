// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.


// ReSharper disable UnusedVariable
// ReSharper disable RedundantAssignment

using Formatting = Argon.Formatting;

public class DemoTests : TestFixtureBase
{
    public class HtmlColor
    {
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
    }

    [Fact]
    public void JsonConverter()
    {
        var red = new HtmlColor
        {
            Red = 255,
            Green = 0,
            Blue = 0
        };

        var json = JsonConvert.SerializeObject(
            red,
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            });
        // {
        //   "Red": 255,
        //   "Green": 0,
        //   "Blue": 0
        // }

        json = JsonConvert.SerializeObject(
            red,
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters =
                {
                    new HtmlColorConverter()
                }
            });
        // "#FF0000"

        var r2 = JsonConvert.DeserializeObject<HtmlColor>(
            json,
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters =
                {
                    new HtmlColorConverter()
                }
            });
        Assert.Equal(255, r2.Red);
        Assert.Equal(0, r2.Green);
        Assert.Equal(0, r2.Blue);

        Assert.Equal(
            """
            "#FF0000"
            """,
            json);
    }

    public class PersonDemo
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Job { get; set; }
    }

    public class Session
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }

    public class HtmlColorConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // create hex string from value
            var color = (HtmlColor) value;
            var hexString = color.Red.ToString("X2")
                            + color.Green.ToString("X2")
                            + color.Blue.ToString("X2");

            // write value to json
            writer.WriteValue($"#{hexString}");
        }

        //public override object ReadJson(JsonReader reader, Type type,
        //    object existingValue, JsonSerializer serializer)
        //{
        //    throw new NotImplementedException();
        //}

        public override object ReadJson(
            JsonReader reader,
            Type type,
            object existingValue,
            JsonSerializer serializer)
        {
            // get hex string
            var hexString = (string) reader.Value;
            hexString = hexString.TrimStart('#');

            // build html color from hex
            return new HtmlColor
            {
                Red = Convert.ToInt32(hexString[..2], 16),
                Green = Convert.ToInt32(hexString.Substring(2, 2), 16),
                Blue = Convert.ToInt32(hexString.Substring(4, 2), 16)
            };
        }

        public override bool CanConvert(Type type) =>
            type == typeof(HtmlColor);
    }

    [Fact]
    public void SerializationGuide()
    {
        var roles = new List<string>
        {
            "User",
            "Admin"
        };

        var roleJson = JsonConvert.SerializeObject(roles, Formatting.Indented);
        // [
        //   "User",
        //   "Admin"
        // ]

        var dailyRegistrations = new Dictionary<DateTime, int>
        {
            {
                new(2014, 6, 1), 23
            },
            {
                new(2014, 6, 2), 50
            }
        };

        var regJson = JsonConvert.SerializeObject(dailyRegistrations, Formatting.Indented);
        // {
        //   "2014-06-01T00:00:00": 23,
        //   "2014-06-02T00:00:00": 50
        // }

        var c = new City
        {
            Name = "Oslo",
            Population = 650000
        };

        var cityJson = JsonConvert.SerializeObject(c, Formatting.Indented);
        // {
        //   "Name": "Oslo",
        //   "Population": 650000
        // }
    }

    [Fact]
    public void SerializationBasics()
    {
        var session = new Session
        {
            Name = "Serialize All The Things",
            Date = new(2014, 6, 4, 0, 0, 0, DateTimeKind.Utc)
        };

        var j = JsonConvert.SerializeObject(
            session,
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            });
        // {
        //   "Name": "Serialize All The Things",
        //   "Date": new Date(1401796800000)
        // }

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "Serialize All The Things",
              "Date": "2014-06-04T00:00:00Z"
            }
            """,
            j);
    }

    [Fact]
    public void DeserializationBasics1()
    {
        var j = """
                {
                  Name: 'Serialize All The Things',
                  Date: '2014-06-03'
                }
                """;

        var s = JsonConvert.DeserializeObject<Session>(j, new JsonSerializerSettings());
        // Name = Serialize All The Things
        // Date = Tuesday, 3 June 2014

        Assert.Equal("Serialize All The Things", s.Name);
    }

    public class City
    {
        public string Name { get; set; }
        public int Population { get; set; }
    }

    public class Employee
    {
        public string Name { get; set; }
    }

    public class Manager : Employee
    {
        public IList<Employee> Reportees { get; set; }
    }

    [Fact]
    public void SerializeReferencesByValue()
    {
        var arnie = new Employee
        {
            Name = "Arnie Admin"
        };
        var mike = new Manager
        {
            Name = "Mike Manager"
        };
        var susan = new Manager
        {
            Name = "Susan Supervisor"
        };

        mike.Reportees =
        [
            arnie,
            susan
        ];
        susan.Reportees =
        [
            arnie
        ];

        var json = JsonConvert.SerializeObject(mike, Formatting.Indented);
        // {
        //   "Reportees": [
        //     {
        //       "Name": "Arnie Admin"
        //     },
        //     {
        //       "Reportees": [
        //         {
        //           "Name": "Arnie Admin"
        //         }
        //       ],
        //       "Name": "Susan Supervisor"
        //     }
        //   ],
        //   "Name": "Mike Manager"
        // }

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Reportees": [
                {
                  "Name": "Arnie Admin"
                },
                {
                  "Reportees": [
                    {
                      "Name": "Arnie Admin"
                    }
                  ],
                  "Name": "Susan Supervisor"
                }
              ],
              "Name": "Mike Manager"
            }
            """,
            json);
    }

    [Fact]
    public void SerializeReferencesWithMetadata()
    {
        var arnie = new Employee
        {
            Name = "Arnie Admin"
        };
        var mike = new Manager
        {
            Name = "Mike Manager"
        };
        var susan = new Manager
        {
            Name = "Susan Supervisor"
        };

        mike.Reportees =
        [
            arnie,
            susan
        ];
        susan.Reportees =
        [
            arnie
        ];

        var json = JsonConvert.SerializeObject(
            mike,
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
        // {
        //   "$id": "1",
        //   "$type": "YourNamespace.Manager, YourAssembly",
        //   "Name": "Mike Manager",
        //   "Reportees": [
        //     {
        //       "$id": "2",
        //       "$type": "YourNamespace.Employee, YourAssembly",
        //       "Name": "Arnie Admin"
        //     },
        //     {
        //       "$id": "3",
        //       "$type": "YourNamespace.Manager, YourAssembly",
        //       "Name": "Susan Supervisor",
        //       "Reportees": [
        //         {
        //           "$ref": "2"
        //         }
        //       ]
        //     }
        //   ]
        // }

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "$type": "DemoTests+Manager, ArgonTests",
              "Reportees": [
                {
                  "$id": "2",
                  "$type": "DemoTests+Employee, ArgonTests",
                  "Name": "Arnie Admin"
                },
                {
                  "$id": "3",
                  "$type": "DemoTests+Manager, ArgonTests",
                  "Reportees": [
                    {
                      "$ref": "2"
                    }
                  ],
                  "Name": "Susan Supervisor"
                }
              ],
              "Name": "Mike Manager"
            }
            """,
            json);
    }

    [Fact]
    public void RoundtripTypesAndReferences()
    {
        var json = """
                   {
                     '$id': '1',
                     '$type': 'DemoTests+Manager, ArgonTests',
                     'Reportees': [
                       {
                         '$id': '2',
                         '$type': 'DemoTests+Employee, ArgonTests',
                         'Name': 'Arnie Admin'
                       },
                       {
                         '$id': '3',
                         '$type': 'DemoTests+Manager, ArgonTests',
                         'Reportees': [
                           {
                             '$ref': '2'
                           }
                         ],
                         'Name': 'Susan Supervisor'
                       }
                     ],
                     'Name': 'Mike Manager'
                   }
                   """;

        var e = JsonConvert.DeserializeObject<Employee>(
            json,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
        // Name = Mike Manager
        // Reportees = Arnie Admin, Susan Supervisor

        var mike = (Manager) e;
        var susan = (Manager) mike.Reportees[1];

        Assert.True(ReferenceEquals(mike.Reportees[0], susan.Reportees[0]));
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class House3
    {
        [JsonProperty]
        public string StreetAddress { get; set; }

        public int Bedrooms { get; set; }
        public decimal FloorArea { get; set; }
        public DateTime BuildDate { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class House2
    {
        [JsonProperty("address")]
        public string StreetAddress { get; set; }

        public int Bedrooms { get; set; }
        public decimal FloorArea { get; set; }
        public DateTime BuildDate { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class House4
    {
        [JsonProperty("address", Order = 2)]
        public string StreetAddress { get; set; }

        public int Bedrooms { get; set; }
        public decimal FloorArea { get; set; }

        [JsonProperty("buildDate", Order = 1)]
        public DateTime BuildDate { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class House5
    {
        [JsonProperty("address", Order = 2)]
        public string StreetAddress { get; set; }

        public int Bedrooms { get; set; }
        public decimal FloorArea { get; set; }

        [JsonProperty("buildDate", Order = 1)]
        public DateTime BuildDate { get; set; }
    }

    [Fact]
    public void SerializeAttributes()
    {
        var house = new House3
        {
            StreetAddress = "221B Baker Street",
            Bedrooms = 2,
            FloorArea = 100m,
            BuildDate = new(1890, 1, 1)
        };

        var json = JsonConvert.SerializeObject(house, Formatting.Indented);
        // {
        //   "StreetAddress": "221B Baker Street",
        //   "Bedrooms": 2,
        //   "FloorArea": 100.0,
        //   "BuildDate": "1890-01-01T00:00:00"
        // }

        // {
        //   "StreetAddress": "221B Baker Street"
        // }

        // {
        //   "address": "221B Baker Street"
        // }

        // {
        //   "buildDate": "1890-01-01T00:00:00",
        //   "address": "221B Baker Street"
        // }

        // {
        //   "buildDate": new Date(-2524568400000),
        //   "address": "221B Baker Street"
        // }
    }

    [Fact]
    public void SerializeDataTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("PackageId", typeof(string));
        dt.Columns.Add("Version", typeof(string));
        dt.Columns.Add("ReleaseDate", typeof(DateTime));

        dt.Rows.Add("Argon", "11.0.1", new DateTime(2018, 2, 17));
        dt.Rows.Add("Argon", "10.0.3", new DateTime(2017, 6, 18));

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        settings.AddDataSetConverters();

        var json = JsonConvert.SerializeObject(dt, settings);

        Console.WriteLine(json);
        // [
        //   {
        //     "PackageId": "Argon",
        //     "Version": "11.0.1",
        //     "ReleaseDate": "2018-02-17T00:00:00"
        //   },
        //   {
        //     "PackageId": "Argon",
        //     "Version": "10.0.3",
        //     "ReleaseDate": "2017-06-18T00:00:00"
        //   }
        // ]

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "PackageId": "Argon",
                "Version": "11.0.1",
                "ReleaseDate": "2018-02-17T00:00:00"
              },
              {
                "PackageId": "Argon",
                "Version": "10.0.3",
                "ReleaseDate": "2017-06-18T00:00:00"
              }
            ]
            """,
            json);
    }

    [Fact]
    public void JsonPathRegex()
    {
        var array = JArray.Parse(
            """
            [
              {
                "PackageId": "Argon",
                "Version": "11.0.1",
                "ReleaseDate": "2018-02-17T00:00:00"
              },
              {
                "PackageId": "NUnit",
                "Version": "3.9.0",
                "ReleaseDate": "2017-11-10T00:00:00"
              }
            ]
            """);

        var packages = array.SelectTokens("$.[?(@.PackageId =~ /^Argon/)]").ToList();

        Console.WriteLine(packages.Count);
        // 1

        Assert.Single(packages);
    }
}