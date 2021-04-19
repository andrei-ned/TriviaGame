using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TriviaGame.Hubs;
using TriviaGame.Models;
using System.Timers;
using System.Diagnostics;

namespace TriviaGame.Services
{
    public class GameService
    {
        private readonly IHubContext<GameHub> gameHub;
        private readonly QuestionService questionService;
        private readonly IGameSettings gameSettings;

        private ConcurrentDictionary<string, PlayerData> playerScores;
        private Timer questionTimer;
        private Stopwatch questionStopwatch;

        private List<Question> questions;
        private int currentQuestionIndex;

        public GameService(IHubContext<GameHub> gameHub, QuestionService questionService, IGameSettings gameSettings)
        {
            this.gameHub = gameHub;
            this.questionService = questionService;
            this.gameSettings = gameSettings;

            playerScores = new ConcurrentDictionary<string, PlayerData>();
            questionTimer = new Timer(gameSettings.SecondsPerQuestion * 1000);
            questionStopwatch = new Stopwatch();

            questionTimer.Elapsed += (sender, e) =>
            {
                if (currentQuestionIndex >= gameSettings.QuestionsPerGame)
                {
                    // Setup new game
                    InitGame();
                    return;
                }

                questionStopwatch.Restart();
                //gameHub.Clients.All.SendAsync("ReceiveChatMessage", "Service", "Hello");

                // Setup and send question
                GameQuestion gameQuestion = new GameQuestion(questions[currentQuestionIndex]);
                currentQuestionIndex++;

                gameHub.Clients.All.SendAsync("ReceiveQuestion", gameQuestion);
            };
            questionTimer.Start();

            InitGame();
        }

        private void InitGame()
        {
            // Clean up old questions
            //questions.Clear();
            currentQuestionIndex = 0;

            // Generate new questions
            questions = questionService.GetRandom(gameSettings.QuestionsPerGame);

            // Reset scores
            foreach (var player in playerScores.Values)
            {
                player.score = 0;
            }
        }

        public void AddPlayer(string playerConnectionId, string name)
        {
            foreach (var player in playerScores.Values)
            {
                gameHub.Clients.Client(playerConnectionId).SendAsync("ReceiveNewPlayer", player.name);
            }
            playerScores.TryAdd(playerConnectionId, new PlayerData(name));
            gameHub.Clients.AllExcept(playerConnectionId).SendAsync("ReceiveNewPlayer", name);
        }

        public void RemovePlayer(string playerConnectionId)
        {
            playerScores.TryRemove(playerConnectionId, out _);
        }

        public void PlayerAnswer(string playerConnectionId, int answerId)
        {
            if (true) // correct answer
            {
                if (playerScores.TryGetValue(playerConnectionId, out PlayerData playerData))
                {
                    playerData.score++;

                    gameHub.Clients.All.SendAsync("ReceivePlayerScore", playerData.name, playerData.score);
                }
            }
        }

        class PlayerData
        {
            public PlayerData(string name)
            {
                this.name = name;
                score = 0;
            }

            public string name;
            public int score;
        }

        class GameQuestion
        {
            public GameQuestion(Question q)
            {
                question = q.question;
                answers = new List<string>();
                answers.AddRange(q.correctAnswers);
                answers.AddRange(q.wrongAnswers);
                answers.OrderBy(x => Guid.NewGuid()); // shuffle
            }

            public string question { get; set; }
            public List<string> answers { get; set; }
        }
    }
}
