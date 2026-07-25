using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Bookings.Commands
{
    /// <summary>
    /// Command tạo đơn đặt phòng mới từ phía khách hàng.
    /// </summary>
    public class CreateBookingCommand : IRequest<Response<Guid>>
    {
        public Guid CustomerId { get; set; }    // Set từ JWT token
        public Guid RoomTypeId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int NumRooms { get; set; } = 1;
        public int NumAdults { get; set; } = 1;
        public int NumChildren { get; set; } = 0;
        public string GuestName { get; set; } = null!;
        public string GuestPhone { get; set; } = null!;
        public string GuestEmail { get; set; } = null!;
        public string? SpecialRequests { get; set; }
    }

    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Response<Guid>>
    {
        private readonly IApplicationDbContext _context;
        public CreateBookingCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<Guid>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate ngày nhận phòng không phải là quá khứ
            // Dùng múìte giờ Việt Nam (UTC+7) để tránh lỗi ngược múc giờ
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
            var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var today = DateOnly.FromDateTime(nowVn);
            if (request.CheckInDate < today)
                throw new ApiException("Ngày nhận phòng không được là ngày trong quá khứ.");

            // 2. Lấy thông tin loại phòng, bao gồm TotalRooms và BasePrice để tính toán
            var roomType = await _context.RoomTypes
                .Include(rt => rt.Hotel)
                .ThenInclude(h => h.Business)
                .FirstOrDefaultAsync(rt => rt.Id == request.RoomTypeId && rt.IsActive, cancellationToken);

            if (roomType == null)
                throw new ApiException("Loại phòng không tồn tại hoặc đã ngừng kinh doanh.");

            // 3. Kiểm tra khách sạn đang hoạt động
            if (roomType.Hotel == null || !roomType.Hotel.IsActive)
                throw new ApiException("Khách sạn hiện không hoạt động hoặc chưa được phê duyệt.");

            // Kiểm tra khách không tự đặt phòng của chính mình
            if (roomType.Hotel.Business != null && roomType.Hotel.Business.OwnerId == request.CustomerId)
                throw new ApiException("Bạn không thể tự đặt phòng tại khách sạn của chính mình.");

            // Kiểm tra số lượng khách
            if (request.NumAdults < 1) throw new ApiException("Số người lớn tối thiểu là 1.");
            if (request.NumChildren < 0) throw new ApiException("Số trẻ em không được âm.");
            if (request.NumAdults > roomType.MaxAdults * request.NumRooms || request.NumChildren > roomType.MaxChildren * request.NumRooms)
                throw new ApiException($"Sức chứa tối đa của 1 phòng là {roomType.MaxAdults} người lớn và {roomType.MaxChildren} trẻ em. Vui lòng chọn thêm số lượng phòng.");

            // 4. Tính số đêm lưu trú
            var numNights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;
            if (numNights <= 0)
                throw new ApiException("Ngày trả phòng phải sau ngày nhận phòng.");
            if (numNights > 30)
                throw new ApiException("Hệ thống chỉ hỗ trợ đặt phòng tối đa 30 đêm.");

            // Bọc toàn bộ quá trình kiểm tra phòng và tạo booking trong execution strategy + transaction
            // (cần dùng CreateExecutionStrategy để tương thích với EnableRetryOnFailure)
            Guid newBookingId = Guid.Empty;
            await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 5. Kiểm tra phòng còn trống:
                    // Overlap: booking.CheckIn < request.CheckOut VÀ booking.CheckOut > request.CheckIn
                    var bookedRooms = await _context.Bookings
                        .Where(b =>
                            b.RoomTypeId == request.RoomTypeId &&
                            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved || b.Status == BookingStatus.Confirmed) &&
                            b.CheckInDate < request.CheckOutDate &&
                            b.CheckOutDate > request.CheckInDate)
                        .SumAsync(b => b.NumRooms, cancellationToken);

                    var availableRooms = roomType.TotalRooms - bookedRooms;
                    if (availableRooms < request.NumRooms)
                        throw new ApiException($"Rất tiếc, loại phòng này chỉ còn {availableRooms} phòng trống hoặc vừa được khách khác đặt hết. Vui lòng tải lại hoặc chọn loại phòng khác.");

                    // 6. Snapshot CancellationPolicyId tại thời điểm đặt phòng
                    var cancelPolicy = await _context.HotelCancellationPolicies
                        .FirstOrDefaultAsync(p => p.HotelId == roomType.HotelId && p.IsActive, cancellationToken);

                    // Lấy chính sách cọc
                    var depositPolicy = await _context.HotelDepositPolicies
                        .FirstOrDefaultAsync(p => p.HotelId == roomType.HotelId && p.IsActive, cancellationToken);
                    var depositPercentage = depositPolicy?.DepositPercentage ?? 50m; // Mặc định 50% nếu chưa có

                    // 7. Tính tổng tiền và cọc
                    var totalPrice = roomType.BasePrice * request.NumRooms * numNights;
                    var depositAmount = Math.Round(totalPrice * (depositPercentage / 100m), 0);
                    var now = DateTime.UtcNow;

                    var booking = new Booking
                    {
                        Id = Guid.NewGuid(),
                        RoomTypeId = request.RoomTypeId,
                        HotelId = roomType.HotelId,
                        CustomerId = request.CustomerId,
                        CheckInDate = request.CheckInDate,
                        CheckOutDate = request.CheckOutDate,
                        NumRooms = request.NumRooms,
                        NumAdults = request.NumAdults,
                        NumChildren = request.NumChildren,
                        TotalPrice = totalPrice,
                        Status = BookingStatus.Pending,
                        GuestName = request.GuestName,
                        GuestPhone = request.GuestPhone,
                        GuestEmail = request.GuestEmail,
                        SpecialRequests = request.SpecialRequests,
                        CancellationPolicyId = cancelPolicy?.Id,   // snapshot — null nếu KS chưa thiết lập policy
                        PenaltyPercentageSnapshot = cancelPolicy?.PenaltyPercentage,
                        DepositPolicyId = depositPolicy?.Id,
                        DepositPercentageSnapshot = depositPercentage,
                        // --- Thanh toán cọc ---
                        PaymentStatus = PaymentStatus.Unpaid,
                        DepositAmount = depositAmount,
                        DepositDeadline = null, // Chỉ thiết lập khi Quản lý duyệt đơn
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    newBookingId = booking.Id;
                }
                catch (ApiException)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return new Response<Guid>(newBookingId, "Đặt phòng thành công! Vui lòng chờ khách sạn xác nhận.");
        }
    }

    public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
    {
        public CreateBookingCommandValidator()
        {
            RuleFor(x => x.RoomTypeId).NotEmpty();

            RuleFor(x => x.CheckInDate).NotEmpty();

            RuleFor(x => x.CheckOutDate).NotEmpty()
                .GreaterThan(x => x.CheckInDate).WithMessage("Ngày trả phòng phải sau ngày nhận phòng.");

            RuleFor(x => x.NumRooms)
                .GreaterThanOrEqualTo(1).WithMessage("Số phòng ít nhất là 1.");

            RuleFor(x => x.NumAdults)
                .GreaterThanOrEqualTo(1).WithMessage("Số người lớn ít nhất là 1.");

            RuleFor(x => x.NumChildren)
                .GreaterThanOrEqualTo(0).WithMessage("Số trẻ em không được âm.");

            RuleFor(x => x.GuestName)
                .NotEmpty().WithMessage("Vui lòng nhập tên người đặt.");

            RuleFor(x => x.GuestPhone)
                .NotEmpty().WithMessage("Vui lòng nhập số điện thoại.")
                .Matches(@"^\d{10,11}$").WithMessage("Số điện thoại phải gồm 10-11 chữ số.");

            RuleFor(x => x.GuestEmail)
                .NotEmpty().WithMessage("Vui lòng nhập email.")
                .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Email không hợp lệ (phải có @ và tên miền hợp lệ).");
        }
    }
}
