using AutoMapper;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.ProvinceDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using FluentValidation;

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

    public class UpdateProvinceCommandValidator : FluentValidation.AbstractValidator<UpdateProvinceCommand>
    {
        public UpdateProvinceCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên tỉnh/thành phố không được để trống.");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã không được để trống.")
                .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Mã không được chứa ký tự đặc biệt.");
            RuleFor(x => x.Slug).NotEmpty().WithMessage("Slug không được để trống.");
            RuleFor(x => x.Type).NotEmpty().WithMessage("Phân loại không được để trống.");
        }
    }
}