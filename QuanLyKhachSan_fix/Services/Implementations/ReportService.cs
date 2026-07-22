using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Services.Interfaces;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<decimal> GetMonthlyRevenueAsync(int year, int month)
        {
            var total = await _db.Payments
                .Where(p => p.PaymentStatus == "success")
                .Where(p => p.PaidAt != null && p.PaidAt.Value.Year == year && p.PaidAt.Value.Month == month)
                .SumAsync(p => (decimal?)p.Amount);

            return total ?? 0m;
        }

        public async Task<double> GetOccupancyRateAsync(DateTime fromDate, DateTime toDate)
        {
            int totalRooms = await _db.Rooms.CountAsync();
            if (totalRooms == 0) return 0;

            int totalNightsAvailable = totalRooms * (toDate - fromDate).Days;
            if (totalNightsAvailable <= 0) return 0;

            var bookingDetails = await _db.BookingDetails
                .Where(bd => bd.Status != "cancelled")
                .Include(bd => bd.Booking)
                .Where(bd => bd.Booking.CheckInDate < toDate && bd.Booking.CheckOutDate > fromDate)
                .ToListAsync();

            int bookedNights = bookingDetails.Sum(bd =>
            {
                var start = bd.Booking.CheckInDate < fromDate ? fromDate : bd.Booking.CheckInDate;
                var end = bd.Booking.CheckOutDate > toDate ? toDate : bd.Booking.CheckOutDate;
                return Math.Max(0, (end - start).Days);
            });

            return Math.Round((double)bookedNights / totalNightsAvailable * 100, 1);
        }

        public async Task<List<RevenueByRoomType>> GetRevenueByRoomTypeAsync(int year, int month)
        {
            var query = await _db.BookingDetails
                .Include(bd => bd.Room).ThenInclude(r => r.RoomType)
                .Include(bd => bd.Booking)
                .Where(bd => bd.Booking.CreatedAt.Year == year && bd.Booking.CreatedAt.Month == month)
                .Where(bd => bd.Status != "cancelled")
                .GroupBy(bd => bd.Room.RoomType.Name)
                .Select(g => new RevenueByRoomType
                {
                    RoomTypeName = g.Key,
                    Revenue = g.Sum(x => x.Price)
                })
                .ToListAsync();

            return query;
        }
    }
}