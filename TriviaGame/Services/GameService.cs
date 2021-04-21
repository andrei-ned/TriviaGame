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

            InitGame();
        }

        private void InitGame()
        {
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
                gameHub.Clients.Client(playerConnectionId).SendAsync("ReceiveNewPlayer", player.name, player.score);
            }
            gameHub.Clients.Client(playerConnectionId).SendAsync("ReceiveGameData", gameSettings.SecondsPerQuestion, gameSettings.SecondsBetweenQuestions);
            players.TryAdd(playerConnectionId, new PlayerData(name));
            gameHub.Clients.AllExcept(playerConnectionId).SendAsync("ReceiveNewPlayer", name, 0);
        }

        public void RemovePlayer(string playerConnectionId)
        {
            gameHub.Clients.All.SendAsync("ReceivePlayerDisconnect", players[playerConnectionId].name);
            players.TryRemove(playerConnectionId, out _);
        }

        public void PlayerAnswer(string playerConnectionId, int answerId)
        {
            gameHub.Clients.All.SendAsync("ReceivePlayerAnswered", players[playerConnectionId].name);

            PlayerData player;
            if (!players.TryGetValue(playerConnectionId, out player))
                return;

            player.answerId = answerId;
            player.scoreThisQuestion = answerId == correctAnswer ? CalculateAnswerScore() : 0;
            player.score += player.scoreThisQuestion;
        }

        private int CalculateAnswerScore()
        {
            double t = Math.Clamp(questionStopwatch.Elapsed.TotalMilliseconds / (gameSettings.SecondsPerQuestion * 1000), 0.0f, 1.0f);
            double score = (1 - t * gameSettings.PointsResponseTimeMultiplier) * gameSettings.PointsPerQuestion;
            return Convert.ToInt32(score);
        }

        private void SendNextQuestion()
        {
            questionStopwatch.Restart();
            GameQuestion gameQuestion = new GameQuestion(questions[currentQuestionIndex]);
            correctAnswer = gameQuestion.answers.FindIndex(x => x == questions[currentQuestionIndex].correctAnswers[0]);
            currentQuestionIndex++;

            gameHub.Clients.All.SendAsync("ReceiveQuestion", gameQuestion, currentQuestionIndex);
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
            if (currentQuestionIndex < questions.Count)
            {
                ResetPlayerAnswers();
                SendNextQuestion();
                questionTimer.Start();
            }
            else
            {
                gameHub.Clients.All.SendAsync("ReceiveChatMessage", "Game", "Match is over");
                // TODO: restart game after end screen
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
            }

            public string name { get; set; }
            public int score { get; set; }
            public int answerId { get; set; }
            public int scoreThisQuestion { get; set; }
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
