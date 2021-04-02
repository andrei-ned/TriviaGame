using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TriviaGame.Controllers
{
    public class GameController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public string SubmitAnswer(int answer)
        {
            // TODO
            return "Received answer " + answer;
        }
    }
}
