using Microsoft.Extensions.Options;

namespace SensorMonitoring.Consumer.Configuration.Validators;

public class MqttSettingsValidation : IValidateOptions<MqttSettings>
{
    public ValidateOptionsResult Validate(string name, MqttSettings options)
    {
        if (string.IsNullOrEmpty(options.TopicSchema))
        {
            return ValidateOptionsResult.Fail("The TopicSchema cannot be null or empty.");
        }

        // Split the TopicSchema by '/'
        options.SplitTopicSchema = options.TopicSchema.Split('/');

        // Ensure it contains 'sensorId'
        int sensorIdIndex = Array.IndexOf(options.SplitTopicSchema, "sensorId");
        if (sensorIdIndex == -1)
        {
            return ValidateOptionsResult.Fail("The TopicSchema must contain 'sensorId'.");
        }

        // Save the location of 'sensorId' in SensorIdPosition
        options.SensorIdPosition = sensorIdIndex;

        return ValidateOptionsResult.Success;
    }
}