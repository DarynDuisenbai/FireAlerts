using Domain.Entities.FireData;

namespace Application.Interfaces
{
    public interface ITelegramBotService
    {
        Task SendNewFireNotificationAsync(CrowdSourcingData fireData);
    }
}
