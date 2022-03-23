using System.Linq;

namespace BeatSaverUpdater.Migration
{
    internal class PlaylistMigrator : IMigrator
    {
        public bool MigrateMap(IPreviewBeatmapLevel oldMap, IPreviewBeatmapLevel newMap)
        {
            var playlists = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true);
            var preventDelete = false;
            
            foreach (var playlist in playlists)
            {
                if (playlist.Any(s => s.PreviewBeatmapLevel == oldMap))
                {
                    if (playlist.TryGetCustomData("syncURL", out var sync))
                    {
                        preventDelete = true;
                    }
                    else
                    {
                        foreach (var song in playlist.Where(s => s.PreviewBeatmapLevel == oldMap))
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
