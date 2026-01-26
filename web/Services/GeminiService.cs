using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new Exception("Gemini API key not found");
        _model = configuration["Gemini:Model"] ?? "gemini-2.0-flash";
    }

    public async Task<AIBuildAnalysis> AnalyzeBuildAsync(List<ComponentInfo> components)
    {
        try
        {
            var componentsText = string.Join("\n", components.Select(c =>
                $"- {c.Type}: {c.Name} (Price: €{c.Price:N2}, TDP: {c.TDP?.ToString() ?? "N/A"}W, Socket: {c.Socket ?? "N/A"})"));

            var totalPrice = components.Sum(c => c.Price);
            var totalTDP = components.Where(c => c.TDP.HasValue).Sum(c => c.TDP!.Value);

            var prompt = $@"You are an expert PC hardware analyst. Analyze this PC build configuration comprehensively.

COMPONENTS:
{componentsText}

TOTAL PRICE: €{totalPrice:N2}
ESTIMATED POWER: {totalTDP}W

Provide a detailed analysis in JSON format with these exact fields:
{{
  ""overallScore"": <0-100 score based on value, performance, compatibility>,
  ""summary"": ""<2-3 sentence overall assessment>"",
  ""isCompatible"": <true/false>,
  ""compatibilityIssues"": [""<list critical compatibility problems or empty if none>""],
  ""bottlenecks"": [""<list performance bottlenecks, e.g. CPU limiting GPU>""],
  ""performanceEstimates"": {{
    ""gaming1080p"": ""<expected FPS range or performance tier>"",
    ""gaming1440p"": ""<expected FPS range or performance tier>"",
    ""gaming4K"": ""<expected FPS range or performance tier>"",
    ""productivity"": ""<rating for video editing, 3D rendering, etc.>""
  }},
  ""tips"": [""<3-5 practical tips for this specific build>""],
  ""upgradeRecommendations"": [""<future upgrade suggestions in priority order>""],
  ""valueRating"": ""<Excellent/Good/Fair/Poor value for money assessment>""
}}

ANALYSIS GUIDELINES:
1. Check socket compatibility (CPU must match motherboard)
2. Check RAM compatibility (DDR4 vs DDR5, speed support)
3. Check PSU wattage (should be 20-30% above total TDP)
4. Check case clearance (GPU length, cooler height)
5. Identify CPU/GPU bottlenecks based on tier matching
6. Consider price-to-performance ratio

Respond ONLY with valid JSON, no markdown, no explanations.";

            var responseText = await CallGeminiApiAsync(prompt, CancellationToken.None);
            responseText = CleanJsonResponse(responseText);

            var result = JsonSerializer.Deserialize<AIBuildAnalysis>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? GetDefaultAnalysis("Failed to parse AI response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing build with Gemini");
            return GetDefaultAnalysis(ex.Message);
        }
    }


    public async Task<ParsedQuery?> ParseQueryAsync(string userQuery)
    {
        try
        {
            var prompt = $@"You are a PC hardware expert. Parse this query and extract:
- budget (number in Euro)
- useCase (gaming/workstation/office/other)
- requirements (specific components)
- priority (performance/value/quiet/compact)

Respond ONLY with JSON:
{{
  ""budget"": 1500,
  ""useCase"": ""gaming"",
  ""requirements"": {{}},
  ""priority"": ""value""
}}

Query: {userQuery}";

            var responseText = await CallGeminiApiAsync(prompt, CancellationToken.None);
            responseText = CleanJsonResponse(responseText);

            return JsonSerializer.Deserialize<ParsedQuery>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing query with Gemini");
            return null;
        }
    }

    private string CleanJsonResponse(string response)
    {
        return response
            .Trim()
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();
    }

    private AIBuildAnalysis GetDefaultAnalysis(string error)
    {
        return new AIBuildAnalysis
        {
            OverallScore = 0,
            Summary = $"Unable to analyze build: {error}",
            IsCompatible = false,
            CompatibilityIssues = new List<string> { "Analysis unavailable" }
        };
    }

    private async Task<string> CallGeminiApiAsync(string prompt, CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 4096,
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling Gemini API with model: {Model}", _model);

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseText);
            throw new Exception($"Gemini API error: {response.StatusCode}");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseText);
        var generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrEmpty(generatedText))
        {
            throw new Exception("No text generated from Gemini API");
        }

        return generatedText;
    }
}

// =========================
// DTOs
// =========================

public class AIBuildAnalysis
{
    [JsonPropertyName("overallScore")]
    public int OverallScore { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("isCompatible")]
    public bool IsCompatible { get; set; }

    [JsonPropertyName("compatibilityIssues")]
    public List<string>? CompatibilityIssues { get; set; }

    [JsonPropertyName("bottlenecks")]
    public List<string>? Bottlenecks { get; set; }

    [JsonPropertyName("performanceEstimates")]
    public PerformanceEstimates? PerformanceEstimates { get; set; }

    [JsonPropertyName("tips")]
    public List<string>? Tips { get; set; }

    [JsonPropertyName("upgradeRecommendations")]
    public List<string>? UpgradeRecommendations { get; set; }

    [JsonPropertyName("valueRating")]
    public string? ValueRating { get; set; }
}

public class PerformanceEstimates
{
    [JsonPropertyName("gaming1080p")]
    public string? Gaming1080p { get; set; }

    [JsonPropertyName("gaming1440p")]
    public string? Gaming1440p { get; set; }

    [JsonPropertyName("gaming4K")]
    public string? Gaming4K { get; set; }

    [JsonPropertyName("productivity")]
    public string? Productivity { get; set; }
}

public class ParsedQuery
{
    [JsonPropertyName("budget")]
    public decimal? Budget { get; set; }

    [JsonPropertyName("useCase")]
    public string UseCase { get; set; } = "other";

    [JsonPropertyName("requirements")]
    public Dictionary<string, object>? Requirements { get; set; }

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "value";
}

public class CompatibilityResult
{
    [JsonPropertyName("compatible")]
    public bool Compatible { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; set; } = new();
}

public class ComponentInfo
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Retailer { get; set; }
    public string? BuyUrl { get; set; }
    public int? TDP { get; set; }
    public string? Socket { get; set; }
}

// Gemini API Response DTOs
public class GeminiApiResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}

public class Content
{
    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; set; }
}

public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}