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

        private const int _customerID = 1;
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

            _orderService = new OrderService(_customerID,
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

        [Test]
        public void PlaceOrder_OrderValid_GetsCustomerInfo()
        {
            // Arrange
            _mockProductRepository.Stub(p => p.IsInStock("1")).Return(true);

            _customer = new Customer() { CustomerId = _customerID };
            _mockCustomerRepository.Stub(c => c.Get(_customerID)).Return(_customer);

            var orderConfirmation = new OrderConfirmation();
            _mockOrderFulfillmentService.Stub(o => o.Fulfill(_order)).Return(orderConfirmation);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            _mockCustomerRepository.AssertWasCalled(c => c.Get(_customerID));
            Assert.That(orderSummary.CustomerId, Is.EqualTo(_customerID));
        }

    }
}
