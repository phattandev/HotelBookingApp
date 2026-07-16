using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Wards.Commands
{
    public class UpdateWardCommand : IRequest<Response<WardDto>>
    {
        public Guid Id { get; set; }
        public short ProvinceId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class UpdateWardCommandHandler : IRequestHandler<UpdateWardCommand, Response<WardDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateWardCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<WardDto>> Handle(UpdateWardCommand request, CancellationToken cancellationToken)
        {
            var ward = await _context.Wards.Include(w => w.Province).FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);
            if (ward == null) return new Response<WardDto>("Không tìm thấy phường/xã với mã " + request.Id);

            _mapper.Map(request, ward);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<WardDto>(_mapper.Map<WardDto>(ward), "Cập nhật phường/xã thành công");
        }
    }
}