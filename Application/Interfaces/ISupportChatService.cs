using Application.DTOs.Support;

namespace Application.Interfaces
{
    public interface ISupportChatService
    {
        // Для пользователей
        Task<ChatDto> CreateSupportTicketAsync(string userId, CreateSupportTicketDto dto);
        Task<ChatDto> SendMessageAsync(string userId, SendMessageDto dto);
        Task<List<ChatDto>> GetUserChatsAsync(string userId);
        Task<ChatDto> GetChatAsync(string chatId, string userId);

        // Для менеджеров
        Task<List<ChatDto>> GetAllChatsAsync(); // Все чаты для менеджеров
        Task<ChatDto> AssignChatToManagerAsync(string chatId, string managerId);
        Task<ChatDto> SendManagerMessageAsync(string managerId, SendMessageDto dto);
        Task<bool> CloseChatAsync(string chatId, string managerId);
        Task<bool> MarkMessagesAsReadAsync(string chatId, string userId);
        Task<List<ChatDto>> GetManagerChatsAsync(string managerId); // Чаты конкретного менеджера
    }
}
