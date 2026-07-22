using QuanLyKhachSan_fix.Models;
using System;

namespace QuanLyKhachSan_fix.Models
{
    public class BookingEditRequest
    {
        public int Id { get; set; }
        public int BookingDetailId { get; set; }

        // request_type: extend | change_room
        public string? RequestType { get; set; }
        public int? OldRoomId { get; set; }
        public int? NewRoomId { get; set; }
        public DateTime? NewCheckOutDate { get; set; }
        public decimal? ExtraFee { get; set; }

        // status: pending | approved | rejected
        public string? Status { get; set; }
        public DateTime RequestedAt { get; set; }

        // handled_by: user_id le tan xu ly
        public int? HandledBy { get; set; }
        public DateTime? HandledAt { get; set; }

        public BookingDetail BookingDetail { get; set; } = null!;
        public Room? OldRoom { get; set; }
        public Room? NewRoom { get; set; }
        public User? HandledByUser { get; set; }
    }
}