using Application.DTOs.Support;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApi.Controllers.Support
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class SupportChatController : ControllerBase
    {
        private readonly ISupportChatService _supportChatService;

        public SupportChatController(ISupportChatService supportChatService)
        {
            _supportChatService = supportChatService;
        }

        // Получение ID текущего пользователя из JWT токена
        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value;
        }

        // Получение роли текущего пользователя
        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "user";
        }

        #region Методы для пользователей

        /// <summary>
        /// Создать новое обращение в поддержку
        /// </summary>
        [HttpPost(ApiRoutes.Support.CreateTicket)]
        public async Task<ActionResult<ChatDto>> CreateSupportTicket([FromBody] CreateSupportTicketDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Пользователь не авторизован");

                var result = await _supportChatService.CreateSupportTicketAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Отправить сообщение в чат
        /// </summary>
        [HttpPost(ApiRoutes.Support.SendMessage)]
        public async Task<ActionResult<ChatDto>> SendMessage([FromBody] SendMessageDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Пользователь не авторизован");

                var userRole = GetCurrentUserRole();

                ChatDto result;
                if (userRole == "manager" || userRole == "admin")
                {
                    result = await _supportChatService.SendManagerMessageAsync(userId, dto);
                }
                else
                {
                    result = await _supportChatService.SendMessageAsync(userId, dto);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Получить все чаты пользователя
        /// </summary>
        [HttpGet(ApiRoutes.Support.MyChats)]
        public async Task<ActionResult<List<ChatDto>>> GetMyChats()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Пользователь не авторизован");

                var result = await _supportChatService.GetUserChatsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Получить конкретный чат пользователя
        /// </summary>
        [HttpGet(ApiRoutes.Support.GetChat)]
        public async Task<ActionResult<ChatDto>> GetChat(string chatId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Пользователь не авторизован");

                var userRole = GetCurrentUserRole();

                ChatDto result;
                if (userRole == "manager" || userRole == "admin")
                {
                    // Менеджеры могут видеть любые чаты
                    var allChats = await _supportChatService.GetAllChatsAsync();
                    result = allChats.FirstOrDefault(c => c.Id == chatId);
                    if (result == null)
                        return NotFound("Чат не найден");
                }
                else
                {
                    result = await _supportChatService.GetChatAsync(chatId, userId);
                }

                // Отмечаем сообщения как прочитанные
                await _supportChatService.MarkMessagesAsReadAsync(chatId, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Методы для менеджеров

        /// <summary>
        /// Получить все чаты (только для менеджеров и админов)
        /// </summary>
        [HttpGet(ApiRoutes.Support.AllChats)]
        [Authorize(Roles = "manager,admin")]
        public async Task<ActionResult<List<ChatDto>>> GetAllChats()
        {
            try
            {
                var result = await _supportChatService.GetAllChatsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Получить чаты назначенные менеджеру
        /// </summary>
        [HttpGet(ApiRoutes.Support.MyAssignedChats)]
        [Authorize(Roles = "manager,admin")]
        public async Task<ActionResult<List<ChatDto>>> GetMyAssignedChats()
        {
            try
            {
                var managerId = GetCurrentUserId();
                if (string.IsNullOrEmpty(managerId))
                    return Unauthorized("Менеджер не авторизован");

                var result = await _supportChatService.GetManagerChatsAsync(managerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Назначить чат менеджеру
        /// </summary>
        [HttpPost(ApiRoutes.Support.AssignChat)]
        [Authorize(Roles = "manager,admin")]
        public async Task<ActionResult<ChatDto>> AssignChatToManager(string chatId)
        {
            try
            {
                var managerId = GetCurrentUserId();
                if (string.IsNullOrEmpty(managerId))
                    return Unauthorized("Менеджер не авторизован");

                var result = await _supportChatService.AssignChatToManagerAsync(chatId, managerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Закрыть чат
        /// </summary>
        [HttpPost(ApiRoutes.Support.CloseChat)]
        [Authorize(Roles = "manager,admin")]
        public async Task<ActionResult<object>> CloseChat(string chatId)
        {
            try
            {
                var managerId = GetCurrentUserId();
                if (string.IsNullOrEmpty(managerId))
                    return Unauthorized("Менеджер не авторизован");

                var result = await _supportChatService.CloseChatAsync(chatId, managerId);

                if (result)
                {
                    return Ok(new { success = true, message = "Чат успешно закрыт" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Не удалось закрыть чат" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Отметить сообщения в чате как прочитанные
        /// </summary>
        [HttpPost(ApiRoutes.Support.MarkAsRead)]
        public async Task<ActionResult<object>> MarkAsRead(string chatId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Пользователь не авторизован");

                var result = await _supportChatService.MarkMessagesAsReadAsync(chatId, userId);

                if (result)
                {
                    return Ok(new { success = true, message = "Сообщения отмечены как прочитанные" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Не удалось отметить сообщения как прочитанные" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Дополнительные методы для статистики

        /// <summary>
        /// Получить статистику чатов (только для админов и менеджеров)
        /// </summary>
        [HttpGet(ApiRoutes.Support.ChatStats)]
        [Authorize(Roles = "manager,admin")]
        public async Task<ActionResult<object>> GetChatStats()
        {
            try
            {
                var allChats = await _supportChatService.GetAllChatsAsync();

                var stats = new
                {
                    TotalChats = allChats.Count,
                    OpenChats = allChats.Count(c => c.Status == "Open"),
                    ClosedChats = allChats.Count(c => c.Status == "Closed"),
                    PendingChats = allChats.Count(c => c.Status == "Pending"),
                    UnassignedChats = allChats.Count(c => string.IsNullOrEmpty(c.AssignedManagerId)),
                    ChatsWithUnreadMessages = allChats.Count(c => c.UnreadMessagesCount > 0)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion
    }
}
