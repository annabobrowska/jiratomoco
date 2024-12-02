using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    static async Task Main(string[] args)
    {
        string jiraUrl = "https://qualityminds.atlassian.net/";
        string username = "anna.bobrowska@qualityminds.pl";
        string targetAuthor = "Anna Bobrowska";
        string apiToken = ""; //own APItoken
        string issueKey = "TPL-41";
        string month = "2024-11";

        using (HttpClient client = new HttpClient())
        {
            client.BaseAddress = new Uri(jiraUrl);
            var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{apiToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            HttpResponseMessage response = await client.GetAsync($"/rest/api/3/issue/{issueKey}/worklog");
            response.EnsureSuccessStatusCode();
            var responseObj = await response.Content.ReadFromJsonAsync<JiraResponse>();
            
            //JObject worklogData = JObject.Parse(responseBody);
            //var responseObj = await client.GetFromJsonAsync<JiraResponse>($"/rest/api/3/issue/{issueKey}/worklog");
            //var responseObj = await response.GetFromJsonAsync<JiraResponse>($"/rest/api/3/issue/{issueKey}/worklog");


            var userWorklogs = responseObj?.Worklogs
                .Where(w => w.Author.DisplayName == targetAuthor)
                .Where(w => DateTime.Parse(w.Started).ToString("yyyy-MM") == month);

            foreach (var worklog in userWorklogs)
            {
                //string started = worklog["started"].ToString();
                //DateTime startedDate = DateTime.Parse(started);
                DateTime startedDate = DateTime.Parse(worklog.Started);
                //string author = worklog["author"]["displayName"].ToString();
                string author = worklog.Author.DisplayName;

                //if (startedDate.ToString("yyyy-MM") == month && author == targetAuthor)
    //{
                string timeSpent = worklog.TimeSpent;
                double hours = ConvertToHours(timeSpent);
                string comment = "";
                if(worklog.Comment != null)
                {
                    
                    var texts = JToken.Parse(worklog.Comment?.ToString()).SelectTokens("$..text");
                    comment = string.Join(", ", texts.Select(t => t?.ToString()));
                }

                Console.WriteLine($"Date: {startedDate.ToString("yyyy-MM-dd")}");
                Console.WriteLine($"Author: {worklog.Author.DisplayName}");
                Console.WriteLine($"Time Spent: {hours} hours");
                Console.WriteLine($"Comment: {comment}");
                Console.WriteLine();
                //}
            }
        }
    }

    public record JiraResponse(JiraWorklog[] Worklogs);

    public record JiraWorklog(
        string Started, 
        string TimeSpent,
        object Comment,
        WorklogAuthor Author);
    public record WorklogAuthor(string DisplayName);

    static double ConvertToHours(string timeSpent)
    {
        double totalHours = 0;
        string[] parts = timeSpent.Split(' ');

        foreach (var part in parts)
        {
            if (part.EndsWith("d"))
            {
                totalHours += double.Parse(part.TrimEnd('d')) * 8;
            }
            else if (part.EndsWith("h"))
            {
                totalHours += double.Parse(part.TrimEnd('h'));
            }
            else if (part.EndsWith("m"))
            {
                totalHours += double.Parse(part.TrimEnd('m')) / 60;
            }
        }

        return totalHours;
    }
}