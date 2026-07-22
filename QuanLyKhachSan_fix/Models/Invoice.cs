using QuanLyKhachSan_fix.Models;
using System;

namespace QuanLyKhachSan_fix.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal? TotalAmount { get; set; }

        // issued_by: user_id - le tan xuat hoa don
        public int? IssuedBy { get; set; }
        public DateTime IssuedAt { get; set; }

        public Booking Booking { get; set; } = null!;
        public User? IssuedByUser { get; set; }
    }
}