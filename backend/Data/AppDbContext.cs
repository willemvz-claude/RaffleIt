using Microsoft.EntityFrameworkCore;
using RaffleIt.API.Models;

namespace RaffleIt.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Raffle> Raffles => Set<Raffle>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.FullName).HasColumnName("full_name");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Raffle>(e =>
        {
            e.ToTable("raffles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Organisation).HasColumnName("organisation");
            e.Property(x => x.Prizes).HasColumnName("prizes");
            e.Property(x => x.NumberOfTickets).HasColumnName("number_of_tickets");
            e.Property(x => x.TicketPrice).HasColumnName("ticket_price");
            e.Property(x => x.Currency).HasColumnName("currency");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.User).WithMany(x => x.Raffles).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("tickets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RaffleId).HasColumnName("raffle_id");
            e.Property(x => x.TicketNumber).HasColumnName("ticket_number");
            e.Property(x => x.IsClaimed).HasColumnName("is_claimed");
            e.Property(x => x.ClaimedName).HasColumnName("claimed_name");
            e.Property(x => x.ClaimedSurname).HasColumnName("claimed_surname");
            e.Property(x => x.ClaimedMobile).HasColumnName("claimed_mobile");
            e.Property(x => x.ClaimedEmail).HasColumnName("claimed_email");
            e.Property(x => x.PurchasedFrom).HasColumnName("purchased_from");
            e.Property(x => x.ClaimedAt).HasColumnName("claimed_at");
            e.HasOne(x => x.Raffle).WithMany(x => x.Tickets).HasForeignKey(x => x.RaffleId);
        });
    }
}
