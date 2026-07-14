using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskieLib;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace TaskieAppService
{
    public sealed class AppServiceController : IBackgroundTask
    {
        private static List<AppServiceConnection> _subscribers = new List<AppServiceConnection>();
        private static object _lock = new object();
        private static FileSystemWatcher _watcher;
        private static ConcurrentDictionary<string, DateTime> _recentlyNotified = new ConcurrentDictionary<string, DateTime>();

        private BackgroundTaskDeferral _deferral;
        private AppServiceConnection _connection;

        public static void Log(string message)
        {
            try
            {
                var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                System.IO.File.AppendAllText(System.IO.Path.Combine(folder.Path, "appservice_log.txt"), $"{DateTime.Now}: {message}\n");
            }
            catch { }
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Log("Run called");
            _deferral = taskInstance.GetDeferral();

            if (!Settings.appService)
            {
                Log("Settings.appService is false. Exiting.");
                _deferral.Complete();
                _deferral = null;
                return;
            }

            lock (_lock)
            {
                if (_watcher == null)
                {
                    _watcher = new FileSystemWatcher(ApplicationData.Current.LocalFolder.Path, "*.json")
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
                    };
                    _watcher.Changed += OnListFileChanged;
                    _watcher.Created += OnListFileChanged;
                    _watcher.Deleted += OnListFileChanged;
                    _watcher.EnableRaisingEvents = true;
                }
            }

            Settings.AppServiceStatusChanged += OnAppServiceStatusChanged;

            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (details == null)
            {
                Log("details == null. Exiting.");
                _deferral.Complete();
                _deferral = null;
                return;
            }

            _connection = details.AppServiceConnection;
            _connection.RequestReceived += OnRequestReceived;
            _connection.ServiceClosed += OnServiceClosed;
            Log("Run successfully hooked events.");
        }

        private void OnAppServiceStatusChanged(object sender, bool e)
        {
            if (!e)
            {
                _connection?.Dispose();
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            var requestDeferral = args.GetDeferral();

            try
            {
                string command = message["command"] as string;
                Log($"Received command: {command}");
                var response = new ValueSet();

                switch (command)
                {
                    case "get-lists":
                        var lists = ListTools.GetLists().Select(l => new AppServiceListSummaryDto { name = l.name, id = l.id.Replace(".json", string.Empty), emoji = l.emoji }).ToList();
                        response["Result"] = JsonSerializer.Serialize(lists);
                        Log("get-lists successful");
                        break;

                    case "get-tasks":
                        if (message.TryGetValue("listId", out object listIdObj) && listIdObj is string listId)
                        {
                            var listData = ListTools.ReadList(listId);
                            var tasks = listData.Tasks.Select(t => new AppServiceTaskItemDto
                            {
                                Name = t.Name,
                                IsDone = t.IsDone,
                                CreationDate = t.CreationDate.Ticks,
                                SubTasks = t.SubTasks.Select(st => new AppServiceSubTaskItemDto
                                {
                                    Name = st.Name,
                                    IsDone = st.IsDone,
                                    CreationDate = st.CreationDate.Ticks,
                                    ParentCreationDate = st.ParentCreationDate?.Ticks
                                }).ToList()
                            }).ToList();
                            response["Result"] = JsonSerializer.Serialize(tasks);
                            Log($"get-tasks successful for listId: {listId}");
                        }
                        else
                        {
                            response["Error"] = "Missing or invalid 'listId'.";
                            Log("get-tasks failed: Missing or invalid listId");
                        }
                        break;

                    case "set-task-status":
                        if (message.TryGetValue("listId", out object listIdForUpdateObj) && listIdForUpdateObj is string listIdForUpdate &&
                            message.TryGetValue("taskId", out object taskIdObj) && taskIdObj is long taskIdTicks &&
                            message.TryGetValue("isDone", out object isDoneObj) && isDoneObj is bool isDone)
                        {
                            var taskId = new DateTime(taskIdTicks);
                            var listData = ListTools.ReadList(listIdForUpdate);
                            
                            var taskToUpdate = listData.Tasks.SelectMany(t => t.SubTasks.Prepend(t)).FirstOrDefault(t => t.CreationDate == taskId);

                            if (taskToUpdate != null)
                            {
                                taskToUpdate.IsDone = isDone;
                                ListTools.SaveList(listIdForUpdate, listData.Tasks, listData.Metadata);
                                response["Result"] = "Success";
                                await NotifySubscribers(listIdForUpdate);
                                Log($"set-task-status successful for listId: {listIdForUpdate}, taskId: {taskId}");
                            }
                            else
                            {
                                response["Error"] = "Task not found.";
                                Log("set-task-status failed: Task not found");
                            }
                        }
                        else
                        {
                            response["Error"] = "Missing or invalid parameters for 'set-task-status'.";
                            Log("set-task-status failed: Missing or invalid parameters");
                        }
                        break;

                    case "subscribe-to-updates":
                        lock (_lock)
                        {
                            if (!_subscribers.Contains(sender))
                            {
                                _subscribers.Add(sender);
                            }
                        }
                        response["Result"] = "Subscribed";
                        Log("subscribe-to-updates successful");
                        break;

                    default:
                        response["Error"] = "Unknown command.";
                        Log($"Unknown command: {command}");
                        break;
                }

                await args.Request.SendResponseAsync(response);
                Log($"Response sent for command: {command}");
            }
            catch (Exception ex)
            {
                Log($"OnRequestReceived exception: {ex}");
                Debug.WriteLine($"OnRequestReceived failed: {ex}");
                var errorResponse = new ValueSet { { "Error", ex.Message } };
                try
                {
                    await args.Request.SendResponseAsync(errorResponse);
                }
                catch (Exception e)
                {
                    Log($"Failed to send error response: {e.Message}");
                    Debug.WriteLine($"Failed to send error response: {e.Message}");
                }
            }
            finally
            {
                requestDeferral.Complete();
            }
        }

        private async void OnListFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                string listId = Path.GetFileNameWithoutExtension(e.Name);
                if (string.IsNullOrEmpty(listId)) return;

                var now = DateTime.UtcNow;

                if (_recentlyNotified.TryGetValue(listId, out var lastTime))
                {
                    if ((now - lastTime).TotalMilliseconds < 500)
                        return;
                }
                _recentlyNotified[listId] = now;

                await Task.Delay(100); // allow file writes to settle
                await NotifySubscribers(listId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnListFileChanged failed: {ex}");
            }
        }

        private async Task NotifySubscribers(string updatedListId)
        {
            var message = new ValueSet { { "update", updatedListId } };
            List<AppServiceConnection> deadConnections = new List<AppServiceConnection>();
            List<AppServiceConnection> currentSubscribers;

            lock (_lock)
            {
                currentSubscribers = new List<AppServiceConnection>(_subscribers);
            }

            foreach (var connection in currentSubscribers)
            {
                try
                {
                    var response = await connection.SendMessageAsync(message);
                    if (response.Status != AppServiceResponseStatus.Success)
                    {
                        deadConnections.Add(connection);
                    }
                }
                catch
                {
                    deadConnections.Add(connection);
                }
            }

            if (deadConnections.Any())
            {
                lock (_lock)
                {
                    foreach (var dead in deadConnections)
                    {
                        _subscribers.Remove(dead);
                    }
                }
            }
        }

        private void OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            Settings.AppServiceStatusChanged -= OnAppServiceStatusChanged;
            lock (_lock)
            {
                _subscribers.Remove(sender);
            }

            if (_connection != null)
            {
                _connection.RequestReceived -= OnRequestReceived;
                _connection.ServiceClosed -= OnServiceClosed;
                _connection = null;
            }

            _deferral?.Complete();
            _deferral = null;
        }
    }
}
