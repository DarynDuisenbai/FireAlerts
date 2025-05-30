using MongoDB.Entities;

namespace Domain.Entities.Identity
{
    [Collection(nameof(UserLocation))]
    public class UserLocation
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime ExpiresAt { get; set; } 
    }
}
