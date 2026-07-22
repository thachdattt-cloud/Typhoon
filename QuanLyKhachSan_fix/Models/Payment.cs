using System;

namespace QuanLyKhachSan_fix.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }

        // payment_method: online | cash
        public string? PaymentMethod { get; set; }

        // payment_status: pending | success | failed | refunded
        public string? PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public Booking Booking { get; set; } = null!;
    }
}