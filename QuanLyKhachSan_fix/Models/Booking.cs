using QuanLyKhachSan_fix.Models;
using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_fix.Models
{
    // status: pending | confirmed | checked_in | checked_out | cancelled
    public class Booking
    {
        public int Id { get; set; }
        public int CustomerId { get; set; } 
        public string? BookingCode { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string? Status { get; set; }
        public decimal? TotalAmount { get; set; }

        // created_by: user_id - khach tu dat hoac le tan dat ho
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User Customer { get; set; } = null!;
        public User? CreatedByUser { get; set; }

        public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
        public ICollection<Cancellation> Cancellations { get; set; } = new List<Cancellation>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}