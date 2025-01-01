using System.Collections.ObjectModel;
using System.ComponentModel;
using System;
using Windows.UI.Notifications;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.ApplicationModel.Resources;

public class ListTask : INotifyPropertyChanged
{
    private DateTime _creationDate;
    private DateTime? _parentCreationDate;
    private string _name;
    private bool _isDone;
    private ObservableCollection<ListTask> _subTasks;
    public ListTask() { _subTasks = new ObservableCollection<ListTask>(); }
    public void AddReminder(DateTimeOffset reminderDateTime, string listId) { ScheduleToastNotification(reminderDateTime, listId); }
    public void RemoveReminder() { RemoveToastNotification(); }
    public bool HasReminder() { return CheckIfReminderExists() && IsReminderInTheFuture(); }

    private ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
    private void ScheduleToastNotification(DateTimeOffset reminderDateTime, string listId)
    {
        var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
        var stringElements = toastXml.GetElementsByTagName("text");
        stringElements[0].AppendChild(toastXml.CreateTextNode(resourceLoader.GetString("ReminderGreeting")));
        stringElements[1].AppendChild(toastXml.CreateTextNode($"{Name}"));
        var toastElement = (XmlElement)toastXml.SelectSingleNode("/toast");
        toastElement.SetAttribute("launch", $"listId={listId}");
        var toast = new ScheduledToastNotification(toastXml, reminderDateTime)
        { Id = GetToastId() };
        ToastNotificationManager.CreateToastNotifier().AddToSchedule(toast);
    }
    private void RemoveToastNotification()
    {
        var notifier = ToastNotificationManager.CreateToastNotifier();
        var scheduledToasts = notifier.GetScheduledToastNotifications();
        foreach (var toast in scheduledToasts)
        {
            if (toast.Id.StartsWith($"Task_{CreationDate.Ticks % 100000000}"))
            {
                notifier.RemoveFromSchedule(toast);
            }
        }
    }
    private bool CheckIfReminderExists()
    {
        var notifier = ToastNotificationManager.CreateToastNotifier();
        var scheduledToasts = notifier.GetScheduledToastNotifications();
        return scheduledToasts.Any(toast => toast.Id.StartsWith($"Task_{CreationDate.Ticks % 100000000}"));
    }

    private bool IsReminderInTheFuture()
    {
        var notifier = ToastNotificationManager.CreateToastNotifier();
        var scheduledToasts = notifier.GetScheduledToastNotifications();
        var taskToast = scheduledToasts.FirstOrDefault(toast => toast.Id.StartsWith($"Task_{CreationDate.Ticks % 100000000}"));
        if (taskToast != null)
        {
            var reminderTime = taskToast.DeliveryTime.DateTime;
            return reminderTime > DateTime.Now;
        }
        return false;
    }

    private string GetToastId()
    {
        return $"Task_{CreationDate.Ticks % 100000000}"; // Ensures ID stays within 8 digits
    }

    public ObservableCollection<ListTask> SubTasks
    {
        get { return _subTasks; }
        set
        {
            if (_subTasks != value)
            {
                _subTasks = value;
                OnPropertyChanged(nameof(SubTasks));
            }
        }
    }

    public DateTime CreationDate
    {
        get { return _creationDate; }
        set
        {
            if (_creationDate != value)
            {
                _creationDate = value;
                OnPropertyChanged(nameof(CreationDate));
            }
        }
    }

    public DateTime? ParentCreationDate
    {
        get { return _parentCreationDate; }
        set
        {
            if (_parentCreationDate != value)
            {
                _parentCreationDate = value;
                OnPropertyChanged(nameof(ParentCreationDate));
            };
        }
    }

    public string Name
    {
        get { return _name; }
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public bool IsDone
    {
        get { return _isDone; }
        set
        {
            if (_isDone != value)
            {
                _isDone = value;
                OnPropertyChanged(nameof(IsDone));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
