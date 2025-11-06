using EWOS_MVC.Models;
using EWOS_MVC.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

public class WindowsAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WindowsAuthMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(1);

    public WindowsAuthMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<WindowsAuthMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var userName = context.User.Identity?.Name;

        if (!string.IsNullOrEmpty(userName))
        {
            var userNameOnly = userName.Split('\\').Last();
            var cacheKey = $"User_{userNameOnly}";

            //cari user berdasarkan windaws autentication
            var user = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                return await db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserName == userNameOnly && u.IsActive);
            });

            //jika tidak ada ambil dari AD service dan tambahkan ke DB
            if (user == null)
            {
                using var scope = _scopeFactory.CreateScope();
                var adUserService = scope.ServiceProvider.GetRequiredService<AdUserService>();

                try
                {
                    //composition dari dir /Services/AdUserServices.cs
                    var adUser = adUserService.GetUserInfo(userNameOnly);
                    if (adUser != null)
                    {
                        //tambahkan user
                        user = new UserModel
                        {
                            UserName = userNameOnly,
                            Name = adUser.DisplayName ?? userNameOnly,
                            Email = adUser.EmailAddress,
                            Badge = adUser.Description,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        db.Users.Add(user);
                        await db.SaveChangesAsync();

                        //tambah role defauld
                        var defaultRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Requestor");
                        if (defaultRole != null)
                        {
                            db.UserRoles.Add(new UserRoleModel
                            {
                                UserId = user.Id,
                                RoleId = defaultRole.Id
                            });
                            await db.SaveChangesAsync();
                        }

                        //reload user dan role
                        user = await db.Users
                            .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                            .FirstAsync(u => u.Id == user.Id);

                        _cache.Set(cacheKey, user, _cacheDuration);
                        _logger.LogInformation("User {UserName} dibuat otomatis dari AD.", userNameOnly);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gagal membaca user dari AD untuk {UserName}", userNameOnly);
                }
            }

            //jika ada user, cek role dan tambahkan ke claims
            if (user != null)
            {
                context.Items["CurrentUser"] = user;

                var identity = new ClaimsIdentity(NegotiateDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));

                foreach (var role in user.UserRoles.Select(ur => ur.Role.Name).Distinct())
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));

                context.User = new ClaimsPrincipal(identity);
            }
        }

        await _next(context);
    }
}
