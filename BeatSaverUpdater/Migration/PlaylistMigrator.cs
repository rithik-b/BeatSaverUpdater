using System.Linq;

namespace BeatSaverUpdater.Migration
{
    internal class PlaylistMigrator : IMigrator
    {
        public bool MigrateMap(CustomPreviewBeatmapLevel oldMap, CustomPreviewBeatmapLevel newMap)
        {
            var playlists = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true);
            var preventDelete = false;
            var mapHash = oldMap.GetBeatmapHash();
            
            foreach (var playlist in playlists)
            {
                if (playlist.Any(s => s.Hash == mapHash))
                {
                    if (playlist.TryGetCustomData("syncURL", out var sync))
                    {
                        preventDelete = true;
                    }
                    else
                    {
                        foreach (var song in playlist.Where(s => s.Hash == mapHash))
                        {
                            song.LevelId = newMap.levelID;
                        }
                        BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetManagerForPlaylist(playlist)?.StorePlaylist(playlist);   
                    }
                }
            }
            
            return preventDelete;
        }
    }
}
