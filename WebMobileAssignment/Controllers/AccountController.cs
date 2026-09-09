using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using System.Net.Mail;
using static WebMobileAssignment.Models.MemberReviewVM;

namespace WebMobileAssignment.Controllers;

public class AccountController(DB db, Helper hp, IStringLocalizer<SharedResource> localizer) : Controller
{

    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }
    public bool CheckUserName(string userName)
    {
        return !db.Users.Any(u => u.UserName == userName);
    }


    public IActionResult Login(string? returnURL)
    {
        ViewBag.ReturnURL = returnURL;
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        var u = db.Users.FirstOrDefault(x => x.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("", localizer["Login credentials do not match."]);
        }

        else if (u.LockoutExpiry != null && u.LockoutExpiry > DateTime.Now)
        {
            var remainingMinutes = Math.Ceiling((u.LockoutExpiry.Value - DateTime.Now).TotalMinutes);
            ModelState.AddModelError("", localizer["Account is temporarily locked due to multiple failed login attempts. Try again in {0} minute(s).",remainingMinutes]);
        }

        else if (u is Member m && m.IsBlocked)
        {
            ModelState.AddModelError("", localizer["This account is blocked."]);
        }

        else if (!hp.VerifyPassword(u.PasswordHash, vm.Password))
        {
            u.FailedLoginAttempts++;

            if (u.FailedLoginAttempts >= 3)
            {
                u.LockoutExpiry = DateTime.Now.AddMinutes(5);
                db.SaveChanges();
                ModelState.AddModelError("", localizer["Too many failed attempts. Your account has been locked for 5 minutes."]);
            }
            else
            {
                db.SaveChanges();
                ModelState.AddModelError("", localizer["Login credentials do not match. Attempt {0} of 3.", u.FailedLoginAttempts]);
            }
        }

        if (ModelState.IsValid)
        {
            u!.FailedLoginAttempts = 0;
            u.LockoutExpiry = null;
            db.SaveChanges();

            TempData["Info"] = localizer["Login successfully."].Value;
            hp.SignIn(u.Email, u.Role, vm.RememberMe);

            if (!string.IsNullOrEmpty(returnURL) && Url.IsLocalUrl(returnURL))
            {
                return Redirect(returnURL);
            }

            if (u.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Anime");
        }

        ViewBag.ReturnURL = returnURL;
        return View(vm);
    }

    public IActionResult Logout()
    {
        TempData["Info"] = localizer["Logout successfully."].Value;
        hp.SignOut();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVM vm)
    {
        string? captchaResponse = Request.Form["g-recaptcha-response"];
        bool isCaptchaValid = await VerifyCaptchaAsync(captchaResponse);

        if (!isCaptchaValid)
        {
            ModelState.AddModelError("", localizer["Please verify that you are not a robot."]);
        }

        if (db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", localizer["Email already in use."]);
        }

        if (db.Users.Any(u => u.UserName == vm.UserName))
        {
            ModelState.AddModelError("UserName", localizer["Username already in use."]);
        }

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (!string.IsNullOrEmpty(err)) ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            string? photoName = null;
            if (vm.Photo != null)
            {
                photoName = hp.SavePhoto(vm.Photo, "images/member");
            }

            var member = new Member
            {
                UserName = vm.UserName.Trim(),
                Email = vm.Email.Trim(),
                PasswordHash = hp.HashPassword(vm.Password),
                ImageLink = photoName,
                IsBlocked = false,
                WatchList = new WatchList()
            };

            db.Members.Add(member);
            await db.SaveChangesAsync();

            TempData["Info"] = localizer["Register successfully. Please login."].Value;
            return RedirectToAction("Login");
        }

        return View(vm);
    }

    public IActionResult ResetPassword() => View();

    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        var u = db.Users.FirstOrDefault(x => x.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", localizer["Email address not found."]);
        }

        if (ModelState.IsValid)
        {
            string randomPass = hp.RandomPassword();
            u!.PasswordHash = hp.HashPassword(randomPass);
            db.SaveChanges();

            var mail = new MailMessage();
            mail.To.Add(new MailAddress(u.Email, u.UserName));
            mail.Subject = "Your Account Password Has Been Reset";
            mail.IsBodyHtml = true;
            mail.Body = $@"
                <h3>Hello {u.UserName},</h3>
                <p>Your password has been temporarily reset to:</p>
                <h2 style='color:red;'>{randomPass}</h2>
                <p>Please log in and change your password immediately.</p>";

            try
            {
                hp.SendEmail(mail);
                TempData["Info"] = localizer["A temporary password has been sent to your email."].Value;
            }
            catch
            {
                TempData["Info"] = localizer["Password reset to <b>{0}</b> (Email proxy failed).",randomPass].Value;
            }

            return RedirectToAction("Login");
        }

        return View(vm);
    }

    [Authorize]
    public IActionResult Profile()
    {
        var u = db.Users.FirstOrDefault(x => x.Email == User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        return View(u);
    }

    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        var m = db.Members.FirstOrDefault(x => x.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Email = m.Email,
            UserName = m.UserName,
            ImageLink = m.ImageLink
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        var m = db.Members.FirstOrDefault(x => x.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (!string.IsNullOrEmpty(err)) ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            m.UserName = vm.UserName.Trim();

            if (vm.Photo != null)
            {
                if (!string.IsNullOrEmpty(m.ImageLink))
                {
                    hp.DeletePhoto(m.ImageLink, "images/member");
                }
                m.ImageLink = hp.SavePhoto(vm.Photo, "images/member");
            }

            db.SaveChanges();
            TempData["Info"] = localizer["Profile updated successfully."].Value;
            return RedirectToAction("Profile");
        }

        vm.Email = m.Email;
        vm.ImageLink = m.ImageLink;
        return View(vm);
    }

    [Authorize]
    public IActionResult UpdatePassword() => View();

    [HttpPost]
    [Authorize]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        var u = db.Users.FirstOrDefault(x => x.Email == User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        if (!hp.VerifyPassword(u.PasswordHash, vm.Current))
        {
            ModelState.AddModelError("Current", localizer["Current password is incorrect."]);
        }

        if (ModelState.IsValid)
        {
            u.PasswordHash = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = localizer["Password updated successfully."].Value;
            return RedirectToAction("Profile");
        }

        return View(vm);
    }
    private async Task<bool> VerifyCaptchaAsync(string? responseToken)
    {
        if (string.IsNullOrEmpty(responseToken)) return false;

        const string secretKey = "6Lft27EtAAAAAHJF2hmbM_2HF6sV4yH5NT6156gx";
        using var client = new HttpClient();

        var response = await client.PostAsync(
            $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={responseToken}",
            null
        );

        if (!response.IsSuccessStatusCode) return false;

        var jsonString = await response.Content.ReadAsStringAsync();
        return jsonString.Contains("\"success\": true");
    }
}