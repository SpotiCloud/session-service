using System.ComponentModel.DataAnnotations;

namespace SessionService.Models
{
    public class Artist
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int SongId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
