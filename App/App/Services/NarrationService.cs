using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using App.Models;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class NarrationService : INarrationService
    {
        private const int MaxQueueSize = 6;

        private readonly ITtsService _tts;
        private readonly IAudioPlaybackService _audioPlayback;
        private readonly IOfflineAudioCacheService _offlineAudioCache;
        private readonly object _queueLock = new();
        private readonly ConcurrentQueue<NarrationQueueItem> _queue = new();
        private readonly SemaphoreSlim _queueProcessorLock = new(1, 1);
        private readonly Dictionary<string, NarrationQueueItem> _requestsByKey = new(StringComparer.Ordinal);

        private bool _queueProcessorStarted;
        private NarrationQueueItem? _currentItem;
        private CancellationTokenSource? _currentPlaybackCts;

        public NarrationService(
            ITtsService tts,
            IAudioPlaybackService audioPlayback,
            IOfflineAudioCacheService offlineAudioCache)
        {
            _tts = tts;
            _audioPlayback = audioPlayback;
            _offlineAudioCache = offlineAudioCache;
        }

        public async Task<NarrationPlaybackResult> PhatThuyetMinhPoiAsync(
            PoiModel poi,
            string? maNgonNgu = null,
            CancellationToken cancellationToken = default,
            NarrationRequestPriority priority = NarrationRequestPriority.Normal,
            bool interruptCurrent = false,
            string? source = null)
        {
            var request = BuildRequest(poi, maNgonNgu, priority, interruptCurrent, source, cancellationToken);
            var shouldInterrupt = interruptCurrent || priority >= NarrationRequestPriority.High;

            NarrationQueueItem? itemToAwait = null;
            Task<NarrationPlaybackResult>? mergedTask = null;
            var cancelledItems = new List<NarrationQueueItem>();
            var shouldStartProcessor = false;

            lock (_queueLock)
            {
                if (_requestsByKey.TryGetValue(request.Key, out var existingItem))
                {
                    if (shouldInterrupt && !ReferenceEquals(existingItem, _currentItem))
                    {
                        Debug.WriteLine($"[NarrationQueue] Promote pending duplicate key={request.Key}, source={request.Source}");
                        existingItem.Cancel();
                        _requestsByKey.Remove(request.Key);
                        cancelledItems.Add(existingItem);
                        ClearPendingQueueLocked(cancelledItems, $"interrupt by {request.Source}");
                        itemToAwait = EnqueueRequestLocked(request, out shouldStartProcessor);
                    }
                    else
                    {
                        Debug.WriteLine($"[NarrationQueue] Merge duplicate key={request.Key}, source={request.Source}");
                        mergedTask = existingItem.Completion.Task;
                    }
                }
                else
                {
                    if (shouldInterrupt)
                    {
                        ClearPendingQueueLocked(cancelledItems, $"interrupt by {request.Source}");
                    }
                    else if (_queue.Count >= MaxQueueSize)
                    {
                        DropOldestPendingRequestLocked(cancelledItems);
                    }

                    itemToAwait = EnqueueRequestLocked(request, out shouldStartProcessor);
                }
            }

            CompleteCancelledPendingRequests(cancelledItems);

            if (mergedTask != null)
            {
                return await WaitForMergedResultAsync(mergedTask, request.RequestedLanguage, cancellationToken);
            }

            if (shouldInterrupt)
            {
                await InterruptCurrentPlaybackAsync(request);
            }

            if (shouldStartProcessor)
            {
                _ = ProcessQueueAsync();
            }

            return itemToAwait == null
                ? NarrationPlaybackResult.Failed(request.RequestedLanguage)
                : await WaitForOwnResultAsync(itemToAwait, cancellationToken);
        }

        private NarrationQueueItem EnqueueRequestLocked(
            NarrationRequest request,
            out bool shouldStartProcessor)
        {
            var item = new NarrationQueueItem(request);
            _queue.Enqueue(item);
            _requestsByKey[request.Key] = item;

            Debug.WriteLine(
                $"[NarrationQueue] Enqueued key={request.Key}, priority={request.Priority}, interrupt={request.InterruptCurrent}, source={request.Source}, queued={_queue.Count}");

            shouldStartProcessor = false;
            if (!_queueProcessorStarted)
            {
                _queueProcessorStarted = true;
                shouldStartProcessor = true;
            }

            return item;
        }

        private NarrationRequest BuildRequest(
            PoiModel poi,
            string? maNgonNgu,
            NarrationRequestPriority priority,
            bool interruptCurrent,
            string? source,
            CancellationToken cancellationToken)
        {
            var requestedLanguage = ResolveLanguage(maNgonNgu);
            var description = PoiDescriptionResolver.GetBestDescriptionWithLanguage(poi, requestedLanguage);
            var offlineMode = Preferences.Get("offline_mode", false);
            var key = BuildDuplicateKey(poi, requestedLanguage, description, offlineMode);

            return new NarrationRequest(
                poi,
                requestedLanguage,
                description.Language,
                description.Text,
                offlineMode,
                priority,
                interruptCurrent,
                string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim(),
                key,
                cancellationToken);
        }

        private async Task<NarrationPlaybackResult> WaitForOwnResultAsync(
            NarrationQueueItem item,
            CancellationToken cancellationToken)
        {
            try
            {
                return cancellationToken.CanBeCanceled
                    ? await item.Completion.Task.WaitAsync(cancellationToken)
                    : await item.Completion.Task;
            }
            catch (OperationCanceledException)
            {
                var wasCurrent = CancelQueueItem(item);
                if (wasCurrent)
                {
                    await StopPlaybackForCancellationAsync("[NarrationQueue] Caller cancelled current narration.");
                }

                return NarrationPlaybackResult.Cancelled(item.Request.RequestedLanguage);
            }
        }

        private static async Task<NarrationPlaybackResult> WaitForMergedResultAsync(
            Task<NarrationPlaybackResult> mergedTask,
            string requestedLanguage,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = cancellationToken.CanBeCanceled
                    ? await mergedTask.WaitAsync(cancellationToken)
                    : await mergedTask;

                return NarrationPlaybackResult.MergedFrom(result);
            }
            catch (OperationCanceledException)
            {
                return NarrationPlaybackResult.Cancelled(requestedLanguage);
            }
        }

        private async Task ProcessQueueAsync()
        {
            await _queueProcessorLock.WaitAsync();

            try
            {
                while (true)
                {
                    var item = DequeueNextValidItem();
                    if (item == null)
                        break;

                    NarrationPlaybackResult result;
                    try
                    {
                        result = await PlayQueuedItemAsync(item);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[NarrationQueue] Failed key={item.Request.Key}: {ex.Message}");
                        result = NarrationPlaybackResult.Failed(
                            item.Request.RequestedLanguage,
                            createdNewSession: true);
                    }

                    Debug.WriteLine($"[NarrationQueue] Completed key={item.Request.Key}, status={result.Status}");
                    item.Completion.TrySetResult(result);
                    FinishCurrentItem(item);
                    item.Dispose();
                }
            }
            finally
            {
                lock (_queueLock)
                {
                    _currentItem = null;
                    _currentPlaybackCts = null;
                    _queueProcessorStarted = false;

                    if (!_queue.IsEmpty)
                    {
                        _queueProcessorStarted = true;
                        _ = ProcessQueueAsync();
                    }
                }

                _queueProcessorLock.Release();
            }
        }

        private NarrationQueueItem? DequeueNextValidItem()
        {
            lock (_queueLock)
            {
                while (_queue.TryDequeue(out var item))
                {
                    if (item.IsCancelled)
                    {
                        item.Dispose();
                        continue;
                    }

                    if (!_requestsByKey.TryGetValue(item.Request.Key, out var registered) ||
                        !ReferenceEquals(registered, item))
                    {
                        item.Dispose();
                        continue;
                    }

                    _currentItem = item;
                    return item;
                }

                _queueProcessorStarted = false;
                return null;
            }
        }

        private async Task<NarrationPlaybackResult> PlayQueuedItemAsync(NarrationQueueItem item)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                item.Request.CancellationToken,
                item.InternalCancellation.Token);

            lock (_queueLock)
            {
                _currentPlaybackCts = linkedCts;
            }

            try
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                Debug.WriteLine(
                    $"[NarrationQueue] Start key={item.Request.Key}, priority={item.Request.Priority}, source={item.Request.Source}");

                return await PlayNarrationAsync(item.Request, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[NarrationQueue] Cancelled key={item.Request.Key}");
                return NarrationPlaybackResult.Cancelled(item.Request.RequestedLanguage, createdNewSession: true);
            }
            finally
            {
                lock (_queueLock)
                {
                    if (ReferenceEquals(_currentItem, item))
                        _currentPlaybackCts = null;
                }
            }
        }

        private async Task<NarrationPlaybackResult> PlayNarrationAsync(
            NarrationRequest request,
            CancellationToken cancellationToken)
        {
            foreach (var language in BuildAudioLanguagePriority(request.RequestedLanguage))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var localPath = _offlineAudioCache.GetCachedAudioPath(request.Poi, language);
                if (!string.IsNullOrWhiteSpace(localPath))
                {
                    _tts.DungPhat();
                    Debug.WriteLine($"[NarrationQueue] Play local audio poi={request.Poi.Id}, lang={language}, path={localPath}");

                    if (await _audioPlayback.PlayAsync(localPath, cancellationToken))
                    {
                        return NarrationPlaybackResult.CompletedAudio(language, request.DescriptionText);
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return NarrationPlaybackResult.Cancelled(request.RequestedLanguage, createdNewSession: true);

                    Debug.WriteLine($"[NarrationQueue] Local audio failed, fallback allowed poi={request.Poi.Id}, lang={language}");
                }

                if (request.OfflineMode)
                    continue;

                var fileName = GetAudioFileName(request.Poi, language);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    var audioUrl = ApiEndpointResolver.BuildPoiAudioUrl(fileName);
                    if (!string.IsNullOrWhiteSpace(audioUrl))
                    {
                        _tts.DungPhat();
                        Debug.WriteLine($"[NarrationQueue] Play server audio poi={request.Poi.Id}, lang={language}, url={audioUrl}");

                        if (await _audioPlayback.PlayAsync(audioUrl, cancellationToken))
                        {
                            return NarrationPlaybackResult.CompletedAudio(language, request.DescriptionText);
                        }

                        if (cancellationToken.IsCancellationRequested)
                            return NarrationPlaybackResult.Cancelled(request.RequestedLanguage, createdNewSession: true);

                        Debug.WriteLine($"[NarrationQueue] Server audio failed, fallback to TTS poi={request.Poi.Id}, lang={language}");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(request.DescriptionText))
            {
                await _audioPlayback.StopAsync();
                Debug.WriteLine($"[NarrationQueue] Empty narration text poi={request.Poi.Id}, lang={request.RequestedLanguage}");
                return NarrationPlaybackResult.Empty(request.RequestedLanguage);
            }

            await _audioPlayback.StopAsync();
            var ttsLanguage = ToTtsLanguageCode(request.DescriptionLanguage);
            var audioKey = $"poi:{request.Poi.Id}:{request.DescriptionLanguage}:text:{HashForKey(request.DescriptionText)}";

            Debug.WriteLine($"[NarrationQueue] Play TTS poi={request.Poi.Id}, lang={request.DescriptionLanguage}, key={audioKey}");
            using var registration = cancellationToken.Register(() => _tts.DungPhat());

            var ttsResult = await _tts.PhatAmAsync(
                request.DescriptionText,
                ttsLanguage,
                audioKey,
                request.Poi.Ten);

            return cancellationToken.IsCancellationRequested
                ? NarrationPlaybackResult.Cancelled(request.RequestedLanguage, createdNewSession: true)
                : NarrationPlaybackResult.FromTts(ttsResult, request.DescriptionLanguage, request.DescriptionText);
        }

        private async Task InterruptCurrentPlaybackAsync(NarrationRequest request)
        {
            NarrationQueueItem? currentItem;
            CancellationTokenSource? currentCts;

            lock (_queueLock)
            {
                currentItem = _currentItem;
                currentCts = _currentPlaybackCts;
                currentItem?.Cancel();
            }

            Debug.WriteLine(
                $"[NarrationQueue] Interrupt requested by source={request.Source}, priority={request.Priority}, currentKey={currentItem?.Request.Key ?? "none"}");

            currentCts?.Cancel();
            await StopPlaybackForCancellationAsync("[NarrationQueue] Interrupt stopped current playback.");
        }

        private async Task StopPlaybackForCancellationAsync(string logMessage)
        {
            Debug.WriteLine(logMessage);
            _tts.DungPhat();
            await _audioPlayback.StopAsync();
        }

        private bool CancelQueueItem(NarrationQueueItem item)
        {
            lock (_queueLock)
            {
                item.Cancel();

                if (ReferenceEquals(_currentItem, item))
                    return true;

                if (_requestsByKey.TryGetValue(item.Request.Key, out var registered) &&
                    ReferenceEquals(registered, item))
                {
                    _requestsByKey.Remove(item.Request.Key);
                    item.Completion.TrySetResult(NarrationPlaybackResult.Cancelled(item.Request.RequestedLanguage));
                }

                return false;
            }
        }

        private void FinishCurrentItem(NarrationQueueItem item)
        {
            lock (_queueLock)
            {
                if (ReferenceEquals(_currentItem, item))
                    _currentItem = null;

                if (_requestsByKey.TryGetValue(item.Request.Key, out var registered) &&
                    ReferenceEquals(registered, item))
                {
                    _requestsByKey.Remove(item.Request.Key);
                }
            }
        }

        private void ClearPendingQueueLocked(List<NarrationQueueItem> cancelledItems, string reason)
        {
            while (_queue.TryDequeue(out var removed))
            {
                if (!_requestsByKey.TryGetValue(removed.Request.Key, out var registered) ||
                    !ReferenceEquals(registered, removed))
                {
                    removed.Dispose();
                    continue;
                }

                removed.Cancel();
                _requestsByKey.Remove(removed.Request.Key);
                cancelledItems.Add(removed);
                Debug.WriteLine($"[NarrationQueue] Drop pending key={removed.Request.Key}, reason={reason}");
            }
        }

        private void DropOldestPendingRequestLocked(List<NarrationQueueItem> cancelledItems)
        {
            while (_queue.TryDequeue(out var removed))
            {
                if (!_requestsByKey.TryGetValue(removed.Request.Key, out var registered) ||
                    !ReferenceEquals(registered, removed))
                {
                    removed.Dispose();
                    continue;
                }

                removed.Cancel();
                _requestsByKey.Remove(removed.Request.Key);
                cancelledItems.Add(removed);
                Debug.WriteLine($"[NarrationQueue] Queue full, dropped oldest key={removed.Request.Key}, max={MaxQueueSize}");
                return;
            }
        }

        private static void CompleteCancelledPendingRequests(IEnumerable<NarrationQueueItem> cancelledItems)
        {
            foreach (var item in cancelledItems)
            {
                item.Completion.TrySetResult(NarrationPlaybackResult.Cancelled(item.Request.RequestedLanguage));
                item.Dispose();
            }
        }

        private string BuildDuplicateKey(
            PoiModel poi,
            string requestedLanguage,
            (string Language, string Text) description,
            bool offlineMode)
        {
            foreach (var language in BuildAudioLanguagePriority(requestedLanguage))
            {
                var localPath = _offlineAudioCache.GetCachedAudioPath(poi, language);
                if (!string.IsNullOrWhiteSpace(localPath))
                    return $"poi:{poi.Id}:{language}:local:{HashForKey(localPath)}";

                if (offlineMode)
                    continue;

                var fileName = GetAudioFileName(poi, language);
                var audioUrl = ApiEndpointResolver.BuildPoiAudioUrl(fileName);
                if (!string.IsNullOrWhiteSpace(audioUrl))
                    return $"poi:{poi.Id}:{language}:server:{HashForKey(audioUrl)}";
            }

            return string.IsNullOrWhiteSpace(description.Text)
                ? $"poi:{poi.Id}:{requestedLanguage}:empty"
                : $"poi:{poi.Id}:{description.Language}:text:{HashForKey(description.Text)}";
        }

        private static string ResolveLanguage(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                language = Preferences.Get("app_language", Preferences.Get("tts_language", "vi-VN"));
            }

            return PoiDescriptionResolver.NormalizeLanguage(language);
        }

        private static string? GetAudioFileName(PoiModel poi, string language)
        {
            var fileName = language switch
            {
                "en" => poi.TenFileAudio_En,
                "zh" => poi.TenFileAudio_Zh,
                _ => poi.TenFileAudio_Vi
            };

            return string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
        }

        private static IEnumerable<string> BuildAudioLanguagePriority(string requestedLanguage)
        {
            yield return requestedLanguage;
        }

        private static string ToTtsLanguageCode(string language) => language switch
        {
            "en" => "en-US",
            "zh" => "zh-CN",
            _ => "vi-VN"
        };

        private static string HashForKey(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
            return Convert.ToHexString(hash)[..16];
        }

        private sealed record NarrationRequest(
            PoiModel Poi,
            string RequestedLanguage,
            string DescriptionLanguage,
            string DescriptionText,
            bool OfflineMode,
            NarrationRequestPriority Priority,
            bool InterruptCurrent,
            string Source,
            string Key,
            CancellationToken CancellationToken);

        private sealed class NarrationQueueItem : IDisposable
        {
            private int _cancelled;

            public NarrationQueueItem(NarrationRequest request)
            {
                Request = request;
                Completion = new TaskCompletionSource<NarrationPlaybackResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                InternalCancellation = new CancellationTokenSource();
            }

            public NarrationRequest Request { get; }

            public TaskCompletionSource<NarrationPlaybackResult> Completion { get; }

            public CancellationTokenSource InternalCancellation { get; }

            public bool IsCancelled => _cancelled != 0;

            public void Cancel()
            {
                Interlocked.Exchange(ref _cancelled, 1);
                try
                {
                    InternalCancellation.Cancel();
                }
                catch
                {
                }
            }

            public void Dispose()
            {
                InternalCancellation.Dispose();
            }
        }
    }
}
