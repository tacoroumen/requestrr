using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// A question the bot asked one user in one room: either a numbered list or a yes/no confirmation.
    /// Discord keeps this state in button and dropdown IDs; Matrix has no components, so the bot keeps it here.
    /// </summary>
    public class MatrixPrompt
    {
        public string PromptEventId { get; set; }
        public IReadOnlyList<Func<Task>> Choices { get; set; } = Array.Empty<Func<Task>>();
        public Func<Task> OnConfirm { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

        public bool IsSelection => Choices.Count > 0;
    }

    public class MatrixConversationStore
    {
        public static readonly TimeSpan PromptLifetime = TimeSpan.FromMinutes(10);

        private readonly ConcurrentDictionary<string, MatrixPrompt> _prompts = new ConcurrentDictionary<string, MatrixPrompt>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private static string Key(string roomId, string userId) => $"{roomId}|{userId}";

        public void Set(string roomId, string userId, MatrixPrompt prompt)
        {
            prompt.ExpiresAtUtc = DateTime.UtcNow + PromptLifetime;
            _prompts[Key(roomId, userId)] = prompt;
        }

        public MatrixPrompt Get(string roomId, string userId)
        {
            if (_prompts.TryGetValue(Key(roomId, userId), out var prompt))
            {
                if (prompt.ExpiresAtUtc > DateTime.UtcNow)
                {
                    return prompt;
                }

                _prompts.TryRemove(Key(roomId, userId), out _);
            }

            return null;
        }

        public void Clear(string roomId, string userId)
        {
            _prompts.TryRemove(Key(roomId, userId), out _);
        }

        public void Reset()
        {
            _prompts.Clear();
        }

        /// <summary>
        /// Runs work for one user in one room at a time, so a fast "1" reply cannot overtake the search that produced the list.
        /// </summary>
        public async Task RunExclusiveAsync(string roomId, string userId, Func<Task> work)
        {
            var gate = _locks.GetOrAdd(Key(roomId, userId), _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();

            try
            {
                await work();
            }
            finally
            {
                gate.Release();
            }
        }

        public void PruneExpired()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in _prompts.Where(x => x.Value.ExpiresAtUtc <= now).ToArray())
            {
                _prompts.TryRemove(entry.Key, out _);
            }
        }
    }
}
