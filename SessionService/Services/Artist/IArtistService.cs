using SessionService.Dtos;

namespace SessionService.Services.Artist
{
    public interface IArtistService
    {
        public Task<ServiceResponse<List<GetArtistDto>>> GetAllArtists();
        public Task<ServiceResponse<GetArtistDto>> GetArtist(int artistId);
        public Task<ServiceResponse<GetArtistDto>> AddArtist(AddArtistDto request);
        public Task<ServiceResponse<GetArtistDto>> UpdateArtist(UpdateArtistDto request);
        public Task<ServiceResponse<GetArtistDto>> DeleteArtist(int artistId);
        public Task<ServiceResponse<GetArtistDto>> DeleteArtistFromSong(int songId);
    }
}
