using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Project_Photo.Areas.Videos.Models;

public partial class VideosDbContext : DbContext
{
    public VideosDbContext(DbContextOptions<VideosDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Channel> Channels { get; set; }
    public virtual DbSet<Video> Videos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(e => e.ChannelId).HasName("PK_Channel");

            entity.ToTable("Channels", "Video");

            entity.Property(e => e.ChannelId).ValueGeneratedNever();
            entity.Property(e => e.ChannelName).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UpdateAt)
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
