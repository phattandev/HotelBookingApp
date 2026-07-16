using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Reviews.Commands;

/// <summary>
/// Command tạo đánh giá cho khách sạn sau khi checkout.
/// Chỉ tạo được khi đơn đặt phòng có Status = Completed.
/// Mỗi đơn chỉ được đánh giá 1 lần.
/// </summary>
public class CreateReviewCommand : IRequest<Response<Guid>>
{
    public Guid BookingId { get; set; }
    public Guid CustomerId { get; set; }  // Set từ JWT

    // Các tiêu chí (nullable = không đánh giá tiêu chí đó)
    public int? ScoreSpace { get; set; }
    public int? ScoreService { get; set; }
    public int? ScoreExperience { get; set; }
    public int? ScoreSafety { get; set; }
    public int? ScoreCleanliness { get; set; }
    public int? ScoreView { get; set; }
    public int? ScoreRoomQuality { get; set; }
    public int? ScoreFood { get; set; }
    public int? ScoreQuietness { get; set; }
    public int? ScoreStaffFriendliness { get; set; }
    public string? Comment { get; set; }
}

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Response<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateReviewCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Response<Guid>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        // 1. Lấy booking và kiểm tra quyền sở hữu
        var booking = await _context.Bookings
            .Include(b => b.RoomType)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
            throw new ApiException("Không tìm thấy đơn đặt phòng.");

        if (booking.CustomerId != request.CustomerId)
            throw new ApiException("Bạn không có quyền đánh giá đơn này.");

        // 2. Chỉ cho phép đánh giá khi đơn đã Completed
        if (booking.Status != BookingStatus.Completed)
            throw new ApiException("Chỉ có thể đánh giá sau khi đã hoàn tất lưu trú (trạng thái Completed).");

        // 3. Kiểm tra đã đánh giá chưa
        var existing = await _context.Reviews
            .AnyAsync(r => r.BookingId == request.BookingId, cancellationToken);
        if (existing)
            throw new ApiException("Bạn đã đánh giá đơn đặt phòng này rồi.");

        // 4. Validate ít nhất 1 tiêu chí hoặc comment
        var hasScore = request.ScoreSpace.HasValue || request.ScoreService.HasValue ||
                       request.ScoreExperience.HasValue || request.ScoreSafety.HasValue ||
                       request.ScoreCleanliness.HasValue || request.ScoreView.HasValue ||
                       request.ScoreRoomQuality.HasValue || request.ScoreFood.HasValue ||
                       request.ScoreQuietness.HasValue || request.ScoreStaffFriendliness.HasValue;

        if (!hasScore && string.IsNullOrWhiteSpace(request.Comment))
            throw new ApiException("Vui lòng đánh giá ít nhất 1 tiêu chí hoặc nhập nhận xét.");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            BookingId = request.BookingId,
            HotelId = booking.RoomType.HotelId,
            CustomerId = request.CustomerId,
            ScoreSpace = request.ScoreSpace,
            ScoreService = request.ScoreService,
            ScoreExperience = request.ScoreExperience,
            ScoreSafety = request.ScoreSafety,
            ScoreCleanliness = request.ScoreCleanliness,
            ScoreView = request.ScoreView,
            ScoreRoomQuality = request.ScoreRoomQuality,
            ScoreFood = request.ScoreFood,
            ScoreQuietness = request.ScoreQuietness,
            ScoreStaffFriendliness = request.ScoreStaffFriendliness,
            Comment = request.Comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        return new Response<Guid>(review.Id, "Cảm ơn bạn đã đánh giá! Nhận xét của bạn sẽ giúp cải thiện chất lượng dịch vụ.");
    }
}
