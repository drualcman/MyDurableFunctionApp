using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace MyDurableFunctionApp
{
    public static class MyDurableFunction
    {
        [FunctionName("MyDurableFunction")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("MyDurableFunction_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("MyDurableFunction_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("MyDurableFunction_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("MyDurableFunction_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("MyDurableFunction_HttpStart_Data")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("MyDurableFunction", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            //Most eficient to recovery the data directly from the Function when is finished
            var response = await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);
            return response;
        }

        [FunctionName("MyDurableFunction_HttpStart")]
         public static async Task<HttpResponseMessage> HttpStartOriginal(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("MyDurableFunction", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("MyDurableFunction_HttpStart_HttpClient")]
        public static async Task<List<string>> HttpStartRecoverDataUsingHttpClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("MyDurableFunction", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            var response = starter.CreateCheckStatusResponse(req, instanceId);
            
            var resultRequest = await response.Content.ReadFromJsonAsync<JsonElement>();
            var responseUri = resultRequest.GetProperty("statusQueryGetUri");
            var resultUriString = responseUri.GetString();
            var resultUri = resultUriString.Split('?');
            var resultUrl = resultUri[0];
            HttpClient httpClient = new HttpClient();
            var clientRequest = await httpClient.GetAsync(resultUrl);
            var clientResponse = await clientRequest.Content.ReadFromJsonAsync<JsonElement>();            
            var clientResult = clientResponse.GetProperty("output");
            List<string> result = clientResult.Deserialize<List<string>>();
            return result;
        }
        
        [FunctionName("MyDurableFunction_HttpStart_MessageResponse")]
        public static async Task<List<string>> HttpStartRecoverDataUsingHttpMessageResponse(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("MyDurableFunction", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            var response = await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);
            List<string> result = await response.Content.ReadFromJsonAsync<List<string>>();
            return result;
        }

    }
}