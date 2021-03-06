﻿using System;
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
        private readonly ITaxRateService _taxRateService;
        private readonly IEmailService _emailService;
        
        private int _customerId;

        private const int ShippingDuration = 7;

        public OrderService(int customerId,
                            IProductRepository productRepository,
                            IOrderFulfillmentService orderFulfillmentService,
                            ICustomerRepository customerRepository,
                            ITaxRateService taxRateService,
                            IEmailService emailService)
        {
            _customerId = customerId;
            _productRepository = productRepository;
            _orderFulfillmentService = orderFulfillmentService;
            _customerRepository = customerRepository;
            _taxRateService = taxRateService;
            _emailService = emailService;
        }

        public OrderSummary PlaceOrder(Order order)
        {
            CheckIfOrderIsValid(order);
            
            var confirmation = _orderFulfillmentService.Fulfill(order);

            var customer = _customerRepository.Get(_customerId);

            var taxList = _taxRateService.GetTaxEntries(customer.PostalCode, customer.Country);

            var netTotal = order.OrderItems.Sum(orderItem => orderItem.Quantity * orderItem.Product.Price);

            var taxEntries = taxList as IList<TaxEntry> ?? taxList.ToList();

            var total = taxEntries.Sum(taxEntry => netTotal * taxEntry.Rate);

            _emailService.SendOrderConfirmationEmail(_customerId, confirmation.OrderId);

            return new OrderSummary()
            {
                CustomerId = _customerId,
                OrderNumber = confirmation.OrderNumber,
                OrderId = confirmation.OrderId,
                OrderItems = order.OrderItems,
                NetTotal = netTotal,
                Taxes = taxEntries,
                Total = total,
                EstimatedDeliveryDate = DateTime.Now.AddDays(7).Date
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
