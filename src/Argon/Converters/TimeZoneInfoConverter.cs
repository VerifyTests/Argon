class TimeZoneInfoConverter :
    JsonConverter<TimeZoneInfo>
{
    public override void WriteJson(JsonWriter writer, TimeZoneInfo value, JsonSerializer serializer) =>
        writer.WriteValue(value.Id);

    public override TimeZoneInfo ReadJson(JsonReader reader, Type type, TimeZoneInfo? existingValue, bool hasExisting, JsonSerializer serializer) =>
        TimeZoneInfo.FindSystemTimeZoneById(reader.GetConverterString(type));
}