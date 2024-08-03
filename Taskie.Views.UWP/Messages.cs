using CommunityToolkit.Mvvm.Messaging.Messages;
using Taskie.ViewModels;

namespace Taskie.Views.UWP;

/// <summary>
/// Notifies about the pending removal of a <see cref="TaskListViewModel"/> from the main list.
/// </summary>
/// <param name="taskListViewModel">The <see cref="TaskListViewModel"/> which will be removed from the main list.</param>
public class RemovingTaskListViewModelMessage(TaskListViewModel taskListViewModel) : ValueChangedMessage<TaskListViewModel>(taskListViewModel);