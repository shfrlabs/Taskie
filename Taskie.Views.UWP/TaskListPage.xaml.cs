using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Taskie.ViewModels;

namespace Taskie.Views.UWP
{
    public sealed partial class TaskListPage : Page
    {
        public TaskListPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is TaskListViewModel taskListViewModel)
            {
                DataContext = taskListViewModel;
            }
            
            base.OnNavigatedTo(e);
        }
    }
}
