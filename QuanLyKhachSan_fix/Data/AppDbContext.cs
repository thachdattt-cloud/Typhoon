using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Models;
using System.Reflection.Emit;

namespace QuanLyKhachSan_fix.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<RoomType> RoomTypes => Set<RoomType>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingDetail> BookingDetails => Set<BookingDetail>();
        public DbSet<BookingEditRequest> BookingEditRequests => Set<BookingEditRequest>();
        public DbSet<Cancellation> Cancellations => Set<Cancellation>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<CheckIn> CheckIns => Set<CheckIn>();
        public DbSet<CheckOut> CheckOuts => Set<CheckOut>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Employee> Employees => Set<Employee>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ---------- users ----------
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.HasKey(x => x.Id);
                e.Property(x => x.Username).HasMaxLength(255).IsRequired();
                e.HasIndex(x => x.Username).IsUnique();
                e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
                e.Property(x => x.FullName).HasMaxLength(255);
                e.Property(x => x.Email).HasMaxLength(255);
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.Phone).HasMaxLength(255);
                e.Property(x => x.Role).HasMaxLength(255);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
                e.Property(x => x.IsActive).HasDefaultValue(true);

                e.HasOne(x => x.Employee)
                    .WithOne(x => x.User)
                    .HasForeignKey<Employee>(x => x.UserId);
            });

            // ---------- room_types ----------
            modelBuilder.Entity<RoomType>(e =>
            {
                e.ToTable("room_types");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(255).IsRequired();
                e.Property(x => x.Description).HasColumnType("nvarchar(max)");
                e.Property(x => x.BasePrice).HasColumnType("decimal(18,2)");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // ---------- rooms ----------
            modelBuilder.Entity<Room>(e =>
            {
                e.ToTable("rooms");
                e.HasKey(x => x.Id);
                e.Property(x => x.RoomNumber).HasMaxLength(255).IsRequired();
                e.HasIndex(x => x.RoomNumber).IsUnique();
                e.Property(x => x.Status).HasMaxLength(255);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.RoomType)
                    .WithMany(x => x.Rooms)
                    .HasForeignKey(x => x.RoomTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- bookings ----------
            modelBuilder.Entity<Booking>(e =>
            {
                e.ToTable("bookings");
                e.HasKey(x => x.Id);
                e.Property(x => x.BookingCode).HasMaxLength(255);
                e.HasIndex(x => x.BookingCode).IsUnique();
                e.Property(x => x.CheckInDate).HasColumnType("date");
                e.Property(x => x.CheckOutDate).HasColumnType("date");
                e.Property(x => x.Status).HasMaxLength(255);
                e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.Customer)
                    .WithMany(x => x.BookingsAsCustomer)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.CreatedByUser)
                    .WithMany(x => x.BookingsCreated)
                    .HasForeignKey(x => x.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- booking_details ----------
            modelBuilder.Entity<BookingDetail>(e =>
            {
                e.ToTable("booking_details");
                e.HasKey(x => x.Id);
                e.Property(x => x.Price).HasColumnType("decimal(18,2)").IsRequired();
                e.Property(x => x.Status).HasMaxLength(255);

                e.HasOne(x => x.Booking)
                    .WithMany(x => x.BookingDetails)
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Room)
                    .WithMany(x => x.BookingDetails)
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- booking_edit_requests ----------
            modelBuilder.Entity<BookingEditRequest>(e =>
            {
                e.ToTable("booking_edit_requests");
                e.HasKey(x => x.Id);
                e.Property(x => x.RequestType).HasMaxLength(255);
                e.Property(x => x.NewCheckOutDate).HasColumnType("date");
                e.Property(x => x.ExtraFee).HasColumnType("decimal(18,2)");
                e.Property(x => x.Status).HasMaxLength(255);
                e.Property(x => x.RequestedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.BookingDetail)
                    .WithMany(x => x.EditRequests)
                    .HasForeignKey(x => x.BookingDetailId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.OldRoom)
                    .WithMany(x => x.EditRequestsAsOldRoom)
                    .HasForeignKey(x => x.OldRoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.NewRoom)
                    .WithMany(x => x.EditRequestsAsNewRoom)
                    .HasForeignKey(x => x.NewRoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.HandledByUser)
                    .WithMany(x => x.BookingEditRequestsHandled)
                    .HasForeignKey(x => x.HandledBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- cancellations ----------
            modelBuilder.Entity<Cancellation>(e =>
            {
                e.ToTable("cancellations");
                e.HasKey(x => x.Id);
                e.Property(x => x.CancelReason).HasColumnType("nvarchar(max)");
                e.Property(x => x.CancellationFee).HasColumnType("decimal(18,2)");
                e.Property(x => x.RefundAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.Status).HasMaxLength(255);
                e.Property(x => x.CancelledAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.Booking)
                    .WithMany(x => x.Cancellations)
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.ProcessedByUser)
                    .WithMany(x => x.CancellationsProcessed)
                    .HasForeignKey(x => x.ProcessedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- payments ----------
            modelBuilder.Entity<Payment>(e =>
            {
                e.ToTable("payments");
                e.HasKey(x => x.Id);
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
                e.Property(x => x.PaymentMethod).HasMaxLength(255);
                e.Property(x => x.PaymentStatus).HasMaxLength(255);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.Booking)
                    .WithMany(x => x.Payments)
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- invoices ----------
            modelBuilder.Entity<Invoice>(e =>
            {
                e.ToTable("invoices");
                e.HasKey(x => x.Id);
                e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.IssuedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.Booking)
                    .WithMany(x => x.Invoices)
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.IssuedByUser)
                    .WithMany(x => x.InvoicesIssued)
                    .HasForeignKey(x => x.IssuedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- checkins ----------
            modelBuilder.Entity<CheckIn>(e =>
            {
                e.ToTable("checkins");
                e.HasKey(x => x.Id);
                e.Property(x => x.CheckinTime).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.BookingDetail)
                    .WithMany(x => x.CheckIns)
                    .HasForeignKey(x => x.BookingDetailId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Staff)
                    .WithMany(x => x.CheckInsPerformed)
                    .HasForeignKey(x => x.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- checkouts ----------
            modelBuilder.Entity<CheckOut>(e =>
            {
                e.ToTable("checkouts");
                e.HasKey(x => x.Id);
                e.Property(x => x.CheckoutTime).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.BookingDetail)
                    .WithMany(x => x.CheckOuts)
                    .HasForeignKey(x => x.BookingDetailId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Staff)
                    .WithMany(x => x.CheckOutsPerformed)
                    .HasForeignKey(x => x.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- notifications ----------
            modelBuilder.Entity<Notification>(e =>
            {
                e.ToTable("notifications");
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).HasMaxLength(255);
                e.Property(x => x.Content).HasColumnType("nvarchar(max)");
                e.Property(x => x.IsRead).HasDefaultValue(false);
                e.Property(x => x.SentAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.User)
                    .WithMany(x => x.Notifications)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- employees ----------
            modelBuilder.Entity<Employee>(e =>
            {
                e.ToTable("employees");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.UserId).IsUnique();
                e.Property(x => x.Position).HasMaxLength(255);
                e.Property(x => x.Salary).HasColumnType("decimal(18,2)");
            });
        }
    }
}