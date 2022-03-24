using BeatSaverUpdater.Migration;
using BeatSaverUpdater.UI;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace BeatSaverUpdater
{
    [Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; } = null!;
        internal static IPALogger Log { get; private set; } = null!;
        internal static PluginMetadata Metadata { get; private set; } = null!;
        internal static bool PlaylistsLibInstalled => PluginManager.GetPluginFromId("BeatSaberPlaylistsLib") != null;
        private static bool SongDetailsInstalled => PluginManager.GetPluginFromId("SongDetailsCache") != null;

        [Init]
        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        /// Only use [Init] with one Constructor.
        /// </summary>
        public Plugin(IPALogger logger, Zenjector zenjector, PluginMetadata metadata, Config conf)
        {
            Instance = this;
            Log = logger;
            Metadata = metadata;
            PluginConfig.Instance = conf.Generated<PluginConfig>();
            zenjector.Install(Location.Menu, (container) =>
            {
                container.BindInterfacesTo<UpdateButton>().AsSingle();
                container.Bind<PopupModal>().AsSingle();

                container.BindInterfacesTo<FavouritesMigrator>().AsSingle();
                if (PlaylistsLibInstalled)
                {
                    container.BindInterfacesTo<PlaylistMigrator>().AsSingle();
                }

                if (SongDetailsInstalled)
                {
                    container.Bind<SongDetailsWrapper>().AsSingle();
                    container.BindInterfacesTo<SettingsViewController>().AsSingle();
                }
            });
        }
    }
}
