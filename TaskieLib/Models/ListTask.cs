using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.System;
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
        private ObservableCollection<AttachmentMetadata> _attachments;
        private string _listId;

        public ListTask(string listId = null)
        {
            if (_listId == null)
            {
                _listId = listId;
            }
            _creationDate = DateTime.UtcNow;
            _subTasks = new ObservableCollection<ListTask>();
            _attachments = new ObservableCollection<AttachmentMetadata>();
        }

        private const string ToastTagFormat = "{0}_{1}";


        public void AddReminder(DateTimeOffset reminderDateTime)
        {
            if (reminderDateTime <= DateTimeOffset.UtcNow)
                throw new ArgumentException("Reminder time must be in the future", nameof(reminderDateTime));

            RemoveReminder();
            ScheduleToastNotification(reminderDateTime);
            SetReminderText();

            System.Diagnostics.Debug.WriteLine($"Reminder added for task '{Name}' at {reminderDateTime} with listId '{_listId}'");
        }


        public void RemoveReminder()
        {
            try
            {
                var notifier = ToastNotificationManager.CreateToastNotifier();
                string tag = GetToastTag();

                var scheduled = notifier.GetScheduledToastNotifications()
                    .Where(t => t.Tag == tag)
                    .ToList();
                foreach (var toast in scheduled)
                {
                    notifier.RemoveFromSchedule(toast);
                }

                ToastNotificationManager.History.Remove(tag, _listId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing reminder: {ex.Message}");
            }
            SetReminderText();
        }

        private DateTimeOffset? GetReminderDateTime()
        {
            try
            {
                var notifier = ToastNotificationManager.CreateToastNotifier();
                string tag = GetToastTag();
                var toast = notifier.GetScheduledToastNotifications()
                    .FirstOrDefault(t => t.Tag == tag);
                return toast?.DeliveryTime;
            }
            catch
            {
                return null;
            }
        }

        public bool HasReminder()
        {
            try
            {
                var notifier = ToastNotificationManager.CreateToastNotifier();
                string tag = GetToastTag();
                var toast = notifier.GetScheduledToastNotifications()
                    .FirstOrDefault(t => t.Tag == tag);
                return toast != null && toast.DeliveryTime > DateTimeOffset.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        private void ScheduleToastNotification(DateTimeOffset reminderDateTime)
        {
            try
            {
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                var textNodes = toastXml.GetElementsByTagName("text");

                textNodes[0].AppendChild(toastXml.CreateTextNode(
                    ResourceLoader.GetForCurrentView().GetString("ReminderGreeting")));
                textNodes[1].AppendChild(toastXml.CreateTextNode(Name));

                var toastElement = (XmlElement)toastXml.SelectSingleNode("//toast");
                toastElement?.SetAttribute("launch",
                    $"action=viewTask&creationDate={CreationDate:o}&listId={_listId}");

                var scheduledToast = new ScheduledToastNotification(toastXml, reminderDateTime)
                {
                    Tag = GetToastTag(),
                    Group = _listId
                };

                ToastNotificationManager.CreateToastNotifier().AddToSchedule(scheduledToast);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scheduling notification: {ex.Message}");
            }
        }

        private string GetToastTag()
            => string.Format(ToastTagFormat, _creationDate.Ticks, _listId);

        public string ListId
        {
            get => _listId;
            set
            {
                if (_listId == value) return;
                _listId = value;
                OnPropertyChanged(nameof(ListId));
            }
        }


        [JsonIgnore]
        public ObservableCollection<AttachmentMetadata> Attachments
        {
            get => _attachments;
            private set
            {
                if (_attachments != value)
                {
                    _attachments = value;
                    OnPropertyChanged();
                }
            }
        }

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

        private string _ReminderText = string.Empty;

        [JsonIgnore]
        public string ReminderText
        {
            get => _ReminderText;
            set
            {
                if (_ReminderText == value)
                    return;
                _ReminderText = value;
                OnPropertyChanged();
            }
        }
        // getting closer
        private RemindersTextConverter _reminderstextconverter = new RemindersTextConverter();
        public void SetReminderText()
        {
            ReminderText = _reminderstextconverter.Convert(GetReminderDateTime()) as string;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private const string AttachmentsRoot = "TaskAttachments";

        public async Task<AttachmentMetadata> AddAttachmentAsync(StorageFile file, string listId)
        {
            var root = ApplicationData.Current.LocalFolder;
            var taskFolder = await EnsureTaskFolderAsync(root, this.CreationDate, listId);
            var newFile = await file.CopyAsync(taskFolder,
                $"{Guid.NewGuid()}_{file.Name}", NameCollisionOption.GenerateUniqueName);
            var props = await newFile.GetBasicPropertiesAsync();
            var meta = new AttachmentMetadata
            {
                Id = Path.GetFileNameWithoutExtension(newFile.Name).Split('_')[0],
                FileName = file.Name,
                MimeType = file.ContentType,
                RelativePath = newFile.Path.Substring(root.Path.Length + 1)
            };
            Attachments.Add(meta);
            return meta;
        }

        public async Task RemoveAttachmentAsync(AttachmentMetadata attachment)
        {
            var root = ApplicationData.Current.LocalFolder;
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(Path.Combine(root.Path, attachment.RelativePath));
                await file.DeleteAsync();
            }
            catch { }
            Attachments.Remove(attachment);
        }

        public void LoadAttachments(string listId)
        {
            DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Low, async () =>
            {
                var root = ApplicationData.Current.LocalFolder;
                Attachments.Clear();
                try
                {
                    var tasksRoot = await root.GetFolderAsync(AttachmentsRoot);
                    var listFolder = await tasksRoot.GetFolderAsync(listId);
                    var taskFolder = await listFolder.GetFolderAsync(this.CreationDate.Ticks.ToString());
                    var files = await taskFolder.GetFilesAsync();
                    foreach (var f in files)
                    {
                        var props = await f.GetBasicPropertiesAsync();
                        var parts = f.Name.Split('_', 2);
                        Attachments.Add(new AttachmentMetadata
                        {
                            Id = parts[0],
                            FileName = parts.Length > 1 ? parts[1] : f.Name,
                            MimeType = f.ContentType,
                            RelativePath = f.Path.Substring(root.Path.Length + 1)
                        });
                    }
                }
                catch { }
            });
        }

        private static async Task<StorageFolder> EnsureTaskFolderAsync(StorageFolder root, DateTime creation, string listId)
        {
            var tasksRoot = await root.CreateFolderAsync(AttachmentsRoot, CreationCollisionOption.OpenIfExists);
            var listFolder = await tasksRoot.CreateFolderAsync(listId, CreationCollisionOption.OpenIfExists);
            return await listFolder.CreateFolderAsync(creation.Ticks.ToString(), CreationCollisionOption.OpenIfExists);
        }
    }
}
