using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskieLib;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace TaskieAppService
{
    public sealed class AppServiceController : IBackgroundTask
    {
        private static List<AppServiceConnection> _subscribers = new List<AppServiceConnection>();
        private static object _lock = new object();

        private BackgroundTaskDeferral _deferral;
        private AppServiceConnection _connection;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (details == null)
            {
                _deferral.Complete();
                _deferral = null;
                return;
            }

            _connection = details.AppServiceConnection;
            _connection.RequestReceived += OnRequestReceived;
            _connection.ServiceClosed += OnServiceClosed;
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            var requestDeferral = args.GetDeferral();

            try
            {
                string command = message["command"] as string;
                var response = new ValueSet();

                switch (command)
                {
                    case "get-lists":
                        var lists = ListTools.GetLists().Select(l => new { l.name, id = l.id.Replace(".json", string.Empty), l.emoji }).ToList();
                        response["Result"] = JsonSerializer.Serialize(lists);
                        break;

                    case "get-tasks":
                        if (message.TryGetValue("listId", out object listIdObj) && listIdObj is string listId)
                        {
                            var listData = ListTools.ReadList(listId);
                            var tasks = listData.Tasks.Select(t => new
                            {
                                t.Name,
                                t.IsDone,
                                CreationDate = t.CreationDate.Ticks,
                                SubTasks = t.SubTasks.Select(st => new
                                {
                                    st.Name,
                                    st.IsDone,
                                    CreationDate = st.CreationDate.Ticks,
                                    ParentCreationDate = st.ParentCreationDate?.Ticks
                                }).ToList()
                            }).ToList();
                            response["Result"] = JsonSerializer.Serialize(tasks);
                        }
                        else
                        {
                            response["Error"] = "Missing or invalid 'listId'.";
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
                            }
                            else
                            {
                                response["Error"] = "Task not found.";
                            }
                        }
                        else
                        {
                            response["Error"] = "Missing or invalid parameters for 'set-task-status'.";
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
                        break;

                    default:
                        response["Error"] = "Unknown command.";
                        break;
                }

                await args.Request.SendResponseAsync(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnRequestReceived failed: {ex}");
                var errorResponse = new ValueSet { { "Error", ex.Message } };
                try
                {
                    await args.Request.SendResponseAsync(errorResponse);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to send error response: {e.Message}");
                }
            }
            finally
            {
                requestDeferral.Complete();
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
