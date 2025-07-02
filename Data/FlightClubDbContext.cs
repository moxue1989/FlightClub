using Microsoft.EntityFrameworkCore;
using FlightClub.Models.Api;

namespace FlightClub.Data;

public class FlightClubDbContext : DbContext
{
    public FlightClubDbContext(DbContextOptions<FlightClubDbContext> options) : base(options)
    {
    }

    public DbSet<ScheduledTask> ScheduledTasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ScheduledTask entity
        modelBuilder.Entity<ScheduledTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
            
            entity.Property(e => e.ScheduledTime)
                .IsRequired();
            
            entity.Property(e => e.TaskType)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Parameters)
                .HasColumnType("TEXT"); // Store as TEXT in SQLite for JSON
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            
            entity.Property(e => e.Priority)
                .HasDefaultValue(1);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("datetime('now')");
            
            entity.Property(e => e.UpdatedAt);
            
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            // Create indexes for better query performance
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TaskType);
            entity.HasIndex(e => e.ScheduledTime);
            entity.HasIndex(e => new { e.Status, e.ScheduledTime });
        });
    }
}
