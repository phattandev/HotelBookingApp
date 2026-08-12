using AutoMapper;
using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Wards.Commands
{
    public class CreateWardCommand : IRequest<Response<WardDto>>
    {
        public Guid ProvinceId { get; set; }
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

    public class CreateWardCommandValidator : AbstractValidator<CreateWardCommand>
    {
        public CreateWardCommandValidator()
        {
            RuleFor(x => x.ProvinceId).NotEmpty().WithMessage("ID Tỉnh/Thành phố không được để trống.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên Phường/Xã không được để trống.");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã không được để trống.")
                .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Mã không được chứa ký tự đặc biệt.");
            RuleFor(x => x.Slug).NotEmpty().WithMessage("Slug không được để trống.");
            RuleFor(x => x.Type).NotEmpty().WithMessage("Phân loại không được để trống.");
        }
    }
}