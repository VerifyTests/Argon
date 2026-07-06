// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon.DataSets;

/// <summary>
/// Converts a <see cref="DataTable" /> to and from JSON.
/// </summary>
public class DataTableConverter :
    JsonConverter
{
    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var table = (DataTable) value;
        var resolver = serializer.ContractResolver as DefaultContractResolver;

        // resolve column names once per table, not once per cell
        var columnCount = table.Columns.Count;
        var columns = new DataColumn[columnCount];
        var resolvedColumnNames = new string[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            var column = table.Columns[i];
            columns[i] = column;
            resolvedColumnNames[i] = resolver?.GetResolvedPropertyName(column.ColumnName) ?? column.ColumnName;
        }

        writer.WriteStartArray();

        foreach (DataRow row in table.Rows)
        {
            writer.WriteStartObject();
            for (var i = 0; i < columnCount; i++)
            {
                var columnValue = row[columns[i]];

                if (serializer.NullValueHandling == NullValueHandling.Ignore &&
                    columnValue == DBNull.Value)
                {
                    continue;
                }

                writer.WritePropertyName(resolvedColumnNames[i]);
                serializer.Serialize(writer, columnValue);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (existingValue is not DataTable table)
        {
            // handle typed datasets
            if (type == typeof(DataTable))
            {
                table = new();
            }
            else
            {
                table = (DataTable) Activator.CreateInstance(type)!;
            }
        }

        // DataTable is inside a DataSet
        // populate the name from the property name
        if (reader.TokenType == JsonToken.PropertyName)
        {
            table.TableName = reader.StringValue;

            reader.ReadAndAssert();

            if (reader.TokenType == JsonToken.Null)
            {
                return table;
            }
        }

        if (reader.TokenType != JsonToken.StartArray)
        {
            throw JsonSerializationException.Create(reader, $"Unexpected JSON token when reading DataTable. Expected StartArray, got {reader.TokenType}.");
        }

        reader.ReadAndAssert();

        while (reader.TokenType != JsonToken.EndArray)
        {
            CreateRow(reader, table, serializer);

            reader.ReadAndAssert();
        }

        return table;
    }

    static void CreateRow(JsonReader reader, DataTable table, JsonSerializer serializer)
    {
        var row = table.NewRow();
        reader.ReadAndAssert();

        while (reader.TokenType == JsonToken.PropertyName)
        {
            var columnName = reader.StringValue;

            reader.ReadAndAssert();

            var column = GetColumn(reader, table, columnName);

            if (column.DataType == typeof(DataTable))
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.ReadAndAssert();
                }

                var nestedTable = new DataTable();

                while (reader.TokenType != JsonToken.EndArray)
                {
                    CreateRow(reader, nestedTable, serializer);

                    reader.ReadAndAssert();
                }

                row[columnName] = nestedTable;
            }
            else if (column.DataType.IsArray &&
                     column.DataType != typeof(byte[]))
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.ReadAndAssert();
                }

                var list = new List<object?>();

                while (reader.TokenType != JsonToken.EndArray)
                {
                    list.Add(reader.Value);
                    reader.ReadAndAssert();
                }

                var destinationArray = Array.CreateInstance(column.DataType.GetElementType()!, list.Count);
                ((IList) list).CopyTo(destinationArray, 0);

                row[columnName] = destinationArray;
            }
            else
            {
                var columnValue = GetColumnValue(reader, serializer, column);

                row[columnName] = columnValue;
            }

            reader.ReadAndAssert();
        }

        row.EndEdit();
        table.Rows.Add(row);
    }

    static object GetColumnValue(JsonReader reader, JsonSerializer serializer, DataColumn column)
    {
        if (reader.Value == null)
        {
            return DBNull.Value;
        }

        return serializer.TryDeserialize(reader, column.DataType) ?? DBNull.Value;
    }

    static DataColumn GetColumn(JsonReader reader, DataTable table, string name)
    {
        var column = table.Columns[name];
        if (column != null)
        {
            return column;
        }

        var columnType = GetColumnDataType(reader);
        column = new(name, columnType);
        table.Columns.Add(column);

        return column;
    }

    static Type GetColumnDataType(JsonReader reader)
    {
        var tokenType = reader.TokenType;

        switch (tokenType)
        {
            case JsonToken.Integer:
            case JsonToken.Boolean:
            case JsonToken.Float:
            case JsonToken.String:
            case JsonToken.Date:
            case JsonToken.Bytes:
                return reader.ValueType!;
            case JsonToken.Null:
            case JsonToken.Undefined:
            case JsonToken.EndArray:
                return typeof(string);
            case JsonToken.StartArray:
                reader.ReadAndAssert();
                if (reader.TokenType == JsonToken.StartObject)
                {
                    // nested datatable
                    return typeof(DataTable);
                }

                var arrayType = GetColumnDataType(reader);
                return arrayType.MakeArrayType();
            default:
                throw JsonSerializationException.Create(reader, $"Unexpected JSON token when reading DataTable: {tokenType}");
        }
    }

    /// <summary>
    /// Determines whether this instance can convert the specified value type.
    /// </summary>
    /// <param name="valueType">Type of the value.</param>
    /// <returns>
    /// <c>true</c> if this instance can convert the specified value type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type valueType) =>
        typeof(DataTable).IsAssignableFrom(valueType);
}