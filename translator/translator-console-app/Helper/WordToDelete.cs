using System.Linq;
using System.Text.RegularExpressions;

namespace translator
{
    public class WordToDelete
    {
        public string Word { get; set; }
        public int IndexInTranscriptArrayBySplit { get; set; }
        /// <summary>
        /// The occurrence count in the transcript, whether it is the 1st, 2nd, 3rd, ... occurrence in the transcript
        /// </summary>
        public int OccurrenceCount { get; set; }
        public int IndexInTranscript { get; set; }

        public WordToDelete(string transcript, string word, int indexInTranscriptArrayBySplit, string[] transcriptArrayBySplit)
        {
            Word = word;
            IndexInTranscriptArrayBySplit = indexInTranscriptArrayBySplit;
            OccurrenceCount = 1;

            SetOccurrenceCount(transcriptArrayBySplit);
            IndexInTranscript = GetIndexInTranscript(transcript);
        }

        private void SetOccurrenceCount(string[] transcriptArrayBySplit)
        {
            for (int i = 0; i < transcriptArrayBySplit.Length; i++)
                if (transcriptArrayBySplit[i].Equals(Word) && i < IndexInTranscriptArrayBySplit)
                    OccurrenceCount++;
        }

        private int GetIndexInTranscript(string transcript)
        {
            Match match = Regex.Matches(transcript, Regex.Escape(Word))
                   .Cast<Match>()
                   .Skip(OccurrenceCount - 1)
                   .FirstOrDefault();
            return match.Index;
        }
    }
}
