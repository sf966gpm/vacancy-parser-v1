namespace Vacancy.Api.Messaging.RabbitMq;

public sealed class HhJobQueueUnavailableException(Exception innerException)
    : Exception("The HH parsing queue is unavailable.", innerException);
