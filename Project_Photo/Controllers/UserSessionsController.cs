using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Project_Photo.Models;
using Project_Photo.Services;
using Project_Photo.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Photo.Controllers
{
    public class UserSessionsController : Controller
    {
        private readonly AAContext _context;
        private readonly ILogger<UserSessionsController> _logger;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;

        public UserSessionsController(
            AAContext context,
            ILogger<UserSessionsController> logger,
            IEmailService emailService,
            IWebHostEnvironment hostEnvironment,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
        }

        // GET: UserSessions/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // 如果已經登入，直接導向首頁
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: UserSessions/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 查詢使用者 - 可以用帳號或Email登入
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u =>
                    (u.Account == model.AccountOrEmail || u.Email == model.AccountOrEmail)
                  && u.AccountStatus == "Active"
                  && u.IsDeleted == false);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "帳號或密碼錯誤，或帳號已被停用");
                return View(model);
            }

            // 驗證密碼
            // TODO: 之後改用 BCrypt 或其他加密方式
            if (user.Password != model.Password)
            {
                ModelState.AddModelError(string.Empty, "帳號或密碼錯誤");
                return View(model);
            }

            // 建立 UserSession 記錄
            var userSession = new UserSession
            {
                UserId = user.UserId,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                IsActive = true,
                LastActivityAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddHours(model.RememberMe ? 720 : 2), // 記住我：30天，否則2小時
                CreatedAt = DateTime.Now
            };

            _context.UserSessions.Add(userSession);
            await _context.SaveChangesAsync();

            // 查詢所有使用者角色（一個用戶可能有多個角色）
            var userRoles = await _context.UserRoles
                .Include(ur => ur.RoleType)
                .Where(ur => ur.UserId == user.UserId && ur.IsActive == true)
                .ToListAsync();

            // 儲存基本資訊到 Session
            HttpContext.Session.SetInt32("UserId", (int)user.UserId);
            HttpContext.Session.SetString("SessionId", userSession.SessionId.ToString());
            HttpContext.Session.SetString("Account", user.Account ?? "");
            HttpContext.Session.SetString("Email", user.Email);

            // DisplayName（優先使用 UserProfile 的 DisplayName，否則用 Account）
            var displayName = user.UserProfile?.DisplayName ?? user.Account ?? "用戶";
            HttpContext.Session.SetString("DisplayName", displayName);

            // 儲存所有角色資訊
            int highestRoleLevel = 4; // 預設為一般用戶
            string primarySystemCode = "";

            if (userRoles.Any())
            {
                // 取得最高權限的角色（Level 最小的）
                var primaryRole = userRoles.OrderBy(ur => ur.RoleType.RoleLevel).First();
                highestRoleLevel = primaryRole.RoleType.RoleLevel;

                // 儲存主要角色資訊
                HttpContext.Session.SetInt32("RoleTypeId", primaryRole.RoleTypeId);
                HttpContext.Session.SetString("RoleCode", primaryRole.RoleType.RoleCode ?? "");
                HttpContext.Session.SetString("RoleName", primaryRole.RoleType.RoleName ?? "");
                HttpContext.Session.SetInt32("RoleLevel", primaryRole.RoleType.RoleLevel);

                // 儲存所有角色代碼（用逗號分隔）
                var allRoleCodes = userRoles.Select(ur => ur.RoleType.RoleCode).ToList();
                HttpContext.Session.SetString("UserRoles", string.Join(",", allRoleCodes));

                // 儲存系統資訊（如果主要角色有關聯系統）
                if (primaryRole.RoleType.SystemId.HasValue)
                {
                    var systemModule = await _context.UserSystemModules
                        .FirstOrDefaultAsync(s => s.SystemId == primaryRole.RoleType.SystemId
                                               && s.IsActive == true);

                    if (systemModule != null)
                    {
                        HttpContext.Session.SetInt32("SystemId", systemModule.SystemId);
                        HttpContext.Session.SetString("SystemCode", systemModule.SystemCode ?? "");
                        HttpContext.Session.SetString("SystemName", systemModule.SystemName ?? "");
                        primarySystemCode = systemModule.SystemCode ?? "";
                    }
                }
            }
            else
            {
                // 沒有角色時，設定空字串
                HttpContext.Session.SetString("UserRoles", "");
            }

            // 更新最後登入時間
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // 根據角色等級導向對應頁面
            if (highestRoleLevel == 1)
            {
                // 超級管理員 - 導向超管後台管理頁面
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else if (highestRoleLevel == 2)
            {
                // 或者根據系統導向不同的管理頁面（如果你有各系統獨立的後台）
                if (!string.IsNullOrEmpty(primarySystemCode))
                {
                    switch (primarySystemCode)
                    {
                        case "PHOTO_SYSTEM":
                            return RedirectToAction("Index", "PhotoAdmin", new { area = "Admin" });

                        case "VIDEO_SYSTEM":
                            return RedirectToAction("Index", "VideoAdmin", new { area = "Admin" });

                        case "SOCIAL_SYSTEM":
                            return RedirectToAction("Index", "SocialAdmin", new { area = "Admin" });

                        case "SHOP_SYSTEM":
                            return RedirectToAction("Index", "ShopAdmin", new { area = "Admin" });

                        case "STUDIO_SYSTEM":
                            return RedirectToAction("Index", "StudioAdmin", new { area = "Admin" });

                        default:
                            // 如果系統代碼不符合，導向通用管理頁面
                            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }
                }
                else
                {
                    // 沒有系統資訊，導向通用管理頁面
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
            }

            // 一般用戶：導向原本要去的頁面或首頁
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 檢查帳號是否已存在
            var accountExists = await _context.Users
                .AnyAsync(u => u.Account == model.Account);

            if (accountExists)
            {
                ModelState.AddModelError("Account", "此帳號已被使用");
                return View(model);
            }

            // 檢查Email是否已存在
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "此Email已被註冊");
                return View(model);
            }

            // 建立新使用者
            var user = new User
            {
                Account = model.Account,
                Email = model.Email,
                Phone = model.Phone,
                Password = model.Password, // TODO: 應該要加密
                AccountType = "Email",
                AccountStatus = "Active",
                IsDeleted = false,
                RegistrationSource = model.RegistrationSource ?? "Web",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            try
            {
                var generalUserRoleType = await _context.UserRoleTypes
                    .Where(rt => rt.RoleLevel == 4
                              && rt.IsActive == true
                              && rt.SystemId == null)
                    .OrderBy(rt => rt.RoleTypeId)
                    .FirstOrDefaultAsync();

                if (generalUserRoleType != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.UserId,
                        RoleTypeId = generalUserRoleType.RoleTypeId,
                        AssignedBy = null, // 系統自動分配，無分配者
                        AssignedAt = DateTime.Now,
                        IsActive = true,
                        ExpiredAt = null, // 永久有效
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"已為用戶 {user.Account} (UserId: {user.UserId}) 自動分配角色: {generalUserRoleType.RoleName}");
                }
                else
                {
                    // 如果找不到一般用戶角色類型，記錄警告但不影響註冊流程
                    _logger.LogWarning("找不到一般用戶角色類型(RoleLevel=4, SystemId=NULL)，請檢查 UserRoleType 資料表");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"為用戶 {user.Account} 分配角色時發生錯誤: {ex.Message}");
            }

            TempData["SuccessMessage"] = "註冊成功！請登入";

            return RedirectToAction(nameof(Login));
        }

        // GET: UserSessions/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var sessionIdString = HttpContext.Session.GetString("SessionId");

            // 記錄日誌
            if (userId.HasValue)
            {
                Console.WriteLine($"User {userId} is logging out...");
            }

            if (userId.HasValue && !string.IsNullOrEmpty(sessionIdString))
            {
                // 根據 SessionId 的實際類型進行轉換
                // 如果 SessionId 是 long 類型
                if (long.TryParse(sessionIdString, out long sessionId))
                {
                    var userSession = await _context.UserSessions
                        .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                    if (userSession != null)
                    {
                        userSession.IsActive = false;
                        userSession.LastActivityAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }

                // 記錄用戶操作日誌
                try
                {
                    var log = new UserLog
                    {
                        UserId = userId.Value,
                        Status = "Success",
                        ActionType = "Logout",
                        ActionCategory = "Authentication",
                        ActionDescription = "用戶登出",
                        UserAgent = Request.Headers["User-Agent"].ToString(),
                        DeviceType = "Web",
                        SystemName = "Main",
                        Severity = "Info",
                        PerformedBy = "User",
                        CreatedAt = DateTime.Now
                    };

                    _context.UserLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // 記錄錯誤但不影響登出流程
                    Console.WriteLine($"Failed to log logout: {ex.Message}");
                }
            }

            // 清除 Session
            HttpContext.Session.Clear();

            // 清除 Cookie（如果有使用）
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            // 重定向到首頁
            return RedirectToAction("Index", "Home");
        }

        // GET: UserSessions/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: UserSessions/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 查詢用戶
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email
                                       && u.IsDeleted == false
                                       && u.AccountStatus == "Active");

            // 無論用戶是否存在,都顯示相同訊息
            if (user == null)
            {
                TempData["SuccessMessage"] = "如果該Email已註冊,我們將發送重置密碼的連結到您的信箱";
                return RedirectToAction(nameof(Login));
            }

            // 取得Email驗證類型
            var emailVerificationType = await _context.UserVerificationTypes
                .FirstOrDefaultAsync(vt => vt.VerificationTypeName == "Email");

            if (emailVerificationType == null)
            {
                _logger.LogError("找不到Email驗證類型");
                TempData["ErrorMessage"] = "系統設定錯誤,請聯繫管理員";
                return View(model);
            }

            // 生成重置Token (使用GUID)
            var resetToken = Guid.NewGuid().ToString("N");

            // 生成6位數驗證碼(備用)
            var verificationCode = new Random().Next(100000, 999999).ToString();

            // 建立驗證記錄
            var verification = new UserVerification
            {
                UserId = user.UserId,
                VerificationTypeId = emailVerificationType.VerificationTypeId,
                VerificationCode = verificationCode,
                VerificationToken = resetToken,
                SentTo = user.Email,
                Purpose = "密碼重置",
                RequestedAt = DateTime.Now,
                ExpiredAt = DateTime.Now.AddHours(1), // Token有效期1小時
                IsVerified = false,
                IsUsed = false,
                FailedAttempts = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.UserVerifications.Add(verification);
            await _context.SaveChangesAsync();

            // 記錄日誌
            try
            {
                var log = new UserLog
                {
                    UserId = user.UserId,
                    Status = "Success",
                    ActionType = "PasswordResetRequest",
                    ActionCategory = "Security",
                    ActionDescription = $"用戶請求重置密碼",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    DeviceType = "Web",
                    SystemName = "Main",
                    Severity = "Info",
                    PerformedBy = "User",
                    CreatedAt = DateTime.Now
                };
                _context.UserLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"記錄密碼重置日誌失敗: {ex.Message}");
            }

            // 發送郵件
            try
            {
                var resetUrl = Url.Action("ResetPassword", "UserSessions",
                    new { token = resetToken, email = user.Email },
                    Request.Scheme);

                // 實際發送Email
                var emailSent = await _emailService.SendPasswordResetEmailAsync(
                    user.Email,
                    resetUrl,
                    verificationCode);

                if (emailSent)
                {
                    _logger.LogInformation($"密碼重置郵件已發送至: {user.Email}");
                }
                else
                {
                    _logger.LogWarning($"密碼重置郵件發送失敗: {user.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"發送密碼重置郵件失敗: {ex.Message}");
            }

            TempData["SuccessMessage"] = "如果該Email已註冊,我們將發送重置密碼的連結到您的信箱";
            return RedirectToAction(nameof(Login));
        }

        // GET: UserSessions/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "無效的重置連結";
                return RedirectToAction(nameof(Login));
            }

            // 驗證Token
            var verification = await _context.UserVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.VerificationToken == token
                                       && v.SentTo == email
                                       && v.IsVerified == false
                                       && v.IsUsed == false
                                       && v.ExpiredAt > DateTime.Now);

            if (verification == null)
            {
                TempData["ErrorMessage"] = "重置連結已過期或無效";
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        // POST: UserSessions/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 驗證Token
            var verification = await _context.UserVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.VerificationToken == model.Token
                                       && v.SentTo == model.Email
                                       && v.IsVerified == false
                                       && v.IsUsed == false
                                       && v.ExpiredAt > DateTime.Now);

            if (verification == null)
            {
                ModelState.AddModelError(string.Empty, "重置連結已過期或無效");
                return View(model);
            }

            var user = verification.User;
            if (user == null || user.IsDeleted || user.AccountStatus != "Active")
            {
                ModelState.AddModelError(string.Empty, "用戶不存在或已被停用");
                return View(model);
            }

            // 更新密碼
            // TODO: 應該使用 BCrypt 或其他加密方式
            user.Password = model.NewPassword;
            user.UpdatedAt = DateTime.Now;

            // 標記驗證為已使用
            verification.IsVerified = true;
            verification.IsUsed = true;
            verification.VerifiedAt = DateTime.Now;
            verification.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // 記錄日誌
            try
            {
                var log = new UserLog
                {
                    UserId = user.UserId,
                    Status = "Success",
                    ActionType = "PasswordReset",
                    ActionCategory = "Security",
                    ActionDescription = "用戶成功重置密碼",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    DeviceType = "Web",
                    SystemName = "Main",
                    Severity = "Info",
                    PerformedBy = "User",
                    CreatedAt = DateTime.Now
                };
                _context.UserLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"記錄密碼重置日誌失敗: {ex.Message}");
            }

            TempData["SuccessMessage"] = "密碼已成功重置,請使用新密碼登入";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult TestEmailSettings()
        {
            // 僅在開發環境使用
            if (!_hostEnvironment.IsDevelopment())
            {
                return NotFound();
            }

            var settings = _configuration.GetSection("EmailSettings");

            var viewModel = new
            {
                SmtpServer = settings["SmtpServer"],
                SmtpPort = settings["SmtpPort"],
                SenderEmail = settings["SenderEmail"],
                Username = settings["Username"],
                EnableSsl = settings["EnableSsl"],
                HasPassword = !string.IsNullOrEmpty(settings["Password"])
            };

            return Json(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> TestEmail(string email)
        {
            // 僅在開發環境使用
            if (!_hostEnvironment.IsDevelopment())
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(email))
            {
                return Content("請提供測試郵箱地址，例如: /UserSessions/TestEmail?email=test@example.com");
            }

            try
            {
                var result = await _emailService.SendEmailAsync(
                    email,
                    "測試郵件 - Project Photo",
                    "<h1>這是一封測試郵件</h1><p>如果您收到此郵件，表示郵件系統運作正常。</p>"
                );

                return Content(result ?
                    "郵件發送成功！請檢查您的收件箱（可能在垃圾郵件夾）" :
                    "郵件發送失敗！請查看應用程式日誌以獲取詳細錯誤訊息");
            }
            catch (Exception ex)
            {
                return Content($"發生錯誤: {ex.Message}\n\n完整錯誤:\n{ex}");
            }
        }
    }
}
