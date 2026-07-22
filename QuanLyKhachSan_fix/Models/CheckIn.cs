using QuanLyKhachSan_fix.Models;
using System;

namespace QuanLyKhachSan_fix.Models
{
    public class CheckIn
    {
        public int Id { get; set; }
        public int BookingDetailId { get; set; }
        public DateTime CheckinTime { get; set; }
        public int StaffId { get; set; }

        public BookingDetail BookingDetail { get; set; } = null!;
        public User Staff { get; set; } = null!;
    }
}