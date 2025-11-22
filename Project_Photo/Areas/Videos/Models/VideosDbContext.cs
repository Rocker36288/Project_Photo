using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Models;

namespace Project_Photo.Areas.Videos.Models;

public partial class VideosDbContext : DbContext
{
    public VideosDbContext(DbContextOptions<VideosDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Channel> Channels { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<CommentLike> CommentLikes { get; set; }

    public virtual DbSet<Following> Followings { get; set; }

    public virtual DbSet<Like> Likes { get; set; }

    public virtual DbSet<View> Views { get; set; }

    public virtual DbSet<Video> Videos { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. 強制映射 Channel 實體 (如前所述，不使用 HasColumnName)
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.Property(e => e.ChannelId).ValueGeneratedNever();

            entity.Property(e => e.ChannelName).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            // 2. 關係配置
            entity.HasOne<User>()
        .WithOne(u => u.Channel)
        .HasForeignKey<Channel>(c => c.ChannelId)
        .HasConstraintName("FK_Channels_User");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK_Comment");

            entity.ToTable("Comments", "Video");

            entity.Property(e => e.CommenContent).HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Video).WithMany(p => p.Comments)
                .HasForeignKey(d => d.VideoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comments_Videos");
        });

        modelBuilder.Entity<CommentLike>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CommentLikes", "Video");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Comment).WithMany()
                .HasForeignKey(d => d.CommentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CommentLikes_Comments");
        });

        modelBuilder.Entity<Following>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Following", "Video");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Channel).WithMany()
                .HasForeignKey(d => d.ChannelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Following_Channels");
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Likes", "Video");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(e => e.VideoId).HasName("PK_Video");

            entity.ToTable("Videos", "Video");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.PrivacyStatus).HasMaxLength(20);
            entity.Property(e => e.ProcessStatus).HasMaxLength(20);
            entity.Property(e => e.Resolution).HasMaxLength(20);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(50);
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VideoUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<View>(entity =>
        {
            entity.HasKey(e => new { e.VideoId, e.UserId });
            entity.ToTable("View", "Video");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Video).WithMany()
                .HasForeignKey(d => d.VideoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VideoView_Videos");
        });

        modelBuilder.Entity<User>().ToTable("User", "dbo");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
