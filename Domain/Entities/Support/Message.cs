using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Entities;

namespace Domain.Entities.Support
{
    [Collection(nameof(Message) + "s")]
    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ChatId { get; set; } // ID чата, к которому относится сообщение
        public string SenderId { get; set; } // ID отправителя
        public string SenderName { get; set; } // Имя отправителя
        public string SenderRole { get; set; } // Роль отправителя (user, manager, admin)

        public string Content { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false; // Прочитано ли сообщение
        public DateTime? ReadAt { get; set; } // Когда прочитано
    }
}
