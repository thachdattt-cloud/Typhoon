using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_fix.Models
{
    // role: customer | receptionist | manager
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public Employee? Employee { get; set; }

        public ICollection<Booking> BookingsAsCustomer { get; set; } = new List<Booking>();
        public ICollection<Booking> BookingsCreated { get; set; } = new List<Booking>();
        public ICollection<BookingEditRequest> BookingEditRequestsHandled { get; set; } = new List<BookingEditRequest>();
        public ICollection<Cancellation> CancellationsProcessed { get; set; } = new List<Cancellation>();
        public ICollection<Invoice> InvoicesIssued { get; set; } = new List<Invoice>();
        public ICollection<CheckIn> CheckInsPerformed { get; set; } = new List<CheckIn>();
        public ICollection<CheckOut> CheckOutsPerformed { get; set; } = new List<CheckOut>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}