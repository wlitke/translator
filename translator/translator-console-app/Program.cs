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
using Microsoft.CognitiveServices.Speech.Translation;

namespace translator
{
    class Program
    {
        public static async Task TranslationContinuousRecognitionAsync()
        {
            string subscriptionKey, region, fromLanguage, targetLanguage, targetVoice;
            ReadConfiguration(out subscriptionKey, out region, out fromLanguage, out targetLanguage, out targetVoice);

            // Creates an instance of a speech translation config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechTranslationConfig.FromSubscription(subscriptionKey, region);
            config.SpeechRecognitionLanguage = fromLanguage;
            config.AddTargetLanguage(targetLanguage);
            config.SpeechSynthesisVoiceName = targetVoice;

            // Support characters for uk-UA
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.InputEncoding = System.Text.Encoding.Unicode;
                Console.OutputEncoding = System.Text.Encoding.Unicode;
            }

            // Creates a translation recognizer using microphone as audio input.
            using (var recognizer = new TranslationRecognizer(config))
            {
                Queue<string> textQueue = new Queue<string>();
                SubscribeToEvents(fromLanguage, recognizer, textQueue);

                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                Console.WriteLine("Say something ...");
                await recognizer.StartContinuousRecognitionAsync();

                while (true)
                {
                    if (textQueue.Count > 0)
                    {
                        string text = textQueue.Dequeue();
                        using (var synthesizer = new SpeechSynthesizer(config))
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
                // Never reached
                /*
                // Stops continuous recognition.
                await recognizer.StopContinuousRecognitionAsync();
                */
            }
        }

        private static void SubscribeToEvents(string fromLanguage, TranslationRecognizer recognizer, Queue<string> textQueue)
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

            recognizer.Recognized += async (s, e) =>
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

        private static void ReadConfiguration(out string subscriptionKey, out string region, out string fromLanguage, out string targetLanguage, out string targetVoice)
        {
            subscriptionKey = ReadSetting("SubscriptionKey");
            region = ReadSetting("Region");
            fromLanguage = ReadSetting("FromLanguage");
            targetLanguage = ReadSetting("TargetLanguage");
            targetVoice = ReadSetting("TargetVoice");
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

        static async Task Main(string[] args)
        {
            await TranslationContinuousRecognitionAsync();
        }
    }
}
// </code>
