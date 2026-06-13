using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.ProvinceDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;

namespace HotelBookingApp.Application.Features.Provinces.Commands
{
    public class CreateProvinceCommand : IRequest<Response<ProvinceDto>>
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }

    public class CreateProvinceCommandHandler : IRequestHandler<CreateProvinceCommand, Response<ProvinceDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateProvinceCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<ProvinceDto>> Handle(CreateProvinceCommand request, CancellationToken cancellationToken)
        {
            var province = _mapper.Map<Province>(request);
            _context.Provinces.Add(province);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<ProvinceDto>(_mapper.Map<ProvinceDto>(province), "Thêm tỉnh/thành phố thành công!");
        }
    }
}