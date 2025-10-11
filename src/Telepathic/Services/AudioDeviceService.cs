#if MACCATALYST || IOS
using AVFoundation;
using Foundation;
#elif ANDROID
using Android.Content;
using Android.Media;
#elif WINDOWS
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
#endif

namespace Telepathic.Services;

/// <summary>
/// Service to manage audio device selection on all platforms (macOS/iOS/Android/Windows)
/// </summary>
public static class AudioDeviceService
{
    /// <summary>
    /// Configure audio session to prefer external microphones and return the selected device info
    /// </summary>
    public static string ConfigureForExternalMicrophone()
    {
#if MACCATALYST || IOS
        try
        {
            var audioSession = AVAudioSession.SharedInstance();

            // Set category to allow recording with external devices - Record mode for speech recognition
            var error = audioSession.SetCategory(
                AVAudioSessionCategory.Record,
                AVAudioSessionCategoryOptions.AllowBluetooth |
                AVAudioSessionCategoryOptions.AllowBluetoothA2DP);

            if (error != null)
            {
                var errorMsg = $"Error setting audio session category: {error.LocalizedDescription}";
                Console.WriteLine(errorMsg);
                return errorMsg;
            }

            // Set mode to optimize for speech recognition
            audioSession.SetMode(AVAudioSessionMode.SpokenAudio, out error);
            if (error != null)
            {
                var errorMsg = $"Error setting audio session mode: {error.LocalizedDescription}";
                Console.WriteLine(errorMsg);
                return errorMsg;
            }

            string selectedDevice = "Built-in Microphone";

            // Try to prefer external microphones
            var availableInputs = audioSession.AvailableInputs;
            Console.WriteLine($"Available inputs count: {availableInputs?.Length ?? 0}");

            if (availableInputs?.Length > 0)
            {
                // Log all available inputs
                foreach (var input in availableInputs)
                {
                    Console.WriteLine($"  - {input.PortName} ({input.PortType})");
                }

                // For Speech Recognition, prefer wired/USB devices over Bluetooth
                // Bluetooth devices (like AirPods) can have issues with speech recognition on macOS
                var preferredInput = availableInputs.FirstOrDefault(input =>
                    input.PortType.ToString().Contains("UsbAudio", StringComparison.OrdinalIgnoreCase) ||
                    input.PortType.ToString().Contains("HeadsetMic", StringComparison.OrdinalIgnoreCase) ||
                    (input.PortType.ToString().Contains("BuiltInMic", StringComparison.OrdinalIgnoreCase) &&
                     !input.PortName.Contains("AirPods", StringComparison.OrdinalIgnoreCase)));

                // If no wired device, look for any non-Bluetooth external device
                if (preferredInput == null)
                {
                    preferredInput = availableInputs.FirstOrDefault(input =>
                        !input.PortName.Contains("Built-in", StringComparison.OrdinalIgnoreCase) &&
                        !input.PortName.Contains("Internal", StringComparison.OrdinalIgnoreCase) &&
                        !input.PortName.Contains("AirPods", StringComparison.OrdinalIgnoreCase) &&
                        !input.PortType.ToString().Contains("Bluetooth", StringComparison.OrdinalIgnoreCase));
                }

                // If we found a preferred device, use it
                if (preferredInput != null)
                {
                    audioSession.SetPreferredInput(preferredInput, out error);
                    if (error != null)
                    {
                        Console.WriteLine($"Error setting preferred input: {error.LocalizedDescription}");
                    }
                    else
                    {
                        selectedDevice = preferredInput.PortName;
                        Console.WriteLine($"Successfully set preferred input to: {selectedDevice}");
                    }
                }
                else
                {
                    // Use the first available input (likely built-in or Bluetooth)
                    var firstInput = availableInputs.FirstOrDefault();
                    if (firstInput != null)
                    {
                        selectedDevice = firstInput.PortName;
                        Console.WriteLine($"Using first available input: {selectedDevice}");

                        // Warn if using Bluetooth for speech recognition
                        var portTypeStr = firstInput.PortType.ToString();
                        if (portTypeStr.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) ||
                            firstInput.PortName.Contains("AirPods", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("⚠️ WARNING: Bluetooth devices may not work reliably with Speech Recognition on macOS");
                            selectedDevice = "⚠️ " + selectedDevice + " (May not work)";
                        }
                    }
                }
            }

            // Activate the session
            audioSession.SetActive(true, out error);
            if (error != null)
            {
                var errorMsg = $"Error activating audio session: {error.LocalizedDescription}";
                Console.WriteLine(errorMsg);
                return errorMsg;
            }

            // Get current route for verification
            var currentRoute = audioSession.CurrentRoute;
            if (currentRoute?.Inputs?.Length > 0)
            {
                var currentInput = currentRoute.Inputs[0];
                selectedDevice = currentInput.PortName;
                Console.WriteLine($"Current active input: {selectedDevice} ({currentInput.PortType})");
            }

            return $"🎤 Using: {selectedDevice}";
        }
        catch (Exception ex)
        {
            var errorMsg = $"Exception in ConfigureForExternalMicrophone: {ex.Message}";
            Console.WriteLine(errorMsg);
            return errorMsg;
        }
#elif ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var audioManager = context?.GetSystemService(Context.AudioService) as AudioManager;
            
            if (audioManager == null)
            {
                Console.WriteLine("AudioManager is null");
                return "❌ AudioManager unavailable";
            }

            // Set audio mode for communication (speech recognition)
            audioManager.Mode = Mode.Normal;
            
            // Check for available audio devices
            var devices = audioManager.GetDevices(GetDevicesTargets.Inputs);
            
            if (devices?.Length > 0)
            {
                // Look for external USB or Bluetooth microphones
                var externalDevice = devices.FirstOrDefault(device => 
                    device.Type == AudioDeviceType.UsbDevice ||
                    device.Type == AudioDeviceType.UsbHeadset ||
                    device.Type == AudioDeviceType.BluetoothSco ||
                    device.Type == AudioDeviceType.BluetoothA2dp ||
                    device.Type == AudioDeviceType.WiredHeadset);

                if (externalDevice != null)
                {
                    var deviceName = externalDevice.ProductName?.ToString() ?? externalDevice.Type.ToString();
                    Console.WriteLine($"Found external audio device: {deviceName} ({externalDevice.Type})");
                    
                    // Enable Bluetooth SCO if it's a Bluetooth device
                    if (externalDevice.Type == AudioDeviceType.BluetoothSco)
                    {
                        audioManager.StartBluetoothSco();
                        audioManager.BluetoothScoOn = true;
                    }
                    
                    // Set speaker phone off to use external device
                    audioManager.SpeakerphoneOn = false;
                    return $"🎤 Using: {deviceName}";
                }
                else
                {
                    Console.WriteLine("No external audio device found, using default");
                    return "🎤 Using: Built-in Microphone";
                }
            }
            return "🎤 Using: Default Microphone";
        }
        catch (Exception ex)
        {
            var errorMsg = $"Exception in ConfigureForExternalMicrophone (Android): {ex.Message}";
            Console.WriteLine(errorMsg);
            return errorMsg;
        }
#elif WINDOWS
        try
        {
            // On Windows, the default audio device is managed by the system
            // We can enumerate devices but not force selection programmatically in UWP
            var defaultDevice = MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Communications);
            
            if (!string.IsNullOrEmpty(defaultDevice))
            {
                Console.WriteLine($"Windows default audio capture device: {defaultDevice}");
                return $"🎤 Using: {defaultDevice}";
            }
            else
            {
                Console.WriteLine("No default audio capture device found on Windows");
                return "❌ No audio device found";
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Exception in ConfigureForExternalMicrophone (Windows): {ex.Message}";
            Console.WriteLine(errorMsg);
            return errorMsg;
        }
#else
        // Platform not supported - do nothing
        Console.WriteLine("Audio device configuration not supported on this platform");
        return "⚠️ Platform not supported";
#endif
    }

    /// <summary>
    /// Get list of available audio input devices
    /// </summary>
    public static List<string> GetAvailableInputDevices()
    {
        var devices = new List<string>();

#if MACCATALYST || IOS
        try
        {
            var audioSession = AVAudioSession.SharedInstance();
            var availableInputs = audioSession.AvailableInputs;

            if (availableInputs != null)
            {
                foreach (var input in availableInputs)
                {
                    devices.Add($"{input.PortName} ({input.PortType})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception getting available input devices: {ex.Message}");
        }
#elif ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var audioManager = context?.GetSystemService(Context.AudioService) as AudioManager;
            
            if (audioManager != null)
            {
                var inputDevices = audioManager.GetDevices(GetDevicesTargets.Inputs);
                
                if (inputDevices != null)
                {
                    foreach (var device in inputDevices)
                    {
                        var deviceName = string.IsNullOrEmpty(device.ProductName?.ToString()) 
                            ? device.Type.ToString() 
                            : device.ProductName.ToString();
                        devices.Add($"{deviceName} ({device.Type})");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception getting available input devices (Android): {ex.Message}");
        }
#elif WINDOWS
        try
        {
            var deviceInfoCollection = DeviceInformation.FindAllAsync(DeviceClass.AudioCapture).GetAwaiter().GetResult();
            
            if (deviceInfoCollection != null)
            {
                foreach (var device in deviceInfoCollection)
                {
                    devices.Add($"{device.Name} (Windows)");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception getting available input devices (Windows): {ex.Message}");
        }
#else
        // Platform not supported
        devices.Add("Platform not supported for device enumeration");
#endif

        return devices;
    }
}
