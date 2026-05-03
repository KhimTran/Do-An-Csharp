using App.Models;

namespace App.Services
{
    public interface INarrationService
    {
        Task<NarrationPlaybackResult> PhatThuyetMinhPoiAsync(
            PoiModel poi,
            string? maNgonNgu = null,
            CancellationToken cancellationToken = default);
    }
}
