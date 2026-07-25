using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Implementations.Manager
{
    public class RoomService : IRoomService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public RoomService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Room>> SearchAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int? roomTypeId = null, string? roomTypeName = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var bookedRoomIds = await db.BookingDetails
                .Where(bd => bd.Status != "cancelled" && bd.Status != "checked_out")
                .Where(bd => bd.Booking.CheckInDate < checkOut && bd.Booking.CheckOutDate > checkIn)
                .Select(bd => bd.RoomId)
                .ToListAsync();

            var query = db.Rooms
                .Include(r => r.RoomType)
                .Where(r => !bookedRoomIds.Contains(r.Id))
                .Where(r => r.Status != "maintenance");

            if (roomTypeId.HasValue)
            {
                // filter by RoomTypeId if FK exists
                query = query.Where(r => EF.Property<int?>(r, "RoomTypeId") == roomTypeId.Value
                                          || (r.RoomType != null && r.RoomType.Id == roomTypeId.Value));
            }
            else if (!string.IsNullOrWhiteSpace(roomTypeName))
            {
                query = query.Where(r => r.RoomType != null && r.RoomType.Name == roomTypeName);
            }

            return await query
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            bool hasConflict = await db.BookingDetails
                .Where(bd => bd.RoomId == roomId)
                .Where(bd => bd.Status != "cancelled" && bd.Status != "checked_out")
                .Where(bd => bd.Booking.CheckInDate < checkOut && bd.Booking.CheckOutDate > checkIn)
                .AnyAsync();

            return !hasConflict;
        }

        public async Task<List<RoomType>> GetAllRoomTypesAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.RoomTypes.OrderBy(rt => rt.BasePrice).ToListAsync();
        }

        public async Task<RoomType> CreateRoomTypeAsync(string name, string? description, decimal basePrice, int maxCapacity)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var roomType = new RoomType
            {
                Name = name,
                Description = description,
                BasePrice = basePrice,
                MaxCapacity = maxCapacity,
                CreatedAt = DateTime.Now
            };

            db.RoomTypes.Add(roomType);
            await db.SaveChangesAsync();
            return roomType;
        }

        public async Task<bool> UpdateRoomTypeAsync(int roomTypeId, string name, string? description, decimal basePrice, int maxCapacity)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var roomType = await db.RoomTypes.FindAsync(roomTypeId);
            if (roomType == null) return false;

            roomType.Name = name;
            roomType.Description = description;
            roomType.BasePrice = basePrice;
            roomType.MaxCapacity = maxCapacity;

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRoomTypeAsync(int roomTypeId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var roomType = await db.RoomTypes.FindAsync(roomTypeId);
            if (roomType == null) return false;

            bool hasRooms = await db.Rooms.AnyAsync(r => r.RoomTypeId == roomTypeId);
            if (hasRooms) return false;

            db.RoomTypes.Remove(roomType);
            await db.SaveChangesAsync();
            return true;
        }
    }
}