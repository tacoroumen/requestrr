using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Overseerr;
using Requestrr.WebApi.RequestrrBot.Locale;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    public class MatrixPendingRequest
    {
        public int RequestId { get; set; }
        public string RoomId { get; set; }
        public string EventId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// True for the copy posted in an approval room, where moderators approve or deny with a reaction.
        /// </summary>
        public bool IsApprovalMessage { get; set; }
        public string RequesterName { get; set; }
    }

    /// <summary>
    /// Remembers "pending approval" messages and edits them in place once the request is approved or declined in Overseerr.
    /// Saved to disk so a restart does not forget them.
    /// </summary>
    public class MatrixPendingRequests
    {
        private const string FileName = "matrix_pending_requests.json";
        private static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);
        private static readonly object FileLock = new object();

        private readonly ILogger _logger;
        private readonly List<MatrixPendingRequest> _requests;

        public MatrixPendingRequests(ILogger logger)
        {
            _logger = logger;
            _requests = Load();
        }

        private static string FilePath => Path.Combine(SettingsFile.SettingsFolder, FileName);

        public void Track(int requestId, string roomId, string eventId, string title, bool isApprovalMessage = false, string requesterName = null)
        {
            if (requestId <= 0 || string.IsNullOrEmpty(eventId)) return;

            lock (_requests)
            {
                _requests.RemoveAll(x => x.RequestId == requestId && x.RoomId == roomId && x.IsApprovalMessage == isApprovalMessage);
                _requests.Add(new MatrixPendingRequest
                {
                    RequestId = requestId,
                    RoomId = roomId,
                    EventId = eventId,
                    Title = title,
                    CreatedUtc = DateTime.UtcNow,
                    IsApprovalMessage = isApprovalMessage,
                    RequesterName = requesterName,
                });
                Save();
            }
        }

        public MatrixPendingRequest FindApprovalMessage(string roomId, string eventId)
        {
            lock (_requests)
            {
                return _requests.FirstOrDefault(x => x.IsApprovalMessage && x.RoomId == roomId && x.EventId == eventId);
            }
        }

        /// <summary>
        /// Edits every message about a request (the requester's and the approval rooms') once it is decided, then forgets them.
        /// </summary>
        public async Task CompleteAsync(MatrixApiClient client, int requestId, bool approved, string decidedBy, CancellationToken token)
        {
            MatrixPendingRequest[] entries;
            lock (_requests) entries = _requests.Where(x => x.RequestId == requestId).ToArray();

            foreach (var entry in entries)
            {
                try
                {
                    await EditAsync(client, entry, approved, decidedBy, token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not edit the message for Overseerr request {requestId} in {entry.RoomId}: {ex.Message}");
                }

                Remove(entry);
            }
        }

        /// <summary>
        /// Asks Overseerr for the status of each tracked request and edits the message when it is no longer pending.
        /// </summary>
        public async Task CheckAsync(MatrixApiClient client, OverseerrClient overseerr, CancellationToken token)
        {
            MatrixPendingRequest[] snapshot;
            lock (_requests) snapshot = _requests.ToArray();

            foreach (var request in snapshot)
            {
                if (token.IsCancellationRequested) return;

                try
                {
                    if (DateTime.UtcNow - request.CreatedUtc > MaxAge)
                    {
                        Remove(request);
                        continue;
                    }

                    var status = await overseerr.TryGetRequestStatusAsync(request.RequestId);

                    if (status == null)
                    {
                        Remove(request);
                    }
                    else if (status == OverseerrClient.MediaRequestStatus.APPROVED || status == OverseerrClient.MediaRequestStatus.DECLINED)
                    {
                        await EditAsync(client, request, status == OverseerrClient.MediaRequestStatus.APPROVED, null, token);
                        Remove(request);
                    }
                }
                catch (MatrixApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Could not edit the pending message for Overseerr request {request.RequestId} in {request.RoomId}: {ex.Message}. Forgetting it.");
                    Remove(request);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not check Overseerr request {request.RequestId}: {ex.Message}");
                }
            }
        }

        private static Task EditAsync(MatrixApiClient client, MatrixPendingRequest request, bool approved, string decidedBy, CancellationToken token)
        {
            var template = request.IsApprovalMessage
                ? (approved ? Language.Current.MatrixApprovalApproved : Language.Current.MatrixApprovalDeclined)
                : (approved ? Language.Current.MatrixRequestApproved : Language.Current.MatrixRequestDeclined);

            var markdown = template
                .Replace("[Title]", request.Title ?? string.Empty)
                .Replace("[Requester]", request.RequesterName ?? string.Empty)
                .Replace("[DecidedBy]", decidedBy ?? "Seerr");

            return client.EditMessageAsync(request.RoomId, request.EventId, MatrixMessage.FromMarkdown(markdown), token);
        }

        private void Remove(MatrixPendingRequest request)
        {
            lock (_requests)
            {
                _requests.Remove(request);
                Save();
            }
        }

        private static List<MatrixPendingRequest> Load()
        {
            lock (FileLock)
            {
                try
                {
                    if (File.Exists(FilePath))
                    {
                        return JsonConvert.DeserializeObject<List<MatrixPendingRequest>>(File.ReadAllText(FilePath)) ?? new List<MatrixPendingRequest>();
                    }
                }
                catch
                {
                    // A corrupt file only means old pending messages stay as they are.
                }

                return new List<MatrixPendingRequest>();
            }
        }

        private void Save()
        {
            lock (FileLock)
            {
                try
                {
                    File.WriteAllText(FilePath, JsonConvert.SerializeObject(_requests, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not save {FileName}: {ex.Message}");
                }
            }
        }
    }
}
