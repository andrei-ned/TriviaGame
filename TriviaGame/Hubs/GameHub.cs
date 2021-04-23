using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TriviaGame.Services;

namespace TriviaGame.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameService gameService;

        public GameHub(GameService gameService)
        {
            this.gameService = gameService;
        }

        public async Task SendChatMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveChatMessage", user, message);
        }

        public void SendQuestionAnswer(int answerId)
        {
            gameService.PlayerAnswer(Context.ConnectionId, answerId);
        }

        public void InitUser(string user)
        {
            gameService.AddPlayer(Context.ConnectionId, user);
        }

        public void SendReady(bool isReady)
        {
            gameService.ReadyPlayer(Context.ConnectionId, isReady);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            gameService.RemovePlayer(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}
