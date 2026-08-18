using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Queries
{
    /// <summary>
    /// Query trả về danh sách gợi ý địa điểm (tỉnh/thành phố và xã/phường)
    /// có ít nhất 1 khách sạn đã được duyệt và đang hoạt động trong hệ thống.
    /// Dùng cho tính năng autocomplete ở ô tìm kiếm địa điểm.
    /// </summary>
    public class GetLocationSuggestionsQuery : IRequest<Response<IEnumerable<LocationSuggestionDto>>>
    {
        /// <summary>Từ khóa tìm kiếm (tối thiểu 1 ký tự)</summary>
        public string Q { get; set; } = string.Empty;

        /// <summary>Số lượng kết quả tối đa trả về (mặc định 8)</summary>
        public int Limit { get; set; } = 8;
    }

    public class GetLocationSuggestionsQueryHandler
        : IRequestHandler<GetLocationSuggestionsQuery, Response<IEnumerable<LocationSuggestionDto>>>
    {
        private readonly IApplicationDbContext _context;

        public GetLocationSuggestionsQueryHandler(IApplicationDbContext context)
            => _context = context;

        public async Task<Response<IEnumerable<LocationSuggestionDto>>> Handle(
            GetLocationSuggestionsQuery request,
            CancellationToken cancellationToken)
        {
            var keyword = (request.Q ?? string.Empty).Trim().ToLower();
            var limit = request.Limit > 0 ? request.Limit : 8;

            // Chỉ xét các KS đã duyệt và đang hoạt động
            var activeHotels = _context.Hotels
                .Where(h => h.ApprovalStatus == Domain.Models.HotelApprovalStatus.Approved && h.IsActive)
                .Include(h => h.Ward).ThenInclude(w => w!.Province);

            // ── 1. Gợi ý theo Province (tỉnh/thành phố) ───────────────────────
            var provinceGroupsQuery = activeHotels
                .Where(h => h.Ward != null && h.Ward.Province != null)
                .GroupBy(h => new { h.Ward!.Province!.Id, h.Ward.Province.Name })
                .Select(g => new
                {
                    g.Key.Id,
                    g.Key.Name,
                    Count = g.Count()
                });

            if (!string.IsNullOrEmpty(keyword))
            {
                provinceGroupsQuery = provinceGroupsQuery
                    .Where(p => p.Name.ToLower().Contains(keyword));
            }

            var provinceGroups = await provinceGroupsQuery
                .OrderByDescending(p => p.Count)
                .Take(limit)
                .ToListAsync(cancellationToken);

            var provinceSuggestions = provinceGroups.Select(p => new LocationSuggestionDto
            {
                DisplayName = p.Name,
                Value = p.Name,
                Type = "province",
                HotelCount = p.Count
            }).ToList();

            // ── 2. Gợi ý theo Ward (xã/phường/quận/huyện) ─────────────────────
            // Chỉ lấy khi còn slot (limit - provinceCount)
            var remainingSlots = limit - provinceSuggestions.Count;
            List<LocationSuggestionDto> wardSuggestions = new();

            if (remainingSlots > 0 && !string.IsNullOrEmpty(keyword))
            {
                var wardGroupsQuery = activeHotels
                    .Where(h => h.Ward != null && h.Ward.Province != null
                                && h.Ward.Name.ToLower().Contains(keyword))
                    .GroupBy(h => new
                    {
                        WardId = h.Ward!.Id,
                        WardName = h.Ward.Name,
                        ProvinceName = h.Ward.Province!.Name
                    })
                    .Select(g => new
                    {
                        g.Key.WardName,
                        g.Key.ProvinceName,
                        Count = g.Count()
                    });

                var wardGroups = await wardGroupsQuery
                    .OrderByDescending(w => w.Count)
                    .Take(remainingSlots)
                    .ToListAsync(cancellationToken);

                wardSuggestions = wardGroups.Select(w => new LocationSuggestionDto
                {
                    DisplayName = $"{w.WardName}, {w.ProvinceName}",
                    Value = w.WardName,
                    Type = "ward",
                    HotelCount = w.Count
                }).ToList();
            }

            // Province trước, Ward sau
            var results = provinceSuggestions.Concat(wardSuggestions);

            return new Response<IEnumerable<LocationSuggestionDto>>(
                results,
                "Lấy gợi ý địa điểm thành công."
            );
        }
    }
}
