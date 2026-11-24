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

        public void Track(int requestId, string roomId, string eventId, string title)
        {
            if (requestId <= 0 || string.IsNullOrEmpty(eventId)) return;

            lock (_requests)
            {
                _requests.RemoveAll(x => x.RequestId == requestId && x.RoomId == roomId);
                _requests.Add(new MatrixPendingRequest { RequestId = requestId, RoomId = roomId, EventId = eventId, Title = title, CreatedUtc = DateTime.UtcNow });
                Save();
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
                    else if (status == OverseerrClient.MediaRequestStatus.APPROVED)
                    {
                        await EditAsync(client, request, Language.Current.MatrixRequestApproved, token);
                        Remove(request);
                    }
                    else if (status == OverseerrClient.MediaRequestStatus.DECLINED)
                    {
                        await EditAsync(client, request, Language.Current.MatrixRequestDeclined, token);
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

        private static Task EditAsync(MatrixApiClient client, MatrixPendingRequest request, string template, CancellationToken token)
        {
            var markdown = template.Replace("[Title]", request.Title);
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
