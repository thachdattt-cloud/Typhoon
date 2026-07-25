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
    public class BookingEditRequestService : IBookingEditRequestService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public BookingEditRequestService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<EditRequestResult> CreateExtendRequestAsync(int bookingDetailId, DateTime newCheckOutDate)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var detail = await db.BookingDetails.Include(bd => bd.Booking).FirstOrDefaultAsync(bd => bd.Id == bookingDetailId);
            if (detail == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy đặt phòng." };
            }

            if (newCheckOutDate.Date <= detail.Booking.CheckOutDate.Date)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Ngày gia hạn phải sau ngày trả phòng hiện tại." };
            }

            var request = new BookingEditRequest
            {
                BookingDetailId = bookingDetailId,
                RequestType = "extend",
                NewCheckOutDate = newCheckOutDate.Date,
                Status = "pending",
                RequestedAt = DateTime.Now
            };

            db.BookingEditRequests.Add(request);
            await db.SaveChangesAsync();

            return new EditRequestResult { Success = true, EditRequest = request };
        }

        public async Task<EditRequestResult> CreateChangeRoomRequestAsync(int bookingDetailId, int newRoomId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var detail = await db.BookingDetails.FirstOrDefaultAsync(bd => bd.Id == bookingDetailId);
            if (detail == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy đặt phòng." };
            }

            var newRoom = await db.Rooms.FirstOrDefaultAsync(r => r.Id == newRoomId);
            if (newRoom == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy phòng muốn đổi." };
            }

            var request = new BookingEditRequest
            {
                BookingDetailId = bookingDetailId,
                RequestType = "change_room",
                OldRoomId = detail.RoomId,
                NewRoomId = newRoomId,
                Status = "pending",
                RequestedAt = DateTime.Now
            };

            db.BookingEditRequests.Add(request);
            await db.SaveChangesAsync();

            return new EditRequestResult { Success = true, EditRequest = request };
        }

        public async Task<List<BookingEditRequest>> GetRequestsByCustomerAsync(int customerId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.BookingEditRequests
                .Include(r => r.BookingDetail).ThenInclude(bd => bd.Booking)
                .Include(r => r.OldRoom)
                .Include(r => r.NewRoom)
                .Where(r => r.BookingDetail.Booking.CustomerId == customerId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<BookingEditRequest>> GetPendingRequestsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.BookingEditRequests
                .Include(r => r.BookingDetail).ThenInclude(bd => bd.Booking)
                .Include(r => r.OldRoom)
                .Include(r => r.NewRoom)
                .Where(r => r.Status == "pending")
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<EditRequestResult> ApproveRequestAsync(int requestId, int handledByUserId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.BookingEditRequests.Include(r => r.BookingDetail).ThenInclude(bd => bd.Booking).FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy yêu cầu." };
            }

            if (request.Status != "pending")
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Yêu cầu đã được xử lý." };
            }

            request.Status = "approved";
            request.HandledBy = handledByUserId;
            request.HandledAt = DateTime.Now;

            if (request.RequestType == "extend" && request.NewCheckOutDate.HasValue)
            {
                request.BookingDetail.Booking.CheckOutDate = request.NewCheckOutDate.Value;
            }
            else if (request.RequestType == "change_room" && request.NewRoomId.HasValue)
            {
                request.BookingDetail.RoomId = request.NewRoomId.Value;
            }

            await db.SaveChangesAsync();

            return new EditRequestResult { Success = true, EditRequest = request };
        }

        public async Task<EditRequestResult> RejectRequestAsync(int requestId, int handledByUserId, string? reason)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var request = await db.BookingEditRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null)
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Không tìm thấy yêu cầu." };
            }

            if (request.Status != "pending")
            {
                return new EditRequestResult { Success = false, ErrorMessage = "Yêu cầu đã được xử lý." };
            }

            request.Status = "rejected";
            request.HandledBy = handledByUserId;
            request.HandledAt = DateTime.Now;

            await db.SaveChangesAsync();

            return new EditRequestResult { Success = true, EditRequest = request };
        }
    }
}
