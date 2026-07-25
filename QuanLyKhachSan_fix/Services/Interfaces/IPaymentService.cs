using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyKhachSan_fix.Models;

namespace QuanLyKhachSan_fix.Services.Interfaces
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Payment? Payment { get; set; }
    }

    public interface IPaymentService
    {
        // Tao 1 lan thanh toan cho booking (method: "online" hoac "cash")
        Task<PaymentResult> CreatePaymentAsync(int bookingId, decimal amount, string method);

        // Lich su thanh toan cua 1 booking
        Task<List<Payment>> GetPaymentsByBookingAsync(int bookingId);
    }
}
