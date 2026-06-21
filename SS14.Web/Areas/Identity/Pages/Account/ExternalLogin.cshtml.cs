using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;

namespace SS14.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private static readonly ConcurrentDictionary<string, PendingRegistration> _pendingRegistrations = new();

    private readonly SignInManager<SpaceUser> _signInManager;
    private readonly UserManager<SpaceUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ExternalLoginModel> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly AccountLogManager _accountLogManager;

    public ExternalLoginModel(
        SignInManager<SpaceUser> signInManager,
        UserManager<SpaceUser> userManager,
        ILogger<ExternalLoginModel> logger,
        IEmailSender emailSender,
        ApplicationDbContext dbContext,
        AccountLogManager accountLogManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _emailSender = emailSender;
        _dbContext = dbContext;
        _accountLogManager = accountLogManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ProviderDisplayName { get; set; }

    public string ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    public bool ShowBirthdayHelp { get; set; }

    public bool EmailEditable { get; set; }

    public bool ShowVerifyCode { get; set; }

    [BindProperty]
    public string PendingRef { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(32, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, underscores and hyphens.")]
        public string UserName { get; set; }

        public string VerificationCode { get; set; }
    }

    private sealed record PendingRegistration(
        string Email,
        string UserName,
        DateTime Birthday,
        string Provider,
        string ProviderKey,
        string Code,
        DateTime ExpiresAt);

    public IActionResult OnGetAsync()
    {
        return RedirectToPage("./Login");
    }

    public IActionResult OnPost(string provider, string returnUrl = null)
    {
        // Request a redirect to the external login provider.
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToPage("./Login", new {ReturnUrl = returnUrl });
        }
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor : true);
        if (result.Succeeded)
        {
            // Update birthday from provider if available
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                var birthday = ExtractBirthdayFromClaims(info.Principal);
                if (birthday.HasValue && birthday.Value != user.Birthday)
                {
                    user.Birthday = birthday.Value;
                    await _userManager.UpdateAsync(user);
                }
            }

            _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
            return LocalRedirect(returnUrl);
        }
        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }
        else
        {
            // If the user does not have an account, then ask the user to create an account.
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;
            
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var suggestedUsername = await GenerateUniqueUsername(info);

            // Make email editable only if it's not a Yandex-provided email
            // Yandex users who log in via Google won't have @yandex.ru email
            EmailEditable = string.IsNullOrEmpty(email) || !IsYandexEmail(email);

            // Pre-check birthday to show setup link immediately if needed
            var initialBirthday = ExtractBirthdayFromClaims(info.Principal);
            ShowBirthdayHelp = !initialBirthday.HasValue;

            Input = new InputModel
            {
                Email = email,
                UserName = suggestedUsername
            };
            
            return Page();
        }
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (!ModelState.IsValid)
        {
            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        // Check if username is already taken
        var existingUser = await _userManager.FindByNameAsync(Input.UserName);
        if (existingUser != null)
        {
            ModelState.AddModelError(string.Empty, "Username is already taken.");
            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        // Extract birthday from provider claims
        var birthday = ExtractBirthdayFromClaims(info.Principal);
        if (!birthday.HasValue)
        {
            ShowBirthdayHelp = true;
            ModelState.AddModelError(string.Empty, "Could not retrieve date of birth from provider. Please ensure your profile has a valid birthday set.");
            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        // Check age requirement (must be 13 or older)
        var age = CalculateAge(birthday.Value);
        if (age < 13)
        {
            ModelState.AddModelError(string.Empty, "You must be at least 13 years old to register.");
            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        // Cap age at 18 for users 18 and older
        var storedBirthday = birthday.Value;
        if (age >= 18)
        {
            storedBirthday = DateTime.UtcNow.AddYears(-18);
        }

        // Generate and send verification code
        var code = new Random().Next(100000, 999999).ToString();
        var refId = Guid.NewGuid().ToString("N");

        _pendingRegistrations[refId] = new PendingRegistration(
            Input.Email,
            Input.UserName,
            storedBirthday,
            info.LoginProvider,
            info.ProviderKey,
            code,
            DateTime.UtcNow.AddMinutes(10));

        // Send code via email
        var subject = "Your Space Station 14 verification code";
        var htmlMessage = $@"
            <h2>Email Verification</h2>
            <p>Your verification code is: <strong style='font-size: 24px;'>{code}</strong></p>
            <p>This code will expire in 10 minutes.</p>";

        await _emailSender.SendEmailAsync(Input.Email, subject, htmlMessage);

        ProviderDisplayName = info.ProviderDisplayName;
        ReturnUrl = returnUrl;
        PendingRef = refId;
        ShowVerifyCode = true;
        return Page();
    }

    public async Task<IActionResult> OnPostVerifyCodeAsync(string returnUrl = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");

        if (string.IsNullOrEmpty(PendingRef) || !_pendingRegistrations.TryRemove(PendingRef, out var pending))
        {
            ErrorMessage = "Verification session expired. Please try again.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            ErrorMessage = "Verification code expired. Please try again.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (Input.VerificationCode != pending.Code)
        {
            ModelState.AddModelError("Input.VerificationCode", "Invalid verification code.");
            ProviderDisplayName = pending.Provider;
            ReturnUrl = returnUrl;
            PendingRef = PendingRef;
            ShowVerifyCode = true;
            return Page();
        }

        var user = new SpaceUser 
        { 
            UserName = pending.UserName, 
            Email = pending.Email,
            Birthday = pending.Birthday,
            EmailConfirmed = true,
            CreatedTime = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user);
        if (result.Succeeded)
        {
            result = await _userManager.AddLoginAsync(user, new UserLoginInfo(pending.Provider, pending.ProviderKey, pending.Provider));
            if (result.Succeeded)
            {
                _logger.LogInformation("User created an account using {Name} provider.", pending.Provider);

                await _accountLogManager.LogAndSave(
                    user,
                    new AccountLogCreated(),
                    _accountLogManager.ActorWithIP(user));

                await _signInManager.SignInAsync(user, isPersistent: false, pending.Provider);

                return LocalRedirect(returnUrl);
            }
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        ProviderDisplayName = pending.Provider;
        ReturnUrl = returnUrl;
        return Page();
    }

    private DateTime? ExtractBirthdayFromClaims(ClaimsPrincipal principal)
    {
        // Try multiple claim types that providers might use
        string[] birthdayClaimTypes = new[]
        {
            ClaimTypes.DateOfBirth,
            "birthday",
            "urn:yandex:birthday",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth",
            "http://schemas.microsoft.com/identity/claims/dateofbirth"
        };

        string bdayClaim = null;
        foreach (var claimType in birthdayClaimTypes)
        {
            bdayClaim = principal.FindFirstValue(claimType);
            if (!string.IsNullOrEmpty(bdayClaim))
                break;
        }

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

    private int CalculateAge(DateTime birthday)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - birthday.Year;
        if (birthday.Date > today.AddYears(-age)) age--;
        return age;
    }

    private static bool IsYandexEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        var domain = email.Split('@')[1].ToLowerInvariant();
        return domain == "yandex.ru"
            || domain == "ya.ru"
            || domain == "yandex.ua"
            || domain == "yandex.by"
            || domain == "yandex.kz"
            || domain == "yandex.com"
            || domain.EndsWith(".yandex.ru");
    }

    private async Task<string> GenerateUniqueUsername(ExternalLoginInfo info)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        
        // Try to get username from various sources
        var baseUsername = info.Principal.FindFirstValue(ClaimTypes.Name) 
                          ?? info.Principal.FindFirstValue("login")
                          ?? info.Principal.FindFirstValue("preferred_username")
                          ?? email?.Split('@')[0]
                          ?? "user";

        // For VK: try to get from page URL or transliterated name
        if (info.LoginProvider == "VK" || info.LoginProvider == "Vkontakte")
        {
            var vkScreen = info.Principal.FindFirstValue("screen_name");
            if (!string.IsNullOrEmpty(vkScreen))
            {
                baseUsername = vkScreen;
            }
        }

        // Sanitize username
        baseUsername = System.Text.RegularExpressions.Regex.Replace(baseUsername, @"[^a-zA-Z0-9_-]", "");
        if (baseUsername.Length < 3) baseUsername = "user";
        if (baseUsername.Length > 32) baseUsername = baseUsername.Substring(0, 32);

        // Check if base username is available
        var username = baseUsername;
        var existingUser = await _userManager.FindByNameAsync(username);
        
        // If taken, add random numbers until we find a unique one
        var random = new Random();
        while (existingUser != null)
        {
            var suffix = random.Next(1000, 9999).ToString();
            username = baseUsername;
            
            // Truncate if needed to fit suffix
            if (username.Length + suffix.Length > 32)
            {
                username = username.Substring(0, 32 - suffix.Length);
            }
            
            username += suffix;
            existingUser = await _userManager.FindByNameAsync(username);
        }

        return username;
    }
}
