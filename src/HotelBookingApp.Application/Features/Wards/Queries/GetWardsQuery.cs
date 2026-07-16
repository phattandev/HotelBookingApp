using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Wards.Queries
{
    public class GetWardsQuery : IRequest<Response<IEnumerable<WardDto>>>
    {
        public Guid? ProvinceId { get; set; }
        public bool IncludeHidden { get; set; } = false;
    }

    public class GetWardsQueryHandler : IRequestHandler<GetWardsQuery, Response<IEnumerable<WardDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetWardsQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Response<IEnumerable<WardDto>>> Handle(GetWardsQuery request, CancellationToken cancellationToken)
        {
            // 🟢 ĐÃ SỬA: Bỏ .Include(w => w.Province) để tránh lỗi vòng lặp JSON 500
            var query = _context.Wards.AsQueryable();

            if (request.ProvinceId.HasValue && request.ProvinceId != Guid.Empty)
            {
                query = query.Where(w => w.ProvinceId == request.ProvinceId.Value);
            }

            if (!request.IncludeHidden)
            {
                query = query.Where(w => w.IsActive == true);
            }

            // Sắp xếp theo tên cho Dropdown dễ tìm
            var wards = await query.OrderBy(w => w.Name).ToListAsync(cancellationToken);

            return new Response<IEnumerable<WardDto>>(_mapper.Map<IEnumerable<WardDto>>(wards), "Lấy thông tin phường xã thành công!");
        }
    }
}