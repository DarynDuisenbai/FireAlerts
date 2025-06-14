﻿namespace WebApi.Controllers
{
    public class ApiRoutes
    {
        public const string Root = "api";
        public const string Base = Root;

        public static class Users
        {
            public const string Register = Base + "/users/register";
            public const string Login = Base + "/users/login";
            public const string UploadProfilePhoto = Base + "/users/uploadProfilePhoto";
            public const string SendVerificationCode = Base + "/users/sendCodeEmail";
            public const string VerifyEmail = Base + "/users/verifyEmail";
            public const string GetProfile = Base + "/users/getProfile";
            public const string UserLocation = Base + "/users/location";
        }
        public static class Admin
        {
            public const string ChangeRole = Base + "/users/changeRole";
            public const string AllUsers = Base + "/users/allUsers";
        }
        public static class Fire
        {
            public const string GetFiresByDate = Base + "/fires/fireByDate";
            public const string SaveCrowdData = Base + "/fires/saveCrowdData";
        }
        public static class CrowdData
        {
            public const string GetCrowdDataByUserId = Base + "/crowd/user/firedata";
        }
        public static class WebHook
        {
            public const string AddWebHook = Base + "/webhook/add";
            public const string GetAllWebHook = Base + "/webhook/getAll";
            public const string UpdateWebhookStatus = Base + "/webhook/updateStatus";
            public const string UpdateWebhook = Base + "/webhook/update";
            public const string SendNotification = Base + "/webhook/sendNotification";
        }
        public static class Support
        {
            public const string CreateTicket = "create-ticket";
            public const string SendMessage = "send-message";
            public const string MyChats = "my-chats";
            public const string GetChat = "chat/{chatId}";
            public const string MarkAsRead = "mark-as-read/{chatId}";

            // Маршруты для менеджеров
            public const string AllChats = "manager/all-chats";
            public const string MyAssignedChats = "manager/my-assigned-chats";
            public const string AssignChat = "manager/assign-chat/{chatId}";
            public const string CloseChat = "manager/close-chat/{chatId}";
            public const string ChatStats = "manager/stats";
        }
    }
}
