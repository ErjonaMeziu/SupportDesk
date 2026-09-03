using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Domain.Entities;

namespace SupportDesk.Api.Data;

public class SupportDeskDbContext : DbContext
{
    public SupportDeskDbContext(DbContextOptions<SupportDeskDbContext> options) : base(options)
    {
    }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<TicketReferenceCounter> TicketReferenceCounters => Set<TicketReferenceCounter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(a => a.FullName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Email).HasMaxLength(256).IsRequired();
            entity.Property(a => a.Department).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(a => a.Email).IsUnique();
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.Property(t => t.Reference).HasMaxLength(20).IsRequired();
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(4000).IsRequired();
            entity.Property(t => t.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(t => t.CustomerEmail).HasMaxLength(256).IsRequired();
            entity.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(t => t.Reference).IsUnique();

            entity.HasOne(t => t.AssignedAgent)
                .WithMany(a => a.AssignedTickets)
                .HasForeignKey(t => t.AssignedAgentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(t => t.Comments)
                .WithOne(c => c.Ticket)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(c => c.AuthorName).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Body).HasMaxLength(4000).IsRequired();
        });

        modelBuilder.Entity<TicketReferenceCounter>(entity =>
        {
            entity.HasKey(c => c.Year);
            entity.Property(c => c.Year).ValueGeneratedNever();
        });
    }
}
