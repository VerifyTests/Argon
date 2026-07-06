namespace Argon;

// ReSharper disable UnusedMember.Global
/// <summary>
/// Converts a <see cref="FSharpMap{TKey,TValue}"/>.
/// </summary>
public class FSharpMapConverter :
    JsonConverter
{
    static MethodInfo writeMap = typeof(FSharpMapConverter).GetMethod("WriteMap")!;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var genericArguments = value.GetType().GetGenericArguments();
        writeMap.MakeGenericMethod(genericArguments[0], genericArguments[1])
            .Invoke(
                null,
                [writer, value, serializer]);
    }

    public static void WriteMap<T, K>(JsonWriter writer, FSharpMap<T, K> value, JsonSerializer serializer)
        where T : notnull =>
        serializer.Serialize(writer, value.ToDictionary(_ => _.Key, _ => _.Value));

    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var arguments = type.GetGenericArguments();
        return readMap.MakeGenericMethod(arguments[0], arguments[1])
            .Invoke(
                null,
                [reader, serializer]);
    }

    static MethodInfo readMap = typeof(FSharpMapConverter).GetMethod("ReadMap")!;

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