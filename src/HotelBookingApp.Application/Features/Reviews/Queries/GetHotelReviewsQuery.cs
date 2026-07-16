using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Reviews.Queries;

public class GetHotelReviewsQuery : IRequest<Response<List<HotelReviewDto>>>
{
    public Guid HotelId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class HotelReviewDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? Comment { get; set; }

    // Điểm các tiêu chí (null = không đánh giá)
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

    /// <summary>Điểm trung bình của tất cả tiêu chí đã đánh giá.</summary>
    public double? AverageScore { get; set; }
}

public class GetHotelReviewsQueryHandler : IRequestHandler<GetHotelReviewsQuery, Response<List<HotelReviewDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetHotelReviewsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Response<List<HotelReviewDto>>> Handle(GetHotelReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _context.Reviews
            .Include(r => r.Customer)
            .Where(r => r.HotelId == request.HotelId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = reviews.Select(r =>
        {
            var scores = new[] { r.ScoreSpace, r.ScoreService, r.ScoreExperience, r.ScoreSafety,
                                 r.ScoreCleanliness, r.ScoreView, r.ScoreRoomQuality, r.ScoreFood,
                                 r.ScoreQuietness, r.ScoreStaffFriendliness }
                          .Where(s => s.HasValue).Select(s => s!.Value).ToList();

            return new HotelReviewDto
            {
                Id = r.Id,
                CustomerName = r.Customer?.FullName ?? r.Customer?.Username ?? "Khách hàng",
                CreatedAt = r.CreatedAt,
                Comment = r.Comment,
                ScoreSpace = r.ScoreSpace,
                ScoreService = r.ScoreService,
                ScoreExperience = r.ScoreExperience,
                ScoreSafety = r.ScoreSafety,
                ScoreCleanliness = r.ScoreCleanliness,
                ScoreView = r.ScoreView,
                ScoreRoomQuality = r.ScoreRoomQuality,
                ScoreFood = r.ScoreFood,
                ScoreQuietness = r.ScoreQuietness,
                ScoreStaffFriendliness = r.ScoreStaffFriendliness,
                AverageScore = scores.Any() ? Math.Round(scores.Average(), 1) : null
            };
        }).ToList();

        return new Response<List<HotelReviewDto>>(dtos);
    }
}
