using System;
using NUnit.Framework;
using OrderEntryMockingPractice.Models;
using OrderEntryMockingPractice.Services;
using Rhino.Mocks;
using System.Collections.Generic;
using System.Linq;
using Rhino.Mocks.Interfaces;

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
        private const decimal FirstProductPriceConstDecimal = 3.0m;
        private const int FirstProductQuantityConstInt = 4;
        private const decimal TaxRateConstDecimal = 2.3m;

        private Order _order;
        private OrderItem _orderItem;
        private IProductRepository _mockProductRepository;
        private IOrderFulfillmentService _mockOrderFulfillmentService;
        private ICustomerRepository _mockCustomerRepository;
        private ITaxRateService _mockTaxRateService;
        private IEnumerable<TaxEntry> _taxList;
        private IEmailService _mockEmailService;

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _mockProductRepository = MockRepository.GenerateMock<IProductRepository>();
            _mockOrderFulfillmentService = MockRepository.GenerateMock<IOrderFulfillmentService>();
            _mockCustomerRepository = MockRepository.GenerateMock<ICustomerRepository>();
            _mockTaxRateService = MockRepository.GenerateMock<ITaxRateService>();
            _mockEmailService = MockRepository.GenerateMock<IEmailService>();

            CreateOrderWithOneItem();

            _orderService = new OrderService(CustomerIdConstInt,
                                             _mockProductRepository,
                                             _mockOrderFulfillmentService,
                                             _mockCustomerRepository,
                                             _mockTaxRateService,
                                             _mockEmailService);
        }

        private void CreateOrderWithOneItem()
        {
            _order = new Order();

            _orderItem = new OrderItem
            {
                Product = new Product
                {
                    Sku = SkuConstString,
                    Price = FirstProductPriceConstDecimal
                },
                Quantity = FirstProductQuantityConstInt
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
            StubTaxRateService(null);

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
            StubTaxRateService(null);

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
            StubTaxRateService(null);
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

            StubTaxRateService(null);

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

            StubTaxRateService(taxList);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            _mockTaxRateService.AssertWasCalled(t => t.GetTaxEntries(PostalCodeConstString, CountryConstString));
            Assert.That(orderSummary.Taxes, Is.EqualTo(taxList));
        }

        private void StubTaxRateService(IEnumerable<TaxEntry> taxList)
        {
            if (taxList == null)
            {
                _taxList = new List<TaxEntry>
                {
                    new TaxEntry
                    {
                        Description = "First entry",
                        Rate = TaxRateConstDecimal
                    }
                };
            }
            else
            {
                _taxList = taxList;
            }
            _mockTaxRateService.Stub(t => t.GetTaxEntries(PostalCodeConstString, CountryConstString)).Return(_taxList);
        }

        [Test]
        public void PlaceOrder_ValidOrderOneItem_NetTotal()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);
            StubGetStandardCustomerFromRepository();
            StubFulfillOrder(new OrderConfirmation());
            StubTaxRateService(null);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            var netTotal = _order.OrderItems.Sum(orderItem => orderItem.Quantity * orderItem.Product.Price);

            Assert.That(orderSummary.NetTotal, Is.EqualTo(netTotal));
        }

        [Test]
        public void PlaceOrder_ValidOrderMultipleItems_NetTotal()
        {
            // Arrange
            var secondarySku = "SKU-0020";
            StubProductRepository(true, SkuConstString);
            StubProductRepository(true, secondarySku);
            StubGetStandardCustomerFromRepository();
            StubFulfillOrder(new OrderConfirmation());

            _orderItem = new OrderItem
            {
                Product = new Product
                {
                    Sku = secondarySku,
                    Price = 5.0m
                },
                Quantity = 2
            };

            StubTaxRateService(null);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            var netTotal = _order.OrderItems.Sum(orderItem => orderItem.Quantity * orderItem.Product.Price);

            Assert.That(orderSummary.NetTotal, Is.EqualTo(netTotal));
        }

        [Test]
        public void PlaceOrder_ValidOrderOneItem_Total()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);
            StubGetStandardCustomerFromRepository();
            StubFulfillOrder(new OrderConfirmation());

            const decimal taxRate = 2.3m;

            StubTaxRateService(null);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Arrange
            var total = orderSummary.NetTotal * TaxRateConstDecimal;

            Assert.That(orderSummary.Total, Is.EqualTo(total));
        }

        [Test]
        public void PlaceOrder_ValidOrderMultipleItemsMultipleTax_Total()
        {
            // Arrange
            const string secondarySku = "SKU-0020";
            StubProductRepository(true, SkuConstString);
            StubProductRepository(true, secondarySku);
            StubGetStandardCustomerFromRepository();
            StubFulfillOrder(new OrderConfirmation());

            _orderItem = new OrderItem
            {
                Product = new Product
                {
                    Sku = secondarySku,
                    Price = 5.0m
                },
                Quantity = 2
            };

            const decimal firstTaxRate = 2.3m;
            const decimal secondTaxRate = 0.8m;
            var taxList = new List<TaxEntry>
            {
                new TaxEntry
                {
                    Description = "First entry",
                    Rate = firstTaxRate
                },
                new TaxEntry()
                {
                    Description = "Second entry",
                    Rate = secondTaxRate
                }
            };

            StubTaxRateService(taxList);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Arrange
            var total = orderSummary.NetTotal * firstTaxRate + orderSummary.NetTotal * secondTaxRate;

            Assert.That(orderSummary.Total, Is.EqualTo(total));
        }

        [Test]
        public void PlaceOrder_ValidOrder_EmailConfirmationSent()
        {
            // Arrange
            StubProductRepository(true, SkuConstString);
            StubGetStandardCustomerFromRepository();

            const int orderId = 538;
            StubFulfillOrder(new OrderConfirmation
            {
                OrderId = orderId
            });
            StubTaxRateService(null);

            // Act
            _orderService.PlaceOrder(_order);

            // Assert
            _mockEmailService.AssertWasCalled(e => e.SendOrderConfirmationEmail(CustomerIdConstInt, orderId));
        }

        [Test]
        public void PlaceOrder_FullValidOrder_FullOrderSummary()
        {
            StubProductRepository(true, SkuConstString);
            StubGetStandardCustomerFromRepository();

            const int orderId = 538;
            const string orderNumber = "ORD-2209";
            StubFulfillOrder(new OrderConfirmation
            {
                OrderId = orderId,
                CustomerId = CustomerIdConstInt,
                OrderNumber = orderNumber
            });
            StubTaxRateService(null);

            // Act
            var orderSummary = _orderService.PlaceOrder(_order);

            // Assert
            var netTotal = _order.OrderItems.Sum(orderItem => orderItem.Quantity * orderItem.Product.Price);
            var total = netTotal * TaxRateConstDecimal;

            Assert.That(orderSummary.OrderId, Is.EqualTo(orderId));
            Assert.That(orderSummary.OrderNumber, Is.EqualTo(orderNumber));
            Assert.That(orderSummary.CustomerId, Is.EqualTo(CustomerIdConstInt));
            Assert.That(orderSummary.OrderItems, Is.EqualTo(_order.OrderItems));
            Assert.That(orderSummary.NetTotal, Is.EqualTo(netTotal));
            Assert.That(orderSummary.Taxes, Is.EqualTo(_taxList));
            Assert.That(orderSummary.Total, Is.EqualTo(total));
            Assert.That(orderSummary.EstimatedDeliveryDate, Is.EqualTo(DateTime.Now.AddDays(7).Date));
        }
    }
}
