using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TaskieLib;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//Szablon elementu Pusta strona jest udokumentowany na stronie https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taskie
{
    /// <summary>
    /// Pusta strona, która może być używana samodzielnie lub do której można nawigować wewnątrz ramki.
    /// </summary>
    public sealed partial class ExportImportPage : Page
    {
        public ExportImportPage()
        {
            this.InitializeComponent();
        }

        private async void export_Click(object sender, RoutedEventArgs e)
        {
            StorageFile exportFile = await Tools.ExportedLists();
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            savePicker.SuggestedFileName = exportFile.Name;
            savePicker.FileTypeChoices.Add(exportFile.FileType, new List<string> { exportFile.FileType });
            StorageFile destinationFile = await savePicker.PickSaveFileAsync();
            if (destinationFile != null)
            {
                await exportFile.CopyAndReplaceAsync(destinationFile);
            }
            else
            {
            }
            File.Delete(exportFile.Path);
        }
    }
}
