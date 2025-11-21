using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Models;
using Project_Photo.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Photo.Controllers
{
    public class UserSessionsController : Controller
    {
        private readonly AaContext _context;

        public UserSessionsController(AaContext context)
        {
            _context = context;
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
            var user = await _context.Users.FirstOrDefaultAsync(u => 
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

            // 查詢使用者角色
            var userRole = await _context.UserRoles
                .Include(ur => ur.RoleType)
                .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.IsActive == true);


            // 儲存到 Session
            HttpContext.Session.SetInt32("UserId", (int)user.UserId);
            HttpContext.Session.SetString("SessionId", userSession.SessionId.ToString());
            HttpContext.Session.SetString("Account", user.Account ?? "");
            HttpContext.Session.SetString("Email", user.Email);

            // 儲存角色資訊（如果有的話）
            int roleLevel = 4; // 預設為一般用戶
            if (userRole != null && userRole.RoleType != null)
            {
                HttpContext.Session.SetInt32("RoleTypeId", userRole.RoleTypeId);
                HttpContext.Session.SetString("RoleCode", userRole.RoleType.RoleCode ?? "");
                HttpContext.Session.SetString("RoleName", userRole.RoleType.RoleName ?? "");
                HttpContext.Session.SetInt32("RoleLevel", userRole.RoleType.RoleLevel);

                roleLevel = userRole.RoleType.RoleLevel;

                // 儲存系統資訊（如果角色有關聯系統）
                if (userRole.RoleType.SystemId.HasValue)
                {
                    var systemModule = await _context.UserSystemModules
                        .FirstOrDefaultAsync(s => s.SystemId == userRole.RoleType.SystemId
                                               && s.IsActive == true);

                    if (systemModule != null)
                    {
                        HttpContext.Session.SetInt32("SystemId", systemModule.SystemId);
                        HttpContext.Session.SetString("SystemCode", systemModule.SystemCode ?? "");
                        HttpContext.Session.SetString("SystemName", systemModule.SystemName ?? "");
                    }
                }
            }

            // 更新最後登入時間
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (roleLevel == 1)
            {
                // 超級管理員 - 導向超管後台管理頁面
                return RedirectToAction("Index", "Admin");
            }
            else if (roleLevel == 2)
            {
                // 系統管理員 - 根據所屬系統導向對應的管理頁面
                var systemCode = HttpContext.Session.GetString("SystemCode");

                if (!string.IsNullOrEmpty(systemCode))
                {
                    switch (systemCode)
                    {
                        case "PHOTO_SYSTEM":
                            return RedirectToAction("Index", "PhotoAdmin");

                        case "VIDEO_SYSTEM":
                            return RedirectToAction("Index", "VideoAdmin");

                        case "SOCIAL_SYSTEM":
                            return RedirectToAction("Index", "SocialAdmin");

                        case "SHOP_SYSTEM":
                            return RedirectToAction("Index", "ShopAdmin");

                        case "STUDIO_SYSTEM":
                            return RedirectToAction("Index", "StudioAdmin");

                        default:
                            // 如果系統代碼不符合，導向通用管理頁面
                            return RedirectToAction("Index", "Admin");
                    }
                }
                else
                {
                    // 沒有系統資訊，導向通用管理頁面
                    return RedirectToAction("Index", "Admin");
                }
            }

            // 導向原本要去的頁面或首頁
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

            TempData["SuccessMessage"] = "註冊成功！請登入";

            return RedirectToAction(nameof(Login));
        }

        // GET: UserSessions/Logout
        public async Task<IActionResult> Logout()
        {
            var sessionIdString = HttpContext.Session.GetString("SessionId");

            if (!string.IsNullOrEmpty(sessionIdString) && long.TryParse(sessionIdString, out long sessionId))
            {
                var userSession = await _context.UserSessions.FindAsync(sessionId);
                if (userSession != null)
                {
                    userSession.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }

            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
        
        // GET: UserSessions
        public async Task<IActionResult> Index()
        {
            var aaContext = _context.UserSessions.Include(u => u.User);
            return View(await aaContext.ToListAsync());
        }

        // GET: UserSessions/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userSession = await _context.UserSessions
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.SessionId == id);
            if (userSession == null)
            {
                return NotFound();
            }

            return View(userSession);
        }

        // GET: UserSessions/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: UserSessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SessionId,UserId,UserAgent,IsActive,LastActivityAt,ExpiresAt,CreatedAt")] UserSession userSession)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userSession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", userSession.UserId);
            return View(userSession);
        }

        // GET: UserSessions/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userSession = await _context.UserSessions.FindAsync(id);
            if (userSession == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", userSession.UserId);
            return View(userSession);
        }

        // POST: UserSessions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("SessionId,UserId,UserAgent,IsActive,LastActivityAt,ExpiresAt,CreatedAt")] UserSession userSession)
        {
            if (id != userSession.SessionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userSession);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserSessionExists(userSession.SessionId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", userSession.UserId);
            return View(userSession);
        }

        // GET: UserSessions/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userSession = await _context.UserSessions
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.SessionId == id);
            if (userSession == null)
            {
                return NotFound();
            }

            return View(userSession);
        }

        // POST: UserSessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var userSession = await _context.UserSessions.FindAsync(id);
            if (userSession != null)
            {
                _context.UserSessions.Remove(userSession);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserSessionExists(long id)
        {
            return _context.UserSessions.Any(e => e.SessionId == id);
        }
    }
}
