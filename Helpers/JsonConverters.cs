using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace MqqtConsumer.Helpers;

public class UnixTimestampConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            // Try to parse the string as a long (Unix timestamp)
            if (long.TryParse(reader.GetString(), out long unixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            }

            throw new JsonException("Invalid Unix timestamp string.");
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            long unixTime = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
        }

        throw new JsonException("Expected Unix timestamp as a string or number.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Convert DateTime to Unix timestamp (long)
        var unixTimestamp = new DateTimeOffset(value).ToUnixTimeSeconds();
        writer.WriteNumberValue(unixTimestamp);
    }
}
