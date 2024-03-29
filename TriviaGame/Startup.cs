using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TriviaGame.Models;
using TriviaGame.Services;

namespace TriviaGame
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<QuestionDatabaseSettings>(Configuration.GetSection(nameof(QuestionDatabaseSettings)));
            services.AddSingleton<IQuestionDatabaseSettings>(sp => sp.GetRequiredService<IOptions<QuestionDatabaseSettings>>().Value);
            services.AddSingleton<QuestionService>();

            services.Configure<GameSettings>(Configuration.GetSection(nameof(GameSettings)));
            services.AddSingleton<IGameSettings>(sp => sp.GetRequiredService<IOptions<GameSettings>>().Value);
            services.AddSingleton<GameService>();

            services.AddSignalR();

            services.AddControllersWithViews();

            //services.AddSingleton()
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHub<Hubs.GameHub>("/gamehub");
            });
        }
    }
}
