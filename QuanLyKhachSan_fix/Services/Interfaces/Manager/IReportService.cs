using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Interfaces.Manager
{
    public class RevenueByRoomType
    {
        public string RoomTypeName { get; set; } = null!;
        public decimal Revenue { get; set; }
    }

    public interface IReportService
    {
        Task<decimal> GetMonthlyRevenueAsync(int year, int month);
        Task<double> GetOccupancyRateAsync(DateTime fromDate, DateTime toDate);
        Task<List<RevenueByRoomType>> GetRevenueByRoomTypeAsync(int year, int month);
    }
}