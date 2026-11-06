namespace Vacancy.Api.Messaging.RabbitMq;

public interface IHhJobPublisher
{
    Task PublishAsync(Guid messageId, string payloadJson, CancellationToken cancellationToken);
}
