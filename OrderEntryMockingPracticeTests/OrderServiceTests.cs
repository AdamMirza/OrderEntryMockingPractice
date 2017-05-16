using System;
using NUnit.Framework;
using OrderEntryMockingPractice.Models;
using OrderEntryMockingPractice.Services;
using Rhino.Mocks;
using System.Collections.Generic;

namespace OrderEntryMockingPracticeTests
{
    [TestFixture]
    public class OrderServiceTests
    {
        private OrderService _orderService;

        private const int CustomerIdConstInt = 8675;
        private const string SkuConstString = "SKU-001";
        private const string CountryConstString = "USA";
        private const string PostalCodeConstString = "98101";

        private Order _order;
        private OrderItem _orderItem;
        private IProductRepository _mockProductRepository;
        private IOrderFulfillmentService _mockOrderFulfillmentService;
        private ICustomerRepository _mockCustomerRepository;
        private ITaxRateService _mockTaxRateService;

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _mockProductRepository = MockRepository.GenerateMock<IProductRepository>();
            _mockOrderFulfillmentService = MockRepository.GenerateMock<IOrderFulfillmentService>();
            _mockCustomerRepository = MockRepository.GenerateMock<ICustomerRepository>();
            _mockTaxRateService = MockRepository.GenerateMock<ITaxRateService>();

            CreateOrderWithOneItem();

            _orderService = new OrderService(CustomerIdConstInt,
                                             _mockProductRepository,
                                             _mockOrderFulfillmentService,
                                             _mockCustomerRepository,
                                             _mockTaxRateService);
        }

        private void CreateOrderWithOneItem()
        {
            _order = new Order();

            _orderItem = new OrderItem
            {
                Product = new Product
                {
                    Sku = SkuConstString
                }
            };

            _order.OrderItems.Add(_orderItem);
        }

        [Test]
        public void PlaceOrder_NonUniqueProductSkus_ExceptionThrownWithInfo()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);
            AddItemToOrderItems(SkuConstString);

            // Act
            var exceptionMessage = Assert.Throws<Exception>(() => _orderService.PlaceOrder(_order));

            // Assert
            Assert.That(exceptionMessage.Message, Is.EqualTo("Order Items are not unique by Product SKU."));
        }

        private void AddItemToOrderItems(string sku)
        {
            var orderItem = new OrderItem
            {
                Product = new Product
                {
                    Sku = sku
                }
            };

            _order.OrderItems.Add(orderItem);
        }

        private void StubProductRepository(bool isInStock, string sku)
        {
            _mockProductRepository.Stub(p => p.IsInStock(sku)).Return(isInStock);
        }

        [Test]
        public void PlaceOrder_ProductNotInStock_ExceptionThrownWithInfo()
        {
            // Arrange
            StubProductRepository(false, SkuConstString);

            // Act
            var exceptionMessage = Assert.Throws<Exception>(() => _orderService.PlaceOrder(_order));

            // Assert
            Assert.That(exceptionMessage.Message, Is.EqualTo("One or more products are out of stock."));
        }

        [Test]
        public void PlaceOrder_OrderValid_SubmitsOrderToOrderFulfillmentService()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);
            StubFulfillOrder(new OrderConfirmation());

            StubGetStandardCustomerFromRepository();

            // Act
            _orderService.PlaceOrder(_order);


            // Assert
            _mockOrderFulfillmentService.AssertWasCalled(o => o.Fulfill(_order));
        }

        [Test]
        public void PlaceOrder_OrderValid_OrderSummaryReturnedWithOrderFulfillmentConfirmationNumber()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);

            const string orderNumber = "123";

            var orderConfirmation = new OrderConfirmation
            {
                OrderNumber = orderNumber
            };

            StubFulfillOrder(orderConfirmation);
            StubGetStandardCustomerFromRepository();

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            Assert.That(orderSummary.OrderNumber, Is.EqualTo(orderNumber));
        }

        [Test]
        public void PlaceOrder_OrderValid_SummaryReturnedWithOrderFulfillmentServiceId()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);

            const int orderId = 123;
            var orderConfirmation = new OrderConfirmation
            {
                OrderId = orderId
            };

            StubFulfillOrder(orderConfirmation);

            StubGetStandardCustomerFromRepository();

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            Assert.That(orderSummary.OrderId, Is.EqualTo(orderId));
        }

        [Test]
        public void PlaceOrder_CustomerValid_GetsCustomerInfo()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);
            StubGetStandardCustomerFromRepository();
            StubFulfillOrder(new OrderConfirmation());

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            _mockCustomerRepository.AssertWasCalled(c => c.Get(CustomerIdConstInt));
            Assert.That(orderSummary.CustomerId, Is.EqualTo(CustomerIdConstInt));
        }

        private void StubGetStandardCustomerFromRepository()
        {
            var customer = new Customer
            {
                CustomerId = CustomerIdConstInt,
                Country = CountryConstString,
                PostalCode = PostalCodeConstString
            };
            _mockCustomerRepository.Stub(c => c.Get(CustomerIdConstInt)).Return(customer);
        }

        private void StubFulfillOrder(OrderConfirmation orderConfirmation)
        {
            _mockOrderFulfillmentService.Stub(o => o.Fulfill(_order)).Return(orderConfirmation);
        }

        [Test]
        public void PlaceOrder_CustomerValid_GetsTaxInfo()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);
            StubGetStandardCustomerFromRepository();

            var orderConfirmation = new OrderConfirmation
            {
                CustomerId = CustomerIdConstInt
            };
            StubFulfillOrder(orderConfirmation);

            var taxList = new List<TaxEntry>
            {
                new TaxEntry
                {
                    Description = "First entry.",
                    Rate = 2.3m
                },
                new TaxEntry()
                {
                    Description = "Second entry.",
                    Rate = 0.098m
                }
            };

            _mockTaxRateService.Stub(t => t.GetTaxEntries(PostalCodeConstString, CountryConstString)).Return(taxList);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            _mockTaxRateService.AssertWasCalled(t => t.GetTaxEntries(PostalCodeConstString, CountryConstString));
            Assert.That(orderSummary.Taxes, Is.EqualTo(taxList));
        }

        [Test]
        public void PlaceOrder_ValidOrder_NetTotal()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);
        }
    }
}
