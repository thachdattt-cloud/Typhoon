using QuanLyKhachSan_fix.Models;
using System;

namespace QuanLyKhachSan_fix.Models
{
    // type: booking_confirm | room_unavailable | login_required | over_capacity
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Type { get; set; }
        public string? Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }

        public User User { get; set; } = null!;
    }
}