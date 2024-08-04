using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;

namespace Taskie.ViewModels;

public partial class TaskListViewModel : ObservableObject
{
    /// <summary>
    /// The list's unique identifier.
    /// </summary>
    public Guid Guid { get; init; }

    /// <summary>
    /// The list's creation timestamp.
    /// </summary>
    public DateTime CreationDate { get; init; }

    /// <summary>
    /// The list's name.
    /// </summary>
    [ObservableProperty] private string _name;

    /// <summary>
    /// The tasks contained in the list.
    /// </summary>
    public ObservableCollection<TaskViewModel> TaskViewModels { get; } = [];

    /// <summary>
    /// Serializes the <see cref="TaskListViewModel"/>.
    /// </summary>
    public string Serialize()
    {
        return JsonConvert.SerializeObject(this);
    }

    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Creates a task with the specified name.
    /// </summary>
    /// <param name="name">The task's name.</param>
    [RelayCommand]
    private void Create(string name)
    {
        TaskViewModels.Insert(0, new TaskViewModel
        {
            Guid = Guid.NewGuid(),
            CreationDate = DateTime.Now,
            Name = name
        });
    }
}