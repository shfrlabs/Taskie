using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ServerCommunication
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUri = "http://localhost:5283/";

    public ServerCommunication()
    {
        _httpClient = new HttpClient();
    }

    // Check if connected to the network
    public async Task<bool> IsConnected()
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
    public async Task<List<ListTask>> GetList(string code)
    {
        var response = await _httpClient.GetAsync($"{_baseUri}getList?code={code}");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ListTask>>(content) ?? new List<ListTask>();
        }

        throw new Exception(await response.Content.ReadAsStringAsync());
    }

    // Add a task to the list
    public async Task<ListTask> AddTask(string code, string taskName)
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
    public async Task<ListTask> RenameTask(string code, DateTime creationDate, string newName)
    {
        var response = await _httpClient.PutAsync($"{_baseUri}renameTask?code={code}&creationDate={creationDate}&newName={newName}", null);

        if (response.IsSuccessStatusCode)
        {
            var updatedTask = JsonSerializer.Deserialize<ListTask>(await response.Content.ReadAsStringAsync());
            return updatedTask;
        }

        throw new Exception(await response.Content.ReadAsStringAsync());
    }

    // Delete a task from the list
    public async Task DeleteTask(string code, DateTime creationDate)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUri}deleteTask?code={code}&creationDate={creationDate}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }

    // Delete a list
    public async Task DeleteList(string code)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUri}deleteList?code={code}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }

    // Rename a list
    public async Task RenameList(string code, string newName)
    {
        var response = await _httpClient.PutAsync($"{_baseUri}renameList?code={code}&newName={newName}", null);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }

    // Compare the list hashes
    public async Task<bool> CompareListHash(string code, List<ListTask> localTasks)
    {
        var hashResponse = await _httpClient.GetAsync($"{_baseUri}getListHash?code={code}");

        if (hashResponse.IsSuccessStatusCode)
        {
            var hashResult = await hashResponse.Content.ReadAsStringAsync();
            var serverHash = JsonSerializer.Deserialize<dynamic>(hashResult).Hash;

            var localHash = GetListHash(localTasks);
            return localHash == serverHash;
        }

        throw new Exception(await hashResponse.Content.ReadAsStringAsync());
    }

    // Compute the hash for the local list
    private string GetListHash(List<ListTask> list)
    {
        var json = JsonSerializer.Serialize(list);
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(bytes);
        }
    }
}