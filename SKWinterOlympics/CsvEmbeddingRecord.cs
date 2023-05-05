namespace SKWinterOlympics;

public class CsvEmbeddingRecord
{
    [CsvHelper.Configuration.Attributes.Name("text")]
    [CsvHelper.Configuration.Attributes.Index(0)]
    public required string Text { get; init; }

    [CsvHelper.Configuration.Attributes.Name("embedding")]
    [CsvHelper.Configuration.Attributes.Index(1)]
    public required string ValuesArray { get; init; }

    static int _key = 0;

    public MemoryRecord ToMemoryRecord()
    {
        var key = "PK_" + _key++.ToString();

        float[]? floatValues = Newtonsoft.Json.JsonConvert.DeserializeObject<float[]>(this.ValuesArray);
        Embedding<float> e = (floatValues != null) ? new Embedding<float>(floatValues) : new();

        MemoryRecordMetadata meta = new(
            isReference: true,
            id: key,
            text: Text,
            description: null,
            externalSourceName: null,
            additionalMetadata: null);

        return new MemoryRecord(meta, e, null);
    }
}