namespace Argon;

// ReSharper disable UnusedMember.Global
/// <summary>
/// Converts a <see cref="FSharpMap{TKey,TValue}"/>.
/// </summary>
public class FSharpMapConverter :
    JsonConverter
{
    // cached closed delegates: MakeGenericMethod + Invoke per call allocates and wraps
    // exceptions in TargetInvocationException
    static ThreadSafeStore<Type, Action<JsonWriter, object, JsonSerializer>> writeMapCache = new(CreateWriteMapDelegate);

    static Action<JsonWriter, object, JsonSerializer> CreateWriteMapDelegate(Type mapType)
    {
        var arguments = mapType.GetGenericArguments();
        var method = typeof(FSharpMapConverter)
            .GetMethod(nameof(WriteMapBoxed), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(arguments[0], arguments[1]);
        return (Action<JsonWriter, object, JsonSerializer>) Delegate.CreateDelegate(
            typeof(Action<JsonWriter, object, JsonSerializer>),
            method);
    }

    static void WriteMapBoxed<T, K>(JsonWriter writer, object value, JsonSerializer serializer)
        where T : notnull =>
        WriteMap(writer, (FSharpMap<T, K>) value, serializer);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
        writeMapCache.Get(value.GetType())(writer, value, serializer);

    public static void WriteMap<T, K>(JsonWriter writer, FSharpMap<T, K> value, JsonSerializer serializer)
        where T : notnull =>
        serializer.Serialize(writer, value.ToDictionary(_ => _.Key, _ => _.Value));

    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        return readMapCache.Get(type)(reader, serializer);
    }

    static MethodInfo readMap = typeof(FSharpMapConverter).GetMethod("ReadMap")!;

    static ThreadSafeStore<Type, Func<JsonReader, JsonSerializer, object>> readMapCache = new(CreateReadMapDelegate);

    static Func<JsonReader, JsonSerializer, object> CreateReadMapDelegate(Type mapType)
    {
        var arguments = mapType.GetGenericArguments();
        return (Func<JsonReader, JsonSerializer, object>) Delegate.CreateDelegate(
            typeof(Func<JsonReader, JsonSerializer, object>),
            readMap.MakeGenericMethod(arguments[0], arguments[1]));
    }

    public static FSharpMap<T, K> ReadMap<T, K>(JsonReader reader, JsonSerializer serializer)
        where T : notnull
    {
        var dictionary = serializer.Deserialize<Dictionary<T, K>>(reader);

        return MapModule.OfSeq(dictionary.Select(_ => new Tuple<T, K>(_.Key, _.Value)));
    }

    public override bool CanConvert(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(FSharpMap<,>);
}