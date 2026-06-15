using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppGen.Core.Models;

public sealed class UiTargetJsonConverter : JsonConverter<UiTarget>
{
    private static UiTarget ParseUiTargetName(string name)
    {
        if (string.Equals(name, "BlazorWeb", StringComparison.OrdinalIgnoreCase))
            return UiTarget.MvcWeb;

        return Enum.Parse<UiTarget>(name, ignoreCase: true);
    }

    public override UiTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return UiTarget.None;

        if (reader.TokenType == JsonTokenType.String)
        {
            var single = reader.GetString();
            return string.IsNullOrWhiteSpace(single)
                ? UiTarget.None
                : ParseUiTargetName(single);
        }

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("uiTargets must be a JSON array or string.");

        var value = UiTarget.None;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            if (reader.TokenType != JsonTokenType.String)
                continue;
            var name = reader.GetString();
            if (string.IsNullOrWhiteSpace(name))
                continue;
            value |= ParseUiTargetName(name);
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, UiTarget value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (UiTarget flag in Enum.GetValues<UiTarget>())
        {
            if (flag == UiTarget.None)
                continue;
            if (value.HasFlag(flag))
                writer.WriteStringValue(flag.ToString());
        }
        writer.WriteEndArray();
    }
}
