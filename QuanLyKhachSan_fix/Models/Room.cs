using QuanLyKhachSan_fix.Models;
using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_fix.Models
{
    // status: available | booked | occupied | maintenance
    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = null!;
        public int RoomTypeId { get; set; }
        public int? Floor { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public RoomType RoomType { get; set; } = null!;
        public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
        public ICollection<BookingEditRequest> EditRequestsAsOldRoom { get; set; } = new List<BookingEditRequest>();
        public ICollection<BookingEditRequest> EditRequestsAsNewRoom { get; set; } = new List<BookingEditRequest>();
    }
}