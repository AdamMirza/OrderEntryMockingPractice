using System;
using System.Collections.Generic;
using System.Linq;
using OrderEntryMockingPractice.Models;

namespace OrderEntryMockingPractice.Services
{
    public class OrderService
    {
        private readonly IProductRepository _productRepository;

        public OrderService(IProductRepository productRepository)
        {
            this._productRepository = productRepository;
        }

        public OrderSummary PlaceOrder(Order order)
        {
            CheckIfOrderIsValid(order);

            return null;
        }

        private void CheckIfOrderIsValid(Order order)
        {
            List<string> productSkus = new List<string>();

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
