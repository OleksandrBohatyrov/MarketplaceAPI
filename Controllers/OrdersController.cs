using System;
using System.Linq;
using System.Security.Claims;
using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public OrdersController(ApplicationDbContext db) => _db = db;

        // --------------------------------------------------
        // GET /api/orders
        // админ видит все, пользователь — только свои покупки
        // --------------------------------------------------
        [HttpGet]
        public IActionResult GetOrders()
        {
            if (User.IsInRole("Admin"))
            {
                var all = _db.Orders.ToList();
                return Ok(all);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var mine = _db.Orders
                .Where(o => o.BuyerId == userId)
                .ToList();
            return Ok(mine);
        }

        // --------------------------------------------------
        // GET /api/orders/{id}
        // --------------------------------------------------
        [HttpGet("{id}")]
        public IActionResult GetOrder(int id)
        {
            var order = _db.Orders.Find(id);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.BuyerId != userId && order.SellerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            return Ok(order);
        }

        // --------------------------------------------------
        // POST /api/orders
        // Создать новый заказ (статус = PendingAddress)
        // --------------------------------------------------
        [HttpPost]
        public IActionResult CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto == null || dto.ProductId <= 0 || dto.Quantity <= 0)
                return BadRequest(new { message = "Incorrect order data" });

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = _db.Products.Find(dto.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found" });
            if (product.SellerId == buyerId)
                return BadRequest(new { message = "Cannot buy your own product" });

            var order = new Order
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                BuyerId = buyerId,
                SellerId = product.SellerId,
                Status = OrderStatus.PendingAddress,
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            _db.SaveChanges();
            return Ok(order);
        }

        // --------------------------------------------------
        // PUT /api/orders/{id}/address
        // Покупатель добавляет адрес —> статус = AwaitingShipment
        // --------------------------------------------------
        [HttpPut("{id}/address")]
        public IActionResult AddAddress(int id, [FromBody] AddressDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ShippingAddress))
                return BadRequest(new { message = "Address is required" });

            var order = _db.Orders.Find(id);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.BuyerId != buyerId)
                return Forbid();
            if (order.Status != OrderStatus.PendingAddress)
                return BadRequest(new { message = "Address already set or order progressed" });

            order.ShippingAddress = dto.ShippingAddress;
            order.Status = OrderStatus.AwaitingShipment;
            _db.SaveChanges();

            return Ok(order);
        }

        // --------------------------------------------------
        // GET /api/orders/my-purchases
        // Покупатель видит свои заказы
        // --------------------------------------------------
        [HttpGet("my-purchases")]
        public IActionResult GetMyPurchases()
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = _db.Orders
                .Where(o => o.BuyerId == buyerId)
                .Include(o => o.Product)
                .ToList();
            return Ok(orders);
        }

        // --------------------------------------------------
        // GET /api/orders/my-sales
        // Продавец видит заказы на его товары
        // --------------------------------------------------
        [HttpGet("my-sales")]
        public IActionResult GetMySales()
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = _db.Orders
                .Where(o => o.SellerId == sellerId)
                .Include(o => o.Product)
                .ToList();
            return Ok(orders);
        }

        // --------------------------------------------------
        // PUT /api/orders/{id}/ship
        // Продавец отмечает отправку —> статус = Shipped
        // --------------------------------------------------
        [HttpPut("{id}/ship")]
        public IActionResult MarkShipped(int id)
        {
            var order = _db.Orders.Find(id);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.SellerId != sellerId)
                return Forbid();
            if (order.Status != OrderStatus.AwaitingShipment)
                return BadRequest(new { message = "Cannot ship in current status" });

            order.Status = OrderStatus.Shipped;
            order.ShippedAt = DateTime.UtcNow;  // если есть такое поле
            _db.SaveChanges();

            return Ok(order);
        }

        // --------------------------------------------------
        // PUT /api/orders/{id}/deliver
        // Покупатель подтверждает получение —> статус = Delivered
        // --------------------------------------------------
        [HttpPut("{id}/deliver")]
        public IActionResult MarkDelivered(int id)
        {
            var order = _db.Orders.Find(id);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.BuyerId != buyerId)
                return Forbid();
            if (order.Status != OrderStatus.Shipped)
                return BadRequest(new { message = "Cannot confirm delivery in current status" });

            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = DateTime.UtcNow;  // если есть такое поле
            _db.SaveChanges();

            return Ok(order);
        }
    }


    public class CreateOrderDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class AddressDto
    {
        public string ShippingAddress { get; set; }
    }
}
