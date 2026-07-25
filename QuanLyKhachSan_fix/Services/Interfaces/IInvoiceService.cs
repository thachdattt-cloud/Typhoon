using QuanLyKhachSan_fix.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyKhachSan_fix.Services.Interfaces
{
    public class InvoiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Invoice? Invoice { get; set; }
    }

    public interface IInvoiceService
    {
        Task<List<Booking>> GetBookingsReadyForInvoiceAsync();
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<decimal> CalculateTotalAmountAsync(int bookingId);
        Task<InvoiceResult> CreateInvoiceAsync(int bookingId, int issuedByUserId);
    }
}
