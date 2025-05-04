using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TaskieLib {
    public partial class Tools {
        public static string[] GetSystemEmojis() {
            List<string> emojis = new List<string>();

            var emojiRanges = new (int Start, int End)[]
            {
            (0x1F600, 0x1F64F), // Emoticons
            (0x1F300, 0x1F5FF), // Miscellaneous Symbols and Pictographs
            (0x1F680, 0x1F6FF), // Transport and Map Symbols
            (0x1F700, 0x1F77F), // Alchemical Symbols
            (0x1F900, 0x1F9FF), // Supplemental Symbols and Pictographs
            (0x1FA70, 0x1FAFF), // Symbols and Pictographs Extended-A
            (0x2600, 0x26FF),   // Miscellaneous Symbols
            (0x2700, 0x27BF),   // Dingbats
            (0xFE00, 0xFE0F),   // Variation Selectors
            };

            foreach (var (start, end) in emojiRanges) {
                for (int codePoint = start; codePoint <= end; codePoint++) {
                    if (char.IsSurrogate((char)codePoint))
                        continue;

                    try {
                        string emoji = char.ConvertFromUtf32(codePoint);
                        emojis.Add(emoji);
                    }
                    catch { }
                }
            }

            return emojis.ToArray();
        }

        private static void SetThemeForTitleBar(bool isLightTheme) {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            var grayColor = Color.FromArgb(255, 128, 128, 128);

            var whiteForegroundColor = Color.FromArgb(255, 255, 255, 255);
            var whitePressedForegroundColor = Color.FromArgb(255, 192, 192, 192);
            var whiteHoverBackgroundColor = Color.FromArgb(24, 255, 255, 255);
            var whitePressedBackgroundColor = Color.FromArgb(16, 255, 255, 255);

            var blackForegroundColor = Color.FromArgb(255, 0, 0, 0);
            var blackPressedForegroundColor = Color.FromArgb(255, 96, 96, 96);
            var blackHoverBackgroundColor = Color.FromArgb(24, 0, 0, 0);
            var blackPressedBackgroundColor = Color.FromArgb(16, 0, 0, 0);

            titleBar.ButtonForegroundColor = isLightTheme ? blackForegroundColor : whiteForegroundColor;
            titleBar.ButtonHoverForegroundColor = isLightTheme ? blackForegroundColor : whiteForegroundColor;
            titleBar.ButtonPressedForegroundColor = isLightTheme ? blackPressedForegroundColor : whitePressedForegroundColor;
            titleBar.ButtonInactiveForegroundColor = grayColor;
            titleBar.InactiveForegroundColor = grayColor;
            titleBar.ForegroundColor = isLightTheme ? blackForegroundColor : whiteForegroundColor;

            titleBar.BackgroundColor = Colors.Transparent;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = isLightTheme ? blackHoverBackgroundColor : whiteHoverBackgroundColor;
            titleBar.ButtonPressedBackgroundColor = isLightTheme ? blackPressedBackgroundColor : whitePressedBackgroundColor;
            titleBar.InactiveBackgroundColor = Colors.Transparent;
        }

        public static async void SetTheme(string theme) {
            try {
                switch (theme) {
                    case "Light":
                        Application.Current.RequestedTheme = ApplicationTheme.Light;
                        break;
                    case "Dark":
                        Application.Current.RequestedTheme = ApplicationTheme.Dark;
                        break;
                    default:
                        break;
                }
            }
            catch {
                //await Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                //{
                //    var themeSetting = Settings.Theme;

                //    if (themeSetting == "System") {
                //        var foregroundColor = Color.FromArgb(255, 255, 255, 255);

                //        (Window.Current.Content as FrameworkElement).RequestedTheme = ElementTheme.Default;

                //        SetThemeForTitleBar(foregroundColor == Color.FromArgb(255, 0, 0, 0));
                //    }
                //    else {
                //        var isLightTheme = themeSetting == "Light";

                //        (Window.Current.Content as FrameworkElement).RequestedTheme = isLightTheme ? ElementTheme.Light : ElementTheme.Dark;

                //        SetThemeForTitleBar(isLightTheme);
                //    }
                //});
            }
        }

        public static async void RemoveAttachmentsFromList(string id) {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("TaskAttachments");
            if (Directory.Exists(Path.Combine(folder.Path, id))) {
                try {
                    StorageFolder attachmentFolder = await folder.GetFolderAsync(id);
                    await attachmentFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                catch { }
            }
        }

        public partial class IncrementalEmojiSource : ObservableCollection<string>, ISupportIncrementalLoading // source for emojis in the "Change emoji" dialog
        {
            private readonly string[] allEmojis;
            private int currentIndex = 0;
            private const int BatchSize = 30;

            public IncrementalEmojiSource(string[] emojis) {
                allEmojis = emojis;
            }

            public bool HasMoreItems => currentIndex < allEmojis.Length;

            public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) {
                return InternalLoadMoreItemsAsync(count).AsAsyncOperation();
            }

            private async Task<LoadMoreItemsResult> InternalLoadMoreItemsAsync(uint count) {
                await Task.Delay(50);

                int itemsToLoad = Math.Min(BatchSize, allEmojis.Length - currentIndex);
                for (int i = 0; i < itemsToLoad; i++) {
                    Add(allEmojis[currentIndex++]);
                }

                return new LoadMoreItemsResult { Count = (uint)itemsToLoad };
            }
        }
    }
}