using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ChatsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // 1) Получить список чатов текущего пользователя
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chats = await _db.Chats
                .Where(c => c.UserAId == userId || c.UserBId == userId)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Include(c => c.UserA)
                .Include(c => c.UserB)
                .ToListAsync();

            var dto = chats.Select(c =>
            {
                var other = c.UserAId == userId ? c.UserB : c.UserA;
                var last = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
                return new
                {
                    c.Id,
                    OtherUser = new { other.Id, other.UserName },
                    LastMessage = last != null ? new { last.Text, last.SentAt } : null
                };
            });

            return Ok(dto);
        }

        // 2) Получить все сообщения чата
        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chat = await _db.Chats.FindAsync(chatId);
            if (chat == null || (chat.UserAId != userId && chat.UserBId != userId))
                return Forbid();

            var msgs = await _db.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentAt)
                .Select(m => new { m.Id, m.SenderId, m.Text, m.SentAt })
                .ToListAsync();
            return Ok(msgs);
        }

        // 3) Создать (или вернуть существующий) чат по товару
        [HttpPost("product/{productId}")]
        public async Task<IActionResult> CreateOrGetChatByProduct(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = await _db.Products.FindAsync(productId);
            if (product == null)
                return NotFound("Product not found");

            var otherId = product.SellerId;
            if (otherId == userId)
                return BadRequest("Нельзя создать чат с самим собой");

            var exists = await _db.Chats.FirstOrDefaultAsync(c =>
                (c.UserAId == userId && c.UserBId == otherId) ||
                (c.UserAId == otherId && c.UserBId == userId));
            if (exists != null)
                return Ok(new { id = exists.Id });

            var chat = new Chat { UserAId = userId, UserBId = otherId };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();
            return Ok(new { id = chat.Id });
        }

        // 4) Отправить сообщение в чат
        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] MessageDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chat = await _db.Chats.FindAsync(chatId);
            if (chat == null || (chat.UserAId != userId && chat.UserBId != userId))
                return Forbid();

            var msg = new Message
            {
                ChatId = chatId,
                SenderId = userId,
                Text = dto.Text
            };
            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = msg.Id,
                text = msg.Text,
                sentAt = msg.SentAt,
                senderId = msg.SenderId
            });
        }

        public class MessageDto
        {
            [Required]
            public string Text { get; set; }
        }
    }
}
