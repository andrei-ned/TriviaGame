using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TriviaGame.Models
{
    public class GameSettings : IGameSettings
    {
        public int QuestionsPerGame { get; set; }
        public int SecondsPerQuestion { get; set; }
        public int SecondsBetweenQuestions { get; set; }
        public int PointsPerQuestion { get; set; }
        public float PointsResponseTimeMultiplier { get; set; }
        public int SecondsBetweenMatches { get; set; }
    }

    public interface IGameSettings
    {
        public int QuestionsPerGame { get; set; }
        public int SecondsPerQuestion { get; set; }
        public int SecondsBetweenQuestions { get; set; }
        public int PointsPerQuestion { get; set; }
        public float PointsResponseTimeMultiplier { get; set; }
        public int SecondsBetweenMatches { get; set; }
    }
}
