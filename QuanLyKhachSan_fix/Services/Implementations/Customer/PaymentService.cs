using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces.Customer;

namespace QuanLyKhachSan_fix.Services.Implementations.Customer
{
    public class PaymentService : IPaymentService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public PaymentService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<PaymentResult> CreatePaymentAsync(int bookingId, decimal amount, string method)
        {
            if (amount <= 0)
            {
                return new PaymentResult { Success = false, ErrorMessage = "Số tiền thanh toán không hợp lệ." };
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null)
            {
                return new PaymentResult { Success = false, ErrorMessage = "Không tìm thấy đặt phòng." };
            }

            if (booking.Status == "cancelled")
            {
                return new PaymentResult { Success = false, ErrorMessage = "Đặt phòng đã bị hủy, không thể thanh toán." };
            }

            bool hasPendingPayment = await db.Payments
                .AnyAsync(p => p.BookingId == bookingId && p.PaymentStatus == "pending");

            if (hasPendingPayment && method != "online")
            {
                return new PaymentResult { Success = false, ErrorMessage = "Đã có 1 yêu cầu thanh toán tiền mặt đang chờ lễ tân xác nhận cho đặt phòng này. Vui lòng đợi xử lý xong trước khi thanh toán tiếp." };
            }

            bool isOnline = method == "online";

            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentMethod = method,
                PaymentStatus = isOnline ? "success" : "pending",
                PaidAt = isOnline ? DateTime.Now : null,
                CreatedAt = DateTime.Now
            };

            db.Payments.Add(payment);

            if (isOnline && booking.Status == "pending")
            {
                booking.Status = "confirmed";
                booking.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();

            return new PaymentResult { Success = true, Payment = payment };
        }

        public async Task<List<Payment>> GetPaymentsByBookingAsync(int bookingId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Payments
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetOutstandingAmountAsync(int bookingId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return 0;

            decimal paid = await db.Payments
                .Where(p => p.BookingId == bookingId && p.PaymentStatus == "success")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            decimal outstanding = (booking.TotalAmount ?? 0) - paid;
            return outstanding < 0 ? 0 : outstanding;
        }

        public async Task<PaymentResult> ConfirmCashPaymentAsync(int paymentId, int staffId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var payment = await db.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                return new PaymentResult { Success = false, ErrorMessage = "Không tìm thấy phiếu thanh toán." };
            }

            if (payment.PaymentStatus != "pending")
            {
                return new PaymentResult { Success = false, ErrorMessage = "Phiếu thanh toán này đã được xử lý." };
            }

            var staffExists = await db.Users.AnyAsync(u => u.Id == staffId);
            if (!staffExists)
            {
                return new PaymentResult { Success = false, ErrorMessage = "Mã nhân viên không tồn tại." };
            }

            payment.PaymentStatus = "success";
            payment.PaidAt = DateTime.Now;

            if (payment.Booking.Status == "pending")
            {
                payment.Booking.Status = "confirmed";
                payment.Booking.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();

            return new PaymentResult { Success = true, Payment = payment };
        }

        public async Task<PaymentResult> CreateStaffCollectedPaymentAsync(int bookingId, decimal amount, string method, int staffId)
        {
            if (amount <= 0)
            {
                return new PaymentResult { Success = false, ErrorMessage = "Số tiền thanh toán không hợp lệ." };
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null)
            {
                return new PaymentResult { Success = false, ErrorMessage = "Không tìm thấy đặt phòng." };
            }

            if (booking.Status == "cancelled")
            {
                return new PaymentResult { Success = false, ErrorMessage = "Đặt phòng đã bị hủy, không thể thanh toán." };
            }

            var staffExists = await db.Users.AnyAsync(u => u.Id == staffId);
            if (!staffExists)
            {
                return new PaymentResult { Success = false, ErrorMessage = "Mã nhân viên không tồn tại." };
            }

            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentMethod = method,
                PaymentStatus = "success",
                PaidAt = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            db.Payments.Add(payment);

            if (booking.Status == "pending")
            {
                booking.Status = "confirmed";
                booking.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();

            return new PaymentResult { Success = true, Payment = payment };
        }
    }
}