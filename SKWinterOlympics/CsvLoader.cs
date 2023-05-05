using CsvHelper;

namespace SKWinterOlympics;

public class CsvLoader
{
    const string CsvUrl = "https://cdn.openai.com/API/examples/data/winter_olympics_2022.csv";
    const string CsvFileName = @"TestData\winter_olympics_2022.csv";

    public CsvLoader(IMemoryStore memoryStore)
    {
        MemoryStore = memoryStore ?? throw new ArgumentNullException(nameof(memoryStore));
    }

    public IMemoryStore MemoryStore { get; }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync(string collectionName)
    {
        // TODO: how to support cancellations?

        await MemoryStore.CreateCollectionAsync(collectionName);

        await foreach (var rec in LoadLocalEmbeddingsAsync(MemoryStore))
        {
            await MemoryStore.UpsertAsync(collectionName, rec);
        }
    }
    
    async Task DownloadHugeCsvIfNecessary(CancellationToken cancellationToken)
    {
        string dir = Path.GetDirectoryName(CsvFileName)!;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(CsvFileName))
            return;

        using var client = new HttpClient();

        using (var stream = await client.GetStreamAsync(CsvUrl, cancellationToken))
        {
            using var fileStream = File.Create(CsvFileName);

            await stream.CopyToAsync(fileStream, cancellationToken);
        }
    }

    async IAsyncEnumerable<MemoryRecord> LoadLocalEmbeddingsAsync(IMemoryStore store, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        // Reads the 209Mb file of the example https://github.com/openai/openai-cookbook/blob/main/examples/Question_answering_using_embeddings.ipynb?ref=mlq.ai
        await DownloadHugeCsvIfNecessary(cancellation);

        using var csvStream = new StreamReader(CsvFileName);
        using var csvReader = new CsvReader(csvStream, CultureInfo.InvariantCulture);

        await foreach (var rec in csvReader.GetRecordsAsync<CsvEmbeddingRecord>())
        {
            yield return rec.ToMemoryRecord();
        }

        // Alternative option
        //var tmp = LoadLocalEmbeddingsAsync(memStore, cancellation);
        //await memStore.UpsertBatchAsync(CollectionName, tmp, cancellation);
    }
}
