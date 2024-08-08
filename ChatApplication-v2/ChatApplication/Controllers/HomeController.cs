using ChatApplication.Databases;
using ChatApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace ChatApplication.Controllers
{
    [Authorize]
    public class HomeController : Controller

    {
        private AppDbContext _ctx;

        public HomeController(AppDbContext ctx)
        {
            _ctx = ctx;
        }
        public IActionResult Index()
        {
            var chats = _ctx.Chats
                .Include(x => x.Users)
                .Where(x => !x.Users
                .Any(y => y.UserId == User.FindFirst(ClaimTypes.NameIdentifier).Value)
                && x.Type == ChatType.Room)
                .ToList();

            return View(chats);
        }

        public IActionResult Find()
        {
            // show users that not already have a private chat room with the current user.
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var users = _ctx.Users
                .Where(x => x.Id != currentUserId &&
                            !_ctx.Chats
                                .Where(c => c.Type == ChatType.Private && c.Users.Any(u => u.UserId == currentUserId))
                                .SelectMany(c => c.Users)
                                .Any(u => u.UserId == x.Id))
                .ToList();

            return View(users);
        }

        public IActionResult Private()
        {
            var chats = _ctx.Chats
                .Include(x => x.Users)
                .ThenInclude(x => x.User)
                .Where(x => x.Type == ChatType.Private
                && x.Users.Any(y => y.UserId == User.FindFirst(ClaimTypes.NameIdentifier).Value))
                .ToList();

            return View(chats);
        }

        public async Task<IActionResult> CreatePrivateRoom (string userId)
        {
            var chat = new Chat
            {
                Type = ChatType.Private,
                Name = _ctx.Users.FirstOrDefault(x => x.Id == userId).UserName
            };

            chat.Users.Add(new ChatUser { UserId = userId });

            chat.Users.Add(new ChatUser { UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value });

            _ctx.Chats.Add(chat);

            await _ctx.SaveChangesAsync();

            return RedirectToAction("Chat", new { id = chat.Id });
        }

        [HttpGet("{id}")]
        public IActionResult Chat(int id)
        {
            var chat = _ctx.Chats
                .Include(x => x.Messages)
                .Include(x => x.Users)
                .ThenInclude(x => x.User)
                .FirstOrDefault(x => x.Id == id);
            return View(chat);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom(string name)
        {
            var chat = new Chat
            {
                Name = name,
                Type = ChatType.Room
            };

            chat.Users.Add(new ChatUser()
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
            });

            _ctx.Chats.Add(chat);
            await _ctx.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> JoinRoom(int id)
        {
            var ChatUser = new ChatUser
            {
                ChatId = id,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value
            };

            _ctx.ChatUsers.Add(ChatUser);
            await _ctx.SaveChangesAsync();
            return RedirectToAction("Chat", "Home", new {id = id});
        }

        [HttpPost]
        public async Task<IActionResult> LeaveRoom(int id)
        {
            var chat = _ctx.Chats
                .Include(x => x.Users)
                .ThenInclude(x => x.User)
                .FirstOrDefault(x => x.Id == id);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var user = chat.Users.FirstOrDefault(x => x.UserId == userId);
            chat.Users.Remove(user);

            _ctx.Chats.Update(chat);
            await _ctx.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
