using QuanLyKhachSan_fix.Models;
using System;

namespace QuanLyKhachSan_fix.Models
{
    public class CheckOut
    {
        public int Id { get; set; }
        public int BookingDetailId { get; set; }
        public DateTime CheckoutTime { get; set; }
        public int StaffId { get; set; }

        public BookingDetail BookingDetail { get; set; } = null!;
        public User Staff { get; set; } = null!;
    }
}