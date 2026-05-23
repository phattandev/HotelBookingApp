using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Wards.Queries
{
    public class GetWardsQuery : IRequest<Response<IEnumerable<WardDto>>> { }

    public class GetWardsQueryHandler : IRequestHandler<GetWardsQuery, Response<IEnumerable<WardDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetWardsQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<IEnumerable<WardDto>>> Handle(GetWardsQuery request, CancellationToken cancellationToken)
        {
            var wards = await _context.Wards.Include(w => w.Province).ToListAsync(cancellationToken);
            return new Response<IEnumerable<WardDto>>(_mapper.Map<IEnumerable<WardDto>>(wards), "Lấy thông tin phường xã thành công!");
        }
    }
}