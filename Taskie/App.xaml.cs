using System;
using TaskieLib;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.UI;
using TaskieLib.Models;

namespace Taskie
{
    sealed partial class App : Application
    {
        public App()
        {
            ValidateFairmarkAttachmentsOnStartup();
            this.InitializeComponent();
            Tools.SetTheme(Settings.Theme);
            this.Suspending += OnSuspending;
        }
        public string ToastActivationArgument { get; private set; }
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            bool canEnablePrelaunch = Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "EnablePrelaunch");
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                }

                Window.Current.Content = rootFrame;
            }

            if (e.Arguments.Contains("listId"))
            {
                ToastActivationArgument = e.Arguments;
            }

            if (e.PrelaunchActivated == false)
            {
                if (canEnablePrelaunch)
                {
                    Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);
                }
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                Window.Current.Activate();

                if (Settings.Theme == "Dark")
                {
                    ApplicationView.GetForCurrentView().TitleBar.ButtonForegroundColor = Windows.UI.Colors.White;
                    ApplicationView.GetForCurrentView().TitleBar.ButtonInactiveForegroundColor = Windows.UI.Colors.White;

                }
                else if (Settings.Theme == "Light")
                {
                    ApplicationView.GetForCurrentView().TitleBar.ButtonForegroundColor = Windows.UI.Colors.Black;
                    ApplicationView.GetForCurrentView().TitleBar.ButtonInactiveForegroundColor = Windows.UI.Colors.Black;
                }
            }
        }
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }

        private async void ValidateFairmarkAttachmentsOnStartup()
        {
            // Connect to Fairmark app service
            var connection = new AppServiceConnection
            {
                AppServiceName = "com.sheferslabs.fairmarkservices",
                PackageFamilyName = "BRStudios.3763783C2F5C2_ynj0a7qyfqv8c"
            };
            var status = await connection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
                return;
            var response = await connection.SendMessageAsync(new Windows.Foundation.Collections.ValueSet());
            if (response.Status != AppServiceResponseStatus.Success)
                return;
            string resultString = response.Message["Result"] as string;
            var fairmarkNotes = System.Text.Json.JsonSerializer.Deserialize<List<FairmarkNoteData>>(resultString);
            var fairmarkNoteIds = new HashSet<string>(fairmarkNotes.Select(n => n.id));

            foreach (var (name, id, emoji) in TaskieLib.ListTools.GetLists())
            {
                var data = TaskieLib.ListTools.ReadList(id);
                bool changed = false;
                foreach (var task in data.Tasks)
                {
                    if (task.FMAttachmentIDs != null)
                    {
                        var toRemove = task.FMAttachmentIDs.Where(a => !fairmarkNoteIds.Contains(a)).ToList();
                        foreach (var att in toRemove)
                        {
                            task.FMAttachmentIDs.Remove(att);
                            changed = true;
                        }
                    }
                }
                if (changed)
                {
                    TaskieLib.ListTools.SaveList(id, data.Tasks, data.Metadata);
                }
            }
        }
    }
}
