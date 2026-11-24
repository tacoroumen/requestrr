using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.config;
using Requestrr.WebApi.RequestrrBot;
using Requestrr.WebApi.RequestrrBot.ChatClients;
using Requestrr.WebApi.RequestrrBot.ChatClients.Matrix;

namespace Requestrr.WebApi.Controllers.LinkedUsers
{
    /// <summary>
    /// Links Discord and Matrix users to Overseerr/Seerr users, so their requests are made as that Seerr user.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("/api/linkedusers")]
    public class LinkedUsersController : ControllerBase
    {
        private readonly ILogger<LinkedUsersController> _logger;

        public LinkedUsersController(ILogger<LinkedUsersController> logger)
        {
            _logger = logger;
        }

        [HttpGet()]
        public IActionResult Get()
        {
            return Ok(new LinkedUsersModel { UserLinks = ChatUserLinks.Read() });
        }

        [HttpGet("overseerr-users")]
        public async Task<IActionResult> GetOverseerrUsersAsync([FromServices] RequestrrServices services, [FromServices] RequestrrBot.ChatClients.Discord.DiscordSettingsProvider downloadSettingsProvider)
        {
            var download = downloadSettingsProvider.Provide();

            if (download.MovieDownloadClient != RequestrrBot.DownloadClients.DownloadClient.Overseerr && download.TvShowDownloadClient != RequestrrBot.DownloadClients.DownloadClient.Overseerr)
            {
                return Ok(new { seerrConfigured = false, users = Array.Empty<object>() });
            }

            try
            {
                var users = await services.OverseerrClient.GetUsersAsync();
                return Ok(new { seerrConfigured = true, users = users.Select(x => new { id = x.Id.ToString(), name = string.IsNullOrWhiteSpace(x.DisplayName) ? $"User {x.Id}" : x.DisplayName, email = x.Email ?? string.Empty, canManageRequests = x.CanManageRequests }) });
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not list Overseerr users: {ex.Message}");
                return BadRequest("Could not load the Seerr users. Set up Overseerr/Seerr under Download Clients first.");
            }
        }

        [HttpGet("chat-users")]
        public async Task<IActionResult> GetChatUsersAsync([FromServices] ChatBot discordBot, [FromServices] MatrixBot matrixBot)
        {
            using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
            var users = new System.Collections.Generic.List<ChatUser>();

            foreach (var source in new Func<System.Threading.CancellationToken, Task<System.Collections.Generic.IReadOnlyList<ChatUser>>>[] { discordBot.GetKnownUsersAsync, matrixBot.GetKnownUsersAsync })
            {
                try
                {
                    users.AddRange(await source(timeout.Token));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not list chat users: {ex.Message}");
                }
            }

            return Ok(users
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => new { id = x.Id, name = x.Name, platform = x.Platform, detail = x.Detail }));
        }

        [HttpPost()]
        public IActionResult Save([FromBody] LinkedUsersModel model)
        {
            var userLinks = (model?.UserLinks ?? Array.Empty<ChatUserLink>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ChatUserId))
                .Select(x => new ChatUserLink { ChatUserId = x.ChatUserId.Trim(), OverseerrUserId = (x.OverseerrUserId ?? string.Empty).Trim() })
                .ToArray();

            if (userLinks.Any(x => !MatrixIds.IsUserId(x.ChatUserId) && !ulong.TryParse(x.ChatUserId, System.Globalization.NumberStyles.Integer, null, out _)))
            {
                return BadRequest("Enter linked users as a Matrix user ID (@name:example.org) or a Discord user ID (a number).");
            }

            if (userLinks.Any(x => !int.TryParse(x.OverseerrUserId, out _)))
            {
                return BadRequest("Pick a Seerr user for every linked chat user.");
            }

            if (userLinks.GroupBy(x => x.ChatUserId, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            {
                return BadRequest("Each chat user can be linked to one Seerr user only.");
            }

            SettingsFile.Write(settings =>
            {
                settings.ChatClients.UserLinks = JToken.FromObject(userLinks);
            });

            return Ok(new { ok = true });
        }
    }

    public class LinkedUsersModel
    {
        public ChatUserLink[] UserLinks { get; set; }
    }
}
