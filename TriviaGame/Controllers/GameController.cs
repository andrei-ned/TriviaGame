using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TriviaGame.Services;

namespace TriviaGame.Controllers
{
    public class GameController : Controller
    {
        [HttpPost]
        public IActionResult Index(string username)
        {
            return View("Play", username);
        }
    }
}
