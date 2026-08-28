// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class SerializeSerializationBinder : TestFixtureBase
{
    #region SerializeSerializationBinderTypes

    public class KnownTypesBinder : ISerializationBinder
    {
        public IList<Type> KnownTypes { get; set; }

        public Type BindToType(string assemblyName, string typeName) =>
            KnownTypes.SingleOrDefault(t => t.Name == typeName);

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }

    public class Car
    {
        public string Maker { get; set; }
        public string Model { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region SerializeSerializationBinderUsage

        var knownTypesBinder = new KnownTypesBinder
        {
            KnownTypes = [typeof(Car)]
        };

        var car = new Car
        {
            Maker = "Ford",
            Model = "Explorer"
        };

        var json = JsonConvert.SerializeObject(car, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            SerializationBinder = knownTypesBinder
        });

        Console.WriteLine(json);
        // {
        //   "$type": "Car",
        //   "Maker": "Ford",
        //   "Model": "Explorer"
        // }

        var newValue = JsonConvert.DeserializeObject(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            SerializationBinder = knownTypesBinder
        });

        Console.WriteLine(newValue.GetType().Name);
        // Car

        #endregion

        Assert.Equal("Car", newValue.GetType().Name);
    }
}