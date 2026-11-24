using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requestrr.WebApi.RequestrrBot.Locale;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// The room, user and connection behind one command, plus helpers to reply and to ask follow-up questions.
    /// </summary>
    public class MatrixCommandContext
    {
        public MatrixApiClient Client { get; }
        public MatrixConversationStore Conversations { get; }
        public IMatrixWorkflowProvider Workflows { get; }
        public string RoomId { get; }
        public string UserId { get; }
        public string DisplayName { get; }

        public MatrixCommandContext(MatrixApiClient client, MatrixConversationStore conversations, IMatrixWorkflowProvider workflows, string roomId, string userId, string displayName)
        {
            Client = client;
            Conversations = conversations;
            Workflows = workflows;
            RoomId = roomId;
            UserId = userId;
            DisplayName = displayName;
        }

        public async Task<string> SendAsync(MatrixMessage message)
        {
            return await Client.SendMessageAsync(RoomId, message);
        }

        public Task<string> SendAsync(string markdown)
        {
            return SendAsync(MatrixMessage.FromMarkdown(markdown));
        }

        /// <summary>
        /// Sends a numbered list and waits for the user to reply with a number (or react with a keycap emoji).
        /// </summary>
        public async Task AskToChooseAsync(MatrixMessage message, IReadOnlyList<string> labels, IReadOnlyList<Func<Task>> choices)
        {
            message.AppendNumberedList(labels)
                   .AppendMarkdown(Language.Current.MatrixSelectionPrompt.Replace(LanguageTokens.Count, labels.Count.ToString()));

            var eventId = await SendAsync(message);
            Conversations.Set(RoomId, UserId, new MatrixPrompt { PromptEventId = eventId, Choices = choices.ToArray() });
        }

        /// <summary>
        /// Sends a yes/no question and runs the action when the user replies "yes" or reacts with ✅.
        /// </summary>
        public async Task AskToConfirmAsync(MatrixMessage message, Func<Task> onConfirm)
        {
            message.AppendLine().AppendMarkdown(Language.Current.MatrixConfirmPrompt);

            var eventId = await SendAsync(message);
            Conversations.Set(RoomId, UserId, new MatrixPrompt { PromptEventId = eventId, OnConfirm = onConfirm });
        }

        public void EndConversation()
        {
            Conversations.Clear(RoomId, UserId);
        }
    }
}
