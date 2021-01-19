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
    [Route("api/[controller]")]
    [ApiController]
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

        [HttpGet("{id:length(24)}", Name = "GetBook")]
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
        public ActionResult<Question> Create(Question question)
        {
            questionService.Create(question);

            return CreatedAtRoute("GetQuestion", new { id = question.Id.ToString() }, question);
        }

        [HttpPut("{id:length(24)}")]
        public IActionResult Update(string id, Question questionIn)
        {
            var question = questionService.Get(id);

            if (question == null)
            {
                return NotFound();
            }

            questionService.Update(id, questionIn);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
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
    }
}
