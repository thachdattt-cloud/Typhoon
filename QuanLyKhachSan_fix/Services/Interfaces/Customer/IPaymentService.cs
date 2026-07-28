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
        // Tao 1 lan thanh toan cho booking (method: "online" hoac "cash")
        Task<PaymentResult> CreatePaymentAsync(int bookingId, decimal amount, string method);

        // Lich su thanh toan cua 1 booking
        Task<List<Payment>> GetPaymentsByBookingAsync(int bookingId);

        // So tien con phai thanh toan = TotalAmount - tong cac Payment da "success"
        // Thanh vien 2 nen goi ham nay TRUOC khi cho Tra phong (CheckOutAsync) hoac Xuat
        // hoa don (CreateInvoiceAsync), de chan truong hop khach chua thanh toan du ma
        // van duoc tra phong / xuat hoa don.
        Task<decimal> GetOutstandingAmountAsync(int bookingId);

        // Le tan xac nhan da thu tien mat tai quay cho 1 phieu thanh toan dang "pending"
        // -> chuyen sang "success". Day la ham DUY NHAT trong toan he thong chuyen payment
        // tu pending sang success cho "cash" - thieu buoc nay thi payment cash se "pending"
        // vinh vien du le tan co xuat hoa don hay khong.
        Task<PaymentResult> ConfirmCashPaymentAsync(int paymentId, int staffId);
    }
}