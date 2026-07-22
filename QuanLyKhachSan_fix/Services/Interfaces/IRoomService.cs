using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Interfaces
{
    public interface IRoomService
    {
        Task<List<Room>> SearchAvailableRoomsAsync(DateTime checkIn, DateTime checkOut);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut);
        Task<List<RoomType>> GetAllRoomTypesAsync();

        Task<RoomType> CreateRoomTypeAsync(string name, string? description, decimal basePrice, int maxCapacity);
        Task<bool> UpdateRoomTypeAsync(int roomTypeId, string name, string? description, decimal basePrice, int maxCapacity);
        Task<bool> DeleteRoomTypeAsync(int roomTypeId);
    }
}