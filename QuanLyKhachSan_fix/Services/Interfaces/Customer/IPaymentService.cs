using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyKhachSan_fix.Models;

namespace QuanLyKhachSan_fix.Services.Interfaces.Customer
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Payment? Payment { get; set; }
    }

    public interface IPaymentService
    {
        Task<PaymentResult> CreatePaymentAsync(int bookingId, decimal amount, string method);

        Task<List<Payment>> GetPaymentsByBookingAsync(int bookingId);

        Task<decimal> GetOutstandingAmountAsync(int bookingId);

        Task<PaymentResult> ConfirmCashPaymentAsync(int paymentId, int staffId);

        Task<PaymentResult> CreateStaffCollectedPaymentAsync(int bookingId, decimal amount, string method, int staffId);
    }
}