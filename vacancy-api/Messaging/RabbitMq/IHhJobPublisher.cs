using Vacancy.Api.Features.HhJobs;

namespace Vacancy.Api.Messaging.RabbitMq;

public interface IHhJobPublisher
{
    Task PublishAsync(CreateHhParseJobRequest request, CancellationToken cancellationToken);
}
