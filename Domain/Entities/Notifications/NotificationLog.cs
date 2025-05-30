using MongoDB.Entities;

namespace Domain.Entities.Notifications
{
    [Collection(nameof(NotificationLog))]
    public class NotificationLog
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string FireId { get; set; }
        public string FireType { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsSuccess { get; set; }
    }
}
