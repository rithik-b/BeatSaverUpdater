namespace BeatSaverUpdater.Migration
{
    interface IMigrator
    {
        public void MigrateMap(IPreviewBeatmapLevel oldMap, IPreviewBeatmapLevel newMap);
    }
}
