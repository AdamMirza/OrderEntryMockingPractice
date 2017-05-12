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
        private IOrderFulfillmentService _mockOrderFulfillmentService = MockRepository.GenerateMock<IOrderFulfillmentService>();

        [SetUp]
        public void SetUp()
        {
            
        }

        [Test]
        public void PlaceOrder_NonUniqueProductSkus_ExceptionThrownWithInfo()
        {
            // Arrange
            var mockProductRepository = MockRepository.GenerateMock<IProductRepository>();
            mockProductRepository.Stub(p => p.IsInStock("1")).Return(true);

            var orderService = new OrderService(mockProductRepository);
            var order = new Order();
            var orderItem1 = new OrderItem
            {
                Product = new Product
                {
                    Sku = "1"
                }
            };

            var orderItem2 = new OrderItem
            {
                Product = new Product
                {
                    Sku = "1"
                }
            };

            order.OrderItems.Add(orderItem1);
            order.OrderItems.Add(orderItem2);
            
            // Act
            var exceptionMessage = Assert.Throws<Exception>(() => orderService.PlaceOrder(order));

            // Assert
            Assert.That(exceptionMessage.Message, Is.EqualTo("Order Items are not unique by Product SKU."));
        }

        [Test]
        public void PlaceOrder_ProductNotInStock_ExceptionThrownWithInfo()
        {
            var mockProductRepository = MockRepository.GenerateMock<IProductRepository>();
            mockProductRepository.Stub(p => p.IsInStock("1")).Return(false);

            // Arrange
            var orderService = new OrderService(mockProductRepository);
            var order = new Order();
            var orderItem = new OrderItem
            {
                Product = new Product
                {
                    Sku = "1"
                }
            };

            order.OrderItems.Add(orderItem);

            // Act
            var exceptionMessage = Assert.Throws<Exception>(() => orderService.PlaceOrder(order));

            // Assert
            Assert.That(exceptionMessage.Message, Is.EqualTo("One or more products are out of stock."));
        }
    }
}
