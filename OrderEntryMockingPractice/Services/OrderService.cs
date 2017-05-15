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
        private readonly ICustomerRepository _customerRepository;
        
        private int _customerId;

        public OrderService(int customerId,
                            IProductRepository productRepository,
                            IOrderFulfillmentService orderFulfillmentService,
                            ICustomerRepository customerRepository)
        {
            _customerId = customerId;
            _productRepository = productRepository;
            _orderFulfillmentService = orderFulfillmentService;
            _customerRepository = customerRepository;
        }

        public OrderSummary PlaceOrder(Order order)
        {
            CheckIfOrderIsValid(order);
            
            var confirmation = _orderFulfillmentService.Fulfill(order);

            var customer = _customerRepository.Get(_customerId);

            return new OrderSummary()
            {
                CustomerId = _customerId,
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
