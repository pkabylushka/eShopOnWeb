using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public static class DeliveryService
    {
        [FunctionName("delivery")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
        databaseName: "Orders",
        collectionName: "Summary",
        ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string orderID = req.Query["orderId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            orderID = orderID ?? data?.OrderId;
            if (orderID == null)
            {
                throw new System.Exception("Order without id");
            }

            await documentsOut.AddAsync(requestBody);

            string responseMessage = $"Order #{orderID} will be delivered soon! This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
