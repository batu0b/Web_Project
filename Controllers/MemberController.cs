using Microsoft.AspNetCore.Mvc;

public class MemberController : Controller
{
    public IActionResult MemberDashboard()
    {
        if (HttpContext.Session.GetString("UserId") == null)
        {
            return RedirectToAction("Login", "User");
        }
        return View();
    }
}
