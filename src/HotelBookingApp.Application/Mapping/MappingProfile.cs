using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using HotelBookingApp.Application.DTOs.ProvinceDto;
using HotelBookingApp.Application.DTOs.UserDto;
using HotelBookingApp.Application.DTOs.WardDto;
using HotelBookingApp.Infrastructure;

namespace HotelBookingApp.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Map 2 chiều giữa Entity và DTO
            CreateMap<Province, ProvinceDto>().ReverseMap();

            // Map từ Command sang Entity để Insert/Update nhanh hơn
            CreateMap<Features.Provinces.Commands.CreateProvinceCommand, Province>();
            CreateMap<Features.Provinces.Commands.UpdateProvinceCommand, Province>();

            CreateMap<Ward, WardDto>()
                .ForMember(dest => dest.ProvinceName, opt => opt.MapFrom(src => src.Province.Name));
            CreateMap<Features.Wards.Commands.CreateWardCommand, Ward>();
            CreateMap<Features.Wards.Commands.UpdateWardCommand, Ward>();

            // --- USER MAPPINGS ---
            CreateMap<User, UserDto>();
            CreateMap<Features.Users.Commands.CreateUserCommand, User>();
            CreateMap<Features.Users.Commands.UpdateUserCommand, User>();
        }
    }
}
