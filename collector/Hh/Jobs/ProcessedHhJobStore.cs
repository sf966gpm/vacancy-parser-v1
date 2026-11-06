using System.Text;
using System.Text.Json;

namespace Collector.Playwright.Hh.Jobs;

public sealed class ProcessedHhJobStore : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<Guid, HhJobStatusMessage> _terminalResults = [];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private FileStream _stream = null!;
    private StreamWriter _writer = null!;

    private ProcessedHhJobStore() { }

    public static async Task<ProcessedHhJobStore> OpenAsync(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        var receiptPath = Path.Combine(directoryPath, "processed-hh-jobs.jsonl");
        var store = new ProcessedHhJobStore();
        if (File.Exists(receiptPath))
        {
            var contents = await File.ReadAllTextAsync(receiptPath);
            foreach (var line in contents.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var result = JsonSerializer.Deserialize<HhJobStatusMessage>(line.TrimEnd('\r'), JsonOptions);
                    if (result is not null && result.JobId != Guid.Empty)
                    {
                        store._terminalResults[result.JobId] = result;
                    }
                }
                catch (JsonException)
                {
                    // Ignore an incomplete final line left by a process interruption.
                }
            }

            if (contents.Length > 0 && contents[^1] != '\n')
            {
                await File.AppendAllTextAsync(receiptPath, Environment.NewLine);
            }
        }

        store._stream = new FileStream(receiptPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        store._writer = new StreamWriter(
            store._stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return store;
    }

    public async Task<HhJobStatusMessage?> GetAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _terminalResults.GetValueOrDefault(jobId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<HhJobStatusMessage> RecordAsync(
        HhJobStatusMessage result,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_terminalResults.TryGetValue(result.JobId, out var existing))
            {
                return existing;
            }

            await _writer.WriteLineAsync(JsonSerializer.Serialize(result, JsonOptions));
            await _writer.FlushAsync(cancellationToken);
            _stream.Flush(true);
            _terminalResults.Add(result.JobId, result);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync();
        _gate.Dispose();
    }
}
