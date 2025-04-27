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
            _creationDate = DateTime.UtcNow; // Use UTC for better serialization consistency
            _subTasks = new ObservableCollection<ListTask>();
        }

        #region Reminders
        public void AddReminder(DateTimeOffset reminderDateTime, string listId)
        {
            if (reminderDateTime < DateTimeOffset.UtcNow)
                throw new ArgumentException("Reminder time must be in the future");

            ValidateMainThread();
            RemoveReminder();
            ScheduleToastNotification(reminderDateTime, listId);
        }

        public void RemoveReminder()
        {
            ValidateMainThread();
            try
            {
                var notifier = ToastNotificationManager.CreateToastNotifier();
                var toastId = GetToastId();
                
                foreach (var toast in notifier.GetScheduledToastNotifications()
                    .Where(t => t.Id == toastId).ToList())
                {
                    notifier.RemoveFromSchedule(toast);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing reminder: {ex.Message}");
            }
        }

        public bool HasReminder()
        {
            ValidateMainThread();
            try
            {
                var toast = ToastNotificationManager.CreateToastNotifier()
                    .GetScheduledToastNotifications()
                    .FirstOrDefault(t => t.Id == GetToastId());

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
                    ResourceLoader.GetForCurrentView().GetString("ReminderGreeting") ?? "Task reminder"));
                
                textNodes[1].AppendChild(toastXml.CreateTextNode(Name));

                var toastElement = (XmlElement)toastXml.SelectSingleNode("/toast");
                toastElement?.SetAttribute("launch", $"listId={listId}");

                var toast = new ScheduledToastNotification(toastXml, reminderDateTime)
                {
                    Id = GetToastId()
                };

                ToastNotificationManager.CreateToastNotifier().AddToSchedule(toast);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scheduling notification: {ex.Message}");
                throw new InvalidOperationException("Failed to schedule notification", ex);
            }
        }

        private string GetToastId() => $"T_{_creationDate.Ticks % 1000000000000000000}"; // 19 digits max
        #endregion

        [JsonPropertyName("SubTasks")]
        public ObservableCollection<ListTask> SubTasks
        {
            get => _subTasks;
            set
            {
                if (_subTasks == value) return;
                
                // Maintain collection reference for binding preservation
                _subTasks.Clear();
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        _subTasks.Add(item);
                    }
                }
                OnPropertyChanged(nameof(SubTasks));
            }
        }

        // Other properties remain the same with improved thread safety
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