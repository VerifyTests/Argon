// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class CustomJsonConverter : TestFixtureBase
{
    #region CustomJsonConverterTypes

    public class KeysJsonConverter(params Type[] types) : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var token = JToken.FromObject(value);

            if (token.Type == JTokenType.Object)
            {
                var o = (JObject) token;
                var propertyNames = o.Properties().Select(_ => _.Name).ToList();

                o.AddFirst(new JProperty("Keys", new JArray(propertyNames)));

                o.WriteTo(writer);
            }
            else
            {
                token.WriteTo(writer);
            }
        }

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");

        public override bool CanRead => false;

        public override bool CanConvert(Type type) =>
            types.Any(t => t == type);
    }

    public class Employee
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public IList<string> Roles { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region CustomJsonConverterUsage

        var employee = new Employee
        {
            FirstName = "James",
            LastName = "Newton-King",
            Roles =
            [
                "Admin"
            ]
        };

        var json = JsonConvert.SerializeObject(employee, Formatting.Indented, new KeysJsonConverter(typeof(Employee)));

        Console.WriteLine(json);
        // {
        //   "Keys": [
        //     "FirstName",
        //     "LastName",
        //     "Roles"
        //   ],
        //   "FirstName": "James",
        //   "LastName": "Newton-King",
        //   "Roles": [
        //     "Admin"
        //   ]
        // }

        var newEmployee = JsonConvert.DeserializeObject<Employee>(json, new KeysJsonConverter(typeof(Employee)));

        Console.WriteLine(newEmployee.FirstName);
        // James

        #endregion

        Assert.Equal("James", newEmployee.FirstName);
    }
}