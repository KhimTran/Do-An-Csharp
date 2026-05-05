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

        public static NarrationPlaybackResult CompletedAudio(
            string language,
            string textForAnalytics,
            bool createdNewSession = true)
            => new(true, createdNewSession, true, language, textForAnalytics, "audio-played");

        public static NarrationPlaybackResult FromTts(TtsPlaybackResult result, string language, string textForAnalytics)
        {
            var status = result.Status == "played" ? "tts-played" : result.Status;
            return new(result.Completed, result.CreatedNewSession, false, language, textForAnalytics, status);
        }

        public static NarrationPlaybackResult MergedFrom(NarrationPlaybackResult result)
            => new(
                result.Completed,
                false,
                result.UsedAudioFile,
                result.Language,
                result.TextForAnalytics,
                result.Completed ? "merged" : result.Status);

        public static NarrationPlaybackResult Queued(string language)
            => new(false, true, false, language, string.Empty, "queued");

        public static NarrationPlaybackResult Cancelled(string language, bool createdNewSession = false)
            => new(false, createdNewSession, false, language, string.Empty, "cancelled");

        public static NarrationPlaybackResult Busy(string language)
            => new(false, false, false, language, string.Empty, "busy");

        public static NarrationPlaybackResult Empty(string language)
            => new(false, false, false, language, string.Empty, "empty");

        public static NarrationPlaybackResult Failed(
            string language,
            string status = "failed",
            bool createdNewSession = false)
            => new(false, createdNewSession, false, language, string.Empty, status);
    }
}
