using Application.DTOs.Support;
using Application.Interfaces;
using Domain.Entities.Identity.Enums;
using Domain.Entities.Support.Enums;
using Domain.Entities.Support;
using MongoDB.Driver;

namespace Application.Service.Support
{
    public class SupportChatService : ISupportChatService
    {
        private readonly IMongoCollection<Chat> _chatsCollection;
        private readonly IMongoCollection<Message> _messagesCollection;
        private readonly IMongoCollection<Domain.Entities.Identity.User> _usersCollection;

        public SupportChatService(IMongoDatabase database)
        {
            _chatsCollection = database.GetCollection<Chat>("Chats");
            _messagesCollection = database.GetCollection<Message>("Messages");
            _usersCollection = database.GetCollection<Domain.Entities.Identity.User>("Users");
        }

        // Создание нового обращения в поддержку
        public async Task<ChatDto> CreateSupportTicketAsync(string userId, CreateSupportTicketDto dto)
        {
            var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                throw new ArgumentException("Пользователь не найден");

            var chat = new Chat
            {
                UserId = userId,
                UserName = user.Username,
                UserEmail = user.Email,
                Subject = dto.Subject,
                Status = ChatStatus.Open
            };

            await _chatsCollection.InsertOneAsync(chat);

            // Создаем первое сообщение
            var message = new Message
            {
                ChatId = chat.Id,
                SenderId = userId,
                SenderName = user.Username,
                SenderRole = Roles.User,
                Content = dto.Message
            };

            await _messagesCollection.InsertOneAsync(message);

            // Обновляем чат
            chat.UnreadMessagesCount = 1;
            await _chatsCollection.ReplaceOneAsync(c => c.Id == chat.Id, chat);

            return await GetChatDtoAsync(chat.Id);
        }

        // Отправка сообщения пользователем
        public async Task<ChatDto> SendMessageAsync(string userId, SendMessageDto dto)
        {
            var chat = await _chatsCollection.Find(c => c.Id == dto.ChatId && c.UserId == userId).FirstOrDefaultAsync();
            if (chat == null)
                throw new ArgumentException("Чат не найден или нет доступа");

            var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();

            var message = new Message
            {
                ChatId = dto.ChatId,
                SenderId = userId,
                SenderName = user.Username,
                SenderRole = Roles.User,
                Content = dto.Content
            };

            await _messagesCollection.InsertOneAsync(message);

            // Обновляем чат
            var update = Builders<Chat>.Update
                .Set(c => c.LastMessageAt, DateTime.UtcNow)
                .Set(c => c.Status, ChatStatus.Pending)
                .Inc(c => c.UnreadMessagesCount, 1);

            await _chatsCollection.UpdateOneAsync(c => c.Id == dto.ChatId, update);

            return await GetChatDtoAsync(dto.ChatId);
        }

        // Получение чатов пользователя
        public async Task<List<ChatDto>> GetUserChatsAsync(string userId)
        {
            var chats = await _chatsCollection
                .Find(c => c.UserId == userId)
                .SortByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var chatDtos = new List<ChatDto>();
            foreach (var chat in chats)
            {
                chatDtos.Add(await GetChatDtoAsync(chat.Id));
            }

            return chatDtos;
        }

        // Получение конкретного чата пользователя
        public async Task<ChatDto> GetChatAsync(string chatId, string userId)
        {
            var chat = await _chatsCollection.Find(c => c.Id == chatId && c.UserId == userId).FirstOrDefaultAsync();
            if (chat == null)
                throw new ArgumentException("Чат не найден или нет доступа");

            return await GetChatDtoAsync(chatId);
        }

        // Получение всех чатов для менеджеров
        public async Task<List<ChatDto>> GetAllChatsAsync()
        {
            var chats = await _chatsCollection
                .Find(_ => true)
                .SortByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var chatDtos = new List<ChatDto>();
            foreach (var chat in chats)
            {
                chatDtos.Add(await GetChatDtoAsync(chat.Id));
            }

            return chatDtos;
        }

        // Назначение чата менеджеру
        public async Task<ChatDto> AssignChatToManagerAsync(string chatId, string managerId)
        {
            var manager = await _usersCollection.Find(u => u.Id == managerId).FirstOrDefaultAsync();
            if (manager == null || manager.Roles != Roles.Manager)
                throw new ArgumentException("Менеджер не найден");

            var update = Builders<Chat>.Update
                .Set(c => c.AssignedManagerId, managerId)
                .Set(c => c.AssignedManagerName, manager.Username);

            await _chatsCollection.UpdateOneAsync(c => c.Id == chatId, update);

            return await GetChatDtoAsync(chatId);
        }

        // Отправка сообщения менеджером
        public async Task<ChatDto> SendManagerMessageAsync(string managerId, SendMessageDto dto)
        {
            var manager = await _usersCollection.Find(u => u.Id == managerId).FirstOrDefaultAsync();
            if (manager == null || (manager.Roles != Roles.Manager && manager.Roles != Roles.Admin))
                throw new ArgumentException("Нет прав доступа");

            var chat = await _chatsCollection.Find(c => c.Id == dto.ChatId).FirstOrDefaultAsync();
            if (chat == null)
                throw new ArgumentException("Чат не найден");

            var message = new Message
            {
                ChatId = dto.ChatId,
                SenderId = managerId,
                SenderName = manager.Username,
                SenderRole = manager.Roles,
                Content = dto.Content
            };

            await _messagesCollection.InsertOneAsync(message);

            // Обновляем чат
            var update = Builders<Chat>.Update
                .Set(c => c.LastMessageAt, DateTime.UtcNow)
                .Set(c => c.Status, ChatStatus.Open);

            // Если менеджер еще не назначен, назначаем
            if (string.IsNullOrEmpty(chat.AssignedManagerId))
            {
                update = update
                    .Set(c => c.AssignedManagerId, managerId)
                    .Set(c => c.AssignedManagerName, manager.Username);
            }

            await _chatsCollection.UpdateOneAsync(c => c.Id == dto.ChatId, update);

            return await GetChatDtoAsync(dto.ChatId);
        }

        // Закрытие чата
        public async Task<bool> CloseChatAsync(string chatId, string managerId)
        {
            var manager = await _usersCollection.Find(u => u.Id == managerId).FirstOrDefaultAsync();
            if (manager == null || (manager.Roles != Roles.Manager && manager.Roles != Roles.Admin))
                return false;

            var update = Builders<Chat>.Update
                .Set(c => c.Status, ChatStatus.Closed)
                .Set(c => c.ClosedAt, DateTime.UtcNow);

            var result = await _chatsCollection.UpdateOneAsync(c => c.Id == chatId, update);
            return result.ModifiedCount > 0;
        }

        // Отметка сообщений как прочитанных
        public async Task<bool> MarkMessagesAsReadAsync(string chatId, string userId)
        {
            // Помечаем все непрочитанные сообщения в чате как прочитанные
            var update = Builders<Message>.Update
                .Set(m => m.IsRead, true)
                .Set(m => m.ReadAt, DateTime.UtcNow);

            await _messagesCollection.UpdateManyAsync(
                m => m.ChatId == chatId && !m.IsRead && m.SenderId != userId,
                update);

            // Сбрасываем счетчик непрочитанных сообщений
            var chatUpdate = Builders<Chat>.Update.Set(c => c.UnreadMessagesCount, 0);
            var result = await _chatsCollection.UpdateOneAsync(c => c.Id == chatId, chatUpdate);

            return result.ModifiedCount > 0;
        }

        // Получение чатов конкретного менеджера
        public async Task<List<ChatDto>> GetManagerChatsAsync(string managerId)
        {
            var chats = await _chatsCollection
                .Find(c => c.AssignedManagerId == managerId)
                .SortByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var chatDtos = new List<ChatDto>();
            foreach (var chat in chats)
            {
                chatDtos.Add(await GetChatDtoAsync(chat.Id));
            }

            return chatDtos;
        }

        // Вспомогательный метод для создания ChatDto
        private async Task<ChatDto> GetChatDtoAsync(string chatId)
        {
            var chat = await _chatsCollection.Find(c => c.Id == chatId).FirstOrDefaultAsync();
            if (chat == null) return null;

            var messages = await _messagesCollection
                .Find(m => m.ChatId == chatId)
                .SortBy(m => m.SentAt)
                .ToListAsync();

            return new ChatDto
            {
                Id = chat.Id,
                UserId = chat.UserId,
                UserName = chat.UserName,
                UserEmail = chat.UserEmail,
                AssignedManagerId = chat.AssignedManagerId,
                AssignedManagerName = chat.AssignedManagerName,
                Status = chat.Status.ToString(),
                CreatedAt = chat.CreatedAt,
                LastMessageAt = chat.LastMessageAt,
                Subject = chat.Subject,
                UnreadMessagesCount = chat.UnreadMessagesCount,
                Messages = messages.Select(m => new MessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.SenderName,
                    SenderRole = m.SenderRole,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead
                }).ToList()
            };
        }
    }
}
