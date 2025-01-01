using Microsoft.Extensions.Options;

namespace SensorConsumer.Configuration.Validators;

public class MqttSettingsValidation : IValidateOptions<MqttSettings>
{
    public ValidateOptionsResult Validate(string name, MqttSettings options)
    {
        if (string.IsNullOrEmpty(options.Topics))
        {
            return ValidateOptionsResult.Fail("The TopicSchema cannot be null or empty.");
        }

        // Set up the TopicSchemas from the Topics string
        // split the topics string by comma
        string[] topics = options.Topics.Split(',');

        List<TopicSchema> schemas = new();
        foreach (string topic in topics)
        {
            string[] topicParts = topic.Split('/');
            TopicSchema schema = new()
            {
                TopicFilter = topic,
                SensorIdPosition = Array.IndexOf(topicParts, "<sensorId>"),
                MetadataNamePosition = Array.IndexOf(topicParts, "<metadataName>")
            };

            // Ensure it has a sensorId
            if (schema.SensorIdPosition == -1)
            {
                return ValidateOptionsResult.Fail("The TopicSchema must contain a <sensorId> placeholder.");
            }

            // replace the placeholders with the actual values
            schema.TopicFilter = schema.TopicFilter.Replace("<sensorId>", "+");

            if (!topic.Contains("<metadataName>"))
            {
                schema.TopicType = TopicType.Measurements;
            }
            else
            {
                schema.TopicType = TopicType.Metadata;
                // Ensure it has a metadataName
                if (schema.MetadataNamePosition == -1)
                {
                    return ValidateOptionsResult.Fail("The TopicSchema must contain a <metadataName> placeholder.");
                }
                schema.TopicFilter = schema.TopicFilter.Replace("<metadataName>", "+");
            }
            schemas.Add(schema);
        }

        options.TopicSchemas = schemas;

        return ValidateOptionsResult.Success;
    }
}