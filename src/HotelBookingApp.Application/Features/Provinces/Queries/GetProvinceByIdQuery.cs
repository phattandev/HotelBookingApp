using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.ProvinceDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;

namespace HotelBookingApp.Application.Features.Provinces.Queries
{
    public class GetProvinceByIdQuery : IRequest<Response<ProvinceDto>>
    {
        public Guid Id { get; set; }
    }

    public class GetProvinceByIdQueryHandler : IRequestHandler<GetProvinceByIdQuery, Response<ProvinceDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetProvinceByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<ProvinceDto>> Handle(GetProvinceByIdQuery request, CancellationToken cancellationToken)
        {
            var province = await _context.Provinces.FindAsync(new object[] { request.Id }, cancellationToken);
            if (province == null) return new Response<ProvinceDto>("Không tìm thấy tỉnh/thành phố với mã " + request.Id);
            return new Response<ProvinceDto>(_mapper.Map<ProvinceDto>(province), "Lấy thông tin thành công: " + province.Name);
        }
    }
}