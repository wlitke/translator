using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;

namespace translator
{
    public class Helper
    {
        public static void ReadConfiguration(out string SpeechmaticsAuthKey, out string SpeechmaticsRtUrl, out string SpeechmaticsLanguage, out string DeepLAuthKey, out string DeepLFromLanguage, out string DeepLTargetLanguage, out string AzureAuthKey, out string AzureRegion, out string AzureTargetVoice)
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

        private static string ReadSetting(string key)
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

        public static void WriteStringBuildersToFiles(StringBuilder recognizeStrBuilder, StringBuilder translatorStrBuilder)
        {
            WriteStringBuilderToFile(DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss") + "_Recognized.txt", recognizeStrBuilder);
            WriteStringBuilderToFile(DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss") + "_Translated.txt", translatorStrBuilder);
        }

        public static void WriteStringBuilderToFile(string fileName, StringBuilder stringBuilder)
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText(Path.Combine(docPath, fileName), stringBuilder.ToString());
        }

        public static string DeleteWordsToDelete(string transcript, LinkedList<WordToDelete> wordsToDelete)
        {
            int indexDelta = 0;
            foreach (WordToDelete wordToDelete in wordsToDelete)
            {
                string oldTranscript = transcript;
                wordToDelete.IndexInTranscript -= indexDelta;
                transcript = ReplaceWithEmptySpace(transcript, wordToDelete);
                indexDelta = oldTranscript.Length - transcript.Length;
            }

            return transcript;
        }

        private static string ReplaceWithEmptySpace(string transcript, WordToDelete wordToDelete)
        {
            if (wordToDelete.IndexInTranscript > 0 && transcript[wordToDelete.IndexInTranscript - 1] == ' ')
                return transcript.Remove(wordToDelete.IndexInTranscript - 1, wordToDelete.Word.Length + 1);
            else if (wordToDelete.IndexInTranscript + wordToDelete.Word.Length < transcript.Length && transcript[wordToDelete.IndexInTranscript + wordToDelete.Word.Length] == ' ')
                return transcript.Remove(wordToDelete.IndexInTranscript, wordToDelete.Word.Length + 1);
            else
                return transcript.Remove(wordToDelete.IndexInTranscript, wordToDelete.Word.Length);
        }

        public static string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
