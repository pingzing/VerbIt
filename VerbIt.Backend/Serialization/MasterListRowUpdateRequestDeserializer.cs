using System.Text.Json;
using System.Text.Json.Serialization;
using VerbIt.DataModels;

namespace VerbIt.Backend.Serialization
{
    public class MasterListRowUpdateRequestDeserializer : JsonConverter<MasterListRowUpdateRequest>
    {
        public override bool CanConvert(Type typeToConvert) => typeof(MasterListRowUpdateRequest).IsAssignableFrom(typeToConvert);

        public override MasterListRowUpdateRequest? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            using (var jsonDocument = JsonDocument.ParseValue(ref reader))
            {
                string propName = nameof(MasterListRowUpdateRequest.UpdateAction);
                propName = char.ToLowerInvariant(propName[0]) + propName.Substring(1);
                if (!jsonDocument.RootElement.TryGetProperty(propName, out JsonElement discriminatorProp))
                {
                    throw new JsonException();
                }

                if (!Enum.TryParse(discriminatorProp.GetString(), out MasterListRowUpdateAction discriminant))
                {
                    throw new JsonException();
                }

                JsonSerializerOptions newOpts = new(options);
                newOpts.Converters.Remove(newOpts.Converters.First(x => x is MasterListRowUpdateRequestDeserializer));

                if (discriminant == MasterListRowUpdateAction.Delete)
                {
                    MasterListRowDeleteRequest finalVal = jsonDocument.Deserialize<MasterListRowDeleteRequest>(newOpts)!;
                    return finalVal;
                }
                else if (discriminant == MasterListRowUpdateAction.Edit)
                {
                    MasterListRowEditRequest finalVal = jsonDocument.Deserialize<MasterListRowEditRequest>(newOpts)!;
                    return finalVal;
                }
                else
                {
                    throw new JsonException();
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, MasterListRowUpdateRequest value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, options);
        }
    }
}
