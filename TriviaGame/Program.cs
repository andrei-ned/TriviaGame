using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;

namespace TriviaGame
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //// test
            //var client = new MongoClient("mongodb+srv://darn:3mnhrCLF91iwa4u5@cluster0.6cmib.mongodb.net/test?retryWrites=true&w=majority");

            //var database = client.GetDatabase("test");

            //var collection = database.GetCollection<BsonDocument>("questions");
            //collection.in

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
