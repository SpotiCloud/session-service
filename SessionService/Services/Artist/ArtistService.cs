using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SessionService.Data;
using SessionService.Dtos;
using SessionService.Services.Event;

namespace SessionService.Services.Artist
{
    public class ArtistService : IArtistService
    {
        private readonly IMapper _mapper;
        private readonly IEventService _eventService;
        private readonly DataContext _context;

        public ArtistService(IMapper mapper, IEventService eventService, DataContext context)
        {
            _mapper = mapper;
            _eventService = eventService;
            _context = context;
        }

        public async Task<ServiceResponse<List<GetArtistDto>>> GetAllArtists()
        {
            ServiceResponse<List<GetArtistDto>> response = new ServiceResponse<List<GetArtistDto>>();
            try
            {
                List<Models.Artist> artists = await _context.artist.ToListAsync();

                if (artists.Count > 0)
                {
                    response.Data = artists.Select(s => _mapper.Map<Models.Artist, GetArtistDto>(s)).ToList();
                }
                else
                {
                    response.Success = false;
                    response.Message = "Nothing was found!";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<GetArtistDto>> GetArtist(int artistId)
        {
            ServiceResponse<GetArtistDto> response = new ServiceResponse<GetArtistDto>();
            try
            {
                Models.Artist? artist = await _context.artist
                    .Where(s => s.Id == artistId)
                    .FirstAsync();

                if (artist != null)
                {
                    response.Data = _mapper.Map<Models.Artist, GetArtistDto>(artist);
                }
                else
                {
                    response.Success = false;
                    response.Message = "Nothing was found!";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<GetArtistDto>> AddArtist(AddArtistDto request)
        {
            ServiceResponse<GetArtistDto> response = new ServiceResponse<GetArtistDto>();

            try
            {
                Models.Artist artist = _mapper.Map<Models.Artist>(request);
                _context.artist.Add(artist);
                await _context.SaveChangesAsync();
                //var test = new { Id = artist.Id, Name = artist.Name };
                //_eventService.Publish(exchange: "artist-exchange", topic: "artist-added", test);

                response.Data = _mapper.Map<GetArtistDto>(artist);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ServiceResponse<GetArtistDto>> UpdateArtist(UpdateArtistDto request)
        {
            ServiceResponse<GetArtistDto> response = new ServiceResponse<GetArtistDto>();

            try
            {
                Models.Artist? artist = _context.Find<Models.Artist>(request.Id);
                artist.Name = request.Name;
                artist.SongId = request.SongId;

                await _context.SaveChangesAsync();

                response.Data = _mapper.Map<GetArtistDto>(artist);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ServiceResponse<GetArtistDto>> DeleteArtist(int artistId)
        {
            ServiceResponse<GetArtistDto> response = new ServiceResponse<GetArtistDto>();

            try
            {
                await _context.artist.Where(s => s.Id == artistId).ExecuteDeleteAsync();

                response.Success = true;
                response.Message = "Nothing was found!";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ServiceResponse<GetArtistDto>> DeleteArtistFromSong(int songId)
        {
            ServiceResponse<GetArtistDto> response = new ServiceResponse<GetArtistDto>();

            try
            {
                await _context.artist.Where(s => s.SongId == songId).ExecuteDeleteAsync();

                response.Success = true;
                response.Message = "Nothing was found!";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }
    }
}
