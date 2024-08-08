using ChatApplication.Databases;
using ChatApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using Microsoft.AspNetCore.Identity;

namespace ChatApplication.Hubs
{
    public class ChatHub : Hub
    {
        private AppDbContext _ctx;
        public ChatHub(AppDbContext ctx)
        {
            _ctx = ctx;
        }
        public string GetConnectionId() =>
            Context.ConnectionId;
        public async Task SendMessage(
            string roomId,
            string message
          )
        {
            var Message = new Message
            {
                ChatId = Convert.ToInt32(roomId),
                Text = message,
                Name = Context.User.Identity.Name,
                Timestamp = DateTime.Now
            };
            _ctx.Messages.Add(Message);
            await _ctx.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveMessage", new
                {
                    Text = Message.Text,
                    Name = Message.Name,
                    Timestamp = Message.Timestamp.ToString("M/d/yyyy h:mm:ss tt")
                });
        }

    }
}
