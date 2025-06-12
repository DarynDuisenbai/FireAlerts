using Application.Interfaces;
using Domain.Entities.FireData;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions;

namespace Application.Service.CrowdService
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly string _mchsChannelId;
        private readonly string _volunteersChannelId;
        private readonly string _webAppUrl;

        public TelegramBotService(IConfiguration configuration)
        {
            var botToken = configuration["Telegram:BotToken"];
            _botClient = new TelegramBotClient(botToken);
            _mchsChannelId = configuration["Telegram:MCHSChannelId"];
            _volunteersChannelId = configuration["Telegram:VolunteersChannelId"];
            _webAppUrl = configuration["WebApp:BaseUrl"];
        }

        public async Task SendNewFireNotificationAsync(CrowdSourcingData fireData)
        {
            // Отправляем в канал МЧС
            await SendToMCHSChannelAsync(fireData);

            // Отправляем в канал волонтеров
            await SendToVolunteersChannelAsync(fireData);
        }

        private async Task SendToMCHSChannelAsync(CrowdSourcingData fireData)
        {
            try
            {
                var message = $"🔥 НОВЫЙ ПОЖАР\n\n" +
                             $"📍 Адрес: {fireData.Address}\n" +
                             $"📅 Время: {fireData.Time_fire:dd.MM.yyyy HH:mm}\n" +
                             $"📝 Описание: {fireData.Definition}\n" +
                             $"🆔 ID: {fireData.Id}\n\n" +
                             $"🔗 Ссылка: {_webAppUrl}/fires/{fireData.Id}";

                await SendMessageToChannelAsync(_mchsChannelId, message, fireData.Photo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке в канал МЧС: {ex.Message}");
            }
        }

        private async Task SendToVolunteersChannelAsync(CrowdSourcingData fireData)
        {
            try
            {
                var message = $"🚨 ТРЕБУЕТСЯ ПОМОЩЬ\n\n" +
                             $"📍 Адрес: {fireData.Address}\n" +
                             $"📅 Время: {fireData.Time_fire:dd.MM.yyyy HH:mm}\n" +
                             $"📝 Описание: {fireData.Definition}\n" +
                             $"🆔 ID: {fireData.Id}\n\n" +
                             $"🔗 Ссылка: {_webAppUrl}/fires/{fireData.Id}";

                await SendMessageToChannelAsync(_volunteersChannelId, message, fireData.Photo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке в канал волонтеров: {ex.Message}");
            }
        }

        private async Task SendMessageToChannelAsync(string channelId, string message, string photoBase64 = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(photoBase64))
                {
                    // Конвертируем base64 в байты
                    var photoBytes = Convert.FromBase64String(photoBase64);
                    using var photoStream = new MemoryStream(photoBytes);
                    var inputFile = new InputFileStream(photoStream, "photo.jpg");

                    // Отправляем фото с подписью
                    await _botClient.SendPhotoAsync(
                        chatId: channelId,
                        photo: inputFile,
                        caption: message
                    );
                }
                else
                {
                    // Отправляем только текст
                    await _botClient.SendTextMessageAsync(
                        chatId: channelId,
                        text: message
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения в Telegram: {ex.Message}");
            }
        }

        public void Dispose()
        {
        }
    }
}