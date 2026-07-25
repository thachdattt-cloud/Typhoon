using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces;

namespace QuanLyKhachSan_fix.Services.Implementations
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

            // "online" gia lap thanh cong ngay; "cash" ghi nhan cho le tan thu tien tai quay
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

            // Neu thanh toan online thanh cong va booking dang pending -> chuyen sang confirmed
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
    }
}
