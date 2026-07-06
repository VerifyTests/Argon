namespace Argon;

// ReSharper disable UnusedMember.Global
/// <summary>
/// Converts a <see cref="FSharpList{T}"/>.
/// </summary>
public class FSharpListConverter :
    JsonConverter
{
    static MethodInfo readList = typeof(FSharpListConverter).GetMethod("ReadList")!;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        foreach (var item in (IEnumerable)value)
        {
            serializer.Serialize(writer, item);
        }
        writer.WriteEndArray();
    }

    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var genericArgument = type.GetGenericArguments()[0];
        return readList.MakeGenericMethod(genericArgument)
            .Invoke(
                null,
                [
                reader,
                serializer
                ]);
    }

    public static FSharpList<T> ReadList<T>(JsonReader reader, JsonSerializer serializer)
    {
        var list = new List<T>();

        reader.Read();
        while (reader.TokenType != JsonToken.EndArray)
        {
            var item = serializer.Deserialize<T>(reader);

            list.Add(item);

            reader.Read();
        }

        return ListModule.OfSeq(list);
    }

    public override bool CanConvert(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(FSharpList<>);
}