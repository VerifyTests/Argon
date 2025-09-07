// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Dynamic;

public class DynamicTests : TestFixtureBase
{
    [Fact]
    public void SerializeDynamicObject()
    {
        var dynamicObject = new TestDynamicObject
        {
            Explicit = true
        };

        dynamic d = dynamicObject;
        d.Int = 1;
        d.Decimal = 99.9d;
        d.ChildObject = new DynamicChildObject();

        var values = new Dictionary<string, object>();

        var c = DefaultContractResolver.Instance;
        var dynamicContract = (JsonDynamicContract) c.ResolveContract(dynamicObject.GetType());
        Assert.NotNull(dynamicContract);

        foreach (var memberName in dynamicObject.GetDynamicMemberNames())
        {
            JsonDynamicContract.TryGetMember(dynamicObject, memberName, out var value);

            values.Add(memberName, value);
        }

        Assert.Equal(d.Int, values["Int"]);
        Assert.Equal(d.Decimal, values["Decimal"]);
        Assert.Equal(d.ChildObject, values["ChildObject"]);

        var json = JsonConvert.SerializeObject(dynamicObject, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Explicit": true,
              "Decimal": 99.9,
              "Int": 1,
              "ChildObject": {
                "Text": null,
                "Integer": 0
              }
            }
            """,
            json);

        var newDynamicObject = JsonConvert.DeserializeObject<TestDynamicObject>(json);
        Assert.True(newDynamicObject.Explicit);

        d = newDynamicObject;

        Assert.Equal(99.9, d.Decimal);
        Assert.Equal(1, d.Int);
        Assert.Equal(dynamicObject.ChildObject.Integer, d.ChildObject.Integer);
        Assert.Equal(dynamicObject.ChildObject.Text, d.ChildObject.Text);
    }

    [Fact]
    public void SerializeDynamicObjectWithObjectTracking()
    {
        dynamic o = new ExpandoObject();
        o.Text = "Text!";
        o.Integer = int.MaxValue;
        o.DynamicChildObject = new DynamicChildObject
        {
            Integer = int.MinValue,
            Text = "Child text!"
        };

        string json = JsonConvert.SerializeObject(o, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        });

        var dynamicChildObjectTypeName = typeof(DynamicChildObject).GetTypeName(TypeNameAssemblyFormatHandling.Full, null);
        var expandoObjectTypeName = typeof(ExpandoObject).GetTypeName(TypeNameAssemblyFormatHandling.Full, null);

        XUnitAssert.AreEqualNormalized($$"""
            {
              "$type": "{{expandoObjectTypeName}}",
              "Text": "Text!",
              "Integer": 2147483647,
              "DynamicChildObject": {
                "$type": "{{dynamicChildObjectTypeName}}",
                "Text": "Child text!",
                "Integer": -2147483648
              }
            }
            """,
            json);

        dynamic n = JsonConvert.DeserializeObject(json, null, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        });

        Assert.IsType<ExpandoObject>(n);
        Assert.Equal("Text!", n.Text);
        Assert.Equal(int.MaxValue, n.Integer);

        Assert.IsType<DynamicChildObject>(n.DynamicChildObject);
        Assert.Equal("Child text!", n.DynamicChildObject.Text);
        Assert.Equal(int.MinValue, n.DynamicChildObject.Integer);
    }

    [Fact]
    public void NoPublicDefaultConstructor()
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        var json = """
                   {
                     "contributors": null
                   }
                   """;

        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DynamicObject>(json, settings));
        Assert.Equal("Unable to find a default constructor to use for type System.Dynamic.DynamicObject. Path 'contributors', line 2, position 17.", exception.Message);
    }

    public class DictionaryDynamicObject : DynamicObject
    {
        public IDictionary<string, object> Values { get; }

        protected DictionaryDynamicObject() =>
            Values = new Dictionary<string, object>();

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Values[binder.Name] = value;
            return true;
        }
    }

    [Fact]
    public void AllowNonPublicDefaultConstructor()
    {
        var settings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        var json = """
            {
              "contributors": null,
              "retweeted": false,
              "text": "Guys SX4 diesel is launched.what are your plans?catch us at #facebook http://bit.ly/dV3H1a #auto #car #maruti #india #delhi",
              "in_reply_to_user_id_str": null,
              "retweet_count": 0,
              "geo": null,
              "id_str": "40678260320768000",
              "in_reply_to_status_id": null,
              "source": "<a href=\"http://www.tweetdeck.com\" rel=\"nofollow\">TweetDeck</a>",
              "created_at": "Thu Feb 24 07:43:47 +0000 2011",
              "place": null,
              "coordinates": null,
              "truncated": false,
              "favorited": false,
              "user": {
                "profile_background_image_url": "http://a1.twimg.com/profile_background_images/206944715/twitter_bg.jpg",
                "url": "http://bit.ly/dcFwWC",
                "screen_name": "marutisuzukisx4",
                "verified": false,
                "friends_count": 45,
                "description": "This is the Official Maruti Suzuki SX4 Twitter ID! Men are Back - mail us on social (at) sx4bymaruti (dot) com",
                "follow_request_sent": null,
                "time_zone": "Chennai",
                "profile_text_color": "333333",
                "location": "India",
                "notifications": null,
                "profile_sidebar_fill_color": "efefef",
                "id_str": "196143889",
                "contributors_enabled": false,
                "lang": "en",
                "profile_background_tile": false,
                "created_at": "Tue Sep 28 12:55:15 +0000 2010",
                "followers_count": 117,
                "show_all_inline_media": true,
                "listed_count": 1,
                "geo_enabled": true,
                "profile_link_color": "009999",
                "profile_sidebar_border_color": "eeeeee",
                "protected": false,
                "name": "Maruti Suzuki SX4",
                "statuses_count": 637,
                "following": null,
                "profile_use_background_image": true,
                "profile_image_url": "http://a3.twimg.com/profile_images/1170694644/Slide1_normal.JPG",
                "id": 196143889,
                "is_translator": false,
                "utc_offset": 19800,
                "favourites_count": 0,
                "profile_background_color": "131516"
              },
              "in_reply_to_screen_name": null,
              "id": 40678260320768000,
              "in_reply_to_status_id_str": null,
              "in_reply_to_user_id": null
            }
            """;

        var foo = JsonConvert.DeserializeObject<DictionaryDynamicObject>(json, settings);

        Assert.False((bool) foo.Values["retweeted"]);
    }

    [Fact]
    public void SerializeDynamicObjectWithNullValueHandlingIgnore()
    {
        dynamic o = new TestDynamicObject();
        o.Text = "Text!";
        o.Int = int.MaxValue;
        o.ChildObject = null; // Tests an explicitly defined property of a dynamic object with a null value.
        o.DynamicChildObject = null; // vs. a completely dynamic defined property.

        string json = JsonConvert.SerializeObject(o, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Explicit": false,
              "Text": "Text!",
              "Int": 2147483647
            }
            """,
            json);
    }

    [Fact]
    public void SerializeDynamicObjectWithNullValueHandlingInclude()
    {
        dynamic o = new TestDynamicObject();
        o.Text = "Text!";
        o.Int = int.MaxValue;
        o.ChildObject = null; // Tests an explicitly defined property of a dynamic object with a null value.
        o.DynamicChildObject = null; // vs. a completely dynamic defined property.

        string json = JsonConvert.SerializeObject(o, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Explicit": false,
              "Text": "Text!",
              "DynamicChildObject": null,
              "Int": 2147483647,
              "ChildObject": null
            }
            """,
            json);
    }

    [Fact]
    public void SerializeDynamicObjectWithDefaultValueHandlingIgnore()
    {
        dynamic o = new TestDynamicObject();
        o.Text = "Text!";
        o.Int = int.MaxValue;
        o.IntDefault = 0;
        o.NUllableIntDefault = default(int?);
        o.ChildObject = null; // Tests an explicitly defined property of a dynamic object with a null value.
        o.DynamicChildObject = null; // vs. a completely dynamic defined property.

        string json = JsonConvert.SerializeObject(o, Formatting.Indented, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Text": "Text!",
              "Int": 2147483647
            }
            """,
            json);
    }
}

public class DynamicChildObject
{
    public string Text { get; set; }
    public int Integer { get; set; }
}

public class TestDynamicObject : DynamicObject
{
    public int Int;

    [JsonProperty] public bool Explicit;

    public DynamicChildObject ChildObject { get; set; }

    internal Dictionary<string, object> Members { get; } = [];

    public override IEnumerable<string> GetDynamicMemberNames() =>
        Members.Keys.Union(["Int", "ChildObject"]);

    public override bool TryConvert(ConvertBinder binder, out object result)
    {
        var targetType = binder.Type;

        if (targetType == typeof(IDictionary<string, object>) ||
            targetType == typeof(IDictionary))
        {
            result = new Dictionary<string, object>(Members);
            return true;
        }

        return base.TryConvert(binder, out result);
    }

    public override bool TryDeleteMember(DeleteMemberBinder binder) =>
        Members.Remove(binder.Name);

    public override bool TryGetMember(GetMemberBinder binder, out object result) =>
        Members.TryGetValue(binder.Name, out result);

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        Members[binder.Name] = value;
        return true;
    }

    public class ErrorSettingDynamicObject : DynamicObject
    {
        public override bool TrySetMember(SetMemberBinder binder, object value) =>
            false;
    }
}