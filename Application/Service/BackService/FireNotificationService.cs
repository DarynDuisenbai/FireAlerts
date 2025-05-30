using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Domain.Entities.FireData;
using Domain.Entities.Identity;
using System.Text.Json;
using System.Text;
using Infrastructure.Settings;
using MongoDB.Entities;
using Domain.Entities.Notifications;

namespace Application.Service.BackService
{
    public class FireNotificationService : BackgroundService
    {
        private readonly ILogger<FireNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IMongoCollection<FireData> _fireCollection;
        private readonly IMongoCollection<CrowdSourcingData> _crowdSourcingCollection;
        private readonly IMongoCollection<Domain.Entities.Identity.User> _userCollection;
        private readonly IMongoCollection<UserLocation> _userLocationCollection;
        private readonly IMongoCollection<NotificationLog> _notificationLogCollection;

        private const double NOTIFICATION_RADIUS_KM = 1.0;

        public FireNotificationService(
            ILogger<FireNotificationService> logger,
            IConfiguration configuration,
            HttpClient httpClient,
            IOptions<MongoDbSettings> mongoSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;

            var mongoClient = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = mongoClient.GetDatabase(mongoSettings.Value.DatabaseName);

            _fireCollection = database.GetCollection<FireData>(nameof(FireData));
            _crowdSourcingCollection = database.GetCollection<CrowdSourcingData>(nameof(CrowdSourcingData));
            _userCollection = database.GetCollection<Domain.Entities.Identity.User>("Users");
            _userLocationCollection = database.GetCollection<UserLocation>(nameof(UserLocation));
            _notificationLogCollection = database.GetCollection<NotificationLog>(nameof(NotificationLog));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessFireNotifications();
                    await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing fire notifications");
                    await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
                }
            }
        }

        public async Task ProcessFireNotifications()
        {
            // Получаем новые пожары за последние 5 минут
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

            // Проверяем FireData
            var newFireDataList = await GetNewFireData(cutoffTime);
            foreach (var fireData in newFireDataList)
            {
                await ProcessFireDataNotification(fireData);
            }

            // Проверяем CrowdSourcingData
            var newCrowdSourcingList = await GetNewCrowdSourcingData(cutoffTime);
            foreach (var crowdData in newCrowdSourcingList)
            {
                await ProcessCrowdSourcingNotification(crowdData);
            }
        }

        private async Task<List<FireData>> GetNewFireData(DateTime cutoffTime)
        {
            var filter = Builders<FireData>.Filter.Gte(x => x.Request_Time, cutoffTime);
            return await _fireCollection.Find(filter).ToListAsync();
        }

        private async Task<List<CrowdSourcingData>> GetNewCrowdSourcingData(DateTime cutoffTime)
        {
            var filter = Builders<CrowdSourcingData>.Filter.Gte(x => x.Time_fire, cutoffTime);
            return await _crowdSourcingCollection.Find(filter).ToListAsync();
        }

        private async Task ProcessFireDataNotification(FireData fireData)
        {
            var nearbyUsers = await GetNearbyUsers(fireData.Latitude, fireData.Longitude);

            foreach (var user in nearbyUsers)
            {
                // Проверяем, не отправляли ли уже уведомление этому пользователю об этом пожаре
                if (await ShouldSendNotification(user.Id, fireData.Id, "FireData"))
                {
                    var message = CreateFireNotificationMessage(fireData);
                    var success = await SendSmsNotification(user.PhoneNumber, message);

                    if (success)
                    {
                        await SaveNotificationLog(user.Id, fireData.Id, "FireData", message);
                        _logger.LogInformation($"Fire notification sent to user {user.Id} for fire at {fireData.Address}");
                    }
                }
            }
        }

        private async Task ProcessCrowdSourcingNotification(CrowdSourcingData crowdData)
        {
            var nearbyUsers = await GetNearbyUsers(crowdData.Latitude, crowdData.Longitude);

            foreach (var user in nearbyUsers)
            {
                // Не отправляем уведомление пользователю о его собственном сообщении
                if (user.Id == crowdData.UserId) continue;

                if (await ShouldSendNotification(user.Id, crowdData.Id, "CrowdSourcingData"))
                {
                    var message = CreateCrowdSourcingNotificationMessage(crowdData);
                    var success = await SendSmsNotification(user.PhoneNumber, message);

                    if (success)
                    {
                        await SaveNotificationLog(user.Id, crowdData.Id, "CrowdSourcingData", message);
                        _logger.LogInformation($"Crowd sourcing notification sent to user {user.Id} for report at {crowdData.Address}");
                    }
                }
            }
        }

        private async Task<List<Domain.Entities.Identity.User>> GetNearbyUsers(double fireLatitude, double fireLongitude)
        {
            // Получаем активные локации пользователей (обновленные за последние 2 минуты)
            var cutoffTime = DateTime.UtcNow.AddMinutes(-2);
            var locationFilter = Builders<UserLocation>.Filter.Gte(x => x.LastUpdated, cutoffTime);

            var activeUserLocations = await _userLocationCollection.Find(locationFilter).ToListAsync();
            var nearbyUserIds = new List<string>();

            // Фильтруем по расстоянию
            foreach (var userLocation in activeUserLocations)
            {
                var distance = CalculateDistance(fireLatitude, fireLongitude,
                    userLocation.Latitude, userLocation.Longitude);

                if (distance <= NOTIFICATION_RADIUS_KM)
                {
                    nearbyUserIds.Add(userLocation.UserId);
                }
            }

            if (!nearbyUserIds.Any())
                return new List<Domain.Entities.Identity.User>();

            // Получаем данные пользователей
            var userFilter = Builders<Domain.Entities.Identity.User>.Filter.In(x => x.Id, nearbyUserIds);
            return await _userCollection.Find(userFilter).ToListAsync();
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Радиус Земли в километрах

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private async Task<bool> ShouldSendNotification(string userId, string fireId, string fireType)
        {
            var filter = Builders<NotificationLog>.Filter.And(
                Builders<NotificationLog>.Filter.Eq(x => x.UserId, userId),
                Builders<NotificationLog>.Filter.Eq(x => x.FireId, fireId),
                Builders<NotificationLog>.Filter.Eq(x => x.FireType, fireType)
            );

            var existingNotification = await _notificationLogCollection.Find(filter).FirstOrDefaultAsync();
            return existingNotification == null;
        }

        private string CreateFireNotificationMessage(FireData fireData)
        {
            var address = string.IsNullOrEmpty(fireData.Address) ? $"{fireData.Latitude:F4}, {fireData.Longitude:F4}" : fireData.Address;
            return $"⚠️ ВНИМАНИЕ! Обнаружен пожар рядом с вами.\nМесто: {address}\nВремя: {fireData.Time_fire:dd.MM.yyyy HH:mm}\nСоблюдайте осторожность!";
        }

        private string CreateCrowdSourcingNotificationMessage(CrowdSourcingData crowdData)
        {
            var address = string.IsNullOrEmpty(crowdData.Address) ? $"{crowdData.Latitude:F4}, {crowdData.Longitude:F4}" : crowdData.Address;
            return $"🔥 Пользователь сообщил о пожаре рядом с вами.\nМесто: {address}\nВремя: {crowdData.Time_fire:dd.MM.yyyy HH:mm}\nОписание: {crowdData.Definition}\nБудьте осторожны!";
        }

        private async Task<bool> SendSmsNotification(string phoneNumber, string message)
        {
            try
            {

                var accountSid = _configuration["SmsProvider:Twilio:AccountSid"];
                var authToken = _configuration["SmsProvider:Twilio:AuthToken"];
                var fromNumber = _configuration["SmsProvider:Twilio:FromNumber"];

                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
                {
                    _logger.LogWarning("Twilio configuration is missing");
                    return false;
                }

                // Форматируем номер телефона (добавляем + если нет)
                if (!phoneNumber.StartsWith("+"))
                {
                    phoneNumber = "+" + phoneNumber;
                }

                var twilioUrl = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

                // Twilio использует form-encoded данные
                var formData = new List<KeyValuePair<string, string>>
                {
                    new("To", phoneNumber),
                    new("From", fromNumber),
                    new("Body", message)
                };

                var formContent = new FormUrlEncodedContent(formData);

                // Basic Auth для Twilio
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                var response = await _httpClient.PostAsync(twilioUrl, formContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"SMS sent successfully via Twilio to {phoneNumber}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to send SMS via Twilio to {phoneNumber}. Status: {response.StatusCode}, Response: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending SMS via Twilio to {phoneNumber}");
                return false;
            }
        }

        private async Task SaveNotificationLog(string userId, string fireId, string fireType, string message)
        {
            var notificationLog = new NotificationLog
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                FireId = fireId,
                FireType = fireType,
                Message = message,
                SentAt = DateTime.UtcNow,
                IsSuccess = true
            };

            await _notificationLogCollection.InsertOneAsync(notificationLog);
        }
    }

}