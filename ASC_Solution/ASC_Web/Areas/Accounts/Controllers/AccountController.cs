using ASC.Model.BaseTypes;
using ASC.Utilities;
using ASC_Web.Areas.Accounts.Models;
using ASC_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq; // Thêm namespace này để sử dụng SingleOrDefault

namespace ASC_Web.Areas.Accounts.Controllers
{
    [Authorize]
    [Area("Accounts")]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, IEmailSender emailSender, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ServiceEngineers()
        {
            var serviceEngineers = await _userManager.GetUsersInRoleAsync(Roles.Engineer.ToString());
            // Hold all service engineers in session
            HttpContext.Session.SetSession("ServiceEngineers", serviceEngineers);

            return View(new ServiceEngineerViewModel
            {
                ServiceEngineers = serviceEngineers == null ? null : serviceEngineers.ToList(),
                Registration = new ServiceEngineerRegistrationViewModel() { IsEdit = false }
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServiceEngineers(ServiceEngineerViewModel serviceEngineer)
        {
            if (!ModelState.IsValid)
            {
                return View(serviceEngineer);
            }

            if (serviceEngineer.Registration.IsEdit)
            {
                // Update User
                var user = await _userManager.FindByEmailAsync(serviceEngineer.Registration.Email);
                user.UserName = serviceEngineer.Registration.UserName;
                IdentityResult result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded) // Sửa logic: hiển thị lỗi khi thất bại
                {
                    result.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }

                // Update Password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                IdentityResult passwordResult = await _userManager.ResetPasswordAsync(user, token, serviceEngineer.Registration.Password);
                if (!passwordResult.Succeeded)
                {
                    passwordResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }

                // Update claims
                user = await _userManager.FindByEmailAsync(serviceEngineer.Registration.Email);
                var claims = await _userManager.GetClaimsAsync(user); // Sửa: sử dụng _userManager.GetClaimsAsync
                var isActiveClaim = claims.SingleOrDefault(p => p.Type == "IsActive"); // Sửa: sử dụng claims thay vì Identity
                if (isActiveClaim != null) // Thêm kiểm tra null
                {
                    var removeClaimResult = await _userManager.RemoveClaimAsync(user, new System.Security.Claims.Claim(isActiveClaim.Type, isActiveClaim.Value));
                    if (!removeClaimResult.Succeeded)
                    {
                        removeClaimResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                        return View(serviceEngineer);
                    }
                }
                var addClaimResult = await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("IsActive", serviceEngineer.Registration.IsActive.ToString()));
                if (!addClaimResult.Succeeded)
                {
                    addClaimResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }
            }
            else
            {
                // Create User
                IdentityUser user = new IdentityUser
                {
                    UserName = serviceEngineer.Registration.UserName,
                    Email = serviceEngineer.Registration.Email,
                    EmailConfirmed = true
                };
                IdentityResult result = await _userManager.CreateAsync(user, serviceEngineer.Registration.Password);
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", serviceEngineer.Registration.Email));
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("IsActive", serviceEngineer.Registration.IsActive.ToString()));
                if (!result.Succeeded)
                {
                    result.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }

                // Assign user to Engineer Role
                var roleResult = await _userManager.AddToRoleAsync(user, Roles.Engineer.ToString());
                if (!roleResult.Succeeded)
                {
                    roleResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }

                if (bool.TryParse(serviceEngineer.Registration.IsActive, out bool isActive) && isActive)
                {
                    await _emailSender.SendEmailAsync(serviceEngineer.Registration.Email, "Account Created/Modified", $"Account Created/Modified \n Password: {serviceEngineer.Registration.Password}");
                }
                else
                {
                    await _emailSender.SendEmailAsync(serviceEngineer.Registration.Email, "Account Deactivated", $"Your account has been deactivated.");
                }
            }

            return RedirectToAction("ServiceEngineers");
        }
    }
}