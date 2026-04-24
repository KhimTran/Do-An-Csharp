namespace App.Services
{
    public enum TtsPlaybackState
    {
        Started,
        Completed,
        Failed,
        Cancelled
    }

    public sealed class TtsPlaybackStateChangedEventArgs : EventArgs
    {
        public TtsPlaybackStateChangedEventArgs(TtsPlaybackState state, string? tenNoiDungHienThi)
        {
            State = state;
            TenNoiDungHienThi = tenNoiDungHienThi;
        }

        public TtsPlaybackState State { get; }

        public string? TenNoiDungHienThi { get; }
    }
}
