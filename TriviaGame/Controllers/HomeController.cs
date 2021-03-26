using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TriviaGame.Models;
using MongoDB.Driver;

namespace TriviaGame.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("InputUsername");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        //[Route("Submit")]
        //public IActionResult SubmitQuestion()
        //{
        //    return View();
        //}

        [HttpPost]
        public IActionResult Index(string username)
        {
            return View("Index", username);
        }

        [HttpPost]
        public string SubmitAnswer(int answer)
        {
            // TODO
            return "Received answer " + answer;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
