using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Taskie.ViewModels;

/// <summary>
/// Requests the removal of a <see cref="TaskListViewModel"/> from the main list.
/// </summary>
/// <param name="taskListViewModel">The <see cref="TaskListViewModel"/> to remove from the main list.</param>
public class RemoveTaskListViewModelMessage(TaskListViewModel taskListViewModel) : ValueChangedMessage<TaskListViewModel>(taskListViewModel);