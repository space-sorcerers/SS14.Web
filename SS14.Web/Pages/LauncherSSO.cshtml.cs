using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Pages;

[AllowAnonymous]
public class LauncherSSOModel : PageModel
{
    private readonly SignInManager<SpaceUser> _signInManager;
    private readonly UserManager<SpaceUser> _userManager;

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();
    public bool ShowProviderButtons { get; set; } = true;
    public string SsoCode { get; set; } = "";

    public LauncherSSOModel(
        SignInManager<SpaceUser> signInManager,
        UserManager<SpaceUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(string code = null)
    {
        if (!string.IsNullOrEmpty(code))
        {
            SsoCode = code;
            ShowProviderButtons = false;
            return Page();
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        ShowProviderButtons = ExternalLogins.Count > 0;
        return Page();
    }

    public IActionResult OnPostExternalLogin(string provider)
    {
        var redirectUrl = Url.Page("./LauncherSSO", pageHandler: "Callback");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string remoteError = null)
    {
        if (remoteError != null)
        {
            return RedirectToPage("./LauncherSSO", new { error = remoteError });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToPage("./LauncherSSO", new { error = "Could not load external login info." });
        }

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                var code = SsoCodeStore.GenerateCode(user.Id);
                return RedirectToPage("./LauncherSSO", new { code });
            }
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./LauncherSSO", new { error = "Account is locked." });
        }

        return RedirectToPage("./LauncherSSO", new { error = "No account linked to this provider. Please sign up on the website first." });
    }
}
