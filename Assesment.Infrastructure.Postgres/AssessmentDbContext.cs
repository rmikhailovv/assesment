using Microsoft.EntityFrameworkCore;
using Assesment.Domain;

namespace Assesment.Infrastructure.Postgres;

public class AssessmentDbContext : DbContext
{
    public DbSet<TreeNode> TreeNodes { get; set; } = null!;
    public DbSet<ExceptionJournal> ExceptionJournal { get; set; } = null!;

    public AssessmentDbContext(DbContextOptions<AssessmentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TreeNode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TreeName).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance optimization
            entity.HasIndex(e => new { e.TreeName, e.ParentId, e.Name }).IsUnique();
            entity.HasIndex(e => e.TreeName);
            entity.HasIndex(e => e.ParentId);
        });

        modelBuilder.Entity<ExceptionJournal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.EventId).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExceptionType).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.StackTrace).IsRequired();
            entity.Property(e => e.QueryParameters).IsRequired();
            entity.Property(e => e.BodyParameters).IsRequired();
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(500);
            
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
