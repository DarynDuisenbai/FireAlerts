namespace Application.DTOs.Support
{
    public class ChatDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string? AssignedManagerId { get; set; }
        public string? AssignedManagerName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public string Subject { get; set; }
        public int UnreadMessagesCount { get; set; }
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
    }
}
