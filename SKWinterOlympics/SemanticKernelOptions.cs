namespace SKWinterOlympics;

public class SemanticKernelOptions
{
    public string Model { get; set; } = "gpt-3.5-turbo";
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
    public string ApiKey { get; set; } = null!;
}