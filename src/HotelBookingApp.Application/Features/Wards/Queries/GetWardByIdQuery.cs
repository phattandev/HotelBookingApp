using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Wards.Queries
{
    public class GetWardByIdQuery : IRequest<Response<WardDto>>
    {
        public int Id { get; set; }
    }

    public class GetWardByIdQueryHandler : IRequestHandler<GetWardByIdQuery, Response<WardDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetWardByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Response<WardDto>> Handle(GetWardByIdQuery request, CancellationToken cancellationToken)
        {
            var ward = await _context.Wards
                .Include(w => w.Province)
                .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

            if (ward == null) return new Response<WardDto>("Không tìm thấy phường/xã với mã" + request.Id);

            return new Response<WardDto>(_mapper.Map<WardDto>(ward), "Tìm thấy phường/xã: " + ward.Name);
        }
    }
}