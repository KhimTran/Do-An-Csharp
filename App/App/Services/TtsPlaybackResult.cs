namespace App.Services
{
    public sealed class TtsPlaybackResult
    {
        private TtsPlaybackResult(bool accepted, bool createdNewSession, bool completed, string status)
        {
            Accepted = accepted;
            CreatedNewSession = createdNewSession;
            Completed = completed;
            Status = status;
        }

        public bool Accepted { get; }

        public bool CreatedNewSession { get; }

        public bool Completed { get; }

        public string Status { get; }

        public static TtsPlaybackResult Rejected(string status)
            => new(false, false, false, status);

        public static TtsPlaybackResult CompletedNewSession()
            => new(true, true, true, "played");

        public static TtsPlaybackResult CompletedMergedSession()
            => new(true, false, true, "merged");

        public static TtsPlaybackResult Cancelled(bool createdNewSession)
            => new(true, createdNewSession, false, "cancelled");

        public static TtsPlaybackResult Failed(bool createdNewSession)
            => new(true, createdNewSession, false, "failed");
    }
}
