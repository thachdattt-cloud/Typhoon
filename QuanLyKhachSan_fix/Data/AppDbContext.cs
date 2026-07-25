using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Models;

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Username).HasColumnName("username").HasMaxLength(255).IsRequired();
                e.HasIndex(x => x.Username).IsUnique();
                e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
                e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255);
                e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(255);
                e.Property(x => x.Role).HasColumnName("role").HasMaxLength(255);
                e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
                e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

                e.HasOne(x => x.Employee)
                    .WithOne(x => x.User)
                    .HasForeignKey<Employee>(x => x.UserId);
            });

            // ---------- room_types ----------
            modelBuilder.Entity<RoomType>(e =>
            {
                e.ToTable("room_types");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
                e.Property(x => x.Description).HasColumnName("description").HasColumnType("nvarchar(max)");
                e.Property(x => x.BasePrice).HasColumnName("base_price").HasColumnType("decimal(18,2)");
                e.Property(x => x.MaxCapacity).HasColumnName("max_capacity");
                e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
            });

            // ---------- rooms ----------
            modelBuilder.Entity<Room>(e =>
            {
                e.ToTable("rooms");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.RoomNumber).HasColumnName("room_number").HasMaxLength(255).IsRequired();
                e.HasIndex(x => x.RoomNumber).IsUnique();
                e.Property(x => x.RoomTypeId).HasColumnName("room_type_id");
                e.Property(x => x.Floor).HasColumnName("floor");
                e.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
                e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.CustomerId).HasColumnName("customer_id");
                e.Property(x => x.BookingCode).HasColumnName("booking_code").HasMaxLength(255);
                e.HasIndex(x => x.BookingCode).IsUnique();
                e.Property(x => x.CheckInDate).HasColumnName("check_in_date").HasColumnType("date");
                e.Property(x => x.CheckOutDate).HasColumnName("check_out_date").HasColumnType("date");
                e.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
                e.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(18,2)");
                e.Property(x => x.CreatedBy).HasColumnName("created_by");
                e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.BookingId).HasColumnName("booking_id");
                e.Property(x => x.RoomId).HasColumnName("room_id");
                e.Property(x => x.Price).HasColumnName("price").HasColumnType("decimal(18,2)").IsRequired();
                e.Property(x => x.GuestCount).HasColumnName("guest_count");
                e.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.BookingDetailId).HasColumnName("booking_detail_id");
                e.Property(x => x.RequestType).HasColumnName("request_type").HasMaxLength(255);
                e.Property(x => x.OldRoomId).HasColumnName("old_room_id");
                e.Property(x => x.NewRoomId).HasColumnName("new_room_id");
                e.Property(x => x.NewCheckOutDate).HasColumnName("new_check_out_date").HasColumnType("date");
                e.Property(x => x.ExtraFee).HasColumnName("extra_fee").HasColumnType("decimal(18,2)");
                e.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
                e.Property(x => x.RequestedAt).HasColumnName("requested_at").HasDefaultValueSql("GETDATE()");
                e.Property(x => x.HandledBy).HasColumnName("handled_by");
                e.Property(x => x.HandledAt).HasColumnName("handled_at");

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.BookingId).HasColumnName("booking_id");
                e.Property(x => x.CancelReason).HasColumnName("cancel_reason").HasColumnType("nvarchar(max)");
                e.Property(x => x.CancellationFee).HasColumnName("cancellation_fee").HasColumnType("decimal(18,2)");
                e.Property(x => x.RefundAmount).HasColumnName("refund_amount").HasColumnType("decimal(18,2)");
                e.Property(x => x.Status).HasColumnName("status").HasMaxLength(255);
                e.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasDefaultValueSql("GETDATE()");
                e.Property(x => x.ProcessedBy).HasColumnName("processed_by");

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.BookingId).HasColumnName("booking_id");
                e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
                e.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(255);
                e.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(255);
                e.Property(x => x.PaidAt).HasColumnName("paid_at");
                e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.BookingId).HasColumnName("booking_id");
                e.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(18,2)");
                e.Property(x => x.IssuedBy).HasColumnName("issued_by");
                e.Property(x => x.IssuedAt).HasColumnName("issued_at").HasDefaultValueSql("GETDATE()");

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.BookingDetailId).HasColumnName("booking_detail_id");
                e.Property(x => x.CheckinTime).HasColumnName("checkin_time").HasDefaultValueSql("GETDATE()");
                e.Property(x => x.StaffId).HasColumnName("staff_id");

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.BookingDetailId).HasColumnName("booking_detail_id");
                e.Property(x => x.CheckoutTime).HasColumnName("checkout_time").HasDefaultValueSql("GETDATE()");
                e.Property(x => x.StaffId).HasColumnName("staff_id");

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.Property(x => x.Type).HasColumnName("type").HasMaxLength(255);
                e.Property(x => x.Content).HasColumnName("content").HasColumnType("nvarchar(max)");
                e.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false);
                e.Property(x => x.SentAt).HasColumnName("sent_at").HasDefaultValueSql("GETDATE()");

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
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.UserId).HasColumnName("user_id");
                e.HasIndex(x => x.UserId).IsUnique();
                e.Property(x => x.Position).HasColumnName("position").HasMaxLength(255);
                e.Property(x => x.HireDate).HasColumnName("hire_date");
                e.Property(x => x.Salary).HasColumnName("salary").HasColumnType("decimal(18,2)");
            });
        }
    }
}