using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Implementations
{
    public class CheckInOutService : ICheckInOutService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public CheckInOutService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<BookingDetail>> GetTodayArrivalsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateTime.Today;

            return await db.BookingDetails
                .Include(bd => bd.Room).ThenInclude(r => r.RoomType)
                .Include(bd => bd.Booking).ThenInclude(b => b.Customer)
                .Where(bd => bd.Status == "reserved")
                .Where(bd => bd.Booking.CheckInDate.Date == today)
                .OrderBy(bd => bd.Room.RoomNumber)
                .ToListAsync();
        }

        public async Task<List<BookingDetail>> GetTodayDeparturesAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateTime.Today;

            return await db.BookingDetails
                .Include(bd => bd.Room).ThenInclude(r => r.RoomType)
                .Include(bd => bd.Booking).ThenInclude(b => b.Customer)
                .Where(bd => bd.Status == "checked_in")
                .Where(bd => bd.Booking.CheckOutDate.Date == today)
                .OrderBy(bd => bd.Room.RoomNumber)
                .ToListAsync();
        }

        public async Task<List<BookingDetail>> GetAllCheckedInAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.BookingDetails
                .Include(bd => bd.Room).ThenInclude(r => r.RoomType)
                .Include(bd => bd.Booking).ThenInclude(b => b.Customer)
                .Where(bd => bd.Status == "checked_in")
                .OrderBy(bd => bd.Room.RoomNumber)
                .ToListAsync();
        }

        public async Task<List<BookingDetail>> SearchReservedByBookingCodeAsync(string bookingCode)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.BookingDetails
                .Include(bd => bd.Room).ThenInclude(r => r.RoomType)
                .Include(bd => bd.Booking).ThenInclude(b => b.Customer)
                .Where(bd => bd.Status == "reserved")
                .Where(bd => bd.Booking.BookingCode != null && bd.Booking.BookingCode.Contains(bookingCode))
                .OrderBy(bd => bd.Room.RoomNumber)
                .ToListAsync();
        }

        public async Task<List<BookingDetail>> SearchCheckedInByBookingCodeAsync(string bookingCode)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.BookingDetails
                .Include(bd => bd.Room).ThenInclude(r => r.RoomType)
                .Include(bd => bd.Booking).ThenInclude(b => b.Customer)
                .Where(bd => bd.Status == "checked_in")
                .Where(bd => bd.Booking.BookingCode != null && bd.Booking.BookingCode.Contains(bookingCode))
                .OrderBy(bd => bd.Room.RoomNumber)
                .ToListAsync();
        }

        public async Task<CheckInOutResult> CheckInAsync(int bookingDetailId, int staffId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var detail = await db.BookingDetails
                .Include(bd => bd.Booking).ThenInclude(b => b.BookingDetails)
                .Include(bd => bd.Room)
                .FirstOrDefaultAsync(bd => bd.Id == bookingDetailId);

            if (detail == null)
                return new CheckInOutResult { Success = false, ErrorMessage = "Không tìm thấy chi tiết đặt phòng." };

            if (detail.Status != "reserved")
                return new CheckInOutResult { Success = false, ErrorMessage = "Phòng này không ở trạng thái chờ nhận phòng." };

            var staffExists = await db.Users.AnyAsync(u => u.Id == staffId);
            if (!staffExists)
                return new CheckInOutResult { Success = false, ErrorMessage = "Mã nhân viên không tồn tại." };

            detail.Status = "checked_in";
            detail.Room.Status = "occupied";

            db.CheckIns.Add(new CheckIn
            {
                BookingDetailId = detail.Id,
                StaffId = staffId,
                CheckinTime = DateTime.Now
            });

            if (detail.Booking.BookingDetails.All(d => d.Id == detail.Id || d.Status == "checked_in" || d.Status == "cancelled"))
            {
                detail.Booking.Status = "checked_in";
                detail.Booking.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();
            return new CheckInOutResult { Success = true };
        }

        public async Task<CheckInOutResult> CheckOutAsync(int bookingDetailId, int staffId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var detail = await db.BookingDetails
                .Include(bd => bd.Booking).ThenInclude(b => b.BookingDetails)
                .Include(bd => bd.Room)
                .FirstOrDefaultAsync(bd => bd.Id == bookingDetailId);

            if (detail == null)
                return new CheckInOutResult { Success = false, ErrorMessage = "Không tìm thấy chi tiết đặt phòng." };

            if (detail.Status != "checked_in")
                return new CheckInOutResult { Success = false, ErrorMessage = "Phòng này không ở trạng thái đang lưu trú." };

            var staffExists = await db.Users.AnyAsync(u => u.Id == staffId);
            if (!staffExists)
                return new CheckInOutResult { Success = false, ErrorMessage = "Mã nhân viên không tồn tại." };

            detail.Status = "checked_out";
            detail.Room.Status = "available";

            db.CheckOuts.Add(new CheckOut
            {
                BookingDetailId = detail.Id,
                StaffId = staffId,
                CheckoutTime = DateTime.Now
            });

            if (detail.Booking.BookingDetails.All(d => d.Id == detail.Id || d.Status == "checked_out" || d.Status == "cancelled"))
            {
                detail.Booking.Status = "checked_out";
                detail.Booking.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();
            return new CheckInOutResult { Success = true };
        }
    }
}
