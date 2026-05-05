using System.Diagnostics;

#if ANDROID
using Android.Media;
#endif

namespace App.Services
{
    public class AudioPlaybackService : IAudioPlaybackService
    {
        private readonly object _stateLock = new();
        private readonly SemaphoreSlim _operationLock = new(1, 1);

#if ANDROID
        private PlaybackSession? _currentSession;
#endif

        public async Task<bool> PlayAsync(string audioSource, CancellationToken cancellationToken = default)
        {
            if (!TryResolveAudioSource(audioSource, out var dataSource))
            {
                return false;
            }

#if ANDROID
            PlaybackSession? session = null;

            try
            {
                await _operationLock.WaitAsync(cancellationToken);
                try
                {
                    StopCurrentPlayback();
                    session = StartPlatformSession(dataSource, cancellationToken);
                }
                finally
                {
                    _operationLock.Release();
                }

                return await WaitForSessionAsync(session);
            }
            catch (OperationCanceledException)
            {
                StopSessionIfCurrent(session, cancelSession: false);
                return false;
            }
            catch (Exception ex)
            {
                StopSessionIfCurrent(session, cancelSession: true);
                Debug.WriteLine($"[AudioPlayback] Loi phat MP3: {ex.Message}");
                return false;
            }
#else
            await StopAsync();
            return false;
#endif
        }

        public async Task StopAsync()
        {
            await _operationLock.WaitAsync();
            try
            {
                StopCurrentPlayback();
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private void StopCurrentPlayback()
        {
#if ANDROID
            PlaybackSession? session;

            lock (_stateLock)
            {
                session = _currentSession;
                _currentSession = null;
            }

            StopSessionPlayback(session, cancelSession: true);
#endif
        }

        private static bool TryResolveAudioSource(string audioSource, out string dataSource)
        {
            dataSource = string.Empty;

            if (string.IsNullOrWhiteSpace(audioSource))
                return false;

            var trimmed = audioSource.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    dataSource = uri.ToString();
                    return true;
                }

                if (uri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(uri.LocalPath))
                {
                    dataSource = uri.LocalPath;
                    return true;
                }
            }

            if (Path.IsPathRooted(trimmed) && File.Exists(trimmed))
            {
                dataSource = trimmed;
                return true;
            }

            return false;
        }

#if ANDROID
        private PlaybackSession StartPlatformSession(string audioSource, CancellationToken cancellationToken)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var player = new MediaPlayer();
            var session = new PlaybackSession(player, linkedCts, completion);

            session.PreparedHandler = (_, _) =>
            {
                if (session.IsPlayerDisposed || linkedCts.IsCancellationRequested)
                {
                    completion.TrySetResult(false);
                    return;
                }

                try
                {
                    player.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AudioPlayback] Khong the start MP3: {ex.Message}");
                    completion.TrySetResult(false);
                }
            };

            session.CompletionHandler = (_, _) => completion.TrySetResult(true);

            session.ErrorHandler = (_, args) =>
            {
                args.Handled = true;
                Debug.WriteLine($"[AudioPlayback] MediaPlayer error what={args.What}, extra={args.Extra}");
                completion.TrySetResult(false);
            };

            session.AttachHandlers();

            lock (_stateLock)
            {
                _currentSession = session;
            }

            session.CancellationRegistration = linkedCts.Token.Register(() =>
            {
                StopSessionIfCurrent(session, cancelSession: false);
            });

            try
            {
                player.SetDataSource(audioSource);
                player.PrepareAsync();
                return session;
            }
            catch
            {
                StopSessionIfCurrent(session, cancelSession: true);
                throw;
            }
        }

        private async Task<bool> WaitForSessionAsync(PlaybackSession session)
        {
            try
            {
                var completed = await session.Completion.Task;
                return completed && !session.Cancellation.IsCancellationRequested;
            }
            finally
            {
                ClearCurrentIfSession(session);
                session.Dispose();
            }
        }

        private void StopSessionIfCurrent(PlaybackSession? session, bool cancelSession)
        {
            if (session == null)
                return;

            var shouldStop = false;
            lock (_stateLock)
            {
                if (ReferenceEquals(_currentSession, session))
                {
                    _currentSession = null;
                    shouldStop = true;
                }
            }

            if (shouldStop)
                StopSessionPlayback(session, cancelSession);
        }

        private void ClearCurrentIfSession(PlaybackSession session)
        {
            lock (_stateLock)
            {
                if (ReferenceEquals(_currentSession, session))
                    _currentSession = null;
            }
        }

        private static void StopSessionPlayback(PlaybackSession? session, bool cancelSession)
        {
            if (session == null)
                return;

            if (cancelSession && !session.Cancellation.IsCancellationRequested)
            {
                try
                {
                    session.Cancellation.Cancel();
                }
                catch
                {
                }
            }

            session.DisposePlayer();
            session.Completion.TrySetResult(false);
        }

        private static void ReleasePlayer(MediaPlayer? player)
        {
            if (player == null)
                return;

            try
            {
                if (player.IsPlaying)
                    player.Stop();
            }
            catch
            {
            }

            try
            {
                player.Reset();
            }
            catch
            {
            }

            try
            {
                player.Release();
            }
            catch
            {
            }

            player.Dispose();
        }

        private sealed class PlaybackSession : IDisposable
        {
            private int _playerDisposed;
            private int _disposed;

            public PlaybackSession(
                MediaPlayer player,
                CancellationTokenSource cancellation,
                TaskCompletionSource<bool> completion)
            {
                Player = player;
                Cancellation = cancellation;
                Completion = completion;
            }

            public MediaPlayer Player { get; }

            public CancellationTokenSource Cancellation { get; }

            public TaskCompletionSource<bool> Completion { get; }

            public CancellationTokenRegistration CancellationRegistration { get; set; }

            public EventHandler? PreparedHandler { get; set; }

            public EventHandler? CompletionHandler { get; set; }

            public EventHandler<MediaPlayer.ErrorEventArgs>? ErrorHandler { get; set; }

            public bool IsPlayerDisposed => _playerDisposed != 0;

            public void AttachHandlers()
            {
                if (PreparedHandler != null)
                    Player.Prepared += PreparedHandler;

                if (CompletionHandler != null)
                    Player.Completion += CompletionHandler;

                if (ErrorHandler != null)
                    Player.Error += ErrorHandler;
            }

            public void DisposePlayer()
            {
                if (Interlocked.Exchange(ref _playerDisposed, 1) != 0)
                    return;

                try
                {
                    if (PreparedHandler != null)
                        Player.Prepared -= PreparedHandler;

                    if (CompletionHandler != null)
                        Player.Completion -= CompletionHandler;

                    if (ErrorHandler != null)
                        Player.Error -= ErrorHandler;
                }
                catch
                {
                }

                ReleasePlayer(Player);
            }

            public void Dispose()
            {
                DisposePlayer();

                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                try
                {
                    CancellationRegistration.Dispose();
                }
                catch
                {
                }

                Cancellation.Dispose();
            }
        }
#endif
    }
}
