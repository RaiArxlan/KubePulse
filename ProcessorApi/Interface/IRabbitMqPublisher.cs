namespace ProcessorApi.Interface;

public interface IRabbitMqPublisher
{
    Task Publish(string queueName, string message);
}
