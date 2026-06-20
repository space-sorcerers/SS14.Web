using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class ExternalLoginsModel : PageModel
{
    private readonly UserManager<SpaceUser> _userManager;
    private readonly SignInManager<SpaceUser> _signInManager;

    public ExternalLoginsModel(
        UserManager<SpaceUser> userManager,
        SignInManager<SpaceUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public IList<UserLoginInfo> CurrentLogins { get; set; }

    public IList<AuthenticationScheme> OtherLogins { get; set; }

    public bool ShowRemoveButton { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID 'user.Id'.");
        }

        CurrentLogins = await _userManager.GetLoginsAsync(user);
        OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
            .Where(auth => CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
            .ToList();
        ShowRemoveButton = user.PasswordHash != null || CurrentLogins.Count > 1;
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID 'user.Id'.");
        }

        var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
        if (!result.Succeeded)
        {
            StatusMessage = "The external login was not removed.";
            return RedirectToPage();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "The external login was removed.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
    {
        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        // Request a redirect to the external login provider to link a login for the current user
        var redirectUrl = Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID 'user.Id'.");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync(user.Id.ToString());
        if (info == null)
        {
            throw new InvalidOperationException($"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
        }

        // Update birthday from VK/Yandex if available
        if (info.LoginProvider is "VK" or "Yandex")
        {
            var birthday = ExtractBirthdayFromClaims(info.Principal);
            if (birthday.HasValue && birthday.Value != user.Birthday)
            {
                user.Birthday = birthday.Value;
                await _userManager.UpdateAsync(user);
            }
        }

        var result = await _userManager.AddLoginAsync(user, info);
        if (!result.Succeeded)
        {
            StatusMessage = "The external login was not added. External logins can only be associated with one account.";
            return RedirectToPage();
        }

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        StatusMessage = "The external login was added.";
        return RedirectToPage();
    }

    private DateTime? ExtractBirthdayFromClaims(ClaimsPrincipal principal)
    {
        var bdayClaim = principal.FindFirstValue(ClaimTypes.DateOfBirth);
        if (string.IsNullOrEmpty(bdayClaim))
            return null;

        // Try various date formats
        // VK format: "d.M.yyyy" (e.g., "15.6.1990")
        // Yandex format: "yyyy-MM-dd" (e.g., "1990-06-15")
        
        string[] formats = new[]
        {
            "yyyy-MM-dd",
            "d.M.yyyy",
            "dd.MM.yyyy",
            "M/d/yyyy",
            "MM/dd/yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(bdayClaim, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }
        }

        // Fallback: try generic parse
        if (DateTime.TryParse(bdayClaim, out var genericDate))
        {
            return DateTime.SpecifyKind(genericDate, DateTimeKind.Utc);
        }

        return null;
    }
}