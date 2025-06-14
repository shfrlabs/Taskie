using NeoSmart.Unicode;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace TaskieLib
{
    public partial class Tools
    {

        public static void SetThemeForTitleBar(string theme)
        {
            bool isLightTheme;

            switch (theme)
            {
                case "Light":
                    isLightTheme = true;
                    break;
                case "Dark":
                    isLightTheme = false;
                    break;
                case "System":
                    var uiSettings = new UISettings();
                    var backgroundColor = uiSettings.GetColorValue(UIColorType.Background);
                    isLightTheme = backgroundColor == Colors.White;
                    break;
                default:
                    goto case "System";
            }

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            var grayColor = Color.FromArgb(255, 128, 128, 128);

            var blackForeground = Color.FromArgb(255, 0, 0, 0);
            var blackPressedForeground = Color.FromArgb(255, 96, 96, 96);

            var whiteForeground = Color.FromArgb(255, 255, 255, 255);
            var whitePressedForeground = Color.FromArgb(255, 192, 192, 192);

            titleBar.ButtonForegroundColor = isLightTheme ? blackForeground : whiteForeground;
            titleBar.ButtonHoverForegroundColor = isLightTheme ? blackForeground : whiteForeground;
            titleBar.ButtonPressedForegroundColor = isLightTheme ? blackPressedForeground : whitePressedForeground;
            titleBar.ButtonInactiveForegroundColor = grayColor;
            titleBar.InactiveForegroundColor = grayColor;
            titleBar.ForegroundColor = isLightTheme ? blackForeground : whiteForeground;

            titleBar.BackgroundColor = Colors.Transparent;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            titleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            titleBar.InactiveBackgroundColor = Colors.Transparent;
        }



        public static void SetTheme(string theme)
        {
            try
            {
                switch (theme)
                {
                    case "Light":
                        Application.Current.RequestedTheme = ApplicationTheme.Light;
                        SetThemeForTitleBar(theme);
                        break;
                    case "Dark":
                        Application.Current.RequestedTheme = ApplicationTheme.Dark;
                        SetThemeForTitleBar(theme);
                        break;
                    default:
                        break;
                }
            }
            catch { }
        }

        public static async void RemoveAttachmentsFromList(string id)
        {
            await ApplicationData.Current.LocalFolder.CreateFolderAsync("TaskAttachments", CreationCollisionOption.OpenIfExists);
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("TaskAttachments");
            if (Directory.Exists(Path.Combine(folder.Path, id)))
            {
                try
                {
                    StorageFolder attachmentFolder = await folder.GetFolderAsync(id);
                    await attachmentFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                catch { }
            }
        }
        public SingleEmoji[] Emojis = Emoji.All.ToArray();
        public partial class IncrementalEmojiSource : ObservableCollection<SingleEmoji>, ISupportIncrementalLoading
        {
            private readonly SingleEmoji[] allEmojis;
            private int currentIndex = 0;
            private const int BatchSize = 30;

            public IncrementalEmojiSource()
            {
                allEmojis = Emoji.All.ToArray();
            }

            public bool HasMoreItems => currentIndex < allEmojis.Length;

            public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                return InternalLoadMoreItemsAsync(count).AsAsyncOperation();
            }

            private async Task<LoadMoreItemsResult> InternalLoadMoreItemsAsync(uint count)
            {
                await Task.Delay(50);

                int itemsToLoad = Math.Min(BatchSize, allEmojis.Length - currentIndex);
                for (int i = 0; i < itemsToLoad; i++)
                {
                    Add(allEmojis[currentIndex++]);
                }

                return new LoadMoreItemsResult { Count = (uint)itemsToLoad };
            }
        }
    }
}