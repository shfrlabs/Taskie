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

    public static async Task<List<ListTask>> GetList(string code)
    {
        Debug.WriteLine("Tried getting list");
        var response = await _httpClient.GetAsync($"{_baseUri}getList?code={code}");
        if (response.IsSuccessStatusCode)
        {
            var content = Regex.Unescape(await response.Content.ReadAsStringAsync());
            if (content.StartsWith("\"") && content.EndsWith("\""))
            {
                content = content.Remove(content.Length - 1);
                content = content.Remove(0, 1);
            }
            return JsonSerializer.Deserialize<List<ListTask>>(content) ?? new List<ListTask>();
        }

        throw new Exception(await response.Content.ReadAsStringAsync());
    }

    public static async Task<ListTask> AddTask(string code, string taskName)
    {
        var newTask = new ListTask
        {
            CreationDate = DateTime.Now,
            Name = taskName,
            IsDone = false
        };
        var response = await _httpClient.PostAsync($"{_baseUri}addTask?code={code}&taskName={taskName}", null);

        if (response.IsSuccessStatusCode)
        {
            return newTask;
        }
        return null;
    }

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

    public static async Task DeleteTask(string code, DateTime creationDate)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUri}deleteTask?code={code}&creationDate={creationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK")}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
    }

    public static async Task ToggleTask(string code, DateTime creationDate)
    {
        var response = await _httpClient.GetAsync($"{_baseUri}toggleTask?code={code}&creationDate={creationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK")}");
        Debug.WriteLine(await response.Content.ReadAsStringAsync());
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
        else
        {
            Debug.WriteLine("Toggled");
        }
    }

    public static async Task DeleteList(string code)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUri}deleteList?code={code}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
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