using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Streamarr.Common.Serializer
{
    /// <summary>
    /// Deserializes JSON booleans or integers (0/1) to bool.
    /// Necessary because SQLite's json_object() stores booleans as integers.
    /// </summary>
    public class STJBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }

            if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var intValue))
            {
                return intValue != 0;
            }

            throw new JsonException($"Cannot convert token type '{reader.TokenType}' to bool.");
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
}
