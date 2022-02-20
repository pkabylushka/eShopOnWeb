using System;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Options;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class WarehouseService : IWarehouseService
{
    private readonly WarehouseSettings _warehauseConfiguration;
    private readonly IReadRepository<Order> _orderRepository;
    private readonly IAppLogger<WarehouseService> _logger;

    public WarehouseService(IOptions<WarehouseSettings> settings,
        IReadRepository<Order> orderRepository,
        IAppLogger<WarehouseService> logger)
    {
        _warehauseConfiguration = settings.Value;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task DeliveryOrder(int orderId)
    {
        var spec = new OrderWithItemsByIdSpec(orderId);
        var order = await _orderRepository.GetBySpecAsync(spec);

        var orderToDelivery = new
        {
            OrderId = order.Id,
            ShippingAddress = order.ShipToAddress,
            ListOfItems = order.OrderItems,
            FinalPrice = order.Total()
        };
        var content = orderToDelivery.ToJson();

        var client = new HttpClient()
        {
            BaseAddress = new Uri(_warehauseConfiguration.WarehouseBaseUrl)
        };
        string deliveryUrl = $"api/delivery?code={_warehauseConfiguration.WarehouseCode}";

        try
        {
            var result = await client.PostAsync(deliveryUrl, new StringContent(content));
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wrong delivery request.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex.Message);
            throw;
        }
    }

    public async Task ReserveOrder(int orderId)
    {
        var spec = new OrderWithItemsByIdSpec(orderId);
        var order = await _orderRepository.GetBySpecAsync(spec);

        var orderToDelivery = new
        {
            OrderId = order.Id,
            ShippingAddress = order.ShipToAddress,
            ListOfItems = order.OrderItems,
            FinalPrice = order.Total()
        };

        await using var client = new ServiceBusClient(_warehauseConfiguration.SenderConnection);
        await using ServiceBusSender sender = client.CreateSender(_warehauseConfiguration.ServiceQueueName);
        try
        {
            var message = new ServiceBusMessage(orderToDelivery.ToJson());
            await sender.SendMessageAsync(message);
        }
        catch (Exception exception)
        {
            _logger.LogWarning($"{DateTime.Now} :: Exception: {exception.Message}");
            throw;
        }
        finally
        {
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }
    }
}
