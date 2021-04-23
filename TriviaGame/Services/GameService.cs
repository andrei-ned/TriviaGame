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
using System.Text.Json;

namespace TriviaGame.Services
{
    public class GameService
    {
        private readonly IHubContext<GameHub> gameHub;
        private readonly QuestionService questionService;
        private readonly IGameSettings gameSettings;

        private ConcurrentDictionary<string, PlayerData> players;
        private Timer questionTimer;
        private Timer questionResultsTimer;
        private Stopwatch questionStopwatch;

        private List<Question> questions;
        private int currentQuestionIndex;
        private int correctAnswer;
        GameQuestion gameQuestion;

        bool isGameRunning = false;

        public GameService(IHubContext<GameHub> gameHub, QuestionService questionService, IGameSettings gameSettings)
        {
            this.gameHub = gameHub;
            this.questionService = questionService;
            this.gameSettings = gameSettings;

            players = new ConcurrentDictionary<string, PlayerData>();

            questionTimer = new Timer(gameSettings.SecondsPerQuestion * 1000);
            questionTimer.AutoReset = false;
            questionTimer.Elapsed += OnQuestonTimerElapsed;

            questionResultsTimer = new Timer(gameSettings.SecondsBetweenQuestions * 1000);
            questionResultsTimer.Elapsed += OnResultsTimerElapsed;
            questionResultsTimer.AutoReset = false;

            questionStopwatch = new Stopwatch();
        }

        private void InitGame()
        {
            isGameRunning = true;

            // Generate new questions
            currentQuestionIndex = 0;
            questions = questionService.GetRandom(gameSettings.QuestionsPerGame);

            // Reset previous scores
            foreach (var player in players.Values)
            {
                player.score = 0;
                player.answerId = -1;
                player.scoreThisQuestion = 0;
            }

            // Start next game
            SendNextQuestion();
            questionTimer.Start();
        }

        public void AddPlayer(string playerConnectionId, string name)
        {
            foreach (var player in players.Values)
            {
                gameHub.Clients.Client(playerConnectionId).SendAsync("ReceiveNewPlayer", player);
            }
            gameHub.Clients.Client(playerConnectionId).SendAsync("ReceiveGameData", gameSettings.SecondsPerQuestion, gameSettings.SecondsBetweenQuestions, players.Values.ToArray(), isGameRunning);
            if (isGameRunning)
                gameHub.Clients.Client(playerConnectionId).SendAsync("ReceiveQuestion", gameQuestion, currentQuestionIndex, gameSettings.QuestionsPerGame, questionStopwatch.Elapsed.Seconds);
            var newPlayer = new PlayerData(name);
            players.TryAdd(playerConnectionId, newPlayer);
            gameHub.Clients.All.SendAsync("ReceiveNewPlayer", newPlayer);
        }

        public void RemovePlayer(string playerConnectionId)
        {
            gameHub.Clients.All.SendAsync("ReceivePlayerDisconnect", players[playerConnectionId].id);
            players.TryRemove(playerConnectionId, out _);

            // No players connected, end the game
            if (players.Count == 0)
            {
                isGameRunning = false;
                questionTimer.Stop();
                questionResultsTimer.Stop();
                return;
            }

            if (!isGameRunning)
            {
                StartGameIfAllPlayersReady();
            }
        }

        public void ReadyPlayer(string playerConnectionId, bool isReady)
        {
            PlayerData player;
            if (!players.TryGetValue(playerConnectionId, out player))
                return;

            player.isReady = isReady;

            gameHub.Clients.All.SendAsync("ReceivePlayerReady", player.id, player.isReady);

            StartGameIfAllPlayersReady();
        }

        public void PlayerAnswer(string playerConnectionId, int answerId)
        {
            PlayerData player;
            if (!players.TryGetValue(playerConnectionId, out player))
                return;

            gameHub.Clients.All.SendAsync("ReceivePlayerAnswered", player.id);

            player.answerId = answerId;
            player.scoreThisQuestion = answerId == correctAnswer ? CalculateAnswerScore() : 0;
            player.score += player.scoreThisQuestion;

            // End question if everyone answered
            if (!questionTimer.Enabled)
                return;
            foreach(var p in players.Values)
            {
                if (p.answerId == -1)
                {
                    return;
                }
            }
            questionTimer.Stop();
            OnQuestonTimerElapsed(null, null);
        }

        private void StartGameIfAllPlayersReady()
        {
            foreach (var p in players.Values)
            {
                if (!p.isReady)
                    return;
            }

            InitGame();
        }

        private int CalculateAnswerScore()
        {
            double t = Math.Clamp(questionStopwatch.Elapsed.TotalMilliseconds / (gameSettings.SecondsPerQuestion * 1000), 0.0f, 1.0f);
            double score = (1 - t * gameSettings.PointsResponseTimeMultiplier) * gameSettings.PointsPerQuestion;
            return Convert.ToInt32(score);
        }

        private void GenerateNextQuestion()
        {
            questionStopwatch.Restart();
            gameQuestion = new GameQuestion(questions[currentQuestionIndex]);
            correctAnswer = gameQuestion.answers.FindIndex(x => x == questions[currentQuestionIndex].correctAnswers[0]);
            currentQuestionIndex++;
        }

        private void SendNextQuestion()
        {
            GenerateNextQuestion();

            gameHub.Clients.All.SendAsync("ReceiveQuestion", gameQuestion, currentQuestionIndex, gameSettings.QuestionsPerGame, questionStopwatch.Elapsed.Seconds);
        }

        private void ResetPlayerAnswers()
        {
            foreach(var player in players.Values)
            {
                player.answerId = -1;
                player.scoreThisQuestion = 0;
            }
        }

        private void OnQuestonTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Send: everyone's answers, everyone's gained points, correct answer
            gameHub.Clients.All.SendAsync("ReceiveQuestionResults", players.Values.ToArray(), correctAnswer);
            questionResultsTimer.Start();
        }

        private void OnResultsTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (currentQuestionIndex >= questions.Count)
            {
                gameHub.Clients.All.SendAsync("ReceiveChatMessage", "Game", "Match is over");
                gameHub.Clients.All.SendAsync("ReceiveGameEnd", gameSettings.SecondsBetweenMatches, players.Values.ToArray());
                System.Threading.Thread.Sleep(gameSettings.SecondsBetweenMatches * 1000);
                InitGame();
            }
            else
            {
                ResetPlayerAnswers();
                SendNextQuestion();
                questionTimer.Start();
            }
        }

        class PlayerData
        {
            public PlayerData(string name)
            {
                this.name = name;
                score = 0;
                answerId = -1;
                scoreThisQuestion = 0;
                id = Guid.NewGuid().ToString();
            }

            public string id { get; set; }
            public string name { get; set; }
            public int score { get; set; }
            public int answerId { get; set; }
            public int scoreThisQuestion { get; set; }
            public bool isReady { get; set; }
        }

        class GameQuestion
        {
            public GameQuestion(Question q)
            {
                question = q.question;
                answers = new List<string>();
                answers.AddRange(q.correctAnswers);
                answers.AddRange(q.wrongAnswers);
                answers.Shuffle();
            }

            public string question { get; set; }
            public List<string> answers { get; set; }
        }
    }
}
