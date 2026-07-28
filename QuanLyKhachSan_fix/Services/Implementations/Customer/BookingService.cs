using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces.Customer;
using QuanLyKhachSan_fix.Services.Interfaces.Manager;

namespace QuanLyKhachSan_fix.Services.Implementations.Customer
{
    public class BookingService : IBookingService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IRoomService _roomService;

        public BookingService(IDbContextFactory<AppDbContext> dbFactory, IRoomService roomService)
        {
            _dbFactory = dbFactory;
            _roomService = roomService;
        }

        public async Task<BookingResult> CreateBookingAsync(int customerId, int roomId, DateTime checkInDate, DateTime checkOutDate, int guestCount)
        {
            if (checkOutDate <= checkInDate)
            {
                return new BookingResult { Success = false, ErrorMessage = "Ngày trả phòng phải sau ngày nhận phòng." };
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var room = await db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Không tìm thấy phòng." };
            }

            // Kiem tra lai lan nua phong con trong trong khoang ngay nay (tranh dat trung)
            bool isAvailable = await _roomService.IsRoomAvailableAsync(roomId, checkInDate, checkOutDate);

            if (!isAvailable)
            {
                return new BookingResult { Success = false, ErrorMessage = "Phòng đã được đặt trong khoảng ngày này." };
            }

            decimal price = room.RoomType?.BasePrice ?? 0m;
            int nights = (checkOutDate.Date - checkInDate.Date).Days;
            decimal totalAmount = price * nights;

            var booking = new Booking
            {
                CustomerId = customerId,
                BookingCode = "BK" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                CheckInDate = checkInDate.Date,
                CheckOutDate = checkOutDate.Date,
                Status = "pending",
                TotalAmount = totalAmount,
                CreatedBy = customerId,
                CreatedAt = DateTime.Now
            };

            booking.BookingDetails.Add(new BookingDetail
            {
                RoomId = roomId,
                Price = price,
                GuestCount = guestCount,
                Status = "reserved"
            });

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            return new BookingResult { Success = true, Booking = booking };
        }

        public async Task<List<Booking>> GetBookingsByCustomerAsync(int customerId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Bookings
                .Include(b => b.BookingDetails).ThenInclude(bd => bd.Room).ThenInclude(r => r.RoomType)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Bookings
                .Include(b => b.BookingDetails).ThenInclude(bd => bd.Room).ThenInclude(r => r.RoomType)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
        }

        public async Task<BookingResult> CancelBookingAsync(int bookingId, int customerId, string? reason)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return new BookingResult { Success = false, ErrorMessage = "Không tìm thấy đặt phòng." };
            }

            if (booking.CustomerId != customerId)
            {
                return new BookingResult { Success = false, ErrorMessage = "Bạn không có quyền hủy đặt phòng này." };
            }

            if (booking.Status != "pending" && booking.Status != "confirmed")
            {
                return new BookingResult { Success = false, ErrorMessage = "Chỉ có thể hủy đặt phòng khi chưa nhận phòng." };
            }

            booking.Status = "cancelled";
            booking.UpdatedAt = DateTime.Now;

            foreach (var detail in booking.BookingDetails)
            {
                detail.Status = "cancelled";
            }

            // SUA: hoan thien luong huy - truoc day tao Cancellation voi Status =
            // "requested" nhung khong co ham nao trong he thong duyet no ca (khong co
            // ApproveCancellation/ProcessCancellation), trong khi Booking da bi huy hoan
            // tat ngay lap tuc -> ban ghi Cancellation bi "mo coi", treo mai o pending, va
            // tien da thanh toan (neu co) khong duoc tinh hoan/khong hoan.
            //
            // Chinh sach don gian: huy truoc gio nhan phong (CheckInDate) it nhat 24h thi
            // hoan 100% da thanh toan; huy sat gio hon thi mat coc (khong hoan). Vi Booking
            // da "cancelled" ngay khi khach bam huy (khong co buoc le tan duyet rieng), nen
            // Cancellation duoc dong luon ("processed") thay vi de "requested" treo vinh vien.
            decimal totalPaid = await db.Payments
                .Where(p => p.BookingId == booking.Id && p.PaymentStatus == "success")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            bool cancelledInTime = (booking.CheckInDate - DateTime.Now) >= TimeSpan.FromHours(24);
            decimal refundAmount = cancelledInTime ? totalPaid : 0;
            decimal cancellationFee = totalPaid - refundAmount;

            db.Cancellations.Add(new Cancellation
            {
                BookingId = booking.Id,
                CancelReason = string.IsNullOrWhiteSpace(reason) ? "Khách hàng tự hủy" : reason,
                CancellationFee = cancellationFee,
                RefundAmount = refundAmount,
                Status = "processed",
                CancelledAt = DateTime.Now,
                ProcessedBy = customerId
            });

            await db.SaveChangesAsync();

            return new BookingResult { Success = true, Booking = booking };
        }
    }
}