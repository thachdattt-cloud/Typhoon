using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_fix.Models
{
    // VD: Standard, Deluxe, Suite
    public class RoomType
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? BasePrice { get; set; }
        public int? MaxCapacity { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}