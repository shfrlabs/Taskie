using System;
using TaskieLib;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Globalization;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Taskie {
    sealed partial class App : Application {
        public App() {
            ApplicationLanguages.PrimaryLanguageOverride = "en-US";
            if (Settings.Theme == "Dark") {
                Application.Current.RequestedTheme = ApplicationTheme.Dark;

            }
            else if (Settings.Theme == "Light") {
                Application.Current.RequestedTheme = ApplicationTheme.Light;
            }
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }
        public string ToastActivationArgument { get; private set; }
        protected override void OnLaunched(LaunchActivatedEventArgs e) {
            bool canEnablePrelaunch = Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "EnablePrelaunch");
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null) {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated) {
                }

                Window.Current.Content = rootFrame;
            }

            if (e.Arguments.Contains("listId")) {
                ToastActivationArgument = e.Arguments;
            }

            if (e.PrelaunchActivated == false) {
                if (canEnablePrelaunch) {
                    Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);
                }
                if (rootFrame.Content == null) {
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                Window.Current.Activate();
                if (Settings.Theme == "Dark") {
                    ApplicationView.GetForCurrentView().TitleBar.ButtonForegroundColor = Windows.UI.Colors.White;
                    ApplicationView.GetForCurrentView().TitleBar.ButtonInactiveForegroundColor = Windows.UI.Colors.White;

                }
                else if (Settings.Theme == "Light") {
                    ApplicationView.GetForCurrentView().TitleBar.ButtonForegroundColor = Windows.UI.Colors.Black;
                    ApplicationView.GetForCurrentView().TitleBar.ButtonInactiveForegroundColor = Windows.UI.Colors.Black;
                }
            }
        }
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e) {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        private void OnSuspending(object sender, SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}
