using QuanLyKhachSan_fix.Models;
using System;

namespace QuanLyKhachSan_fix.Models
{
    // status: requested | processed
    public class Cancellation
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string? CancelReason { get; set; }
        public decimal? CancellationFee { get; set; }
        public decimal? RefundAmount { get; set; }
        public string? Status { get; set; }
        public DateTime CancelledAt { get; set; }
        public int? ProcessedBy { get; set; }

        public Booking Booking { get; set; } = null!;
        public User? ProcessedByUser { get; set; }
    }
}