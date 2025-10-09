using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TaskieLib.Models;
using Windows.ApplicationModel.AppService;
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
        private List<string> _fairmarkattachments;
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
            if (_fairmarkattachments == null) _fairmarkattachments = new List<string>();
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

        [JsonPropertyName("FairmarkAttachments")]
        public List<string> FMAttachmentIDs {
            get => _fairmarkattachments ?? (_fairmarkattachments = new List<string>());
            set {
                _fairmarkattachments = value ?? new List<string>();
                Debug.WriteLine($"FMAttachmentIDs set to: {string.Join(", ", _fairmarkattachments)}");
                OnPropertyChanged(nameof(FMAttachmentIDs));
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
                FileType = file.FileType,
                RelativePath = newFile.Path.Substring(root.Path.Length + 1)
            };
            Attachments.Add(meta);
            return meta;
        }

        public AttachmentMetadata AddFairmarkAttachment(FairmarkNoteData note, string listId) {
            if (!FMAttachmentIDs.Contains(note.id))
                FMAttachmentIDs.Add(note.id);
            if (!Attachments.Any(a => a.IsFairmark && a.Id == note.id)) {
                var meta = new AttachmentMetadata {
                    Id = note.id,
                    IsFairmark = true,
                    FileName = note.name,
                    FileType = ".fairmark",
                    Emoji = note.emoji,
                    Colors = note.colors
                };
                Attachments.Add(meta);
                return meta;
            }
            return Attachments.First(a => a.IsFairmark && a.Id == note.id);
        }

        public async Task RemoveAttachmentAsync(AttachmentMetadata attachment)
        {
            var root = ApplicationData.Current.LocalFolder;
            if (!attachment.IsFairmark) {
                try {
                    var file = await StorageFile.GetFileFromPathAsync(Path.Combine(root.Path, attachment.RelativePath));
                    await file.DeleteAsync();
                }
                catch { }
            }
            Attachments.Remove(attachment);
            if (attachment.IsFairmark)
                FMAttachmentIDs?.Remove(attachment.Id);
        }

        public void LoadAttachments(string listId)
        {
            DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Low, async () =>
            {
                var root = ApplicationData.Current.LocalFolder;
                // Clear all attachments before repopulating
                Attachments.Clear();
                // Load Fairmark attachments from IDs
                try {
                    var connection = new AppServiceConnection {
                        AppServiceName = "com.sheferslabs.fairmarkservices",
                        PackageFamilyName = "BRStudios.3763783C2F5C2_ynj0a7qyfqv8c"
                    };
                    var status = await connection.OpenAsync();
                    if (status != AppServiceConnectionStatus.Success)
                        return;
                    var response = await connection.SendMessageAsync(new Windows.Foundation.Collections.ValueSet());
                    if (response.Status != AppServiceResponseStatus.Success)
                        return;
                    string resultString = response.Message["Result"] as string;
                    var fairmarkNotes = System.Text.Json.JsonSerializer.Deserialize<List<FairmarkNoteData>>(resultString);
                    var fairmarkNoteIds = new HashSet<string>(fairmarkNotes.Select(n => n.id));

                    foreach (var id in FMAttachmentIDs.Where(id => fairmarkNoteIds.Contains(id))) {
                        var note = fairmarkNotes.First(n => n.id == id);
                        Attachments.Add(new AttachmentMetadata {
                            Id = note.id,
                            IsFairmark = true,
                            FileName = note.name,
                            FileType = ".fairmark",
                            Emoji = note.emoji,
                            Colors = note.colors
                        });
                    }
                }
                catch {
                }

                // Load file attachments from folder
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
                        if (!Attachments.Any(a => !a.IsFairmark && a.FileName == (parts.Length > 1 ? parts[1] : f.Name)))
                        {
                            Attachments.Add(new AttachmentMetadata
                            {
                                Id = parts[0],
                                FileName = parts.Length > 1 ? parts[1] : f.Name,
                                FileType = f.FileType,
                                RelativePath = f.Path.Substring(root.Path.Length + 1)
                            });
                        }
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
