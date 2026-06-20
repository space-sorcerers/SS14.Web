using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class SetPasswordModel : PageModel
{
    private readonly UserManager<SpaceUser> _userManager;
    private readonly SpaceUserManager _spaceUserManager;

    public SetPasswordModel(
        UserManager<SpaceUser> userManager,
        SpaceUserManager spaceUserManager)
    {
        _userManager = userManager;
        _spaceUserManager = spaceUserManager;
    }

    [TempData]
    public string StatusMessage { get; set; }

    public string CurrentCode { get; set; } = "";
    public long RemainingSeconds { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

        if (string.IsNullOrEmpty(user.LegacyPassKey))
        {
            user.LegacyPassKey = TotpHelper.GenerateSecret();
            await _spaceUserManager.UpdateAsync(user);
        }

        CurrentCode = TotpHelper.GenerateCode(user.LegacyPassKey);
        RemainingSeconds = TotpHelper.GetRemainingSeconds();
        return Page();
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnGetRefreshCodeAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.LegacyPassKey))
            return Content("");

        var code = TotpHelper.GenerateCode(user.LegacyPassKey);
        return Content(code);
    }

    public async Task<IActionResult> OnPostRegenerateAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

        user.LegacyPassKey = TotpHelper.GenerateSecret();
        await _spaceUserManager.UpdateAsync(user);

        StatusMessage = "New secret generated.";
        return RedirectToPage();
    }
}
