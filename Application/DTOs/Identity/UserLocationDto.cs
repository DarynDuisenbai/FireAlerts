using MongoDB.Entities;

namespace Domain.Entities.FireData
{
    public class UserLocationDto
    {
        public string UsertId { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
        public DateTime time { get; set; }
    }
}
