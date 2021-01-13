using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace TriviaGame.Hubs
{
    public class GameHub : Hub
    {
        public async Task SendChatMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveChatMessage", user, message);
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
