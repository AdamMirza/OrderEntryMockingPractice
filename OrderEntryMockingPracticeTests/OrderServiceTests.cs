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
        private Order _order;
        private OrderItem _orderItem;
        private IProductRepository _mockProductRepository;
        private IOrderFulfillmentService _mockOrderFulfillmentService;

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _mockProductRepository = MockRepository.GenerateMock<IProductRepository>();
            _mockOrderFulfillmentService = MockRepository.GenerateMock<IOrderFulfillmentService>();

            CreateOrderWithOneItem();

            _orderService = new OrderService(_mockProductRepository, _mockOrderFulfillmentService);
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
            _mockProductRepository.Stub(p => p.IsInStock("1")).Return(true);

            var orderItem2 = new OrderItem
            {
                Product = new Product
                {
                    Sku = "1"
                }
            };

            _order.OrderItems.Add(orderItem2);
            
            // Act
            var exceptionMessage = Assert.Throws<Exception>(() => _orderService.PlaceOrder(_order));

            // Assert
            Assert.That(exceptionMessage.Message, Is.EqualTo("Order Items are not unique by Product SKU."));
        }

        [Test]
        public void PlaceOrder_ProductNotInStock_ExceptionThrownWithInfo()
        {
            // Arrange
            _mockProductRepository.Stub(p => p.IsInStock("1")).Return(false);

            // Act
            var exceptionMessage = Assert.Throws<Exception>(() => _orderService.PlaceOrder(_order));

            // Assert
            Assert.That(exceptionMessage.Message, Is.EqualTo("One or more products are out of stock."));
        }

        [Test]
        public void PlaceOrder_OrderValid_SubmitsOrderToOrderFulfillmentService()
        {
            // Arrange
            _mockProductRepository.Stub(p => p.IsInStock("1")).Return(true);
            _mockOrderFulfillmentService.Stub(o => o.Fulfill(_order));

            // Act
            _orderService.PlaceOrder(_order);


            // Assert
            _mockOrderFulfillmentService.AssertWasCalled(o => o.Fulfill(_order));
        }

        [Test]
        public void PlaceOrder_OrderValid_OrderSummaryReturnedWithOrderFulfillmentConfirmationNumber()
        {
            // Arrange
            _mockProductRepository.Stub(p => p.IsInStock("1")).Return(true);

            const string orderNumber = "123";

            var orderConfirmation = new OrderConfirmation()
            {
                OrderNumber = orderNumber
            };

            _mockOrderFulfillmentService.Stub(o => o.Fulfill(_order)).Return(orderConfirmation);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            Assert.That(orderSummary.OrderNumber, Is.EqualTo(orderNumber));
        }

        [Test]
        public void PlaceOrder_OrderValid_SummaryReturnedWithOrderFulfillmentServiceId()
        {
            // Arrange
            _mockProductRepository.Stub(p => p.IsInStock("1")).Return(true);

            const int orderId = 123;

            var orderConfirmation = new OrderConfirmation()
            {
                OrderId = orderId
            };

            _mockOrderFulfillmentService.Stub(o => o.Fulfill(_order)).Return(orderConfirmation);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            Assert.That(orderSummary.OrderId, Is.EqualTo(orderId));
        }
    }
}
