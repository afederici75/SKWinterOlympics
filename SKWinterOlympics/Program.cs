// https://github.com/openai/openai-cookbook/blob/main/examples/Question_answering_using_embeddings.ipynb?ref=mlq.ai

// Loads configuration
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .Build();

var services = new ServiceCollection();
services.Configure<SemanticKernelOptions>(config.GetSection("SemanticKernel"));

var svcProvider = services.BuildServiceProvider();
var options = svcProvider.GetRequiredService<IOptions<SemanticKernelOptions>>()
                         .Value;

// Loads the CSV into the memory store
IMemoryStore memoryStore = new VolatileMemoryStore();
CsvLoader csvLoader = new CsvLoader(memoryStore);
Console.WriteLine("Loading CSV file...");

const string CollectionName = "winterOlympics";

await csvLoader.InitializeAsync(CollectionName);

// Creates a semantic memory. We'll use use this below to find semantic matches
OpenAITextEmbeddingGeneration gen = new(options.EmbeddingModel, options.ApiKey);
ISemanticTextMemory memory = new SemanticTextMemory(memoryStore, gen);

var predefinedQuestions = new[] {
    "Which athletes won the gold medal in curling at the 2022 Winter Olympics?",
    "who winned gold metals in kurling at the olimpics",  // misspelled question
    "How many records were set at the 2022 Winter Olympics?", // counting question SOMEHOW I GET NO COUNT? Model issue?
    "Did Jamaica or Cuba have more athletes at the 2022 Winter Olympics?", // comparison question
    "What is 2+2?", // question outside of the scope
    "Which Olympic sport is the most entertaining?", // subjective question
    "Who won the gold medal in curling at the 2018 Winter Olympics?" // question outside of the scope  
};


while (true)
{
    // Lets the user type a question or picks one from the default list
    Console.Write("Ask a question about the 2022 Winter Olympics (or press Enter for a random question):");
    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        var rnd = new Random();
        question = predefinedQuestions[rnd.Next(predefinedQuestions.Length - 1)];
        Console.WriteLine("RANDOM QUESTION: " + question);
    }

    // Looks for semantic matches in the local data
    var id = 0;
    var semanticMatches = string.Empty;
    await foreach (var localMatch in memory.SearchAsync(CollectionName, question, limit: 5))
    {
        Console.WriteLine($"Semantic result #{id++}, Relevance: {localMatch.Relevance}.");
        semanticMatches += $"\n\nWikipedia article section:\n{localMatch.Metadata.Text}\n";
    }

    // Ask the question        
    IChatCompletion chatCompletion = new OpenAIChatCompletion(options.Model, options.ApiKey);
    ChatHistory newChat = chatCompletion.CreateNewChat(
        instructions: "You answer questions about the 2022 Winter Olympics.");

    var ask = "Use the below articles on the 2022 Winter Olympics to answer the subsequent question. " +
               "If the answer cannot be found in the articles, write 'I could not find an answer.'\n" +
               semanticMatches + '\n' +
               $"Question: {question}";

    newChat.AddMessage(ChatHistory.AuthorRoles.User, ask);

    string response = await chatCompletion.GenerateMessageAsync(newChat, new ChatRequestSettings
    {
        Temperature = 0
    });

    Console.WriteLine("------------------------------------------------------");
    Console.WriteLine($"QUESTION: {question}");
    Console.WriteLine($"RESPONSE:\n{response}");
    //Console.WriteLine($"SEMANTIC MATCHES:\n{semanticMatches}");
    Console.WriteLine("------------------------------------------------------");   
}