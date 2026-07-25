using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_fix.Data;
using QuanLyKhachSan_fix.Models;
using QuanLyKhachSan_fix.Services.Interfaces.Receptionist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Implementations.Receptionist
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public InvoiceService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Booking>> GetBookingsReadyForInvoiceAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails).ThenInclude(bd => bd.Room)
                .Include(b => b.Invoices)
                .Where(b => b.Status == "checked_out")
                .Where(b => !b.Invoices.Any())
                .OrderBy(b => b.CheckOutDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Invoices
                .Include(i => i.Booking).ThenInclude(b => b.Customer)
                .Include(i => i.IssuedByUser)
                .OrderByDescending(i => i.IssuedAt)
                .ToListAsync();
        }

        public async Task<decimal> CalculateTotalAmountAsync(int bookingId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return 0;

            if (booking.TotalAmount.HasValue && booking.TotalAmount.Value > 0)
                return booking.TotalAmount.Value;

            return booking.BookingDetails.Sum(bd => bd.Price);
        }

        public async Task<InvoiceResult> CreateInvoiceAsync(int bookingId, int issuedByUserId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var booking = await db.Bookings
                .Include(b => b.BookingDetails)
                .Include(b => b.Invoices)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return new InvoiceResult { Success = false, ErrorMessage = "Không tìm thấy đơn đặt phòng." };

            if (booking.Status != "checked_out")
                return new InvoiceResult { Success = false, ErrorMessage = "Chỉ có thể xuất hóa đơn sau khi khách đã trả phòng." };

            if (booking.Invoices.Any())
                return new InvoiceResult { Success = false, ErrorMessage = "Đơn đặt phòng này đã có hóa đơn." };

            var issuerExists = await db.Users.AnyAsync(u => u.Id == issuedByUserId);
            if (!issuerExists)
                return new InvoiceResult { Success = false, ErrorMessage = "Mã nhân viên không tồn tại." };

            var totalAmount = booking.TotalAmount.HasValue && booking.TotalAmount.Value > 0
                ? booking.TotalAmount.Value
                : booking.BookingDetails.Sum(bd => bd.Price);

            var invoice = new Invoice
            {
                BookingId = booking.Id,
                TotalAmount = totalAmount,
                IssuedBy = issuedByUserId,
                IssuedAt = DateTime.Now
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            return new InvoiceResult { Success = true, Invoice = invoice };
        }
    }
}
