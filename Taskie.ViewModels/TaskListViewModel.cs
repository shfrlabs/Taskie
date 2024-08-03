using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Taskie.ViewModels;

public partial class TaskListViewModel : ObservableObject
{
    /// <summary>
    /// The list's creation timestamp.
    /// </summary>
    public DateTime CreationDate { get; init;  }
    
    /// <summary>
    /// The list's name.
    /// </summary>
    [ObservableProperty] private string _name;
    
    /// <summary>
    /// The tasks contained in the list.
    /// </summary>
    public ObservableCollection<TaskViewModel> TaskViewModels { get; } = [];
}