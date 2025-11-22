using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Admin.ViewModels;
using Project_Photo.Areas.Admin.ViewModels.User;
using Project_Photo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserManagementController : Controller
    {
        private readonly AaContext _context;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(AaContext context, ILogger<UserManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/UserManagement
        public async Task<IActionResult> Index()
        {
            try
            {
                // 取得所有用戶資料,包含相關的角色資訊
                var users = await _context.Users
                    .Include(u => u.UserProfile)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.RoleType)
                    .Where(u => u.IsDeleted == false) // 只顯示未刪除的用戶
                    .OrderByDescending(u => u.UserId)
                    .ToListAsync();

                // 轉換成 ViewModel
                var userViewModels = users.Select(u => new UserListViewModel
                {
                    UserId = u.UserId,
                    Account = u.Account,
                    Email = u.Email,
                    Phone = u.Phone,
                    DisplayName = u.UserProfile?.DisplayName ?? "未設定",
                    AccountType = u.AccountType,
                    AccountStatus = u.AccountStatus,
                    CreatedAt = u.CreatedAt,
                    Roles = u.UserRoles
                        .Where(ur => ur.IsActive == true)
                        .Select(ur => ur.RoleType.RoleName)
                        .ToList()
                }).ToList();

                return View(userViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得用戶列表時發生錯誤");
                TempData["Error"] = "取得用戶列表時發生錯誤";
                return View(new List<UserListViewModel>());
            }
        }

        // GET: Admin/UserManagement/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.UserPrivateInfo)
                .Include(u => u.UserSecurity)
                .Include(u => u.UserSecurityStatus)
                .Include(u => u.UserRoles.Where(ur => ur.IsActive == true))
                    .ThenInclude(ur => ur.RoleType)
                .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return NotFound();
                }

                // 取得角色對應的系統資訊
                var systemIds = user.UserRoles
                    .Where(ur => ur.RoleType.SystemId.HasValue)
                    .Select(ur => ur.RoleType.SystemId.Value)
                    .Distinct()
                    .ToList();

                var systems = await _context.UserSystemModules
                    .Where(s => systemIds.Contains(s.SystemId))
                    .ToDictionaryAsync(s => s.SystemId, s => s.SystemName);

                // 取得最近的 Session 記錄
                var recentSessions = await _context.UserSessions
                    .Where(s => s.UserId == id)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(10)
                    .Select(s => new UserSessionInfo
                    {
                        SessionId = s.SessionId,
                        UserAgent = s.UserAgent,
                        IsActive = s.IsActive,
                        LastActivityAt = s.LastActivityAt,
                        ExpiresAt = s.ExpiresAt,
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                // 取得最近的操作記錄
                var recentLogs = await _context.UserLogs
                    .Where(l => l.UserId == id)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(20)
                    .Select(l => new UserLogInfo
                    {
                        LogId = l.LogId,
                        Status = l.Status,
                        ActionType = l.ActionType,
                        ActionCategory = l.ActionCategory,
                        ActionDescription = l.ActionDescription,
                        IpAddress = l.Ipaddress,
                        SystemName = l.SystemName,
                        Severity = l.Severity,
                        CreatedAt = l.CreatedAt
                    })
                    .ToListAsync();

                // 建立 ViewModel
                var viewModel = new UserDetailsViewModel
                {
                    // User 基本資訊
                    UserId = user.UserId,
                    Account = user.Account,
                    Email = user.Email,
                    Phone = user.Phone,
                    AccountType = user.AccountType,
                    AccountStatus = user.AccountStatus,
                    IsDeleted = user.IsDeleted,
                    DeletedAt = user.DeletedAt,
                    RegistrationSource = user.RegistrationSource,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,

                    // UserProfile
                    DisplayName = user.UserProfile?.DisplayName,
                    Avatar = user.UserProfile?.Avatar,
                    CoverImage = user.UserProfile?.CoverImage,
                    Bio = user.UserProfile?.Bio,
                    Website = user.UserProfile?.Website,
                    Location = user.UserProfile?.Location,

                    // UserPrivateInfo
                    RealName = user.UserPrivateInfo?.RealName,
                    Gender = user.UserPrivateInfo?.Gender,
                    BirthDate = user.UserPrivateInfo?.BirthDate,
                    FullAddress = user.UserPrivateInfo?.FullAddress,
                    City = user.UserPrivateInfo?.City,
                    Country = user.UserPrivateInfo?.Country,
                    PostalCode = user.UserPrivateInfo?.PostalCode,
                    IdNumber = user.UserPrivateInfo?.IdNumber,

                    // UserSecurity
                    OtpEnabled = user.UserSecurity?.OtpEnabled ?? false,
                    SecurityCreatedAt = user.UserSecurity?.CreatedAt,
                    SecurityUpdatedAt = user.UserSecurity?.UpdatedAt,

                    // UserSecurityStatus
                    FailedLoginAttempts = user.UserSecurityStatus?.FailedLoginAttempts ?? 0,
                    LastFailedLoginAt = user.UserSecurityStatus?.LastFailedLoginAt,
                    IsLocked = user.UserSecurityStatus?.IsLocked ?? false,
                    LockedAt = user.UserSecurityStatus?.LockedAt,
                    LockedUntil = user.UserSecurityStatus?.LockedUntil,
                    LockedReason = user.UserSecurityStatus?.LockedReason,
                    LockedBy = user.UserSecurityStatus?.LockedBy,

                    // 角色列表 - 修正這裡
                    Roles = user.UserRoles.Select(ur =>
                    {
                        // 從 dictionary 中查找系統名稱
                        var systemName = "未知系統";
                        if (ur.RoleType.SystemId.HasValue && systems.ContainsKey(ur.RoleType.SystemId.Value))
                        {
                            systemName = systems[ur.RoleType.SystemId.Value];
                        }

                        return new UserRoleInfo
                        {
                            UserRoleId = ur.UserRoleId,
                            RoleName = ur.RoleType.RoleName,
                            RoleCode = ur.RoleType.RoleCode,
                            RoleDescription = ur.RoleType.RoleDescription,
                            RoleLevel = ur.RoleType.RoleLevel,
                            SystemName = systemName,
                            IsActive = ur.IsActive,
                            AssignedAt = ur.AssignedAt,
                            ExpiredAt = ur.ExpiredAt
                        };
                    }).ToList(),

                    // Session 記錄
                    RecentSessions = recentSessions,

                    // 操作記錄
                    RecentLogs = recentLogs
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得用戶詳情時發生錯誤 UserId: {UserId}", id);
                TempData["Error"] = "取得用戶詳情時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/UserManagement/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/UserManagement/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 檢查帳號是否已存在
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Account == model.Account || u.Email == model.Email);

                    if (existingUser != null)
                    {
                        ModelState.AddModelError("", "帳號或Email已存在");
                        return View(model);
                    }

                    // 建立新用戶
                    var user = new User
                    {
                        Account = model.Account,
                        Email = model.Email,
                        Phone = model.Phone,
                        Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        AccountType = model.AccountType,
                        AccountStatus = model.AccountStatus,
                        IsDeleted = false,
                        RegistrationSource = model.RegistrationSource,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // 建立用戶基本資料
                    var userProfile = new UserProfile
                    {
                        UserId = user.UserId,
                        DisplayName = model.DisplayName,
                        Avatar = model.Avatar,
                        CoverImage = model.CoverImage,
                        Bio = model.Bio,
                        Website = model.Website,
                        Location = model.Location,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.UserProfiles.Add(userProfile);

                    var userPrivateInfo = new UserPrivateInfo
                    {
                        UserId = user.UserId,
                        RealName = model.RealName,
                        Gender = model.Gender,
                        BirthDate = model.BirthDate,
                        FullAddress = model.FullAddress,
                        City = model.City,
                        Country = model.Country,
                        PostalCode = model.PostalCode,
                        IdNumber = model.IdNumber,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.UserPrivateInfos.Add(userPrivateInfo);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "用戶建立成功";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "建立用戶時發生錯誤");
                    ModelState.AddModelError("", "建立用戶時發生錯誤");
                }
            }

            return View(model);
        }

        // GET: Admin/UserManagement/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .Include(u => u.UserPrivateInfo)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return NotFound();
                }

                var viewModel = new UserEditViewModel
                {
                    // User 基本資訊
                    UserId = user.UserId,
                    Account = user.Account,
                    Email = user.Email,
                    Phone = user.Phone,
                    AccountType = user.AccountType,
                    AccountStatus = user.AccountStatus,
                    RegistrationSource = user.RegistrationSource,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,

                    // UserProfile
                    DisplayName = user.UserProfile?.DisplayName,
                    Avatar = user.UserProfile?.Avatar,
                    CoverImage = user.UserProfile?.CoverImage,
                    Bio = user.UserProfile?.Bio,
                    Website = user.UserProfile?.Website,
                    Location = user.UserProfile?.Location,

                    // UserPrivateInfo
                    RealName = user.UserPrivateInfo?.RealName,
                    Gender = user.UserPrivateInfo?.Gender,
                    BirthDate = user.UserPrivateInfo?.BirthDate,
                    FullAddress = user.UserPrivateInfo?.FullAddress,
                    City = user.UserPrivateInfo?.City,
                    Country = user.UserPrivateInfo?.Country,
                    PostalCode = user.UserPrivateInfo?.PostalCode,
                    IdNumber = user.UserPrivateInfo?.IdNumber
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入編輯頁面時發生錯誤 UserId: {UserId}", id);
                TempData["Error"] = "載入編輯頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }

        }

        // POST: Admin/UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, UserEditViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users
                        .Include(u => u.UserProfile)
                        .Include(u => u.UserPrivateInfo)
                        .FirstOrDefaultAsync(u => u.UserId == id);

                    if (user == null)
                    {
                        return NotFound();
                    }

                    var duplicatUser = await _context.Users
                        .Where(u => u.UserId != id && (u.Account == model.Account || u.Email == model.Email))
                        .FirstOrDefaultAsync();

                    if (duplicatUser != null)
                    {
                        ModelState.AddModelError("", "帳號或Email已被其他用戶使用");
                        return View(model);
                    }

                    user.Account = model.Account;
                    user.Email = model.Email;
                    user.Phone = model.Phone;
                    user.AccountType = model.AccountType;
                    user.AccountStatus = model.AccountStatus;
                    user.RegistrationSource = model.RegistrationSource;
                    user.UpdatedAt = DateTime.Now;

                    // 更新或建立 UserProfile
                    if (user.UserProfile == null)
                    {
                        user.UserProfile = new UserProfile
                        {
                            UserId = user.UserId,
                            CreatedAt = DateTime.Now
                        };
                        _context.UserProfiles.Add(user.UserProfile);
                    }

                    user.UserProfile.DisplayName = model.DisplayName;
                    user.UserProfile.Avatar = model.Avatar;
                    user.UserProfile.CoverImage = model.CoverImage;
                    user.UserProfile.Bio = model.Bio;
                    user.UserProfile.Website = model.Website;
                    user.UserProfile.Location = model.Location;
                    user.UserProfile.UpdatedAt = DateTime.Now;

                    // 更新或建立 UserPrivateInfo
                    if (user.UserPrivateInfo == null)
                    {
                        user.UserPrivateInfo = new UserPrivateInfo
                        {
                            UserId = user.UserId,
                            CreatedAt = DateTime.Now
                        };
                        _context.UserPrivateInfos.Add(user.UserPrivateInfo);
                    }

                    user.UserPrivateInfo.RealName = model.RealName;
                    user.UserPrivateInfo.Gender = model.Gender;
                    user.UserPrivateInfo.BirthDate = model.BirthDate;
                    user.UserPrivateInfo.FullAddress = model.FullAddress;
                    user.UserPrivateInfo.City = model.City;
                    user.UserPrivateInfo.Country = model.Country;
                    user.UserPrivateInfo.PostalCode = model.PostalCode;
                    user.UserPrivateInfo.IdNumber = model.IdNumber;
                    user.UserPrivateInfo.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "用戶資料更新成功";
                    return RedirectToAction(nameof(Details), new { id = user.UserId });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(model.UserId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新用戶時發生錯誤");
                    ModelState.AddModelError("", "更新用戶時發生錯誤");
                }
            }

            return View(model);
        }

        // GET: Admin/UserManagement/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .Include(u => u.UserRoles)
                    .Include(u => u.UserSessions)
                    .Include(u => u.UserLogs)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return NotFound();
                }

                var viewModel = new UserDeleteViewModel
                {
                    UserId = user.UserId,
                    Account = user.Account,
                    Email = user.Email,
                    Phone = user.Phone,
                    DisplayName = user.UserProfile?.DisplayName,
                    AccountType = user.AccountType,
                    AccountStatus = user.AccountStatus,
                    CreatedAt = user.CreatedAt,
                    RoleCount = user.UserRoles.Count,
                    SessionCount = user.UserSessions.Count,
                    LogCount = user.UserLogs.Count
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入刪除確認頁面時發生錯誤 UserId: {UserId}", id);
                TempData["Error"] = "載入刪除確認頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }

        }

        // POST: Admin/UserManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null) { return NotFound(); }

                // 軟刪除
                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
                user.AccountStatus = "Deleted";
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("用戶已軟刪除 UserId: {UserId}, Account: {Account}", user.UserId, user.Account);
                TempData["Success"] = $"用戶 {user.Account} 已刪除";
                

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除用戶時發生錯誤");
                TempData["Error"] = "刪除用戶時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool UserExists(long id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(long id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .Include(u => u.UserPrivateInfo)
                    .Include(u => u.UserSecurity)
                    .Include(u => u.UserSecurityStatus)
                    .Include(u => u.UserRoles)
                    .Include(u => u.UserSessions)
                    .Include(u => u.UserLogs)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return NotFound();
                }

                // 刪除相關資料
                if (user.UserProfile != null)
                {
                    _context.UserProfiles.Remove(user.UserProfile);
                }

                if (user.UserPrivateInfo != null)
                {
                    _context.UserPrivateInfos.Remove(user.UserPrivateInfo);
                }

                if (user.UserSecurity != null)
                {
                    _context.UserSecurities.Remove(user.UserSecurity);
                }

                if (user.UserSecurityStatus != null)
                {
                    _context.UserSecurityStatuses.Remove(user.UserSecurityStatus);
                }

                // 刪除角色關聯
                _context.UserRoles.RemoveRange(user.UserRoles);

                // 刪除 Session 記錄
                _context.UserSessions.RemoveRange(user.UserSessions);

                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                _logger.LogWarning("用戶已永久刪除 UserId: {UserId}, Account: {Account}", user.UserId, user.Account);
                TempData["Success"] = $"用戶 {user.Account} 已永久刪除";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "永久刪除用戶時發生錯誤 UserId: {UserId}", id);
                TempData["Error"] = "永久刪除用戶時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
