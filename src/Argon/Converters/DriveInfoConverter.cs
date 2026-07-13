class DriveInfoConverter :
    JsonConverter<DriveInfo>
{
    public override void WriteJson(JsonWriter writer, DriveInfo value, JsonSerializer serializer) =>
        writer.WriteValue(value.Name.Replace('\\', '/'));

    public override DriveInfo ReadJson(JsonReader reader, Type type, DriveInfo? existingValue, bool hasExisting, JsonSerializer serializer) =>
        new(reader.GetConverterString(type));
}