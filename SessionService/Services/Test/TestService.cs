using SessionService.Services.Event;

namespace SessionService.Services.Test
{
    public class Song {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class TestService: BackgroundService
    {
        private readonly IEventService eventService;
        private readonly ILogger<TestService> logger;

        public TestService(IEventService eventService, ILogger<TestService> logger)
        {
            this.eventService = eventService;
            this.logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            eventService.subscribe<Song>(
                exchange: "song-exchange", queue: "song-added-session", topic: "song-added", onSongAdded
            );

            return Task.CompletedTask;
        }

        private void onSongAdded(Song song)
        {
            logger.LogInformation($"Song added with name: {song.Name}");
        }
    }
}
