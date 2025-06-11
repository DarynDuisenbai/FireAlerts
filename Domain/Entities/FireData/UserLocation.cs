using MongoDB.Entities;

namespace Domain.Entities.FireData
{
    [Collection(nameof(UserLocation))]
    public class UserLocation
    {
        public string Id { get; set; }
        public string UsertId { get; set; }
        public long lat { get; set; }
        public long lng { get; set; }
    }
}
