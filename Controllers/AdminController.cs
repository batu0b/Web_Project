using Microsoft.AspNetCore.Mvc;

public class AdminController : Controller
{
    public IActionResult AdminDashboard()
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToAction("Login", "User");
        }
        return View();
    }

}
