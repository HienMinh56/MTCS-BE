using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service;


namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
        {
            var currentUser = HttpContext.User;
            var result = await _notificationService.SendNotificationAsync(request.UserId, request.Title, request.Body, currentUser);
            return Ok(result);
        }

        [HttpPost("sendWeb")]
        public async Task<IActionResult> SendNotificationWeb([FromBody] NotificationRequest request)
        {
            var currentUser = HttpContext.User;
            var result = await _notificationService.SendNotificationWebAsync(request.UserId, request.Title, request.Body, currentUser);
            return Ok(result);
        }
    }
}
