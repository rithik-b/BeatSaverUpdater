using System.Threading.Tasks;

namespace BeatSaverUpdater
{
    public class SongDetailsWrapper
    {
        public async Task<bool> SongExists(string hash)
        {
            var songDetails = await SongDetailsCache.SongDetails.Init();
            return songDetails.songs.FindByHash(hash, out var song);
        }
    }
}