using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public static class ReserverService
    {
        [FunctionName("reserve")]
        public static async Task RunAsync([ServiceBusTrigger("eshop-queue", Connection = "ReceiverConnection")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            dynamic data = JsonConvert.DeserializeObject(myQueueItem);
            int? orderID = data?.OrderId;
            if (orderID == null)
            {
                throw new System.Exception("Order without id");
            }

            if (data?.FinalPrice > 100)
            {
                throw new Exception("Just for testing error behaviour.");
            }

            var blobStorageConnection = System.Environment.GetEnvironmentVariable("MyBlobStorageConnection");

            var containerClient = new BlobContainerClient(blobStorageConnection, "orderscontainer");
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient($"Order {orderID} {System.DateTime.UtcNow.ToString("dd-mm-yyyy HH-mm-ss")}.json");

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(myQueueItem)))
            {
                await blobClient.UploadAsync(ms);
            }
        }
    }
}
