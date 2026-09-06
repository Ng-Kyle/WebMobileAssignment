using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace WebMobileAssignment.Controllers;

public class LanguageController : Controller
{
    public IActionResult Index(string culture)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture))
        );
        return Redirect(Request.Headers.Referer.ToString());
    }
}
