-- ============================================
-- ?? 社群系統資料庫設計
-- ============================================

CREATE DATABASE SocialNetwork;
GO

USE SocialNetwork;
GO

-- ============================================
-- 1. 使用者資料
-- ============================================
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(200) NOT NULL,
    Bio NVARCHAR(300),
    AvatarUrl NVARCHAR(300),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- ============================================
-- 2. 貼文資料（個人、首頁、社團、活動皆共用）
-- ============================================
CREATE TABLE Posts (
    PostID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    GroupID INT NULL,
    EventID INT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    ImageUrl NVARCHAR(300),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- ============================================
-- 3. 留言資料
-- ============================================
CREATE TABLE Comments (
    CommentID INT IDENTITY(1,1) PRIMARY KEY,
    PostID INT NOT NULL,
    UserID INT NOT NULL,
    Content NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (PostID) REFERENCES Posts(PostID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- ============================================
-- 4. 按讚紀錄
-- ============================================
CREATE TABLE Likes (
    LikeID INT IDENTITY(1,1) PRIMARY KEY,
    PostID INT NOT NULL,
    UserID INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (PostID) REFERENCES Posts(PostID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT UQ_Like UNIQUE (PostID, UserID)
);
GO

-- ============================================
-- 5. 追蹤關係
-- ============================================
CREATE TABLE Follows (
    FollowerID INT NOT NULL,
    FollowingID INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (FollowerID, FollowingID),
    FOREIGN KEY (FollowerID) REFERENCES Users(UserID),
    FOREIGN KEY (FollowingID) REFERENCES Users(UserID)
);
GO

-- ============================================
-- 6. 私訊（Direct Message）
-- ============================================
CREATE TABLE Messages (
    MessageID INT IDENTITY(1,1) PRIMARY KEY,
    SenderID INT NOT NULL,
    ReceiverID INT NOT NULL,
    Content NVARCHAR(500) NOT NULL,
    SentAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (SenderID) REFERENCES Users(UserID),
    FOREIGN KEY (ReceiverID) REFERENCES Users(UserID)
);
GO

-- ============================================
-- 7. 社團
-- ============================================
CREATE TABLE Groups (
    GroupID INT IDENTITY(1,1) PRIMARY KEY,
    GroupName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    OwnerID INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (OwnerID) REFERENCES Users(UserID)
);
GO

-- ============================================
-- 8. 社團成員
-- ============================================
CREATE TABLE GroupMembers (
    GroupID INT NOT NULL,
    UserID INT NOT NULL,
    Role NVARCHAR(20) DEFAULT 'Member',
    JoinedAt DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (GroupID, UserID),
    FOREIGN KEY (GroupID) REFERENCES Groups(GroupID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- ============================================
-- 9. 活動
-- ============================================
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    HostID INT NOT NULL,
    StartTime DATETIME,
    EndTime DATETIME,
    Location NVARCHAR(200),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (HostID) REFERENCES Users(UserID)
);
GO

-- ============================================
-- 10. 活動參與
-- ============================================
CREATE TABLE EventParticipants (
    EventID INT NOT NULL,
    UserID INT NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Going',
    JoinedAt DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (EventID, UserID),
    FOREIGN KEY (EventID) REFERENCES Events(EventID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO
