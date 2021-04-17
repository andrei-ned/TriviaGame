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
    }

    public interface IGameSettings
    {
        public int QuestionsPerGame { get; set; }
        public int SecondsPerQuestion { get; set; }
    }
}
