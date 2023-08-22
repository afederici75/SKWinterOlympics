#region -------------- Loads configuration --------------

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IMemoryStore, VolatileMemoryStore>();
services.AddSingleton<CsvLoader, CsvLoader>();
services.Configure<SemanticKernelOptions>(config.GetSection("SemanticKernel"));

var svcProvider = services.BuildServiceProvider();
var options = svcProvider.GetRequiredService<IOptions<SemanticKernelOptions>>()
                         .Value;
if (string.IsNullOrWhiteSpace(options.ApiKey))
    throw new Exception("OpenAI API key is missing. Please add it to the user secrets.");

#endregion

#region -------------- Loads the CSV into the memory store --------------

// Loads the CSV into the memory store
var memoryStore = svcProvider.GetRequiredService<IMemoryStore>();
var csvLoader = svcProvider.GetRequiredService<CsvLoader>();

csvLoader.MemoryRecordLoaded += (index, rec) =>
{ 
    if (index % 1000==0)
        Console.WriteLine($"Loaded {index} memory records...");
};
Console.WriteLine("Loading CSV file (It might take a minute or so the first time)...");

const string CollectionName = "winterOlympics";

await csvLoader.InitializeAsync(CollectionName);

// Creates a semantic memory. We'll use use this below to find semantic matches
OpenAITextEmbeddingGeneration gen = new(options.EmbeddingModel, options.ApiKey);
ISemanticTextMemory memory = new SemanticTextMemory(memoryStore, gen);

#endregion

var predefinedQuestions = new[] {
    "Which athletes won the gold medal in curling at the 2022 Winter Olympics?",
    "who winned gold metals in kurling at the olimpics",  // misspelled question
    "How many records were set at the 2022 Winter Olympics?", // counting question SOMEHOW I GET NO COUNT? Model issue?
    "Did Jamaica or Cuba have more athletes at the 2022 Winter Olympics?", // comparison question
    "What is 2+2?", // question outside of the scope
    "Which Olympic sport is the most entertaining?", // subjective question
    "Who won the gold medal in curling at the 2018 Winter Olympics?" // question outside of the scope  
};

var defColor = Console.ForegroundColor;
var questionIndex = -1;

while (true)
{
    // Lets the user type a question or picks one from the default list
    Console.Write("Ask a question about the 2022 Winter Olympics (or press Enter for a random question):");
    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        if (questionIndex < predefinedQuestions.Length - 1)
            questionIndex++;
        else 
            questionIndex = 0; 
        question = predefinedQuestions[questionIndex];

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("RANDOM QUESTION: " + question);
        Console.ForegroundColor = defColor;
    }

    // Looks for semantic matches in the local data
    var id = 0;
    var semanticMatches = string.Empty;
    await foreach (var localMatch in memory.SearchAsync(CollectionName, question, limit: 5))
    {
        Console.WriteLine($"Semantic result #{id++}, Relevance: {localMatch.Relevance}.");
        semanticMatches += $"\n\nWikipedia article section:\n{localMatch.Metadata.Text}\n";
    }

    // Ask the question using the semantic matches as context
    IChatCompletion chatCompletion = new OpenAIChatCompletion(options.Model, options.ApiKey);
    ChatHistory newChat = chatCompletion.CreateNewChat(instructions: "You answer questions about the 2022 Winter Olympics.");

    var ask = "Use the below articles on the 2022 Winter Olympics to answer the subsequent question. " +
               "If the answer cannot be found in the articles, write 'I could not find an answer.'\n" +
               semanticMatches + '\n' +
               $"Question: {question}";

    newChat.AddMessage(AuthorRole.User, ask);

    string response = await chatCompletion.GenerateMessageAsync(newChat, new ChatRequestSettings
    {
        Temperature = 0
    });

    Console.WriteLine("------------------------------------------------------");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"QUESTION: {question}");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"RESPONSE: {response}");
    //Console.WriteLine($"SEMANTIC MATCHES:\n{semanticMatches}");
    Console.WriteLine("------------------------------------------------------");   
    Console.ForegroundColor = defColor; 
}