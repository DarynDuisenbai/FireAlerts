using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Entities;
using Domain.Entities.Support.Enums;

namespace Domain.Entities.Support
{
    [Collection(nameof(Chat) + "s")]
    public class Chat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; } 
        public string UserName { get; set; }
        public string UserEmail { get; set; }

        public string? AssignedManagerId { get; set; } // ID менеджера, который взял чат
        public string? AssignedManagerName { get; set; } // Имя менеджера

        public ChatStatus Status { get; set; } = ChatStatus.Open;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public string Subject { get; set; }
        public int UnreadMessagesCount { get; set; } = 0; 
    }
}
