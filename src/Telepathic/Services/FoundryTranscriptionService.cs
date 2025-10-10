using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Telepathic.Services;

public class FoundryTranscriptionService : ITranscriptionService
{
    private readonly IChatClientService _chatClientService;
    private readonly string _deploymentName = Preferences.Default.Get("aoai_whisper_deployment", "whisper");
    // ^ use your actual deployment name, not the model name

    public FoundryTranscriptionService(IChatClientService chatSvc)
    {
        _chatClientService = chatSvc ?? throw new ArgumentNullException(nameof(chatSvc));
    }

    public async Task<string> TranscribeAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);

        // Get the Azure OpenAI client from the chat client service
        var azureClient = _chatClientService.GetAzureOpenAIClient();

        // Azure OpenAI (Foundry) audio transcription call
        var result = await azureClient
            .GetAudioClient(_deploymentName)             // deployment in your Foundry/Azure OpenAI resource
            .TranscribeAudioAsync(stream, "file.wav",     // filename hint; format autodetected from bytes
                                   cancellationToken: ct);

        return result.Value.Text.Trim();
    }
}
