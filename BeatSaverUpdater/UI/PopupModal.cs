using System;
using System.ComponentModel;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using IPA.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeatSaverUpdater.UI
{
    internal class PopupModal : INotifyPropertyChanged, IProgress<double>
    {
        private readonly StandardLevelDetailViewController levelDetailViewController;
        private bool parsed;
        public event PropertyChangedEventHandler? PropertyChanged;

        private Action? primaryButtonPressed;
        private Action? secondaryButtonPressed;

        private string _text = "";
        private string _primaryButtonText = "Yes";
        private string _secondaryButtonText = "No";
        private bool _primaryButtonActive = true;
        private bool _secondaryButtonActive = true;

        private LoadingControl? loadingControl;

        [UIComponent("modal")]
        private readonly RectTransform modalTransform = null!;

        private Vector3 modalPosition;

        [UIComponent("vertical")]
        private readonly RectTransform verticalTransform = null!;

        [UIParams]
        private readonly BSMLParserParams parserParams = null!;

        public PopupModal(StandardLevelDetailViewController levelDetailViewController)
        {
            this.levelDetailViewController = levelDetailViewController;
        }

        private void Parse()
        {
            if (!parsed)
            {
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "BeatSaverUpdater.UI.PopupModal.bsml"), levelDetailViewController.gameObject, this);
                modalPosition = modalTransform.localPosition;
                loadingControl = Object.Instantiate(levelDetailViewController.GetField<LoadingControl, StandardLevelDetailViewController>("_loadingControl"), verticalTransform);
                Object.Destroy(loadingControl.GetComponent<Touchable>());
                loadingControl.transform.SetSiblingIndex(1);
                loadingControl.gameObject.SetActive(false);
                parsed = true;
            }
        }

        public void ShowYesNoModal(string text, Action? primaryButtonPressedCallBack, string primaryButtonText = "Yes", string secondaryButtonText = "No", Action? secondaryButtonPressedCallback = null)
        {
            Parse();
            modalTransform.localPosition = modalPosition;

            Text = text;
            PrimaryButtonText = primaryButtonText;
            SecondaryButtonText = secondaryButtonText;

            primaryButtonPressed = primaryButtonPressedCallBack;
            secondaryButtonPressed = secondaryButtonPressedCallback;

            PromptMode();
            parserParams.EmitEvent("open-modal");
        }

        public void ShowDownloadingModal(string text, Action? secondaryButtonPressedCallback, string secondaryButtonText = "Cancel")
        {
            Parse();
            modalTransform.localPosition = modalPosition;

            Text = text;
            SecondaryButtonText = secondaryButtonText;
            secondaryButtonPressed = secondaryButtonPressedCallback;

            DownloadingMode();
            parserParams.EmitEvent("open-modal");
        }

        public void ShowLoadingModal(string text)
        {
            Parse();
            modalTransform.localPosition = modalPosition;

            Text = text;
            if (loadingControl != null)
            {
                loadingControl.ShowLoading(" ");
            }

            LoadingMode();
            parserParams.EmitEvent("open-modal");
        }

        public void HideModal() => parserParams.EmitEvent("close-modal");

        public void Report(double value)
        {
            if (loadingControl != null && loadingControl.isActiveAndEnabled)
            {
                loadingControl.ShowDownloadingProgress(" ", (float)value);
            }
        }

        #region Modal

        // Methods

        private void PromptMode()
        {
            PrimaryButtonActive = true;
            SecondaryButtonActive = true;
            if (loadingControl != null)
            {
                loadingControl.gameObject.SetActive(false);
            }
        }

        private void DownloadingMode()
        {
            PrimaryButtonActive = false;
            SecondaryButtonActive = true;
            if (loadingControl != null)
            {
                loadingControl.gameObject.SetActive(true);
            }
        }

        private void LoadingMode()
        {
            PrimaryButtonActive = false;
            SecondaryButtonActive = false;
            if (loadingControl != null)
            {
                loadingControl.gameObject.SetActive(true);
            }
        }

        [UIAction("primary-button-pressed")]
        private void PrimaryButtonPressed()
        {
            primaryButtonPressed?.Invoke();
            primaryButtonPressed = null;
        }

        [UIAction("secondary-button-pressed")]
        private void SecondaryButtonPressed()
        {
            secondaryButtonPressed?.Invoke();
            secondaryButtonPressed = null;
        }

        // Values

        [UIValue("text")]
        private string Text
        {
            get => _text;
            set
            {
                _text = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }

        [UIValue("primary-button-text")]
        private string PrimaryButtonText
        {
            get => _primaryButtonText;
            set
            {
                _primaryButtonText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrimaryButtonText)));
            }
        }

        [UIValue("secondary-button-text")]
        private string SecondaryButtonText
        {
            get => _secondaryButtonText;
            set
            {
                _secondaryButtonText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SecondaryButtonText)));
            }
        }

        [UIValue("primary-button-active")]
        private bool PrimaryButtonActive
        {
            get => _primaryButtonActive;
            set
            {
                _primaryButtonActive = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrimaryButtonActive)));
            }
        }

        [UIValue("secondary-button-active")]
        private bool SecondaryButtonActive
        {
            get => _secondaryButtonActive;
            set
            {
                _secondaryButtonActive = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SecondaryButtonActive)));
            }
        }

        #endregion
    }
}
