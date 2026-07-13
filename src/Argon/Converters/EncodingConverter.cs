namespace Argon;

public class EncodingConverter :
    JsonConverter<Encoding>
{
    public override void WriteJson(JsonWriter writer, Encoding value, JsonSerializer serializer) =>
        writer.WriteValue(value.WebName);

    public override Encoding ReadJson(JsonReader reader, Type type, Encoding? existingValue, bool hasExisting, JsonSerializer serializer) =>
        Encoding.GetEncoding(reader.GetConverterString(type));
}