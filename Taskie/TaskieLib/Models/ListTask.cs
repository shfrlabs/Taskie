using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace TaskieLib
{
    public class ListTask : INotifyPropertyChanged
    {
        private DateTime _creationDate;
        private DateTime? _parentCreationDate;
        private string _name;
        private bool _isDone;
        private ObservableCollection<ListTask> _subTasks;

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ObservableCollection<ListTask>))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ListTask))]
        public ListTask()
        {
            _creationDate = DateTime.UtcNow; // Use UTC for consistency
            _subTasks = new ObservableCollection<ListTask>();
        }

        #region Reminders
        private const string ToastTagFormat = "{0}_{1}"; // Format: CreationTicks_ListId

        /// <summary>
        /// Adds a toast reminder for this task at the specified future time.
        /// </summary>
        public void AddReminder(DateTimeOffset reminderDateTime, string listId)
        {
            if (reminderDateTime <= DateTimeOffset.UtcNow)
                throw new ArgumentException("Reminder time must be in the future", nameof(reminderDateTime));

            RemoveReminder(listId);
            ScheduleToastNotification(reminderDateTime, listId);
        }

        /// <summary>
        /// Removes any scheduled or delivered reminder for this task.
        /// </summary>
        public void RemoveReminder(string listId)
        {
            try
            {
                var notifier = ToastNotificationManager.CreateToastNotifier();
                var tag = GetToastTag(listId);

                // Remove scheduled notifications
                var scheduled = notifier.GetScheduledToastNotifications()
                    .Where(t => t.Tag == tag)
                    .ToList();
                foreach (var toast in scheduled)
                {
                    notifier.RemoveFromSchedule(toast);
                }

                // Remove from action center if already delivered
                ToastNotificationManager.History.Remove(tag, listId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing reminder: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a reminder is currently scheduled for this task.
        /// </summary>
        public bool HasReminder(string listId)
        {
            try
            {
                var notifier = ToastNotificationManager.CreateToastNotifier();
                var tag = GetToastTag(listId);
                var toast = notifier.GetScheduledToastNotifications()
                    .FirstOrDefault(t => t.Tag == tag);
                return toast != null && toast.DeliveryTime > DateTimeOffset.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        private void ScheduleToastNotification(DateTimeOffset reminderDateTime, string listId)
        {
            try
            {
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                var textNodes = toastXml.GetElementsByTagName("text");

                textNodes[0].AppendChild(toastXml.CreateTextNode(
                    ResourceLoader.GetForCurrentView().GetString("ReminderGreeting")));
                textNodes[1].AppendChild(toastXml.CreateTextNode(Name));

                // Set launch args so app can navigate to the specific task
                var toastElement = (XmlElement)toastXml.SelectSingleNode("//toast");
                toastElement?.SetAttribute("launch",
                    $"action=viewTask&creationDate={CreationDate:o}&listId={listId}");

                var scheduledToast = new ScheduledToastNotification(toastXml, reminderDateTime)
                {
                    Tag = GetToastTag(listId),
                    Group = listId
                };

                ToastNotificationManager.CreateToastNotifier().AddToSchedule(scheduledToast);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scheduling notification: {ex.Message}");
            }
        }

        private string GetToastTag(string listId)
            => string.Format(ToastTagFormat, _creationDate.Ticks, listId);
        #endregion

        [JsonPropertyName("SubTasks")]
        public ObservableCollection<ListTask> SubTasks
        {
            get => _subTasks;
            set
            {
                if (_subTasks == value) return;
                _subTasks.Clear();
                if (value != null)
                {
                    foreach (var item in value)
                        _subTasks.Add(item);
                }
                OnPropertyChanged(nameof(SubTasks));
            }
        }

        [JsonPropertyName("CreationDate")]
        public DateTime CreationDate
        {
            get => _creationDate;
            set
            {
                if (_creationDate == value) return;
                _creationDate = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("ParentCreationDate")]
        public DateTime? ParentCreationDate
        {
            get => _parentCreationDate;
            set
            {
                if (_parentCreationDate == value) return;
                _parentCreationDate = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("Name")]
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        [JsonPropertyName("IsDone")]
        public bool IsDone
        {
            get => _isDone;
            set
            {
                if (_isDone == value) return;
                _isDone = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    
        private static void ValidateMainThread()
        {
            if (!CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                throw new InvalidOperationException("Must be called from UI thread");
            }
        }
    }
}
