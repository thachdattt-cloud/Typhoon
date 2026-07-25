using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuanLyKhachSan_fix.Services.Interfaces.Manager;

namespace QuanLyKhachSan_fix.Services.Implementations.Manager
{
    public class ReportService : IReportService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ReportService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<decimal> GetMonthlyRevenueAsync(int year, int month)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var total = await db.Payments
                .Where(p => p.PaymentStatus == "success")
                .Where(p => p.PaidAt != null && p.PaidAt.Value.Year == year && p.PaidAt.Value.Month == month)
                .SumAsync(p => (decimal?)p.Amount);

            return total ?? 0m;
        }

        public async Task<double> GetOccupancyRateAsync(DateTime fromDate, DateTime toDate)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            int totalRooms = await db.Rooms.CountAsync();
            if (totalRooms == 0) return 0;

            int totalNightsAvailable = totalRooms * (toDate - fromDate).Days;
            if (totalNightsAvailable <= 0) return 0;

            var bookingDetails = await db.BookingDetails
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
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.BookingDetails
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
        }
    }
}