namespace App.Services
{
    public sealed class NarrationPlaybackResult
    {
        private NarrationPlaybackResult(
            bool completed,
            bool createdNewSession,
            bool usedAudioFile,
            string language,
            string textForAnalytics,
            string status)
        {
            Completed = completed;
            CreatedNewSession = createdNewSession;
            UsedAudioFile = usedAudioFile;
            Language = language;
            TextForAnalytics = textForAnalytics;
            Status = status;
        }

        public bool Completed { get; }

        public bool CreatedNewSession { get; }

        public bool UsedAudioFile { get; }

        public string Language { get; }

        public string TextForAnalytics { get; }

        public string Status { get; }

        public static NarrationPlaybackResult CompletedAudio(string language, string textForAnalytics)
            => new(true, true, true, language, textForAnalytics, "audio-played");

        public static NarrationPlaybackResult FromTts(TtsPlaybackResult result, string language, string textForAnalytics)
            => new(result.Completed, result.CreatedNewSession, false, language, textForAnalytics, result.Status);

        public static NarrationPlaybackResult Empty(string language)
            => new(false, false, false, language, string.Empty, "empty");

        public static NarrationPlaybackResult Failed(string language, string status = "failed")
            => new(false, false, false, language, string.Empty, status);
    }
}
