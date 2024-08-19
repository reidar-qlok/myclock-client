using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace myclock_client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string predictionEndpoint = configuration["LSPredictionEndpoint"];
                string predictionKey = configuration["LSPredictionKey"];
                string projectName = configuration["LSProjectName"];
                string deploymentName = configuration["LSDeploymentName"];

                // Create a client for the Language service model
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", predictionKey);

                // Get user input (until they enter "quit")
                string userText = "";
                while (userText.ToLower() != "quit")
                {
                    Console.WriteLine("\nEnter some text ('quit' to stop)");
                    userText = Console.ReadLine();
                    if (userText.ToLower() != "quit")
                    {
                        // Prepare the request body
                        var requestBody = new
                        {
                            kind = "Conversation",
                            analysisInput = new
                            {
                                conversationItem = new
                                {
                                    id = "1",
                                    text = userText,
                                    modality = "text",
                                    language = "en",
                                    participantId = "1"
                                }
                            },
                            parameters = new
                            {
                                projectName = projectName,
                                verbose = true,
                                deploymentName = deploymentName,
                                stringIndexType = "TextElement_V8"
                            }
                        };

                        // Serialize JSON content
                        string jsonContent = JsonSerializer.Serialize(requestBody);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        // Call the Language service model to get intent and entities
                        HttpResponseMessage response = await client.PostAsync(predictionEndpoint, content);
                        string jsonResponse = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("API Response: " + jsonResponse);

                            // Deserialize the JSON response
                            var predictionResult = JsonSerializer.Deserialize<PredictionResponse>(jsonResponse);

                            if (predictionResult?.Result?.Prediction != null)
                            {
                                var topIntent = predictionResult.Result.Prediction.TopIntent;
                                var entities = predictionResult.Result.Prediction.Entities;

                                // Apply the appropriate action based on the top intent
                                switch (topIntent.ToLower())
                                {
                                    case "gettime":
                                        var locationEntity = entities?.FirstOrDefault(e => e.Category == "Location");
                                        if (locationEntity != null)
                                        {
                                            string location = locationEntity.Text;
                                            Console.WriteLine(GetTime(location));
                                        }
                                        else
                                        {
                                            Console.WriteLine("Please specify a location.");
                                        }
                                        break;

                                    case "getdate":
                                        var dayEntity = entities?.FirstOrDefault(e => e.Category == "Day");
                                        if (dayEntity != null)
                                        {
                                            string day = dayEntity.Text;
                                            Console.WriteLine(GetDate(day));
                                        }
                                        else
                                        {
                                            Console.WriteLine("Please specify a day.");
                                        }
                                        break;

                                    case "getday":
                                        var dateEntity = entities?.FirstOrDefault(e => e.Category == "Date");
                                        if (dateEntity != null)
                                        {
                                            string date = dateEntity.Text;
                                            Console.WriteLine(GetDay(date));
                                        }
                                        else
                                        {
                                            Console.WriteLine("Please specify a date.");
                                        }
                                        break;

                                    default:
                                        Console.WriteLine("Sorry, I didn't understand that.");
                                        break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Prediction is null. Check API response structure.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Error: {response.StatusCode}");
                            Console.WriteLine(jsonResponse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static string GetTime(string location)
        {
            var timeString = "";
            var time = DateTime.Now;

            switch (location.ToLower())
            {
                case "local":
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "london":
                    time = DateTime.UtcNow;
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "sydney":
                    time = DateTime.UtcNow.AddHours(11);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "new york":
                    time = DateTime.UtcNow.AddHours(-5);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "nairobi":
                    time = DateTime.UtcNow.AddHours(3);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "tokyo":
                    time = DateTime.UtcNow.AddHours(9);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                case "delhi":
                    time = DateTime.UtcNow.AddHours(5.5);
                    timeString = time.Hour.ToString() + ":" + time.Minute.ToString("D2");
                    break;
                default:
                    timeString = "I don't know what time it is in " + location;
                    break;
            }

            return timeString;
        }

        static string GetDate(string day)
        {
            string date_string = "I can only determine dates for today or named days of the week.";
            DayOfWeek weekDay;
            if (Enum.TryParse(day, true, out weekDay))
            {
                int weekDayNum = (int)weekDay;
                int todayNum = (int)DateTime.Today.DayOfWeek;
                int offset = weekDayNum - todayNum;
                date_string = DateTime.Today.AddDays(offset).ToShortDateString();
            }
            return date_string;
        }

        static string GetDay(string date)
        {
            string day_string = "Enter a date in MM/DD/YYYY format.";
            DateTime dateTime;
            if (DateTime.TryParse(date, out dateTime))
            {
                day_string = dateTime.DayOfWeek.ToString();
            }
            return day_string;
        }
    }

    // Prediction response classes
    public class PredictionResponse
    {
        [JsonPropertyName("result")]
        public Result Result { get; set; }
    }

    public class Result
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("prediction")]
        public Prediction Prediction { get; set; }
    }

    public class Prediction
    {
        [JsonPropertyName("topIntent")]
        public string TopIntent { get; set; }

        [JsonPropertyName("projectKind")]
        public string ProjectKind { get; set; }

        [JsonPropertyName("intents")]
        public List<Intent> Intents { get; set; }

        [JsonPropertyName("entities")]
        public List<Entity> Entities { get; set; }
    }

    public class Intent
    {
        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("confidenceScore")]
        public float ConfidenceScore { get; set; }
    }

    public class Entity
    {
        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("confidenceScore")]
        public float ConfidenceScore { get; set; }
    }
}
