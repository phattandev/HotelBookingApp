using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.ProvinceDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;

namespace HotelBookingApp.Application.Features.Provinces.Commands
{
    public class DeleteProvinceCommand : IRequest<Response<ProvinceDto>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteProvinceCommandHandler : IRequestHandler<DeleteProvinceCommand, Response<ProvinceDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public DeleteProvinceCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<ProvinceDto>> Handle(DeleteProvinceCommand request, CancellationToken cancellationToken)
        {
            var province = await _context.Provinces.FindAsync(new object[] { request.Id }, cancellationToken);
            if (province == null) return new Response<ProvinceDto>("Không tìm thấy tỉnh/thành phố với mã" + request.Id);

            var dto = _mapper.Map<ProvinceDto>(province);
            province.IsActive = !(province.IsActive ?? true);
            await _context.SaveChangesAsync(cancellationToken);

            string status = province.IsActive == true ? "Hiện" : "Ẩn";
            return new Response<ProvinceDto>(dto, $"{status} thành công tỉnh/thành phố: {dto.Name}");
        }
    }
}