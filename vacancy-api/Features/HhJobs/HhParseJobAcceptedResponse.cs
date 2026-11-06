namespace Vacancy.Api.Features.HhJobs;

/// <summary>Indicates that the HH parsing job was accepted by the queue.</summary>
public sealed record HhParseJobAcceptedResponse(string Status);
