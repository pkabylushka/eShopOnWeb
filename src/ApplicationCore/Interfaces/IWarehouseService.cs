using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces;

public interface IWarehouseService
{
    Task ReserveOrder(int orderId);
    Task DeliveryOrder(int orderId);
}
