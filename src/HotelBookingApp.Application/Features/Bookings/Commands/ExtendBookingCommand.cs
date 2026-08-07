using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Bookings.Commands
{
    public class ExtendBookingCommand : IRequest<Response<Guid>>
    {
        public Guid BookingId { get; set; }
        public Guid ManagerId { get; set; }
        public DateOnly NewCheckOutDate { get; set; }
        public List<Queries.ExtensionItemInput> Items { get; set; } = new();
    }

    public class ExtendBookingCommandHandler : IRequestHandler<ExtendBookingCommand, Response<Guid>>
    {
        private readonly IApplicationDbContext _context;

        public ExtendBookingCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response<Guid>> Handle(ExtendBookingCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);

            if (assignment == null)
                throw new ApiException("Bạn không có quyền quản lý khách sạn nào.");

            var originalBooking = await _context.Bookings
                .Include(b => b.Items)
                    .ThenInclude(i => i.RoomType)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.HotelId == assignment.HotelId, cancellationToken);

            if (originalBooking == null)
                throw new ApiException("Không tìm thấy đơn đặt phòng.");

            if (originalBooking.Status != BookingStatus.Confirmed || originalBooking.PaymentStatus != PaymentStatus.Paid)
                throw new ApiException("Chỉ có thể gia hạn cho đơn đã được xác nhận và thanh toán cọc.");

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone));

            if (today < originalBooking.CheckInDate || today >= originalBooking.CheckOutDate)
                throw new ApiException("Chỉ có thể gia hạn khi khách đang trong thời gian lưu trú tại khách sạn.");

            var numNights = request.NewCheckOutDate.DayNumber - originalBooking.CheckOutDate.DayNumber;
            if (numNights <= 0)
                throw new ApiException("Ngày trả phòng mới phải sau ngày trả phòng hiện tại.");
            if (numNights > 30)
                throw new ApiException("Chỉ có thể gia hạn tối đa 30 đêm.");

            Guid newBookingId = Guid.Empty;

            await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    decimal totalPrice = 0;
                    var bookingItems = new List<BookingItem>();
                    int newNumAdults = 0;
                    int newNumChildren = 0;

                    foreach (var reqItem in request.Items)
                    {
                        if (reqItem.NumRooms <= 0) continue;

                        var originalItem = originalBooking.Items.FirstOrDefault(i => i.RoomTypeId == reqItem.RoomTypeId);
                        if (originalItem == null)
                            throw new ApiException("Loại phòng yêu cầu không nằm trong đơn gốc.");
                        if (reqItem.NumRooms > originalItem.NumRooms)
                            throw new ApiException($"Số lượng phòng yêu cầu cho loại phòng '{originalItem.RoomType.Name}' vượt quá số lượng trong đơn gốc.");

                        var rt = originalItem.RoomType;

                        // Check availability for [oldCheckOutDate, newCheckOutDate)
                        var bookedRooms = await _context.BookingItems
                            .Where(bi =>
                                bi.RoomTypeId == rt.Id &&
                                (bi.Booking.Status == BookingStatus.Pending || bi.Booking.Status == BookingStatus.Approved || bi.Booking.Status == BookingStatus.Confirmed) &&
                                bi.Booking.CheckInDate < request.NewCheckOutDate &&
                                bi.Booking.CheckOutDate > originalBooking.CheckOutDate)
                            .SumAsync(bi => (int?)bi.NumRooms, cancellationToken) ?? 0;

                        var availableRooms = rt.TotalRooms - bookedRooms;
                        if (availableRooms < reqItem.NumRooms)
                            throw new ApiException($"Loại phòng '{rt.Name}' chỉ còn {availableRooms} phòng trống trong khoảng thời gian gia hạn. Khách cần {reqItem.NumRooms} phòng.");

                        var subTotal = originalItem.UnitPrice * reqItem.NumRooms * numNights;
                        totalPrice += subTotal;

                        newNumAdults += rt.MaxAdults * reqItem.NumRooms;
                        newNumChildren += rt.MaxChildren * reqItem.NumRooms;

                        bookingItems.Add(new BookingItem
                        {
                            Id = Guid.NewGuid(),
                            RoomTypeId = rt.Id,
                            NumRooms = reqItem.NumRooms,
                            UnitPrice = originalItem.UnitPrice,
                            SubTotal = subTotal
                        });
                    }

                    if (bookingItems.Count == 0)
                        throw new ApiException("Vui lòng chọn ít nhất một loại phòng để gia hạn.");

                    // Giới hạn lại số người tối đa bằng cách lấy min của số lượng tối đa sức chứa và số khách khai báo ban đầu.
                    int finalAdults = Math.Min(originalBooking.NumAdults, newNumAdults);
                    int finalChildren = Math.Min(originalBooking.NumChildren, newNumChildren);

                    // Đảm bảo có ít nhất 1 người lớn
                    if (finalAdults < 1) finalAdults = 1;

                    var now = DateTime.UtcNow;

                    var newBooking = new Booking
                    {
                        Id = Guid.NewGuid(),
                        HotelId = originalBooking.HotelId,
                        CustomerId = originalBooking.CustomerId,
                        CheckInDate = originalBooking.CheckOutDate,
                        CheckOutDate = request.NewCheckOutDate,
                        NumAdults = finalAdults,
                        NumChildren = finalChildren,
                        TotalPrice = totalPrice,
                        Status = BookingStatus.Confirmed,
                        GuestName = originalBooking.GuestName,
                        GuestPhone = originalBooking.GuestPhone,
                        GuestEmail = originalBooking.GuestEmail,
                        SpecialRequests = "Đơn gia hạn cho mã phòng: " + originalBooking.Id,
                        CancellationPolicyId = null,
                        PenaltyPercentageSnapshot = null,
                        DepositPolicyId = null,
                        DepositPercentageSnapshot = null,
                        PaymentStatus = PaymentStatus.Paid,
                        DepositAmount = 0, // Không yêu cầu cọc cho đơn gia hạn (khách trả tại quầy)
                        PaidAt = now,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Items = bookingItems
                    };

                    _context.Bookings.Add(newBooking);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    newBookingId = newBooking.Id;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return new Response<Guid>(newBookingId, "Gia hạn phòng thành công. Đơn mới đã được tự động duyệt và xác nhận.");
        }
    }

    public class ExtendBookingCommandValidator : AbstractValidator<ExtendBookingCommand>
    {
        public ExtendBookingCommandValidator()
        {
            RuleFor(x => x.BookingId).NotEmpty();
            RuleFor(x => x.NewCheckOutDate).NotEmpty();
            RuleFor(x => x.Items).NotEmpty().WithMessage("Vui lòng chọn ít nhất 1 loại phòng.");
        }
    }
}
