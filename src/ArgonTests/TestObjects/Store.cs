// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class Store
{
    public StoreColor Color = StoreColor.Yellow;
    public DateTime Established = new(2010, 1, 22, 1, 1, 1, DateTimeKind.Utc);
    public double Width = 1.1;
    public int Employees = 999;
    public int[] RoomsPerFloor = [1, 2, 3, 4, 5, 6, 7, 8, 9];
    public bool Open;
    public char Symbol = '@';

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<string> Mottos = [];

    public decimal Cost = 100980.1M;
    public string Escape = "\r\n\t\f\b?{\\r\\n\"\'";

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<Product> product = [];

    public Store()
    {
        Mottos.Add("Hello World");
        Mottos.Add("öäüÖÄÜ\\'{new Date(12345);}[222]_µ@²³~");
        Mottos.Add(null);
        Mottos.Add(" ");

        var rocket = new Product
        {
            Name = "Rocket",
            ExpiryDate = new(2000, 2, 2, 23, 1, 30, DateTimeKind.Utc)
        };
        var alien = new Product
        {
            Name = "Alien"
        };

        product.Add(rocket);
        product.Add(alien);
    }
}