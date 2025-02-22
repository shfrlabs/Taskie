using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

public class ListTask : INotifyPropertyChanged {
    private DateTime _creationDate;
    private DateTime? _parentCreationDate;
    private string _name;
    private bool _isDone;
    private ObservableCollection<ListTask> _subTasks;

    public ListTask() {
        _subTasks = new ObservableCollection<ListTask>();
    }

    #region Reminders
    // Method to add a reminder (toast notification)
    public void AddReminder(DateTimeOffset reminderDateTime, string listId) {
        // Schedule the toast notification
        ScheduleToastNotification(reminderDateTime, listId);
    }

    // Method to remove a reminder (toast notification)
    public void RemoveReminder() {
        // Remove the scheduled toast notification
        RemoveToastNotification();
    }

    // Method to check if a reminder exists
    public bool HasReminder() {
        // Check if a scheduled toast notification exists and if the reminder time is in the future
        return CheckIfReminderExists() && IsReminderInTheFuture();
    }

    private ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
    // Helper method to schedule a toast notification
    private void ScheduleToastNotification(DateTimeOffset reminderDateTime, string listId) {
        var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
        var stringElements = toastXml.GetElementsByTagName("text");
        stringElements[0].AppendChild(toastXml.CreateTextNode(resourceLoader.GetString("ReminderGreeting")));
        stringElements[1].AppendChild(toastXml.CreateTextNode($"{Name}"));

        // Add arguments to the toast notification
        var toastElement = (XmlElement)toastXml.SelectSingleNode("/toast");
        toastElement.SetAttribute("launch", $"listId={listId}");

        var toast = new ScheduledToastNotification(toastXml, reminderDateTime) {
            Id = GetToastId()
        };

        ToastNotificationManager.CreateToastNotifier().AddToSchedule(toast);
    }

    // Helper method to remove a toast notification
    private void RemoveToastNotification() {
        var notifier = ToastNotificationManager.CreateToastNotifier();
        var scheduledToasts = notifier.GetScheduledToastNotifications();

        // Iterate through all scheduled notifications and remove those with matching IDs
        foreach (var toast in scheduledToasts) {
            if (toast.Id.StartsWith($"Task_{CreationDate.Ticks % 100000000}")) {
                notifier.RemoveFromSchedule(toast);
            }
        }
    }

    // Helper method to check if the toast notification exists
    private bool CheckIfReminderExists() {
        var notifier = ToastNotificationManager.CreateToastNotifier();
        var scheduledToasts = notifier.GetScheduledToastNotifications();

        // Check if any scheduled toast has the matching ID for this task
        return scheduledToasts.Any(toast => toast.Id.StartsWith($"Task_{CreationDate.Ticks % 100000000}"));
    }

    private bool IsReminderInTheFuture() {
        var notifier = ToastNotificationManager.CreateToastNotifier();
        var scheduledToasts = notifier.GetScheduledToastNotifications();

        // Find the toast notification with the matching ID and check if the reminder time is in the future
        var taskToast = scheduledToasts.FirstOrDefault(toast => toast.Id.StartsWith($"Task_{CreationDate.Ticks % 100000000}"));
        if (taskToast != null) {
            var reminderTime = taskToast.DeliveryTime.DateTime;
            return reminderTime > DateTime.Now;
        }

        // If no matching toast found, return false
        return false;
    }

    // Helper method to generate a unique toast ID based on the creation date
    private string GetToastId() {
        return $"Task_{CreationDate.Ticks % 100000000}"; // Ensures ID stays within 8 digits
    }
    #endregion

    public ObservableCollection<ListTask> SubTasks {
        get { return _subTasks; }
        set {
            if (_subTasks != value) {
                _subTasks = value;
                OnPropertyChanged(nameof(SubTasks));
            }
        }
    }

    public DateTime CreationDate {
        get { return _creationDate; }
        set {
            if (_creationDate != value) {
                _creationDate = value;
                OnPropertyChanged(nameof(CreationDate));
            }
        }
    }

    public DateTime? ParentCreationDate {
        get { return _parentCreationDate; }
        set {
            if (_parentCreationDate != value) {
                _parentCreationDate = value;
                OnPropertyChanged(nameof(ParentCreationDate));
            }
            ;
        }
    }

    public string Name {
        get { return _name; }
        set {
            if (_name != value) {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public bool IsDone {
        get { return _isDone; }
        set {
            if (_isDone != value) {
                _isDone = value;
                OnPropertyChanged(nameof(IsDone));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
