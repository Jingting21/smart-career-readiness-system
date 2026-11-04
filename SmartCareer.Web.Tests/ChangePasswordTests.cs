using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartCareer.Web.Areas.Identity.Pages.Account.Manage;
using SmartCareer.Web.Data;
using SmartCareer.Web.Models;

namespace SmartCareer.Web.Tests;

public sealed class ChangePasswordTests
{
    [Fact]
    public async Task SameCurrentAndNewPasswordIsRejectedWithoutUpdatingPassword()
    {
        await using var fixture = await Fixture.CreateAsync();
        var model = fixture.CreateModel("Current#Pass1", "Current#Pass1");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState["Input.NewPassword"]!.Errors,
            error => error.ErrorMessage == "Your new password must be different from your current password.");
        Assert.Null(model.StatusMessage);
        Assert.True(await fixture.UserManager.CheckPasswordAsync(fixture.User, "Current#Pass1"));
        Assert.Equal(0, fixture.SignInManager.RefreshCount);
    }

    [Fact]
    public async Task IncorrectCurrentPasswordIsRejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        var model = fixture.CreateModel("Incorrect#Pass1", "Different#Pass2");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState["Input.OldPassword"]!.Errors,
            error => error.ErrorMessage == "The current password is incorrect.");
        Assert.True(await fixture.UserManager.CheckPasswordAsync(fixture.User, "Current#Pass1"));
        Assert.Equal(0, fixture.SignInManager.RefreshCount);
    }

    [Fact]
    public async Task ValidDifferentPasswordIsAccepted()
    {
        await using var fixture = await Fixture.CreateAsync();
        var model = fixture.CreateModel("Current#Pass1", "Different#Pass2");

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Your password has been changed successfully.", model.StatusMessage);
        Assert.True(await fixture.UserManager.CheckPasswordAsync(fixture.User, "Different#Pass2"));
        Assert.False(await fixture.UserManager.CheckPasswordAsync(fixture.User, "Current#Pass1"));
        Assert.Equal(1, fixture.SignInManager.RefreshCount);
    }

    [Fact]
    public async Task IdentityPasswordComplexityIsStillEnforced()
    {
        await using var fixture = await Fixture.CreateAsync();
        var model = fixture.CreateModel("Current#Pass1", "alllowercase");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(await fixture.UserManager.CheckPasswordAsync(fixture.User, "Current#Pass1"));
        Assert.Equal(0, fixture.SignInManager.RefreshCount);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider provider;
        private readonly IServiceScope scope;
        public UserManager<ApplicationUser> UserManager { get; }
        public TestSignInManager SignInManager { get; }
        public ApplicationUser User { get; }

        private Fixture(ServiceProvider provider, IServiceScope scope, UserManager<ApplicationUser> userManager,
            TestSignInManager signInManager, ApplicationUser user)
        {
            this.provider = provider; this.scope = scope; UserManager = userManager; SignInManager = signInManager; User = user;
        }

        public static async Task<Fixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase($"ChangePassword-{Guid.NewGuid():N}"));
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8; options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true; options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            }).AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager();
            var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = "student@example.test", Email = "student@example.test", FullName = "Student" };
            Assert.True((await manager.CreateAsync(user, "Current#Pass1")).Succeeded);
            var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            var signIn = new TestSignInManager(manager, new HttpContextAccessor { HttpContext = httpContext },
                new UserClaimsPrincipalFactory<ApplicationUser>(manager, Options.Create(new IdentityOptions())),
                Options.Create(new IdentityOptions()), scope.ServiceProvider.GetRequiredService<ILogger<SignInManager<ApplicationUser>>>(),
                new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())), new DefaultUserConfirmation<ApplicationUser>());
            return new Fixture(provider, scope, manager, signIn, user);
        }

        public ChangePasswordModel CreateModel(string oldPassword, string newPassword)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, User.Id)], "Test"));
            return new ChangePasswordModel(UserManager, SignInManager)
            {
                Input = new ChangePasswordModel.InputModel { OldPassword = oldPassword, NewPassword = newPassword, ConfirmPassword = newPassword },
                PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } }
            };
        }

        public async ValueTask DisposeAsync() { scope.Dispose(); await provider.DisposeAsync(); }
    }

    private sealed class TestSignInManager(
        UserManager<ApplicationUser> userManager, IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger, IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : SignInManager<ApplicationUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        public int RefreshCount { get; private set; }
        public override Task RefreshSignInAsync(ApplicationUser user) { RefreshCount++; return Task.CompletedTask; }
    }
}
