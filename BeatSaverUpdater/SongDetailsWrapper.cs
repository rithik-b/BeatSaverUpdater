using System.Threading.Tasks;

namespace BeatSaverUpdater
{
    internal class SongDetailsWrapper
    {
        public async Task<bool> SongExists(string hash)
        {
            var songDetails = await SongDetailsCache.SongDetails.Init();
            return songDetails.songs.FindByHash(hash, out var song);
        }
    }
}