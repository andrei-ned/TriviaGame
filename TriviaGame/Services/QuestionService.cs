using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using TriviaGame.Models;

namespace TriviaGame.Services
{
    public class QuestionService
    {
        private readonly IMongoCollection<Question> questions;

        public QuestionService(IQuestionDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            questions = database.GetCollection<Question>(settings.QuestionCollectionName);
        }

        public List<Question> Get() =>
            questions.Find(book => true).ToList();

        public Question Get(string id) =>
            questions.Find(question => question.Id == id).FirstOrDefault();

        public Question Create(Question question)
        {
            questions.InsertOne(question);
            return question;
        }

        public void Update(string id, Question questionIn) =>
            questions.ReplaceOne(question => question.Id == id, questionIn);

        public void Remove(Question questionIn) =>
            questions.DeleteOne(question => question.Id == questionIn.Id);

        public void Remove(string id) =>
            questions.DeleteOne(question => question.Id == id);
    }
}
