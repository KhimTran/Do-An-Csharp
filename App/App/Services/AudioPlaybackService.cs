using System.Diagnostics;

#if ANDROID
using Android.Media;
#endif

namespace App.Services
{
    public class AudioPlaybackService : IAudioPlaybackService
    {
        private readonly object _stateLock = new();
        private CancellationTokenSource? _currentCts;
        private TaskCompletionSource<bool>? _currentCompletion;

#if ANDROID
        private MediaPlayer? _currentPlayer;
#endif

        public async Task<bool> PlayAsync(string audioUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(audioUrl) ||
                !Uri.TryCreate(audioUrl.Trim(), UriKind.Absolute, out var uri) ||
                (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                 !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            await StopAsync();

            try
            {
                return await PlayPlatformAsync(uri.ToString(), cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayback] Loi phat MP3: {ex.Message}");
                return false;
            }
        }

        public Task StopAsync()
        {
            StopCurrentPlayback();
            return Task.CompletedTask;
        }

        private void StopCurrentPlayback()
        {
            CancellationTokenSource? cts;
            TaskCompletionSource<bool>? completion;

#if ANDROID
            MediaPlayer? player;
#endif

            lock (_stateLock)
            {
                cts = _currentCts;
                completion = _currentCompletion;
                _currentCts = null;
                _currentCompletion = null;

#if ANDROID
                player = _currentPlayer;
                _currentPlayer = null;
#endif
            }

            try
            {
                cts?.Cancel();
            }
            catch
            {
            }

#if ANDROID
            DisposePlayer(player);
#endif

            completion?.TrySetResult(false);
        }

#if ANDROID
        private async Task<bool> PlayPlatformAsync(string audioUrl, CancellationToken cancellationToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var player = new MediaPlayer();

            EventHandler? preparedHandler = null;
            EventHandler? completionHandler = null;
            EventHandler<MediaPlayer.ErrorEventArgs>? errorHandler = null;

            preparedHandler = (_, _) =>
            {
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

            completionHandler = (_, _) => completion.TrySetResult(true);

            errorHandler = (_, args) =>
            {
                args.Handled = true;
                completion.TrySetResult(false);
            };

            player.Prepared += preparedHandler;
            player.Completion += completionHandler;
            player.Error += errorHandler;

            lock (_stateLock)
            {
                _currentCts = linkedCts;
                _currentCompletion = completion;
                _currentPlayer = player;
            }

            using var registration = linkedCts.Token.Register(() =>
            {
                StopCurrentPlayback();
                completion.TrySetResult(false);
            });

            try
            {
                player.SetDataSource(audioUrl);
                player.PrepareAsync();

                var completed = await completion.Task;
                return completed && !linkedCts.Token.IsCancellationRequested;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioPlayback] Loi tai/phat MP3: {ex.Message}");
                return false;
            }
            finally
            {
                player.Prepared -= preparedHandler;
                player.Completion -= completionHandler;
                player.Error -= errorHandler;

                lock (_stateLock)
                {
                    if (ReferenceEquals(_currentPlayer, player))
                    {
                        _currentPlayer = null;
                        _currentCts = null;
                        _currentCompletion = null;
                    }
                }

                DisposePlayer(player);
            }
        }

        private static void DisposePlayer(MediaPlayer? player)
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
#else
        private Task<bool> PlayPlatformAsync(string audioUrl, CancellationToken cancellationToken)
            => Task.FromResult(false);
#endif
    }
}
