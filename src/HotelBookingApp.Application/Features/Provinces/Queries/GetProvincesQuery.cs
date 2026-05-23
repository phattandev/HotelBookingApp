using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.ProvinceDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Provinces.Queries
{
    public class GetProvincesQuery : IRequest<Response<IEnumerable<ProvinceDto>>> { }

    public class GetProvincesQueryHandler : IRequestHandler<GetProvincesQuery, Response<IEnumerable<ProvinceDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetProvincesQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<IEnumerable<ProvinceDto>>> Handle(GetProvincesQuery request, CancellationToken cancellationToken)
        {
            var provinces = await _context.Provinces.ToListAsync(cancellationToken);
            if (provinces == null) return new Response<IEnumerable<ProvinceDto>>("Lấy thông tin thất bại!");
            return new Response<IEnumerable<ProvinceDto>>(_mapper.Map<IEnumerable<ProvinceDto>>(provinces), "Lấy thông tin tỉnh và thành phố thành thành công!");
        }
    }
}