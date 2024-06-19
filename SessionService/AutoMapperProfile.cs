using AutoMapper;
using SessionService.Dtos;
using SessionService.Models;

namespace SessionService
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            //All "Artist" related mappings
            CreateMap<Artist, GetArtistDto>();
            CreateMap<AddArtistDto, Artist>();
            CreateMap<UpdateArtistDto, Artist>();
        }
    }
}
