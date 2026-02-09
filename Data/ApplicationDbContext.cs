using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Models;

namespace PortfolioHub.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<PortfolioItem> PortfolioItems => Set<PortfolioItem>();
    public DbSet<PortfolioMedia> PortfolioMedia => Set<PortfolioMedia>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Profile>()
            .HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<Profile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Profile>()
            .HasIndex(p => p.NormalizedUsername)
            .IsUnique();

        builder.Entity<Profile>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        builder.Entity<Profile>()
            .HasOne(p => p.AvatarFile)
            .WithMany()
            .HasForeignKey(p => p.AvatarFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Profile>()
            .HasOne(p => p.CoverFile)
            .WithMany()
            .HasForeignKey(p => p.CoverFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<UserBadge>()
            .HasIndex(ub => new { ub.UserId, ub.BadgeId })
            .IsUnique();
    }
}
