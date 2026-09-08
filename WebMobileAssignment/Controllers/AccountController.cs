using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using static WebMobileAssignment.Models.MemberReviewVM;

namespace WebMobileAssignment.Controllers;

public class AccountController(DB db, Helper hp) : Controller
{
    // 1. REMOTE VALIDATION
    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }

    // 2. LOGIN & LOGOUT
    public IActionResult Login(string? returnURL)
    {
        ViewBag.ReturnURL = returnURL;
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        var u = db.Users.FirstOrDefault(x => x.Email == vm.Email);

        if (u == null || !hp.VerifyPassword(u.PasswordHash, vm.Password))
        {
            ModelState.AddModelError("", "Login credentials do not match.");
        }
        else if (u is Member m && m.IsBlocked)
        {
            ModelState.AddModelError("", "This account is blocked.");
        }

        if (ModelState.IsValid)
        {
            TempData["Info"] = "Login successfully.";
            hp.SignIn(u!.Email, u.Role, vm.RememberMe);

            if (!string.IsNullOrEmpty(returnURL) && Url.IsLocalUrl(returnURL))
            {
                return Redirect(returnURL);
            }

            // Redirect Admin to the Admin portal, Member to the Anime catalog
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
        TempData["Info"] = "Logout successfully.";
        hp.SignOut();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // 3. REGISTRATION
    public IActionResult Register() => View();

    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        if (db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Email already in use.");
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
            db.SaveChanges();

            TempData["Info"] = "Register successfully. Please login.";
            return RedirectToAction("Login");
        }

        return View(vm);
    }

    // 4. PASSWORD RESET
    public IActionResult ResetPassword() => View();

    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        var u = db.Users.FirstOrDefault(x => x.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email address not found.");
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
                TempData["Info"] = "A temporary password has been sent to your email.";
            }
            catch
            {
                TempData["Info"] = $"Password reset to <b>{randomPass}</b> (Email proxy failed).";
            }

            return RedirectToAction("Login");
        }

        return View(vm);
    }

    // 5. USER PROFILE & PASSWORD MANAGEMENT
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
            TempData["Info"] = "Profile updated successfully.";
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
            ModelState.AddModelError("Current", "Current password is incorrect.");
        }

        if (ModelState.IsValid)
        {
            u.PasswordHash = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = "Password updated successfully.";
            return RedirectToAction("Profile");
        }

        return View(vm);
    }
}