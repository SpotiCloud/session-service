using SessionService.Dtos;
using SessionService.Services.Artist;
using SessionService.Services.Event;

namespace SessionService.Services.Test
{
    public class TestService: BackgroundService
    {
        private readonly IEventService eventService;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<TestService> logger;

        public class tempArtist
        {
            public int SongId { get; set; }
            public string Name { get; set; }
        }

        public TestService(IEventService eventService, IServiceProvider serviceProvider, ILogger<TestService> logger)
        {
            this.eventService = eventService;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            eventService.subscribe<tempArtist>(
                exchange: "song-added-exchange", queue: "song-added-session", topic: "song-added", onSongAdded
            );

            eventService.subscribe<string>(
                exchange: "song-deleted-exchange", queue: "song-deleted-session", topic: "song-deleted", onSongDeleted
            );

            return Task.CompletedTask;
        }

        private void onSongAdded(tempArtist artist)
        {
            AddArtistDto newArtist = new AddArtistDto { SongId = artist.SongId, Name = artist.Name };

            using (var scope = serviceProvider.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<IArtistService>().AddArtist(newArtist);
            }
        }

        private void onSongDeleted(string songId)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<IArtistService>().DeleteArtistFromSong(int.Parse(songId));
            }
        }
    }
}
