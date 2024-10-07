using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

public class ServerCommunication
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly string _baseUri = "http://localhost:5283/";

    // Check if connected to the network
    public static async Task<bool> IsConnected()
    {
        try
        {
            var response = await _httpClient.GetAsync(_baseUri);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Get the task list from the server
    public static async Task<List<ListTask>> GetList(string code)
    {
        Debug.WriteLine("Tried getting list");
        var response = await _httpClient.GetAsync($"{_baseUri}getList?code={code}");
        if (response.IsSuccessStatusCode)
        {
            var content = Regex.Unescape(await response.Content.ReadAsStringAsync());
            // Remove the surrounding quotes if necessary
            if (content.StartsWith("\"") && content.EndsWith("\""))
            {
                content = content.Remove(content.Length - 1);
                content = content.Remove(0, 1);
            }
            return JsonSerializer.Deserialize<List<ListTask>>(content) ?? new List<ListTask>();
        }

        throw new Exception(await response.Content.ReadAsStringAsync());
    }

    // Add a task to the list
    public static async Task<ListTask> AddTask(string code, string taskName)
    {
        var newTask = new ListTask
        {
            CreationDate = DateTime.Now,
            Name = taskName,
            IsDone = false
        };

        var content = new StringContent(JsonSerializer.Serialize(newTask), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUri}addTask?code={code}&taskName={taskName}", content);

        if (response.IsSuccessStatusCode)
        {
            return newTask;
        }

        throw new Exception(await response.Content.ReadAsStringAsync());
    }

    // Rename a task in the list
    public static async Task<ListTask> RenameTask(string code, DateTime creationDate, string newName)
    {
        var response = await _httpClient.PutAsync($"{_baseUri}renameTask?code={code}&creationDate={creationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK")}&newName={newName}", null);

        if (response.IsSuccessStatusCode)
        {
            var updatedTask = JsonSerializer.Deserialize<ListTask>(await response.Content.ReadAsStringAsync());
            return updatedTask;
        }

        throw new Exception(await response.Content.ReadAsStringAsync());
    }

    // Delete a task from the list
    public static async Task DeleteTask(string code, DateTime creationDate)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUri}deleteTask?code={code}&creationDate={creationDate}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }

    // Toggle a task from the list
    public static async Task ToggleTask(string code, DateTime creationDate)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUri}toggleTask?code={code}&creationDate={creationDate}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }

    // Delete a list
    public static async Task DeleteList(string code)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUri}deleteList?code={code}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }

    // Rename a list
    public static async Task RenameList(string code, string newName)
    {
        var response = await _httpClient.PutAsync($"{_baseUri}renameList?code={code}&newName={newName}", null);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }

    // Compare the list hashes
    public static async Task<bool> CompareListHash(string code, List<ListTask> localTasks)
    {
        var hashResponse = await _httpClient.GetAsync($"{_baseUri}getListHash?code={code}");

        if (hashResponse.IsSuccessStatusCode)
        {
            var hashResult = await hashResponse.Content.ReadAsStringAsync();

            var localHash = GetListHash(localTasks);
            return localHash == hashResult;
        }

        throw new Exception(await hashResponse.Content.ReadAsStringAsync());
    }

    // Compute the hash for the local list
    private static string GetListHash(List<ListTask> list)
    {
        var json = JsonSerializer.Serialize(list);
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(bytes);
        }
    }

    // Save a list on the server
    public static async Task SaveList(string code, string listContent)
    {
        var response = await _httpClient.PostAsync($"{_baseUri}saveList?code={code}", new StringContent(listContent));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }
}