using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// Builds a Matrix message with a plain-text body and an HTML body.
    /// Locale strings use Discord-flavoured markdown (**bold**, __underline__, `code`, [text](url)),
    /// so the builder converts that subset to HTML.
    /// </summary>
    public class MatrixMessage
    {
        private static readonly Regex LinkRegex = new Regex(@"\[([^\]]+)\]\((https?://[^\s)]+)\)", RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex UnderlineRegex = new Regex(@"__(.+?)__", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex CodeRegex = new Regex(@"`([^`]+)`", RegexOptions.Compiled);

        private readonly StringBuilder _plain = new StringBuilder();
        private readonly StringBuilder _html = new StringBuilder();

        public List<string> MentionedUserIds { get; } = new List<string>();
        public string ReplyToEventId { get; set; }
        public bool IsNotice { get; set; } = true;

        /// <summary>
        /// A poster to send as an image right before the text, like the image in a Discord embed.
        /// </summary>
        public string ImageUrl { get; set; }

        public string PlainText => ToPlainText(_plain.ToString()).TrimEnd();
        public string Html => _html.ToString();

        /// <summary>
        /// Copies a message, so the same details can be sent to another room with different text after them.
        /// </summary>
        public static MatrixMessage FromParts(MatrixMessage other)
        {
            var copy = new MatrixMessage { ImageUrl = other.ImageUrl, IsNotice = other.IsNotice };
            copy._plain.Append(other._plain);
            copy._html.Append(other._html);
            copy.MentionedUserIds.AddRange(other.MentionedUserIds);
            return copy;
        }

        public static MatrixMessage FromMarkdown(string markdown)
        {
            return new MatrixMessage().AppendMarkdown(markdown);
        }

        public MatrixMessage AppendMarkdown(string markdown)
        {
            markdown ??= string.Empty;
            _plain.Append(markdown);
            _html.Append(MarkdownToHtml(markdown));
            return this;
        }

        public MatrixMessage AppendLine(string markdown = "")
        {
            AppendMarkdown(markdown);
            _plain.Append('\n');
            _html.Append("<br>");
            return this;
        }

        public MatrixMessage AppendHeading(string text)
        {
            _plain.Append(text).Append('\n');
            _html.Append("<h4>").Append(WebUtility.HtmlEncode(text)).Append("</h4>");
            return this;
        }

        public MatrixMessage AppendLink(string text, string url)
        {
            _plain.Append($"{text}: {url}");
            _html.Append($"<a href=\"{WebUtility.HtmlEncode(url)}\">{WebUtility.HtmlEncode(text)}</a>");
            return this;
        }

        public MatrixMessage AppendMention(string userId, string displayName)
        {
            var name = string.IsNullOrWhiteSpace(displayName) ? userId : displayName;
            _plain.Append(name);
            _html.Append($"<a href=\"https://matrix.to/#/{WebUtility.HtmlEncode(userId)}\">{WebUtility.HtmlEncode(name)}</a>");
            MentionedUserIds.Add(userId);
            return this;
        }

        public MatrixMessage AppendNumberedList(IEnumerable<string> items)
        {
            var index = 1;

            if (_plain.Length > 0 && _plain[_plain.Length - 1] != '\n')
            {
                _plain.Append('\n');
            }

            _html.Append("<ol>");

            foreach (var item in items)
            {
                _plain.Append($"{index}. {item}\n");
                _html.Append("<li>").Append(MarkdownToHtml(item)).Append("</li>");
                index++;
            }

            _html.Append("</ol>");
            return this;
        }

        /// <summary>
        /// Drops the bold and underline markers from the plain-text body; clients without HTML support show it as is.
        /// </summary>
        public static string ToPlainText(string markdown)
        {
            var text = BoldRegex.Replace(markdown ?? string.Empty, "$1");
            return UnderlineRegex.Replace(text, "$1");
        }

        public static string MarkdownToHtml(string markdown)
        {
            var html = WebUtility.HtmlEncode(markdown ?? string.Empty);
            html = LinkRegex.Replace(html, m => $"<a href=\"{m.Groups[2].Value}\">{m.Groups[1].Value}</a>");
            html = BoldRegex.Replace(html, "<strong>$1</strong>");
            html = UnderlineRegex.Replace(html, "<u>$1</u>");
            html = CodeRegex.Replace(html, "<code>$1</code>");
            return html.Replace("\r\n", "\n").Replace("\n", "<br>");
        }
    }
}
