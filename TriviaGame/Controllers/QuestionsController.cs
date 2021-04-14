using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TriviaGame.Models;
using TriviaGame.Services;

namespace TriviaGame.Controllers
{
    //[Route("api/[controller]")]
    //[ApiController]
    public class QuestionsController : Controller
    {
        private readonly QuestionService questionService;

        public QuestionsController(QuestionService questionService)
        {
            this.questionService = questionService;
        }

        [HttpGet]
        public ActionResult<List<Question>> Get() =>
            questionService.Get();

        [HttpGet("{id:length(24)}")]
        public ActionResult<Question> Get(string id)
        {
            var question = questionService.Get(id);

            if (question == null)
            {
                return NotFound();
            }

            return question;
        }

        [HttpPost]
        [Route("SubmitSucces")]
        public ActionResult<Question> Create(Question q)
        {
            q.isValidated = false;
            questionService.Create(q);

            //return CreatedAtRoute("GetQuestion", new { id = q.Id.ToString() }, q);
            return View("QuestionReceived");
        }

        [HttpPut("{id:length(24)}")]
        [Route("Questions/Approve")]
        public IActionResult Approve(Question questionIn)
        {
            var question = questionService.Get(questionIn.Id);

            if (question == null)
            {
                return NotFound();
            }

            questionIn.isValidated = true;
            questionService.Update(questionIn.Id, questionIn);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        [Route("Questions/Delete")]
        public IActionResult Delete(string id)
        {
            var question = questionService.Get(id);

            if (question == null)
            {
                return NotFound();
            }

            questionService.Remove(question.Id);

            return NoContent();
        }

        [Route("Submit")]
        public IActionResult SubmitQuestion()
        {
            return View();
        }

        [Route("Moderate")]
        public ActionResult<Question> ModerateQuestions()
        {
            return View(questionService.GetInvalid());
        }
    }
}
