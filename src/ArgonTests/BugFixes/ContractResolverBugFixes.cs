// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class ContractResolverBugFixes : TestFixtureBase
{
    // Bug: CreateProperties used List.Sort (unstable introsort) so properties without an
    // explicit Order lost their declaration order once the type had more than 16 members
    // and any property carried an Order.

    [Fact]
    public void Property_order_is_stable_when_a_property_has_order()
    {
        var json = JsonConvert.SerializeObject(new ManyProperties());
        var names = JObject.Parse(json)
            .Properties()
            .Select(_ => _.Name)
            .ToList();

        var expected = new List<string>();
        for (var i = 1; i <= 18; i++)
        {
            expected.Add($"P{i:D2}");
        }

        // The ordered property sorts last (Order 1 > the implicit -1 of the rest),
        // and the unordered properties keep declaration order.
        expected.Add("Ordered");

        Assert.Equal(expected, names);
    }

    [Fact]
    public void Default_converters_contains_a_single_encoding_converter()
    {
        var count = DefaultContractResolver.Converters.Count(_ => _.GetType().Name == "EncodingConverter");

        Assert.Equal(1, count);
    }

    public class ManyProperties
    {
        public int P01 { get; set; }
        public int P02 { get; set; }
        public int P03 { get; set; }
        public int P04 { get; set; }
        public int P05 { get; set; }
        public int P06 { get; set; }
        public int P07 { get; set; }
        public int P08 { get; set; }
        public int P09 { get; set; }
        public int P10 { get; set; }
        public int P11 { get; set; }
        public int P12 { get; set; }
        public int P13 { get; set; }
        public int P14 { get; set; }
        public int P15 { get; set; }
        public int P16 { get; set; }
        public int P17 { get; set; }
        public int P18 { get; set; }

        [JsonProperty(Order = 1)]
        public int Ordered { get; set; }
    }
}
