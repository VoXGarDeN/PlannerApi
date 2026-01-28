using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PlannerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private class AppUser
        {
            public string UserName { get; set; } = "";
            public string PasswordHash { get; set; } = "";
        }

        private static readonly List<AppUser> _users;
        private static readonly PasswordHasher<AppUser> _hasher = new PasswordHasher<AppUser>();

        static AccountController()
        {
            _users = new List<AppUser>();

            var admin = new AppUser { UserName = "admin" };
            admin.PasswordHash = _hasher.HashPassword(admin, "admin");
            _users.Add(admin);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AllowAnonymous]
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return Content(@"<!DOCTYPE html>
<html lang='ru'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Вход в Planner</title>
    <style>
        :root { --primary:#6366f1; --primary-hover:#4f46e5; --bg: linear-gradient(135deg,#667eea 0%,#764ba2 100%); }
        *{box-sizing:border-box;margin:0;padding:0;font-family:Inter,system-ui,-apple-system,Segoe UI,Roboto,Arial;}
        body{background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);min-height:100vh;display:flex;align-items:center;justify-content:center;padding:20px;}
        .card{background:#fff;border-radius:20px;max-width:420px;width:100%;box-shadow:0 20px 60px rgba(0,0,0,0.12);overflow:hidden;}
        .header{padding:36px;text-align:center;background:linear-gradient(180deg,rgba(99,102,241,0.06),transparent);}
        .logo{width:64px;height:64px;border-radius:14px;background:var(--primary);display:inline-flex;align-items:center;justify-content:center;color:#fff;font-weight:800;font-size:28px;margin-bottom:12px;box-shadow:0 8px 20px rgba(79,70,229,0.18);}
        h1{font-size:22px;color:#111827;margin-bottom:6px}
        p.lead{color:#6b7280;font-size:14px}
        .body{padding:26px;}
        label{display:block;font-weight:600;font-size:12px;color:#4b5563;margin-bottom:6px}
        input{width:100%;padding:12px 14px;border-radius:10px;border:2px solid #f3f4f6;background:#f9fafb;font-size:15px}
        input:focus{outline:none;border-color:var(--primary);box-shadow:0 0 0 6px rgba(99,102,241,0.06);background:#fff}
        .btn{width:100%;padding:14px;border-radius:12px;border:none;background:var(--primary);color:#fff;font-weight:700;font-size:15px;margin-top:14px;cursor:pointer;transition:transform .15s,box-shadow .15s}
        .btn:active{transform:translateY(1px)}
        .error{display:none;background:#fff5f5;border:1px solid #fee2e2;color:#b91c1c;padding:10px;border-radius:8px;margin-bottom:12px;text-align:center}
        .footer{padding:18px;text-align:center;background:#f8fafc;font-size:13px;color:#6b7280}
    </style>
</head>
<body>
    <div class='card' role='main'>
        <div class='header'>
            <div class='logo'>P</div>
            <h1>Planner System</h1>
            <p class='lead'>Вход в аккаунт</p>
        </div>

        <div class='body'>
            <div id='error' class='error'>Неверный логин или пароль</div>

            <form id='loginForm' method='post' action='/Account/Login'>
                <label for='username'>ЛОГИН</label>
                <input id='username' name='username' type='text' placeholder='' autocomplete='username' required />

                <label for='password' style='margin-top:12px'>ПАРОЛЬ</label>
                <input id='password' name='password' type='password' placeholder='••••••••' autocomplete='current-password' required />

                <button id='btn' class='btn' type='submit'>Войти</button>
            </form>
        </div>

        <div class='footer'>
            Войдите как <strong>admin</strong> для демо. Пароли хранятся в защищённом виде (хеш).
        </div>
    </div>

    <script>
        (function(){
            var form = document.getElementById('loginForm');
            var btn = document.getElementById('btn');
            var err = document.getElementById('error');
            if (window.location.search.indexOf('error') !== -1) { err.style.display = 'block'; }
            form.addEventListener('submit', function(){
                btn.disabled = true;
                btn.textContent = 'Проверка...';
                btn.style.opacity = '0.85';
            });
        })();
    </script>
</body>
</html>", "text/html; charset=utf-8");
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {
            var user = _users.FirstOrDefault(u => string.Equals(u.UserName, username, StringComparison.OrdinalIgnoreCase));
            if (user != null)
            {
                var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
                if (verify == PasswordVerificationResult.Success || verify == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    if (verify == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        user.PasswordHash = _hasher.HashPassword(user, password);
                    }

                    var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.UserName), new Claim(ClaimTypes.Role, "Admin") };
                    var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var props = new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(ci), props);

                    // ИЗМЕНЕНО: Перенаправление на главную страницу дашборда
                    return Redirect("/");
                }
            }

            return Content(@"<!doctype html><html lang='ru'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Ошибка</title>
<style>body{display:flex;align-items:center;justify-content:center;height:100vh;background:linear-gradient(135deg,#667eea,#764ba2);font-family:Inter,system-ui,-apple-system;color:#111} .card{background:#fff;padding:36px;border-radius:14px;max-width:420px;text-align:center;box-shadow:0 20px 60px rgba(0,0,0,0.12)} .bar{height:4px;background:#ef4444;width:0%;animation:load 2s linear forwards;border-radius:2px;margin-top:18px}@keyframes load{from{width:0}to{width:100%}}</style>
<script>setTimeout(function(){location.href='/Account/Login?error=true'},2000)</script>
</head><body><div class='card'><h2>Ошибка входа</h2><p>Неверные данные. Через пару секунд вы вернётесь на страницу входа.</p><div class='bar'></div></div></body></html>", "text/html; charset=utf-8");
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Account/Login");
        }
    }
}