using System.Linq;

namespace BeatSaverUpdater.Migration
{
    internal class PlaylistMigrator : IMigrator
    {
        public void MigrateMap(IPreviewBeatmapLevel oldMap, IPreviewBeatmapLevel newMap)
        {
            var playlists = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true);
            foreach (var playlist in playlists)
            {
                if (playlist.Any(s => s.PreviewBeatmapLevel == oldMap) && !playlist.TryGetCustomData("syncURL", out var sync))
                {
                    foreach (var song in playlist.Where(s => s.PreviewBeatmapLevel == oldMap))
                    {
                        song.LevelId = newMap.levelID;
                    }
                    BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetManagerForPlaylist(playlist)?.StorePlaylist(playlist);
                }
            }
        }
    }
}
