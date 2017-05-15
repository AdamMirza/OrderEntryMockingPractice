using System;
using System.Collections.Generic;
using System.Linq;
using OrderEntryMockingPractice.Models;

namespace OrderEntryMockingPractice.Services
{
    public class OrderService
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderFulfillmentService _orderFulfillmentService;

        public OrderService(IProductRepository productRepository,
                            IOrderFulfillmentService orderFulfillmentService)
        {
            _productRepository = productRepository;
            _orderFulfillmentService = orderFulfillmentService;
        }

        public OrderSummary PlaceOrder(Order order)
        {
            CheckIfOrderIsValid(order);

            var confirmation = _orderFulfillmentService.Fulfill(order);

            return new OrderSummary()
            {
                OrderNumber = confirmation.OrderNumber,
                OrderId = confirmation.OrderId
            };
        }

        private void CheckIfOrderIsValid(Order order)
        {
            var productSkus = new List<string>();

            foreach (var item in order.OrderItems)
            {
                if (productSkus.Contains(item.Product.Sku))
                {
                    throw new Exception("Order Items are not unique by Product SKU.");
                }

                if (!_productRepository.IsInStock(item.Product.Sku))
                {
                    throw new Exception("One or more products are out of stock.");
                }

                productSkus.Add(item.Product.Sku);
            }
        }
    }
}
