namespace Spacebook.Controllers
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;
    using Microsoft.AspNetCore.Mvc;

    using Spacebook.Models;
    using Spacebook.Data;

    public class AuthController : Controller
    {
        private readonly SignInManager<SpacebookUser> signInManager;
        private readonly UserManager<SpacebookUser> userManager;
        private readonly IUserStore<SpacebookUser> userStore;
        private readonly IUserEmailStore<SpacebookUser> emailStore;
        private readonly ILogger<AuthController> logger;

        public AuthController(SignInManager<SpacebookUser> signInManager, UserManager<SpacebookUser> userManager, ILogger<AuthController> logger, IUserStore<SpacebookUser> userStore)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.logger = logger;
            this.userStore = userStore;
            this.emailStore = GetEmailStore();
        }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public IActionResult AccessDenied()
        {
            return this.View();
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Login model, string? returnUrl = null)
        {
            returnUrl ??= this.Url.Content("~/");

            if (this.ModelState.IsValid)
            {
                var user = await this.signInManager.UserManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    this.ModelState.AddModelError(string.Empty, "Invalid email address.");
                    return this.View("Index");
                }

                var result = await this.signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                if (!result.Succeeded)
                {
                    this.ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return this.View("Index");
                }

                return this.LocalRedirect(returnUrl);
            }

            return this.View("Index");
        }

        public IActionResult Register()
        {
            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Register model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await userStore.SetUserNameAsync(user, model.Username, CancellationToken.None);
                await emailStore.SetEmailAsync(user, model.Email, CancellationToken.None);

                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            } 
            else 
            {
                this.ModelState.AddModelError(string.Empty, "Invalid registration attempt.");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            await this.signInManager.SignOutAsync();
            this.logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return this.LocalRedirect(returnUrl);
            }
            else
            {
                return this.RedirectToPage("Index");
            }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(this.ErrorMessage))
            {
                this.ModelState.AddModelError(string.Empty, this.ErrorMessage);
            }

            returnUrl ??= this.Url.Content("~/");

            await this.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            this.ReturnUrl = returnUrl;
        }

        private SpacebookUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<SpacebookUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(SpacebookUser)}'. " +
                    $"Ensure that '{nameof(SpacebookUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<SpacebookUser> GetEmailStore()
        {
            if (!this.userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<SpacebookUser>)userStore;
        }
    }
}
