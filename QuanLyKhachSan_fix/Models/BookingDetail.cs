using QuanLyKhachSan_fix.Models;
using System.Collections.Generic;

namespace QuanLyKhachSan_fix.Models
{
    // status: reserved | checked_in | checked_out | cancelled
    public class BookingDetail
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int RoomId { get; set; }

        // gia tai thoi diem dat
        public decimal Price { get; set; }
        public int? GuestCount { get; set; }
        public string? Status { get; set; }

        public Booking Booking { get; set; } = null!;
        public Room Room { get; set; } = null!;

        public ICollection<BookingEditRequest> EditRequests { get; set; } = new List<BookingEditRequest>();
        public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
        public ICollection<CheckOut> CheckOuts { get; set; } = new List<CheckOut>();
    }
}