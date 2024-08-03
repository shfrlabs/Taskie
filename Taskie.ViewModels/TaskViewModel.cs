using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Taskie.ViewModels;

public partial class TaskViewModel : ObservableObject
{
    /// <summary>
    /// The task's creation timestamp.
    /// </summary>
    public DateTime CreationDate { get; }

    /// <summary>
    /// The task's name.
    /// </summary>
    [ObservableProperty] private string _name;
    
    /// <summary>
    /// The task's completion status.
    /// </summary>
    [ObservableProperty] private bool _isDone;
}