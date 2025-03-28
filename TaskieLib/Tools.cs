using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace TaskieLib {
    public class Tools {
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

        public class IncrementalEmojiSource : ObservableCollection<string>, ISupportIncrementalLoading // source for emojis in the "Change emoji" dialog
        {
            private readonly string[] allEmojis;
            private int currentIndex = 0;
            private const int BatchSize = 50;

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