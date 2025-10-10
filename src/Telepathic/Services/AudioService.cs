using Plugin.Maui.Audio;

namespace Telepathic.Services;

public class AudioService : IAudioService
{
    private readonly IAudioRecorder _recorder;
    public string RecordedFilePath { get; private set; }

    public AudioService(IAudioManager audioManager)
    {
        _recorder = audioManager.CreateRecorder();
        RecordedFilePath = Path.Combine(FileSystem.Current.CacheDirectory, "dictation.wav");
    }

    public async Task StartRecordingAsync(CancellationToken ct)
    {
        // Clean up any previous recordings
        if (File.Exists(RecordedFilePath))
        {
            File.Delete(RecordedFilePath);
        }

        // Start a new recording
        await _recorder.StartAsync(RecordedFilePath); //ct
    }

    public async Task StopRecordingAsync()
    {
        await _recorder.StopAsync();
    }
}