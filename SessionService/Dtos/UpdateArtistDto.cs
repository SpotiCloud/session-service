namespace SessionService.Dtos
{
    public class UpdateArtistDto
    {
        public int Id { get; set; } = 0;
        public int SongId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
    }
}
