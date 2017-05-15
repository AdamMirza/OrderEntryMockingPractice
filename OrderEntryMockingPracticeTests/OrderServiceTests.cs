using System;
using NUnit.Framework;
using OrderEntryMockingPractice.Models;
using OrderEntryMockingPractice.Services;
using Rhino.Mocks;

namespace OrderEntryMockingPracticeTests
{
    [TestFixture]
    public class OrderServiceTests
    {
        private OrderService _orderService;

        private const int CustomerId = 1;
        private Customer _customer;
        private Order _order;
        private OrderItem _orderItem;
        private IProductRepository _mockProductRepository;
        private IOrderFulfillmentService _mockOrderFulfillmentService;
        private ICustomerRepository _mockCustomerRepository;

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _mockProductRepository = MockRepository.GenerateMock<IProductRepository>();
            _mockOrderFulfillmentService = MockRepository.GenerateMock<IOrderFulfillmentService>();
            _mockCustomerRepository = MockRepository.GenerateMock<ICustomerRepository>();

            CreateOrderWithOneItem();

            _orderService = new OrderService(CustomerId,
                                             _mockProductRepository,
                                             _mockOrderFulfillmentService,
                                             _mockCustomerRepository);
        }

        private void CreateOrderWithOneItem()
        {
            _order = new Order();

            _orderItem = new OrderItem
            {
                Product = new Product
                {
                    Sku = "1"
                }
            };

            _order.OrderItems.Add(_orderItem);
        }

        [Test]
        public void PlaceOrder_NonUniqueProductSkus_ExceptionThrownWithInfo()
        {
            // Arrange
            StubProductRepository(true, "1");
            AddItemToOrderItems("1");

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
            StubProductRepository(false, "1");

            // Act
            var exceptionMessage = Assert.Throws<Exception>(() => _orderService.PlaceOrder(_order));

            // Assert
            Assert.That(exceptionMessage.Message, Is.EqualTo("One or more products are out of stock."));
        }

        [Test]
        public void PlaceOrder_OrderValid_SubmitsOrderToOrderFulfillmentService()
        {
            // Arrange
            StubProductRepository(true, "1");
            StubFulfillOrder(new OrderConfirmation());

            // Act
            _orderService.PlaceOrder(_order);


            // Assert
            _mockOrderFulfillmentService.AssertWasCalled(o => o.Fulfill(_order));
        }

        [Test]
        public void PlaceOrder_OrderValid_OrderSummaryReturnedWithOrderFulfillmentConfirmationNumber()
        {
            // Arrange
            StubProductRepository(true, "1");

            const string orderNumber = "123";

            var orderConfirmation = new OrderConfirmation()
            {
                OrderNumber = orderNumber
            };

            StubFulfillOrder(orderConfirmation);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            Assert.That(orderSummary.OrderNumber, Is.EqualTo(orderNumber));
        }

        [Test]
        public void PlaceOrder_OrderValid_SummaryReturnedWithOrderFulfillmentServiceId()
        {
            // Arrange
            StubProductRepository(true, "1");

            const int orderId = 123;
            var orderConfirmation = new OrderConfirmation()
            {
                OrderId = orderId
            };

            StubFulfillOrder(orderConfirmation);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            Assert.That(orderSummary.OrderId, Is.EqualTo(orderId));
        }

        [Test]
        public void PlaceOrder_CustomerValid_GetsCustomerInfo()
        {
            // Arrange
            StubProductRepository(true, "1");

            _customer = new Customer() { CustomerId = CustomerId };
            _mockCustomerRepository.Stub(c => c.Get(CustomerId)).Return(_customer);

            StubFulfillOrder(new OrderConfirmation());

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            _mockCustomerRepository.AssertWasCalled(c => c.Get(CustomerId));
            Assert.That(orderSummary.CustomerId, Is.EqualTo(CustomerId));
        }

        private void StubFulfillOrder(OrderConfirmation orderConfirmation)
        {
            _mockOrderFulfillmentService.Stub(o => o.Fulfill(_order)).Return(orderConfirmation);
        }
    }
}
