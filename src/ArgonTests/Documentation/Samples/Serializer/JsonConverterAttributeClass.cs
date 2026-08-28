// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class JsonConverterAttributeClass : TestFixtureBase
{
    #region JsonConverterAttributeClassTypes

    public class UserConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var user = (User) value;

            writer.WriteValue(user.UserName);
        }

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
        {
            var user = new User
            {
                UserName = (string) reader.Value
            };

            return user;
        }

        public override bool CanConvert(Type type) =>
            type == typeof(User);
    }

    [JsonConverter(typeof(UserConverter))]
    public class User
    {
        public string UserName { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region JsonConverterAttributeClassUsage

        var user = new User
        {
            UserName = @"domain\username"
        };

        var json = JsonConvert.SerializeObject(user, Formatting.Indented);

        Console.WriteLine(json);
        // "domain\\username"

        #endregion

        Assert.Equal(
            """
            "domain\\username"
            """,
            json);
    }
}