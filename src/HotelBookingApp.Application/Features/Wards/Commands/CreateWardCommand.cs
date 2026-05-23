using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Wards.Commands
{
    public class CreateWardCommand : IRequest<Response<WardDto>>
    {
        public short ProvinceId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }

    public class CreateWardCommandHandler : IRequestHandler<CreateWardCommand, Response<WardDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateWardCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context; _mapper = mapper;
        }

        public async Task<Response<WardDto>> Handle(CreateWardCommand request, CancellationToken cancellationToken)
        {
            var ward = _mapper.Map<Ward>(request);
            _context.Wards.Add(ward);
            await _context.SaveChangesAsync(cancellationToken);

            // Load thêm Province để trả về ProvinceName đầy đủ trong DTO
            var createdWard = await _context.Wards.Include(w => w.Province).FirstAsync(w => w.Id == ward.Id, cancellationToken);
            return new Response<WardDto>(_mapper.Map<WardDto>(createdWard), "Đã thêm thành công phường/xã: " + createdWard.Name);
        }
    }
}