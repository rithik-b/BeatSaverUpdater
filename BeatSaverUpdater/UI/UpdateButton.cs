using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Components;
using SongDetailsCache;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRUIControls;
using Zenject;

namespace BeatSaverUpdater.UI
{
    internal class UpdateButton : IInitializable, IDisposable
    {
        private ClickableImage? image;
        private SongDetails? songDetails;
        private CancellationTokenSource? tokenSource;
        private string? downloadedLevelHash;
        private readonly DiContainer container;
        private readonly LevelCollectionNavigationController levelCollectionNavigationController;
        private readonly StandardLevelDetailViewController standardLevelDetailViewController;
        private readonly PopupModal popupModal;

        public UpdateButton(DiContainer container, LevelCollectionNavigationController levelCollectionNavigationController, StandardLevelDetailViewController standardLevelDetailViewController, PopupModal popupModal)
        {
            this.container = container;
            this.levelCollectionNavigationController = levelCollectionNavigationController;
            this.standardLevelDetailViewController = standardLevelDetailViewController;
            this.popupModal = popupModal;
        }

        public void Initialize()
        {
            _ = InitializeAsync();

            standardLevelDetailViewController.didChangeContentEvent += ContentChanged;
        }

        public void Dispose()
        {
            if (image != null)
            {
                image.OnClickEvent -= Clicked;
            }
        }

        private async Task InitializeAsync()
        {
            image = CreateImage();
            using Stream? mrs = Plugin.Metadata.Assembly.GetManifestResourceStream("BeatSaverUpdater.Images.Logo.png");
            using MemoryStream ms = new MemoryStream();
            if (mrs != null)
            {
                await mrs.CopyToAsync(ms);
            }

            image.OnClickEvent += Clicked;
            image.sprite = BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(ms.ToArray());
            image.sprite.texture.wrapMode = TextureWrapMode.Clamp;
            image.gameObject.SetActive(false);

            songDetails = await SongDetails.Init();
        }

        private ClickableImage CreateImage()
        {
            GameObject gameObject = new GameObject("BeatSaverUpdater");
            ClickableImage image = gameObject.AddComponent<ClickableImage>();
            image.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;

            image.rectTransform.SetParent(standardLevelDetailViewController.transform);
            image.rectTransform.localPosition = new Vector3(32f, 32f, 0f);
            image.rectTransform.localScale = new Vector3(.3f, .3f, .3f);
            image.rectTransform.sizeDelta = new Vector2(20f, 20f);
            gameObject.AddComponent<LayoutElement>();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
            container.InstantiateComponent<VRGraphicRaycaster>(gameObject);

            return image;
        }

        private void ContentChanged(StandardLevelDetailViewController standardLevelDetailViewController, StandardLevelDetailViewController.ContentType contentType)
        {
            if (contentType == StandardLevelDetailViewController.ContentType.OwnedAndReady)
            {
                _ = Task.Run(() => BeatmapSelected(standardLevelDetailViewController.beatmapLevel));
            }
        }

        private async Task BeatmapSelected(IPreviewBeatmapLevel beatmapLevel)
        {
            tokenSource?.Cancel();
            tokenSource = new CancellationTokenSource();

            if (image != null && songDetails != null)
            {
                if (beatmapLevel is CustomPreviewBeatmapLevel customPreviewBeatmapLevel && !customPreviewBeatmapLevel.levelID.EndsWith(" WIP"))
                {
                    if (!songDetails.songs.FindByHash(customPreviewBeatmapLevel.GetBeatmapHash(), out var song))
                    {
                        image.gameObject.SetActive(await customPreviewBeatmapLevel.NeedsUpdate(tokenSource.Token));
                        return;
                    }
                }
                image.gameObject.SetActive(false);
            }
        }

        private void Clicked(PointerEventData _)
        {
            popupModal.ShowYesNoModal("This map has an update on BeatSaver. Do you want to download it?", UpdateRequested);
        }

        private async void UpdateRequested()
        {
            tokenSource?.Cancel();
            tokenSource = new CancellationTokenSource();
            popupModal.ShowDownloadingModal("Updating map", () => tokenSource.Cancel());
            downloadedLevelHash = await standardLevelDetailViewController.beatmapLevel.UpdateBeatmap(tokenSource.Token, popupModal);
            if (downloadedLevelHash != null)
            {
                SongCore.Loader.SongsLoadedEvent += OnSongsLoaded;
                SongCore.Loader.Instance.RefreshSongs(false);
            }
        }

        private void OnSongsLoaded(SongCore.Loader arg1, System.Collections.Concurrent.ConcurrentDictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            SongCore.Loader.SongsLoadedEvent -= OnSongsLoaded;
            var downloadedLevel = SongCore.Loader.GetLevelByHash(downloadedLevelHash ?? "");
            if (downloadedLevel != null)
            {
                popupModal.ShowYesNoModal("Map Updated!", () => GoToLevel(downloadedLevel), "Go to map!", "Dismiss");
            }
            else
            {
                popupModal.HideModal();
            }
        }

        private void GoToLevel(CustomPreviewBeatmapLevel downloadedLevel)
        {
            popupModal.HideModal();
            levelCollectionNavigationController.SelectLevel(downloadedLevel);
        }
    }
}
