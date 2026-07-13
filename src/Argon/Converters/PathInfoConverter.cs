// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable RedundantSuppressNullableWarningExpression
class PathInfoConverter :
    JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
        writer.WriteValue(value.ToString()!.Replace('\\', '/'));

    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.Null or JsonToken.Undefined)
        {
            return null;
        }

        // only a genuine null maps to null; a non-string token (number, bool, ...) would otherwise
        // be silently dropped to null, losing data instead of reporting the mismatch
        if (reader.Value is not string value)
        {
            throw JsonSerializationException.Create(reader, $"Unexpected token {reader.TokenType} when parsing a {type.Name}. Expected a string or null.");
        }

        var path = value.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if (type == typeof(DirectoryInfo))
        {
            return new DirectoryInfo(path);
        }

        return new FileInfo(path);
    }

    public override bool CanConvert(Type type) =>
        type == typeof(FileInfo) ||
        type == typeof(DirectoryInfo);
}