namespace Project_Photo.Areas.Admin.ViewModels.User
{
    public class UserDetailsViewModel
    {
        // User 基本資訊
        public long UserId { get; set; }
        public string Account { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string AccountType { get; set; }
        public string AccountStatus { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? RegistrationSource { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // UserProfile 公開資料
        public string? DisplayName { get; set; }
        public string? Avatar { get; set; }
        public string? CoverImage { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }

        // UserPrivateInfo 私人資料
        public string? RealName { get; set; }
        public string? Gender { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? FullAddress { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? IdNumber { get; set; }

        // UserSecurity 安全資訊
        public bool OtpEnabled { get; set; }
        public DateTime? SecurityCreatedAt { get; set; }
        public DateTime? SecurityUpdatedAt { get; set; }

        // UserSecurityStatus 安全狀態
        public int FailedLoginAttempts { get; set; }
        public DateTime? LastFailedLoginAt { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LockedAt { get; set; }
        public DateTime? LockedUntil { get; set; }
        public string? LockedReason { get; set; }
        public string? LockedBy { get; set; }

        // 角色列表
        public List<UserRoleInfo> Roles { get; set; } = new List<UserRoleInfo>();

        // 登入記錄
        public List<UserSessionInfo> RecentSessions { get; set; } = new List<UserSessionInfo>();

        // 操作記錄
        public List<UserLogInfo> RecentLogs { get; set; } = new List<UserLogInfo>();
    }

    public class UserRoleInfo
    {
        public long UserRoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleCode { get; set; }
        public string? RoleDescription { get; set; }
        public int RoleLevel { get; set; }
        public string SystemName { get; set; }
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }

    public class UserSessionInfo
    {
        public long SessionId { get; set; }
        public string? UserAgent { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserLogInfo
    {
        public long LogId { get; set; }
        public string Status { get; set; }
        public string ActionType { get; set; }
        public string ActionCategory { get; set; }
        public string? ActionDescription { get; set; }
        public string? IpAddress { get; set; }
        public string SystemName { get; set; }
        public string Severity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}