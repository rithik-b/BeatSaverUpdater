namespace BeatSaverUpdater.Migration
{
    internal class FavouritesMigrator : IMigrator
    {
        private readonly PlayerDataModel playerDataModel;

        public FavouritesMigrator(PlayerDataModel playerDataModel)
        {
            this.playerDataModel = playerDataModel;
        }

        public bool MigrateMap(CustomPreviewBeatmapLevel oldMap, CustomPreviewBeatmapLevel newMap)
        {
            if (playerDataModel.playerData.IsLevelUserFavorite(oldMap))
            {
                playerDataModel.playerData.RemoveLevelFromFavorites(oldMap);
                playerDataModel.playerData.AddLevelToFavorites(newMap);
                playerDataModel.Save();
            }
            return false;
        }
    }
}
