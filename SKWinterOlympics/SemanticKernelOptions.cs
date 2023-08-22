namespace SKWinterOlympics;

public class SemanticKernelOptions
{
    public string Model { get; set; } = "gpt-3.5-turbo";
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
    public string ApiKey { get; set; } = null!;

    public void Validate()
    {
        if (string.IsNullOrEmpty(Model))
            throw new Exception($"{nameof(Model)} missing.");

        if (string.IsNullOrEmpty(EmbeddingModel))
            throw new Exception($"{nameof(EmbeddingModel)} missing.");

        if (string.IsNullOrEmpty(ApiKey))
            throw new Exception($"{nameof(ApiKey)} missing.");
    }
}