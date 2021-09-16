using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace GitHubOAuth2._0
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
            services.AddRazorPages();
            services.AddAuthentication(options =>//переопределяем схему и делаем схему гита по умолчанию
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;// указываем, что будем использовтаь куки
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "GitHub";
            })
               .AddCookie()//добавляем куки
               .AddOAuth("GitHub", options =>//настройка схемы для гитхаба
               {
                   options.ClientId = Configuration["GitHub:ClientId"];
                   options.ClientSecret = Configuration["GitHub:ClientSecret"];
                   options.CallbackPath = new PathString("/github-oauth");
                   options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                   options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                   options.UserInformationEndpoint = "https://api.github.com/user";
                   options.SaveTokens = true; //гарантирует, что токены будут сохраняться после завершения каждого запроса.
                   options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                   options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");//Обработчику auth также нужны действия с утверждениями, чтобы понять, какую информацию нужно ввести.
                   options.ClaimActions.MapJsonKey("urn:github:login", "login");//Обработчику auth также нужны действия с утверждениями, чтобы понять, какую информацию нужно ввести.
                   options.ClaimActions.MapJsonKey("urn:github:url", "html_url");//Обработчику auth также нужны действия с утверждениями, чтобы понять, какую информацию нужно ввести.
                   options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");//Обработчику auth также нужны действия с утверждениями, чтобы понять, какую информацию нужно ввести.
                   options.Events = new OAuthEvents
                   {
                       OnCreatingTicket = async context =>
                       {
                           var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                           request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                           request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                           var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                           response.EnsureSuccessStatusCode();
                           var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                           context.RunClaimActions(json.RootElement);
                       }
                   };
               });
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
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

                 app.UseAuthorization();
            //app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });
        }
    }
}
