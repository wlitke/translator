using DeepL;
using DeepL.Model;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Newtonsoft.Json;
using Speechmatics.Realtime.Client;
using Speechmatics.Realtime.Client.Config;
using Speechmatics.Realtime.Client.Enumerations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace translator
{
    class Program
    {
        private static AudioDevice inputDevice, outputDevice;
        private static BlockingStream AudioStream;
        private static bool IsStereo;

        private static async Task TranslateAndSynthesizeText(string DeepLAuthKey, string fromLanguage, string targetLangauge, SpeechConfig speechConfig, AudioConfig audioCfgOut, Queue<string> textQueue)
        {
            if (textQueue.Count > 0)
            {
                string text = textQueue.Dequeue();
                if (text.Length > 0)
                {
                    Console.WriteLine($"Recognized text: {text}");
                    TextResult translatedText = await Translate(DeepLAuthKey, text, fromLanguage, targetLangauge);
                    using (var synthesizer = new SpeechSynthesizer(speechConfig, audioCfgOut))
                    {
                        using (var result = await synthesizer.SpeakTextAsync(translatedText.ToString()))
                        {
                            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                            {
                                Console.WriteLine($"Speech synthesized: {translatedText}");
                            }
                        }
                    }
                }
            }
        }

        private static async Task<TextResult> Translate(string DeepLAuthKey, string text, string fromLanguage, string targetLangauge)
        {
            var translator = new Translator(DeepLAuthKey);
            var translatedText = await translator.TranslateTextAsync(
                  text,
                  fromLanguage,
                  targetLangauge);
            return translatedText;
        }

        private static SpeechConfig SetSpeechConfig(string subscriptionKey, string region, string targetVoice)
        {
            var speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            speechConfig.SpeechSynthesisVoiceName = targetVoice;

            // Support characters for, e.g., uk-UA
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.InputEncoding = System.Text.Encoding.Unicode;
                Console.OutputEncoding = System.Text.Encoding.Unicode;
            }
            return speechConfig;
        }

        private static void ReadConfiguration(out string SpeechmaticsAuthKey, out string SpeechmaticsRtUrl, out string SpeechmaticsLanguage, out string DeepLAuthKey, out string DeepLFromLanguage, out string DeepLTargetLanguage, out string AzureAuthKey, out string AzureRegion, out string AzureTargetVoice)
        {
            SpeechmaticsAuthKey = ReadSetting("SpeechmaticsAuthKey");
            SpeechmaticsRtUrl = ReadSetting("SpeechmaticsRtUrl");
            SpeechmaticsLanguage = ReadSetting("SpeechmaticsLanguage");
            DeepLAuthKey = ReadSetting("DeepLAuthKey");
            DeepLFromLanguage = ReadSetting("DeepLFromLanguage");
            DeepLTargetLanguage = ReadSetting("DeepLTargetLanguage");
            AzureAuthKey = ReadSetting("AzureAuthKey");
            AzureRegion = ReadSetting("AzureRegion");
            AzureTargetVoice = ReadSetting("AzureTargetVoice");
        }

        static string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                return appSettings[key] ?? "Not Found";
            }
            catch (ConfigurationErrorsException)
            {
                return "Error reading app settings";
            }
        }

        class AudioDevice
        {
            public AudioDevice(string name, string id, MMDevice mmDevice)
            {
                Name = name;
                ID = id;
                MMDevice = mmDevice;
            }
            public string Name { get; set; }
            public string ID { get; set; }
            public MMDevice MMDevice { get; set; }
        }

        enum DeviceType
        {
            input,
            output
        }

        private static void HandleAudioDeviceSelection(DeviceType deviceType)
        {
            Dictionary<Int16, AudioDevice> devices = new Dictionary<Int16, AudioDevice>();
            var enumerator = new MMDeviceEnumerator();

            PrintDevices(devices, enumerator, deviceType);
            HandleDeviceKeySelection(devices, deviceType);

            Console.WriteLine();
        }

        private static void PrintDevices(Dictionary<Int16, AudioDevice> devices, MMDeviceEnumerator enumerator, DeviceType deviceType)
        {
            Console.WriteLine("Device(s) for {0}:", deviceType);

            DataFlow flow = DataFlow.All;
            if (deviceType == DeviceType.input)
                flow = DataFlow.Capture;
            else if (deviceType == DeviceType.output)
                flow = DataFlow.Render;
            else { }

            Int16 i = 1;
            foreach (var endpoint in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
            {
                devices.Add(i, new AudioDevice(endpoint.FriendlyName, endpoint.ID, endpoint));
                Console.WriteLine("({0})\t{1}", i, endpoint.FriendlyName);
                i++;
            }
        }

        private static void HandleDeviceKeySelection(Dictionary<Int16, AudioDevice> devices, DeviceType deviceType)
        {
            if (devices.Count > 0)
            {
                Int16 deviceKey = 0;
                while (deviceKey < 1 || deviceKey > devices.Count)
                {
                    try
                    {
                        Console.Write("Select {0} device by number: ", deviceType);
                        deviceKey = Int16.Parse(Console.ReadLine());
                        if (deviceType == DeviceType.input)
                            inputDevice = devices[deviceKey];
                        else if (deviceType == DeviceType.output)
                            outputDevice = devices[deviceKey];
                        else { }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("The entered value is invalid. Please enter an existing number.");
                    }
                }
            }
            else
            {
                Console.WriteLine("There is no {0} device. Please install an {0} device.", deviceType);
                // Environment.Exit(0);
            }
        }

        private static void TimerCallback(object o)
        {
            Environment.Exit(0);
        }

        static async Task Main(string[] args)
        {
            //int seconds = 60 * 60;
            //int systemTime = 1000 * seconds; // in milliseconds
            //Timer timer = new Timer(TimerCallback, null, systemTime, systemTime);

            if (args.Length > 0 && File.Exists(args[0]) && args[0].ToLower().EndsWith(".mp3"))
            {
                HandleAudioDeviceSelection(DeviceType.output);
                await SpeechFromFileToSpeech(args[0]);
            }
            else
            {
                HandleAudioDeviceSelection(DeviceType.input);
                HandleAudioDeviceSelection(DeviceType.output);
                await SpeechFromRecordingToSpeech();
            }
        }

        /// <summary>
        /// Transcribe speech from file into text, translate the text, and convert the translated text to speech.
        /// </summary>
        /// <param name="mp3File">The path to the mp3 file</param>
        /// <returns></returns>
        private static async Task SpeechFromFileToSpeech(string mp3File)
        {
            using (var stream = File.Open(mp3File, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    string SpeechmaticsAuthKey, SpeechmaticsRtUrl, SpeechmaticsLanguage, DeepLAuthKey, DeepLFromLanguage, DeepLTargetLanguage, AzureAuthKey, AzureRegion, AzureTargetVoice;
                    ReadConfiguration(out SpeechmaticsAuthKey, out SpeechmaticsRtUrl, out SpeechmaticsLanguage, out DeepLAuthKey, out DeepLFromLanguage, out DeepLTargetLanguage, out AzureAuthKey, out AzureRegion, out AzureTargetVoice);

                    Queue<string> textQueue = new Queue<string>();
                    var config = new SmRtApiConfig(SpeechmaticsLanguage)
                    {
                        AddTranscriptCallback = s => textQueue.Enqueue(s),
                        // AddPartialTranscriptMessageCallback = s => Console.Write("* " + s.transcript),
                        ErrorMessageCallback = s => Console.WriteLine(ToJson(s)),
                        WarningMessageCallback = s => Console.WriteLine(ToJson(s)),
                        Insecure = true,
                        AuthToken = SpeechmaticsAuthKey
                    };

                    var api = new SmRtApi(SpeechmaticsRtUrl,
                        stream,
                        config
                    );

                    await TranscribeTranslateSpeech(api, SpeechmaticsRtUrl, DeepLAuthKey, DeepLFromLanguage, DeepLTargetLanguage, AzureAuthKey, AzureRegion, AzureTargetVoice, textQueue, false);
                }
                catch (AggregateException e)
                {
                    Console.WriteLine(e);
                }
            }
            Console.WriteLine("End of stream");
            Console.ReadLine();
        }

        /// <summary>
        /// Transcribe speech from recording into text, translate the text, and convert the translated text to speech.
        /// </summary>
        /// <returns></returns>
        private static async Task SpeechFromRecordingToSpeech()
        {
            var wasapiClient = new WasapiCapture(inputDevice.MMDevice);
            var sampleRate = wasapiClient.WaveFormat.SampleRate;
            var channels = wasapiClient.WaveFormat.Channels;

            IsStereo = channels == 2;

            //Console.WriteLine("Sample rate {0}", sampleRate);
            //Console.WriteLine("Bits per sample {0}", wasapiClient.WaveFormat.BitsPerSample);
            //Console.WriteLine("Channels {0}", channels);
            //Console.WriteLine("Encoding {0}", wasapiClient.WaveFormat.Encoding);
            wasapiClient.DataAvailable += WaveSourceOnDataAvailable;

            var recorderTask = new Task(() =>
            {
                wasapiClient.StartRecording();
            });
            AudioStream = new BlockingStream(1024 * 1024);

            using (var stream = AudioStream)
            {
                try
                {
                    string SpeechmaticsAuthKey, SpeechmaticsRtUrl, SpeechmaticsLanguage, DeepLAuthKey, DeepLFromLanguage, DeepLTargetLanguage, AzureAuthKey, AzureRegion, AzureTargetVoice;
                    ReadConfiguration(out SpeechmaticsAuthKey, out SpeechmaticsRtUrl, out SpeechmaticsLanguage, out DeepLAuthKey, out DeepLFromLanguage, out DeepLTargetLanguage, out AzureAuthKey, out AzureRegion, out AzureTargetVoice);

                    Queue<string> textQueue = new Queue<string>();
                    var config = new SmRtApiConfig(SpeechmaticsLanguage, sampleRate, AudioFormatType.Raw, AudioFormatEncoding.PcmF32Le)
                    {
                        AddTranscriptCallback = s => textQueue.Enqueue(s),
                        // AddPartialTranscriptMessageCallback = s => Console.Write("* " + s.transcript),
                        ErrorMessageCallback = s => Console.WriteLine(ToJson(s)),
                        WarningMessageCallback = s => Console.WriteLine(ToJson(s)),
                        Insecure = true,
                        BlockSize = 8192,
                        AuthToken = SpeechmaticsAuthKey
                    };

                    var api = new SmRtApi(SpeechmaticsRtUrl,
                        stream,
                        config
                    );

                    // Start recording audio
                    recorderTask.Start();

                    await TranscribeTranslateSpeech(api, SpeechmaticsRtUrl, DeepLAuthKey, DeepLFromLanguage, DeepLTargetLanguage, AzureAuthKey, AzureRegion, AzureTargetVoice, textQueue, true);
                }
                catch (AggregateException e)
                {
                    Console.WriteLine(e);
                }
            }
            Console.WriteLine("End of stream");
            Console.ReadLine();
        }

        private static async Task TranscribeTranslateSpeech(SmRtApi api, string SpeechmaticsRtUrl, string DeepLAuthKey, string DeepLFromLanguage, string DeepLTargetLanguage, string AzureAuthKey, string AzureRegion, string AzureTargetVoice, Queue<string> textQueue, bool recording)
        {
            Console.WriteLine($"Connecting to {SpeechmaticsRtUrl}");
            // Start Speechmatics API task for transcription
            var apiTask = new Task(() =>
            {
                api.Run();
            });
            apiTask.Start();
            if (recording) Console.WriteLine("Say something ...");

            SpeechConfig speechConfig = SetSpeechConfig(AzureAuthKey, AzureRegion, AzureTargetVoice);
            AudioConfig audioCfgOut = outputDevice == null ? AudioConfig.FromDefaultSpeakerOutput() : AudioConfig.FromSpeakerOutput(outputDevice.ID);
            while (true)
            {
                await TranslateAndSynthesizeText(DeepLAuthKey, DeepLFromLanguage, DeepLTargetLanguage, speechConfig, audioCfgOut, textQueue);
            }
        }

        private static string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// We want to turn the 2 streams into 1.
        /// </summary>
        /// <param name="waveInEventArgs"></param>
        /// <returns></returns>
        private static byte[] SquashStereo(WaveInEventArgs waveInEventArgs)
        {
            var data = waveInEventArgs.Buffer;
            // TODO: do not allocate a block every time this is called
            var audioBytes = new byte[waveInEventArgs.BytesRecorded / 2];
            // offset into the receiving buffer
            var offset = 0;

            for (var i = 0; i < waveInEventArgs.BytesRecorded; i += 8)
            {
                var s = BitConverter.ToSingle(data, i) / 2.0f + BitConverter.ToSingle(data, i + 4) / 2.0f;
                Buffer.BlockCopy(BitConverter.GetBytes(s), 0, audioBytes, offset, 4);
                offset += 4;
            }

            return audioBytes;
        }
        private static void WaveSourceOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {

            // Use this code to save the audio to a .raw file to examine in Audacity
            //
            //using (var f = File.OpenWrite("./audio.raw"))
            //{
            //    f.Seek(0, SeekOrigin.End);
            //    f.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
            //    //f.Write(squashed, 0, squashed.Length);
            //}
            if (IsStereo)
            {
                var squashed = SquashStereo(waveInEventArgs);
                AudioStream.Write(squashed, 0, squashed.Length);
            }
            else
            {
                AudioStream.Write(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);
            }
        }
    }
}
