using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using Plugin.Maui.CalendarStore;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using Telepathic.Data.UserMemory;
using Telepathic.Tools;

namespace Telepathic;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseSkiaSharp()
			.ConfigureSyncfusionToolkit()
			.ConfigureMauiHandlers(handlers =>
			{
			})
			.AddAudio(
				playbackOptions =>
				{
#if IOS || MACCATALYST
					playbackOptions.Category = AVFoundation.AVAudioSessionCategory.Playback;
#endif
#if ANDROID
					// playbackOptions.AudioContentType = Android.Media.AudioContentType.Music;
					// playbackOptions.AudioUsageKind = Android.Media.AudioUsageKind.Media;
#endif
				},
				recordingOptions =>
				{
#if IOS || MACCATALYST
					recordingOptions.Category = AVFoundation.AVAudioSessionCategory.Record;
					recordingOptions.Mode = AVFoundation.AVAudioSessionMode.Default;
					recordingOptions.CategoryOptions = AVFoundation.AVAudioSessionCategoryOptions.MixWithOthers;
#endif
				})
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
				fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
			})
			.ConfigureMauiHandlers(handlers =>
			{
			});

#if DEBUG
		builder.Logging.AddDebug();
		builder.Services.AddLogging(configure => configure.AddDebug());
#endif
		builder.Services.AddSingleton(CalendarStore.Default);
		builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
		builder.Services.AddSingleton<ProjectRepository>();
		builder.Services.AddSingleton<TaskRepository>();
		builder.Services.AddSingleton<CategoryRepository>();
		builder.Services.AddSingleton<TagRepository>();
		builder.Services.AddSingleton<SeedDataService>();
		Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<IUserMemoryStore, SqliteUserMemoryStore>(builder.Services);
		builder.Services.AddSingleton<ModalErrorHandler>();
		builder.Services.AddSingleton<MainPageModel>();
		builder.Services.AddSingleton<ProjectListPageModel>();
		builder.Services.AddSingleton<ManageMetaPageModel>();
		builder.Services.AddSingleton<MemoryDebugPageModel>();
		builder.Services.AddSingleton<UserProfilePageModel>();
		builder.Services.AddSingleton<MyDataPageModel>();
		builder.Services.AddSingleton<DeviceSensorsPageModel>();
		builder.Services.AddSingleton<IAudioService, AudioService>();
		builder.Services.AddSingleton<ITranscriptionService, FoundryTranscriptionService>();
		builder.Services.AddSingleton<IChatClientService, ChatClientService>();

		// Platform-specific services
#if ANDROID
		builder.Services.AddSingleton<Services.ILightSensorService, Platforms.Android.LightSensorService>();
		builder.Services.AddSingleton<Services.IHealthService, Platforms.Android.HealthService>();
#elif IOS
		builder.Services.AddSingleton<Services.ILightSensorService, Platforms.iOS.LightSensorService>();
		builder.Services.AddSingleton<Services.IHealthService, Platforms.iOS.HealthService>();
#elif MACCATALYST
		builder.Services.AddSingleton<Services.ILightSensorService, Platforms.MacCatalyst.LightSensorService>();
		builder.Services.AddSingleton<Services.IHealthService, Platforms.MacCatalyst.HealthService>();
#elif WINDOWS
		builder.Services.AddSingleton<Services.ILightSensorService, Platforms.Windows.LightSensorService>();
		builder.Services.AddSingleton<Services.IHealthService, Platforms.Windows.HealthService>();
#else
		builder.Services.AddSingleton<Services.ILightSensorService, Platforms.Android.LightSensorService>();
		builder.Services.AddSingleton<Services.IHealthService, Platforms.Android.HealthService>();
#endif

		builder.Services.AddSingleton<LocationTools>();
		builder.Services.AddSingleton<TaskAssistAnalyzer>();
		builder.Services.AddSingleton<TaskAssistHandler>();
		builder.Services.AddTransientWithShellRoute<ProjectDetailPage, ProjectDetailPageModel>("project");
		builder.Services.AddTransientWithShellRoute<TaskDetailPage, TaskDetailPageModel>("task");
		builder.Services.AddTransientWithShellRoute<VoicePage, VoicePageModel>("voice");
		builder.Services.AddTransientWithShellRoute<PhotoPage, PhotoPageModel>("photo");
		builder.Services.AddTransientWithShellRoute<MemoryDebugPage, MemoryDebugPageModel>("memory-debug");
		builder.Services.AddTransientWithShellRoute<MyDataPage, MyDataPageModel>("mydata");
		builder.Services.AddTransientWithShellRoute<DeviceSensorsPage, DeviceSensorsPageModel>("sensors");

		return builder.Build();
	}
}
