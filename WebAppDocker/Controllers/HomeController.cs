using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Diagnostics;
using WebAppDocker.Models;

namespace WebAppDocker.Controllers
{
    [Authorize()]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Online([FromServices] IConnectionMultiplexer connection)
        {
            List<Logins> logins = [];
            RedisValue[] users = await connection.GetDatabase()
                .SetMembersAsync("users_with_sessions");
            foreach (var user in users)
            {
                logins.Add(new Logins(user.ToString()));
            }
            return View(logins);
        }

        [HttpPost]
        public async Task<IActionResult> ForceLogout([FromServices] IConnectionMultiplexer connection, string userId)
        {
            var _db = connection.GetDatabase();
            var userKey = $"user_sessions:{userId}";
            var sessions = await _db.SetMembersAsync(userKey);
            foreach (var session in sessions)
            {
                await _db.KeyDeleteAsync(session.ToString());
            }
            await _db.KeyDeleteAsync(userKey);
            await _db.SetRemoveAsync("users_with_sessions", userId);
            await _db.KeyDeleteAsync($"auth:{userId}");
            return RedirectToAction(nameof(Online));
        }
    }
}