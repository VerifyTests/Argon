// Copyright 2017 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Argon.NodaTime;
using NodaTime;
using NodaTime.Text;
using Formatting = Argon.Formatting;

public class DelegatingConverterBaseTest
{
    [Fact]
    public void Serialize()
    {
        var expected = "{'ShortDate':'2017-02-20','LongDate':'20 February 2017'}"
            .Replace("'", "\"");
        var date = new LocalDate(2017, 2, 20);
        var entity = new Entity {ShortDate = date, LongDate = date};
        var actual = JsonConvert.SerializeObject(entity, Formatting.None);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Deserialize()
    {
        var json = "{'ShortDate':'2017-02-20','LongDate':'20 February 2017'}"
            .Replace("'", "\"");
        var expectedDate = new LocalDate(2017, 2, 20);
        var entity = JsonConvert.DeserializeObject<Entity>(json);
        Assert.Equal(expectedDate, entity.ShortDate);
        Assert.Equal(expectedDate, entity.LongDate);
    }

    public class Entity
    {
        [JsonConverter(typeof(ShortDateConverter))]
        public LocalDate ShortDate { get; set; }

        [JsonConverter(typeof(LongDateConverter))]
        public LocalDate LongDate { get; set; }
    }

    public class ShortDateConverter() : DelegatingConverterBase(NodaConverters.LocalDateConverter);

    public class LongDateConverter() : DelegatingConverterBase(converter)
    {
        // No need to create a new one of these each time...
        static readonly JsonConverter converter =
            new NodaPatternConverter<LocalDate>(LocalDatePattern.CreateWithInvariantCulture("d MMMM yyyy"));
    }
}