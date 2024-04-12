﻿namespace TBAStatReader;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Worker(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) : IHostedService
{
    private readonly ILogger _log = loggerFactory.CreateLogger<Worker>();
    private readonly HttpClient _client = httpClientFactory.CreateClient("Orchestrator");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Welcome to the TBA Chat bot! What would you like to know about FIRST competitions, past or present?");

        do
        {
            Console.Write("> ");
            var question = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(question))
            {
                break;
            }

            CircularCharArray progress = CircularCharArray.ProgressSpinner;

            var response = _client.PostAsync("api/messages", new StringContent(question), cancellationToken);
            while (!response.IsCompleted)
            {
                Console.Write(progress.Next());
                Console.CursorLeft--;
            }

            var responseResult = await response;
            responseResult.EnsureSuccessStatusCode();

            var responseString = await responseResult.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine(responseString);

        } while (!cancellationToken.IsCancellationRequested);
    }

    //    public async Task StartAsync(CancellationToken cancellationToken)
    //    {
    //        _log.LogDebug("API key: {apiKey}", _apiKey);

    //        Console.WriteLine("Welcome to the TBA Chat bot! What would you like to know about FIRST competitions, past or present?");
    //        IKernelBuilder builder = Kernel.CreateBuilder();
    //        builder.Services.AddSingleton(loggerFactory);
    //        builder.Plugins
    //            //.AddFromObject(new TeamApi(clientConfig))
    //            .AddFromObject(new TBAAgent(clientConfig, loggerFactory));

    //        if (config["AzureOpenAIKey"] is not null)
    //        {
    //            builder.AddAzureOpenAIChatCompletion(
    //                config["AzureOpenDeployment"]!,
    //                config["AzureOpenAIEndpoint"]!,
    //                config["AzureOpenAIKey"]!,
    //                httpClient: httpClientFactory.CreateClient("AzureOpenAi"));
    //        }
    //        else
    //        {
    //            builder.AddAzureOpenAIChatCompletion(
    //                config["AzureOpenDeployment"]!,
    //                config["AzureOpenAIEndpoint"]!,
    //                new DefaultAzureCredential(),
    //                httpClient: httpClientFactory.CreateClient("AzureOpenAi"));
    //        }

    //        Kernel kernel = builder.Build();

    //        var baseSystemPrompt = $@"You are an information assistant for users asking questions about the FIRST Robotics competition.
    //In these competitions, two ""alliances"" - red & blue - each consisting of 3 ""teams"" compete against one another to score points based on the rules defined for the season's game. A given ""event"" consists of many ""matches"" (individual scoring rounds between alliances).
    //Many events are held around the world at any given time, with a season consisting of 6 weeks of competition followed by the World Championships and some ""off-season"" events following.
    //You have been given access to the data catalog of FIRST via API calls, use these to your advantage to answer questions about past and current events, matches, and teams in the competition.
    //If you aren't able to figure out how to answer the question, tell the user in a polite way and ask them for another question.

    //Here some some samples of the results you can expect from the API:

    //### SAMPLE MATCH SEARCH RESULT
    //{{
    //  ""matches"": [
    //    {{
    //      ""comp_level"": 5,
    //      ""winning_alliance"": 0,
    //      ""key"": ""2024alhu_f1m1"",
    //      ""set_number"": 1,
    //      ""match_number"": 1,
    //      ""alliances"": {{
    //        ""red"": {{
    //          ""score"": 120,
    //          ""team_keys"": [
    //            ""frc5002"",
    //            ""frc4635"",
    //            ""frc2481""
    //          ],
    //          ""surrogate_team_keys"": [],
    //          ""dq_team_keys"": []
    //        }},
    //        ""blue"": {{
    //          ""score"": 60,
    //          ""team_keys"": [
    //            ""frc7111"",
    //            ""frc4265"",
    //            ""frc6517""
    //          ],
    //          ""surrogate_team_keys"": [],
    //          ""dq_team_keys"": []
    //        }}
    //      }},
    //      ""event_key"": ""2024alhu"",
    //      ""time"": 1712436840,
    //      ""actual_time"": 1712438011,
    //      ""predicted_time"": 1712438103,
    //      ""post_result_time"": 1712438219,
    //      ""videos"": [
    //        {{
    //          ""type"": ""youtube"",
    //          ""key"": ""K3Jv2DdIN1A""
    //        }}
    //      ]
    //    }}
    //  ]
    //}}

    //### SAMPLE EVENTS SEARCH RESULT OBJECT
    //{{
    //  ""events"": [
    //    {{
    //      ""address"": ""700 Monroe St SW, Huntsville, AL 35801, USA"",
    //      ""city"": ""Huntsville"",
    //      ""country"": ""USA"",
    //      ""district"": null,
    //      ""division_keys"": [],
    //      ""end_date"": ""2024-04-06"",
    //      ""event_code"": ""alhu"",
    //      ""event_type"": 0,
    //      ""event_type_string"": ""Regional"",
    //      ""first_event_code"": ""alhu"",
    //      ""first_event_id"": null,
    //      ""gmaps_place_id"": ""ChIJBwUw_FdrYogRMsP0V0W5Zqk"",
    //      ""gmaps_url"": ""https://maps.google.com/?q=700+Monroe+St+SW,+Huntsville,+AL+35801,+USA&ftid=0x88626b57fc300507:0xa966b94557f4c332"",
    //      ""key"": ""2024alhu"",
    //      ""lat"": 34.72671,
    //      ""lng"": -86.5903795,
    //      ""location_name"": ""700 Monroe St SW"",
    //      ""name"": ""Rocket City Regional"",
    //      ""parent_event_key"": null,
    //      ""playoff_type"": 10,
    //      ""playoff_type_string"": ""Double Elimination Bracket (8 Alliances)"",
    //      ""postal_code"": ""35801"",
    //      ""short_name"": ""Rocket City"",
    //      ""start_date"": ""2024-04-03"",
    //      ""state_prov"": ""AL"",
    //      ""timezone"": ""America/Chicago"",
    //      ""webcasts"": [
    //        {{
    //          ""channel"": ""firstinspires5"",
    //          ""type"": ""twitch""
    //        }}
    //      ],
    //      ""website"": ""http://firstinalabama.org/events/frc-events/"",
    //      ""week"": 5,
    //      ""year"": 2024
    //    }}
    //  ]
    //}}

    //### SAMPLE TEAM DETAIL OBJECT
    //{{
    //  ""address"": null,
    //  ""city"": ""Maple Valley"",
    //  ""country"": ""USA"",
    //  ""gmaps_place_id"": null,
    //  ""gmaps_url"": null,
    //  ""key"": ""frc2046"",
    //  ""lat"": null,
    //  ""lng"": null,
    //  ""location_name"": null,
    //  ""motto"": null,
    //  ""name"": ""The Boeing Company/Washington State OSPI/Tahoma School District/The Truck Shop/THiNC/Ratheon/Gene Haas Foundation/1-800-Got-Junk/West Coast Products&Tahoma High School"",
    //  ""nickname"": ""Bear Metal"",
    //  ""postal_code"": ""98038"",
    //  ""rookie_year"": 2007,
    //  ""school_name"": ""Tahoma High School"",
    //  ""state_prov"": ""Washington"",
    //  ""team_number"": 2046,
    //  ""website"": ""http://tahomarobotics.org/""
    //}}

    //When constructing a JMESPath for the JSON structures above, act as follows:
    //1. Use exact references to the item(s) you need from within the structure, (avoid wildcards, recursive search, etc.)
    //2. Do not use JMESPath expression syntax ('&' operator)
    //3. Surround literal values with single quotes (e.g. 'red' or '5')

    //This is the chat history so far in JSON format, use this as context into the conversation.

    //### CHAT SO FAR
    //";

    //        var settings = new OpenAIPromptExecutionSettings
    //        {
    //            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    //            User = Environment.MachineName
    //        };

    //        var chatHistory = new ChatHistory();
    //        chatHistory.AddAssistantMessage("Welcome to the TBA Chat bot! What would you like to know about FIRST competitions, past or present?");

    //        do
    //        {
    //            Console.Write("> ");
    //            var question = Console.ReadLine();
    //            if (string.IsNullOrWhiteSpace(question))
    //            {
    //                break;
    //            }

    //            StringBuilder assistantResponse = new();
    //            CircularCharArray progress = CircularCharArray.ProgressSpinner;
    //            try
    //            {
    //                do
    //                {
    //                    try
    //                    {
    //                        settings.ChatSystemPrompt = $@"{baseSystemPrompt}{chatHistory.Serialize()}";
    //                        await foreach (StreamingKernelContent token in kernel.InvokePromptStreamingAsync(question, new(settings), cancellationToken: cancellationToken))
    //                        {
    //                            var tokenString = token.ToString();
    //                            if (string.IsNullOrEmpty(tokenString) && Console.CursorLeft is 0)
    //                            {
    //                                Console.Write(progress.Next());
    //                                Console.CursorLeft--;

    //                                continue;
    //                            }

    //                            Console.Write(tokenString);
    //                            assistantResponse.Append(tokenString);
    //                        }

    //                        break;
    //                    }
    //                    catch (HttpOperationException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests && ex.InnerException is Azure.RequestFailedException rex)
    //                    {
    //                        Azure.Response? resp = rex.GetRawResponse();
    //                        if (resp?.Headers.TryGetValue("Retry-After", out var waitTime) is true)
    //                        {
    //                            _log.LogWarning("Responses Throttled! Must wait {retryAfter} seconds to try again...", waitTime);
    //                            var waitTimeRemaining = int.Parse(waitTime);
    //                            while (waitTimeRemaining-- >= 0)
    //                            {
    //                                Console.WriteLine($"{waitTimeRemaining}s");
    //                                Console.CursorTop--;

    //                                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
    //                            }
    //                        }
    //                    }
    //                } while (true);

    //                chatHistory.AddUserMessage(question);
    //                chatHistory.AddAssistantMessage(assistantResponse.ToString());

    //                Console.WriteLine();
    //            }
    //            catch (Exception e)
    //            {

    //            }
    //        }
    //        while (!cancellationToken.IsCancellationRequested);
    //    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
