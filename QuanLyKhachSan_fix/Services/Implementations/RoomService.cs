using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Implementations
{
    public class RoomService : IRoomService
    {
        private readonly AppDbContext _db;

        public RoomService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Room>> SearchAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
        {
            var bookedRoomIds = await _db.BookingDetails
                .Where(bd => bd.Status != "cancelled" && bd.Status != "checked_out")
                .Where(bd => bd.Booking.CheckInDate < checkOut && bd.Booking.CheckOutDate > checkIn)
                .Select(bd => bd.RoomId)
                .ToListAsync();

            return await _db.Rooms
                .Include(r => r.RoomType)
                .Where(r => !bookedRoomIds.Contains(r.Id))
                .Where(r => r.Status != "maintenance")
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            bool hasConflict = await _db.BookingDetails
                .Where(bd => bd.RoomId == roomId)
                .Where(bd => bd.Status != "cancelled" && bd.Status != "checked_out")
                .Where(bd => bd.Booking.CheckInDate < checkOut && bd.Booking.CheckOutDate > checkIn)
                .AnyAsync();

            return !hasConflict;
        }

        public async Task<List<RoomType>> GetAllRoomTypesAsync()
        {
            return await _db.RoomTypes.OrderBy(rt => rt.BasePrice).ToListAsync();
        }

        public async Task<RoomType> CreateRoomTypeAsync(string name, string? description, decimal basePrice, int maxCapacity)
        {
            var roomType = new RoomType
            {
                Name = name,
                Description = description,
                BasePrice = basePrice,
                MaxCapacity = maxCapacity,
                CreatedAt = DateTime.Now
            };

            _db.RoomTypes.Add(roomType);
            await _db.SaveChangesAsync();
            return roomType;
        }

        public async Task<bool> UpdateRoomTypeAsync(int roomTypeId, string name, string? description, decimal basePrice, int maxCapacity)
        {
            var roomType = await _db.RoomTypes.FindAsync(roomTypeId);
            if (roomType == null) return false;

            roomType.Name = name;
            roomType.Description = description;
            roomType.BasePrice = basePrice;
            roomType.MaxCapacity = maxCapacity;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRoomTypeAsync(int roomTypeId)
        {
            var roomType = await _db.RoomTypes.FindAsync(roomTypeId);
            if (roomType == null) return false;

            bool hasRooms = await _db.Rooms.AnyAsync(r => r.RoomTypeId == roomTypeId);
            if (hasRooms) return false;

            _db.RoomTypes.Remove(roomType);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}