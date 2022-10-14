//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

// <code>
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

namespace translator
{
    class Program
    {
        public static async Task TranslationContinuousRecognitionAsync()
        {
            string subscriptionKey, region, fromLanguage, targetLanguage, targetVoice, microphoneInputID, speakerOutputID;
            ReadConfiguration(out subscriptionKey, out region, out fromLanguage, out targetLanguage, out targetVoice, out microphoneInputID, out speakerOutputID);

            SpeechTranslationConfig translationCfg = SetTranslationConfig(subscriptionKey, region, fromLanguage, targetLanguage, targetVoice);
            AudioConfig audioCfgIn = string.IsNullOrEmpty(microphoneInputID) ? AudioConfig.FromDefaultMicrophoneInput() : AudioConfig.FromMicrophoneInput(microphoneInputID);
            AudioConfig audioCfgOut = string.IsNullOrEmpty(speakerOutputID) ? AudioConfig.FromDefaultSpeakerOutput() : AudioConfig.FromSpeakerOutput(speakerOutputID);

            // Creates a translation recognizer using microphone as audio input.
            using (var recognizer = new TranslationRecognizer(translationCfg, audioCfgIn))
            {
                Queue<string> textQueue = new();
                SubscribeToEvents(recognizer, fromLanguage, textQueue);

                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                Console.WriteLine("Say something ...");
                await recognizer.StartContinuousRecognitionAsync();

                while (true)
                {
                    await SynthesizeText(translationCfg, audioCfgOut, textQueue);
                }
                // Never reached
                // Stops continuous recognition.
                /* await recognizer.StopContinuousRecognitionAsync();*/
            }
        }

        private static async Task SynthesizeText(SpeechTranslationConfig translationCfg, AudioConfig audioCfgOut, Queue<string> textQueue)
        {
            if (textQueue.Count > 0)
            {
                string text = textQueue.Dequeue();
                using (var synthesizer = new SpeechSynthesizer(translationCfg, audioCfgOut))
                {
                    using (var result = await synthesizer.SpeakTextAsync(text))
                    {
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            Console.WriteLine($"Speech synthesized to speaker for text [{text}]");
                        }
                    }
                }
            }
        }

        private static SpeechTranslationConfig SetTranslationConfig(string subscriptionKey, string region, string fromLanguage, string targetLanguage, string targetVoice)
        {
            // Creates an instance of a speech translation config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var translationCfg = SpeechTranslationConfig.FromSubscription(subscriptionKey, region);
            translationCfg.SpeechRecognitionLanguage = fromLanguage;
            translationCfg.AddTargetLanguage(targetLanguage);
            translationCfg.SpeechSynthesisVoiceName = targetVoice;

            // Support characters for, e.g., uk-UA
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.InputEncoding = System.Text.Encoding.Unicode;
                Console.OutputEncoding = System.Text.Encoding.Unicode;
            }

            return translationCfg;
        }

        private static void SubscribeToEvents(TranslationRecognizer recognizer, string fromLanguage, Queue<string> textQueue)
        {
            // Skip handling the recognizing event
            /* recognizer.Recognizing += (s, e) =>
            {
                Console.WriteLine($"RECOGNIZING in '{fromLanguage}': Text={e.Result.Text}");
                foreach (var element in e.Result.Translations)
                {
                    Console.WriteLine($"    TRANSLATING into '{element.Key}': {element.Value}");
                }
            };*/

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.TranslatedSpeech)
                {
                    Console.WriteLine($"\nFinal result: Reason: {e.Result.Reason.ToString()}, recognized text in {fromLanguage}: {e.Result.Text}");
                    foreach (var element in e.Result.Translations)
                    {
                        Console.WriteLine($"    TRANSLATED into '{element.Key}': {element.Value}");
                        textQueue.Enqueue(element.Value);
                    }
                }
            };

            // Skip handling the synthesizing event
            /* recognizer.Synthesizing += (s, e) =>
            {
                var audio = e.Result.GetAudio();
                Console.WriteLine(audio.Length != 0
                    ? $"AudioSize: {audio.Length}"
                    : $"AudioSize: {audio.Length} (end of synthesis data)");
            };*/

            recognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"\nRecognition canceled. Reason: {e.Reason}; ErrorDetails: {e.ErrorDetails}");
            };

            recognizer.SessionStarted += (s, e) =>
            {
                Console.WriteLine("\nSession started event.");
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("\nSession stopped event.");
            };
        }

        private static void ReadConfiguration(out string subscriptionKey, out string region, out string fromLanguage, out string targetLanguage, out string targetVoice, out string microphoneInputID, out string speakerOutputID)
        {
            subscriptionKey = ReadSetting("SubscriptionKey");
            region = ReadSetting("Region");
            fromLanguage = ReadSetting("FromLanguage");
            targetLanguage = ReadSetting("TargetLanguage");
            targetVoice = ReadSetting("TargetVoice");
            microphoneInputID = ReadSetting("MicrophoneInputID");
            speakerOutputID = ReadSetting("SpeakerOutputID");
        }

        static string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                return appSettings[key] ?? null;
            }
            catch (ConfigurationErrorsException)
            {
                return "Error reading app settings";
            }
        }

        static async Task Main(string[] args)
        {
            await TranslationContinuousRecognitionAsync();
        }
    }
}
// </code>
