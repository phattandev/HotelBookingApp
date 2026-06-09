using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.ProvinceDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;

namespace HotelBookingApp.Application.Features.Provinces.Commands
{
    public class UpdateProvinceCommand : IRequest<Response<ProvinceDto>>
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class UpdateProvinceCommandHandler : IRequestHandler<UpdateProvinceCommand, Response<ProvinceDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateProvinceCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<ProvinceDto>> Handle(UpdateProvinceCommand request, CancellationToken cancellationToken)
        {
            var province = await _context.Provinces.FindAsync(new object[] { request.Id }, cancellationToken);
            if (province == null) return new Response<ProvinceDto>("Không tìm thấy tỉnh/thành phố với mã " + request.Id);

            _mapper.Map(request, province);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<ProvinceDto>(_mapper.Map<ProvinceDto>(province), "Cập nhật tỉnh/thành phố thành công!");
        }
    }
}