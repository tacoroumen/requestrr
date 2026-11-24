using Newtonsoft.Json;

namespace Requestrr.WebApi.RequestrrBot.Locale
{
    public static class LanguageTokens
    {
        public static string TvShowTitle = "[TvShowTitle]";
        public static string TvShowTVDBID = "[TvShowTVDBID]";
        public static string SeasonNumber = "[SeasonNumber]";
        public static string MovieTitle = "[MovieTitle]";
        public static string MusicArtistName = "[MusicArtistName]";
        public static string AuthorUsername = "[AuthorUsername]";
        public static string BotUsername = "[BotUsername]";
        public static string MovieTMDB = "[MovieTMDB]";
        public static string CommandPrefix = "[CommandPrefix]";

        public static string IssueLabel = "[Issue]";
        public static string IssueTitle = "[Title]";
        public static string IssueUsername = "[Username]";
        public static string RequestStatus = "[RequestStatus]";
        public static string ApproveEmoji = "[ApproveEmoji]";
        public static string DenyEmoji = "[DenyEmoji]";

        public static string FullCommandList = "[FullCommandList]";

        public static string MovieCommandTitle = "[MovieCommandTitle]";
        public static string MovieCommandIssue = "[MovieCommandIssue]";

        public static string IssueEnabledStart = "[IssueEnabledStart]";
        public static string IssueEnabledEnd = "[IssueEnabledEnd]";

        public static string Count = "[Count]";
    }

    public class Language
    {
        public static Language Current = null;

#if DEBUG
        public static string BuildVersion = "Development";
#else
        public static string BuildVersion = "v2.1.11";
#endif

        public string Error { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Help.Dropdown")]
        public string DiscordCommandMovieRequestHelpDropdown { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Help.Search.Dropdown")]
        public string DiscordCommandTvRequestHelpSearchDropdown { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Help.Seasons.Dropdown")]
        public string DiscordCommandTvRequestHelpSeasonsDropdown { get; set; }

        [JsonProperty("Discord.Command.Tv.Issue.Help.Episodes.Dropdown")]
        public string DiscordCommandTvIssueHelpEpisodesDropdown { get; set; }

        [JsonProperty("Discord.Command.Tv.Issue.Help.Episodes.FullSeason")]
        public string DiscordCommandTvIssueHelpEpisodesFullSeason { get; set; }

        [JsonProperty("Discord.Command.MissingRoles")]
        public string DiscordCommandMissingRoles { get; set; }

        [JsonProperty("Discord.Command.NotAvailableInChannel")]
        public string DiscordCommandNotAvailableInChannel { get; set; }

        [JsonProperty("Discord.Command.UnknownPrecondition")]
        public string DiscordCommandUnknownPrecondition { get; set; }

        [JsonProperty("Discord.Command.Sending")]
        public string DiscordCommandSending { get; set; }

        [JsonProperty("Discord.Command.Request.Group.Name")]
        public string DiscordCommandRequestGroupName { get; set; }

        [JsonProperty("Discord.Command.Request.Group.Description")]
        public string DiscordCommandRequestGroupDescription { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Title.Name")]
        public string DiscordCommandMovieRequestTitleName { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Title.Description")]
        public string DiscordCommandMovieRequestTitleDescription { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Title.Option.Name")]
        public string DiscordCommandMovieRequestTitleOptionName { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Title.Option.Description")]
        public string DiscordCommandMovieRequestTitleOptionDescription { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Tmbd.Name")]
        public string DiscordCommandMovieRequestTmbdName { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Tmbd.Description")]
        public string DiscordCommandMovieRequestTmbdDescription { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Tmbd.Option.Name")]
        public string DiscordCommandMovieRequestTmbdOptionName { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Tmbd.Option.Description")]
        public string DiscordCommandMovieRequestTmbdOptionDescription { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Title.Name")]
        public string DiscordCommandTvRequestTitleName { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Title.Description")]
        public string DiscordCommandTvRequestTitleDescription { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Title.Option.Name")]
        public string DiscordCommandTvRequestTitleOptionName { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Title.Option.Description")]
        public string DiscordCommandTvRequestTitleOptionDescription { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Tvdb.Name")]
        public string DiscordCommandTvRequestTvdbName { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Tvdb.Description")]
        public string DiscordCommandTvRequestTvdbDescription { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Tvdb.Option.Name")]
        public string DiscordCommandTvRequestTvdbOptionName { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Tvdb.Option.Description")]
        public string DiscordCommandTvRequestTvdbOptionDescription { get; set; }

        [JsonProperty("Discord.Command.Ping.Request.Name")]
        public string DiscordCommandPingRequestName { get; set; }

        [JsonProperty("Discord.Command.Ping.Request.Description")]
        public string DiscordCommandPingRequestDescription { get; set; }

        [JsonProperty("Discord.Command.Help.Request.Name")]
        public string DiscordCommandHelpRequestName { get; set; }

        [JsonProperty("Discord.Command.Help.Request.Description")]
        public string DiscordCommandHelpRequestDescription { get; set; }

        [JsonProperty("Discord.Notification.Tv.Channel.Season")]
        public string DiscordNotificationTvChannelSeason { get; set; }

        [JsonProperty("Discord.Notification.Tv.ChannelFirstEpisode")]
        public string DiscordNotificationTvChannelFirstEpisode { get; set; }

        [JsonProperty("Discord.Notification.Tv.DM.Season")]
        public string DiscordNotificationTvDMSeason { get; set; }

        [JsonProperty("Discord.Notification.Tv.DM.FirstEpisode")]
        public string DiscordNotificationTvDMFirstEpisode { get; set; }

        [JsonProperty("Discord.Notification.Movie.Channel")]
        public string DiscordNotificationMovieChannel { get; set; }

        [JsonProperty("Discord.Notification.Movie.DM")]
        public string DiscordNotificationMovieDM { get; set; }

        [JsonProperty("Discord.Command.Ping.Response")]
        public string DiscordCommandPingResponse { get; set; }

        [JsonProperty("Discord.Command.Help")]
        public string DiscordCommandHelp { get; set; }

        [JsonProperty("Discord.Command.Help.Message")]
        public string DiscordCommandHelpMessage { get; set; }

        [JsonProperty("Discord.Command.Movie.NotFound")]
        public string DiscordCommandMovieNotFound { get; set; }

        [JsonProperty("Discord.Command.Movie.NotFoundTMDB")]
        public string DiscordCommandMovieNotFoundTMDB { get; set; }

        [JsonProperty("Discord.Command.Movie.AlreadyAvailable")]
        public string DiscordCommandMovieAlreadyAvailable { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Success")]
        public string DiscordCommandMovieRequestSuccess { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Pending")]
        public string DiscordCommandMovieRequestPending { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Pending.Silent")]
        public string DiscordCommandMovieRequestPendingSilent { get; set; }
        [JsonProperty("Discord.Command.RequestButtonDenied")]
        public string DiscordCommandRequestButtonDenied { get; set; }

        [JsonProperty("Discord.Command.RequestButtonSuccess")]
        public string DiscordCommandRequestButtonSuccess { get; set; }

        [JsonProperty("Discord.Command.NotifyMe")]
        public string DiscordCommandNotifyMe { get; set; }

        [JsonProperty("Discord.Command.NotifyMeSuccess")]
        public string DiscordCommandNotifyMeSuccess { get; set; }

        [JsonProperty("Discord.Command.RequestedButton")]
        public string DiscordCommandRequestedButton { get; set; }

        [JsonProperty("Discord.Command.AvailableButton")]
        public string DiscordCommandAvailableButton { get; set; }

        [JsonProperty("Discord.Command.RequestButton")]
        public string DiscordCommandRequestButton { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Help")]
        public string DiscordCommandMovieRequestHelp { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Confirm")]
        public string DiscordCommandMovieRequestConfirm { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.AlreadyExist")]
        public string DiscordCommandMovieRequestAlreadyExist { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.AlreadyExistNotified")]
        public string DiscordCommandMovieRequestAlreadyExistNotified { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Denied")]
        public string DiscordCommandMovieRequestDenied { get; set; }

        [JsonProperty("Discord.Command.Movie.NotReleased")]
        public string DiscordCommandMovieNotReleased { get; set; }

        [JsonProperty("Discord.Command.Movie.Notification.Success")]
        public string DiscordCommandMovieNotificationSuccess { get; set; }

        [JsonProperty("Discord.Command.Movie.Notification.Request")]
        public string DiscordCommandMovieNotificationRequest { get; set; }

        [JsonProperty("Discord.Command.Movie.CancelCommand")]
        public string DiscordCommandMovieCancelCommand { get; set; }

        [JsonProperty("Discord.Embed.Movie.Search")]
        public string DiscordEmbedMovieSearch { get; set; }

        [JsonProperty("Discord.Embed.Movie.SearchResult")]
        public string DiscordEmbedMovieSearchResult { get; set; }

        [JsonProperty("Discord.Embed.Movie.Quality")]
        public string DiscordEmbedMovieQuality { get; set; }

        [JsonProperty("Discord.Embed.Movie.InTheaters")]
        public string DiscordEmbedMovieInTheaters { get; set; }

        [JsonProperty("Discord.Embed.Movie.Release")]
        public string DiscordEmbedMovieRelease { get; set; }

        [JsonProperty("Discord.Embed.Movie.WatchNow")]
        public string DiscordEmbedMovieWatchNow { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Help.Search")]
        public string DiscordCommandTvRequestHelpSearch { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Help.Seasons")]
        public string DiscordCommandTvRequestHelpSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Confirm.Season")]
        public string DiscordCommandTvRequestConfirmSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Confirm.FutureSeasons")]
        public string DiscordCommandTvRequestConfirmFutureSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Confirm.AllSeasons")]
        public string DiscordCommandTvRequestConfirmAllSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Success.Season")]
        public string DiscordCommandTvRequestSuccessSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Success.FutureSeasons")]
        public string DiscordCommandTvRequestSuccessFutureSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Success.AllSeasons")]
        public string DiscordCommandTvRequestSuccessAllSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.Season")]
        public string DiscordCommandTvRequestPendingSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.FutureSeasons")]
        public string DiscordCommandTvRequestPendingFutureSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.AllSeasons")]
        public string DiscordCommandTvRequestPendingAllSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.Season.Silent")]
        public string DiscordCommandTvRequestPendingSeasonSilent { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.FutureSeasons.Silent")]
        public string DiscordCommandTvRequestPendingFutureSeasonsSilent { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.AllSeasons.Silent")]
        public string DiscordCommandTvRequestPendingAllSeasonsSilent { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Unsupported")]
        public string DiscordCommandTvRequestUnsupported { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyAvailable.Series")]
        public string DiscordCommandTvRequestAlreadyAvailableSeries { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyAvailable.Season")]
        public string DiscordCommandTvRequestAlreadyAvailableSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.Series")]
        public string DiscordCommandTvRequestAlreadyExistSeries { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.Season")]
        public string DiscordCommandTvRequestAlreadyExistSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.FutureSeason.Found")]
        public string DiscordCommandTvRequestAlreadyExistFutureSeasonFound { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.FutureSeason.Requested")]
        public string DiscordCommandTvRequestAlreadyExistFutureSeasonRequested { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.FutureSeason.Available")]
        public string DiscordCommandTvRequestAlreadyExistFutureSeasonAvailable { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExistNotified.Season")]
        public string DiscordCommandTvRequestAlreadyExistNotifiedSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExistNotified.FutureSeason.Found")]
        public string DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonFound { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExistNotified.FutureSeason.Requested")]
        public string DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonRequested { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExistNotified.FutureSeason.Available")]
        public string DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonAvailable { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Denied")]
        public string DiscordCommandTvRequestDenied { get; set; }

        [JsonProperty("Discord.Command.Tv.NotFoundTVDBID")]
        public string DiscordCommandTvNotFoundTVDBID { get; set; }

        [JsonProperty("Discord.Command.Tv.NotFound")]
        public string DiscordCommandTvNotFound { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Request.Season")]
        public string DiscordCommandTvNotificationRequestSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Request.FutureSeason.Missing")]
        public string DiscordCommandTvNotificationRequestFutureSeasonMissing { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Request.FutureSeason.Requested")]
        public string DiscordCommandTvNotificationRequestFutureSeasonRequested { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Request.FutureSeason.Available")]
        public string DiscordCommandTvNotificationRequestFutureSeasonAvailable { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Success.FutureSeasons")]
        public string DiscordCommandTvNotificationSuccessFutureSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Success.Season")]
        public string DiscordCommandTvNotificationSuccessSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.CancelCommand")]
        public string DiscordCommandTvCancelCommand { get; set; }

        [JsonProperty("Discord.Embed.Tv.Search")]
        public string DiscordEmbedTvSearch { get; set; }

        [JsonProperty("Discord.Embed.Tv.SearchResult")]
        public string DiscordEmbedTvSearchResult { get; set; }

        [JsonProperty("Discord.Embed.Tv.Network")]
        public string DiscordEmbedTvNetwork { get; set; }

        [JsonProperty("Discord.Embed.Tv.Status")]
        public string DiscordEmbedTvStatus { get; set; }

        [JsonProperty("Discord.Embed.Tv.Quality")]
        public string DiscordEmbedTvQuality { get; set; }

        [JsonProperty("Discord.Embed.Tv.AllSeasons")]
        public string DiscordEmbedTvAllSeasons { get; set; }

        [JsonProperty("Discord.Embed.Tv.FutureSeasons")]
        public string DiscordEmbedTvFutureSeasons { get; set; }

        [JsonProperty("Discord.Embed.Tv.Season")]
        public string DiscordEmbedTvSeason { get; set; }

        [JsonProperty("Discord.Embed.Tv.FullyRequested")]
        public string DiscordEmbedTvFullyRequested { get; set; }

        [JsonProperty("Discord.Embed.Tv.PartiallyRequested")]
        public string DiscordEmbedTvPartiallyRequested { get; set; }

        [JsonProperty("Discord.Embed.Tv.Seasons")]
        public string DiscordEmbedTvSeasons { get; set; }

        [JsonProperty("Discord.Embed.Tv.WatchNow")]
        public string DiscordEmbedTvWatchNow { get; set; }



        [JsonProperty("Discord.Command.Movie.Issue.Select")]
        public string DiscordCommandMovieIssueSelect { get; set; }

        [JsonProperty("Discord.Command.Issue.CreatedBy")]
        public string DiscordCommandIssueCreatedBy { get; set; }

        [JsonProperty("Discord.Command.Issue.Help.Dropdown")]
        public string DiscordCommandIssueHelpDropdown { get; set; }

        [JsonProperty("Discord.Command.IssueButton")]
        public string DiscordCommandIssueButton { get; set; }

        [JsonProperty("Discord.Command.Movie.Issue.Success")]
        public string DiscordCommandMovieIssueSuccess { get; set; }

        [JsonProperty("Discord.Command.Movie.Issue.Failed")]
        public string DiscordCommandMovieIssueFailed { get; set; }



        [JsonProperty("Discord.Command.Tv.Issue.Select")]
        public string DiscordCommandTvIssueSelect { get; set; }

        [JsonProperty("Discord.Command.Tv.Issue.Success")]
        public string DiscordCommandTvIssueSuccess { get; set; }

        [JsonProperty("Discord.Command.Tv.Issue.Failed")]
        public string DiscordCommandTvIssueFailed { get; set; }



        [JsonProperty("Discord.Command.Issue.Name")]
        public string DiscordCommandIssueName { get; set; }
        [JsonProperty("Discord.Command.Issue.Description")]
        public string DiscordCommandIssueDescription { get; set; }

        [JsonProperty("Discord.Command.Issue.Interaction.Title")]
        public string DiscordCommandIssueInteractionTitle { get; set; }

        [JsonProperty("Discord.Command.Issue.Interaction.Label")]
        public string DiscordCommandIssueInteractionLabel { get; set; }

        [JsonProperty("Discord.Command.Issue.Interaction.Placeholder")]
        public string DiscordCommandIssueInteractionPlaceholder { get; set; }


        [JsonProperty("Discord.Command.Movie.Issue.Title.Name")]
        public string DiscordCommandMovieIssueTitleName { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Title.Description")]
        public string DiscordCommandMovieIssueTitleDescription { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Title.Option.Name")]
        public string DiscordCommandMovieIssueTitleOptionName { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Title.Option.Description")]
        public string DiscordCommandMovieIssueTitleOptionDescription { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Tmdb.Name")]
        public string DiscordCommandMovieIssueTmdbName { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Tmdb.Description")]
        public string DiscordCommandMovieIssueTmdbDescription { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Tmdb.Option.Name")]
        public string DiscordCommandMovieIssueTmdbOptionName { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Tmdb.Option.Description")]
        public string DiscordCommandMovieIssueTmdbOptionDescription { get; set; }


        [JsonProperty("Discord.Command.Tv.Issue.Title.Name")]
        public string DiscordCommandTvIssueTitleName { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Title.Description")]
        public string DiscordCommandTvIssueTitleDescription { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Title.Option.Name")]
        public string DiscordCommandTvIssueTitleOptionName { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Title.Option.Description")]
        public string DiscordCommandTvIssueTitleOptionDescription { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Tvdb.Name")]
        public string DiscordCommandTvIssueTvdbName { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Tvdb.Description")]
        public string DiscordCommandTvIssueTvdbDescription { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Tvdb.Option.Name")]
        public string DiscordCommandTvIssueTvdbOptionName { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Tvdb.Option.Description")]
        public string DiscordCommandTvIssueTvdbOptionDescription { get; set; }




        [JsonProperty("Discord.Command.Music.Request.Artist.Description")]
        public string DiscordCommandMusicRequestArtistDescription { get; set; }

        [JsonProperty("Discord.Command.Music.Request.Artist.Option.Name")]
        public string DiscordCommandMusicRequestArtistOptionName { get; set; }

        [JsonProperty("Discord.Command.Music.Request.Artist.Option.Description")]
        public string DiscordCommandMusicRequestArtistOptionDescription { get; set; }

        [JsonProperty("Discord.Notification.Music.Artist.Channel")]
        public string DiscordNotificationMusicArtistChannel { get; set; }

        [JsonProperty("Discord.Notification.Music.Artist.DM")]
        public string DiscordNotificationMusicArtistDM { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Help")]
        public string DiscordCommandMusicArtistRequestHelp { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Help.Dropdown")]
        public string DiscordCommandMusicArtistRequestHelpDropdown { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Confirm")]
        public string DiscordCommandMusicArtistRequestConfirm { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.AlreadyAvailable")]
        public string DiscordCommandMusicArtistAlreadyAvailable { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.NotFound")]
        public string DiscordCommandMusicArtistNotFound { get; set; }

        [JsonProperty("Discord.Embed.Music.Quality")]
        public string DiscordEmbedMusicQuality { get; set; }

        [JsonProperty("Discord.Embed.Music.ListenNow")]
        public string DiscordEmbedMusicListenNow { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Success")]
        public string DiscordCommandMusicArtistRequestSuccess { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Denied")]
        public string DiscordCommandMusicArtistRequestDenied { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.AlreadyExistNotified")]
        public string DiscordCommandMusicArtistRequestAlreadyExistNotified { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Notification.Request")]
        public string DiscordCommandMusicArtistNotificationRequest { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Notification.Success")]
        public string DiscordCommandMusicArtistNotificationSuccess { get; set; }

        [JsonProperty("Discord.Command.Request.Approved")]
        public string DiscordCommandRequestApproved { get; set; }

        [JsonProperty("Discord.Command.Request.Denied")]
        public string DiscordCommandRequestDenied { get; set; }

        [JsonProperty("Discord.Command.Request.Pending.Admin")]
        public string DiscordCommandRequestPendingAdmin { get; set; }

        [JsonProperty("Discord.Command.Request.Admin.Summary")]
        public string DiscordCommandRequestAdminSummary { get; set; }
    
        [JsonProperty("Matrix.Request.Approved")]
        public string MatrixRequestApproved { get; set; } = "Your request for **[Title]** has been approved.";

        [JsonProperty("Matrix.Request.Declined")]
        public string MatrixRequestDeclined { get; set; } = "Your request for **[Title]** has been declined.";

        [JsonProperty("Matrix.Approval.Prompt")]
        public string MatrixApprovalPrompt { get; set; } = "[Requester] requested this and it needs approval. Moderators of this room can react with ✅ to approve or ❌ to deny.";

        [JsonProperty("Matrix.Approval.Approved")]
        public string MatrixApprovalApproved { get; set; } = "**[Title]** requested by [Requester]: approved by [DecidedBy].";

        [JsonProperty("Matrix.Approval.Declined")]
        public string MatrixApprovalDeclined { get; set; } = "**[Title]** requested by [Requester]: denied by [DecidedBy].";

        [JsonProperty("Matrix.Approval.NotAllowed")]
        public string MatrixApprovalNotAllowed { get; set; } = "You are not allowed to approve or deny requests. Ask a Seerr admin to give your Seerr account the Manage Requests permission.";

        [JsonProperty("Matrix.Issue.Select")]
        public string MatrixIssueSelect { get; set; } = "Which one do you want to report a problem with?";

        [JsonProperty("Matrix.Issue.Type")]
        public string MatrixIssueType { get; set; } = "What is wrong with **[Title]**?";

        [JsonProperty("Matrix.Issue.Describe.Movie")]
        public string MatrixIssueDescribeMovie { get; set; } = "Describe the **[Issue]** problem with **[Title]** in one message, or reply `cancel` to stop.";

        [JsonProperty("Matrix.Issue.Describe.Tv")]
        public string MatrixIssueDescribeTv { get; set; } = "Describe the **[Issue]** problem with **[Title]** in one message. Mention the season and episode if it is only one episode. Reply `cancel` to stop.";

        [JsonProperty("Matrix.Issue.NotFound")]
        public string MatrixIssueNotFound { get; set; } = "I could not find **[Title]** in the library. You can only report problems with media that is available.";

        [JsonProperty("Matrix.Request.NotAllowed")]
        public string MatrixRequestNotAllowed { get; set; } = "You are not allowed to request [Category] here. Ask a room admin for access.";

        [JsonProperty("Matrix.UnknownCommand")]
        public string MatrixUnknownCommand { get; set; } = "I don't know `[Command]`. Available commands: [CommandList]. Send `[HelpCommand]` for details.";

        [JsonProperty("Matrix.NoRequestCommands")]
        public string MatrixNoRequestCommands { get; set; } = "Movie and TV requests are not set up yet. An admin needs to configure Radarr, Sonarr or Overseerr in the Requestrr web UI.";

        [JsonProperty("Matrix.Help.Message")]
        public string MatrixHelpMessage { get; set; } = "Hello [AuthorUsername]! I am [BotUsername], your media request bot.\n\n[Access]\n\n[FullCommandList]\n\n[Examples]\n\nWhen I ask a question, tap one of the reactions under it or type the answer: a number to pick from a list, `yes` to confirm, `cancel` to stop. If I find more than one match, I show a list so you can pick the right one.";

        [JsonProperty("Matrix.Help.Section.Movies")]
        public string MatrixHelpSectionMovies { get; set; } = "🎬 Movies";

        [JsonProperty("Matrix.Help.Section.TvShows")]
        public string MatrixHelpSectionTvShows { get; set; } = "📺 TV shows";

        [JsonProperty("Matrix.Help.Section.Music")]
        public string MatrixHelpSectionMusic { get; set; } = "🎵 Music";

        [JsonProperty("Matrix.Help.Section.Issues")]
        public string MatrixHelpSectionIssues { get; set; } = "⚠️ Report a problem";

        [JsonProperty("Matrix.Help.Section.Other")]
        public string MatrixHelpSectionOther { get; set; } = "ℹ️ Other";

        [JsonProperty("Matrix.Help.Category.Note")]
        public string MatrixHelpCategoryNote { get; set; } = "(uses the **[Category]** settings)";

        [JsonProperty("Matrix.Help.Movie.Title")]
        public string MatrixHelpMovieByTitle { get; set; } = "search for a movie and request it";

        [JsonProperty("Matrix.Help.Movie.Tmdb")]
        public string MatrixHelpMovieByTmdbId { get; set; } = "request a movie by its TMDB id";

        [JsonProperty("Matrix.Help.Tv.Title")]
        public string MatrixHelpTvShowByTitle { get; set; } = "search for a tv show and pick the seasons you want";

        [JsonProperty("Matrix.Help.Tv.Tvdb")]
        public string MatrixHelpTvShowByTvdbId { get; set; } = "request a tv show by its TVDB id";

        [JsonProperty("Matrix.Help.Music.Artist")]
        public string MatrixHelpMusicArtist { get; set; } = "search for an artist and request their music";

        [JsonProperty("Matrix.Help.Issue.Movie")]
        public string MatrixHelpMovieIssue { get; set; } = "report a video, audio or subtitle problem with a movie in the library";

        [JsonProperty("Matrix.Help.Issue.Tv")]
        public string MatrixHelpTvShowIssue { get; set; } = "report a video, audio or subtitle problem with a tv show in the library";

        [JsonProperty("Matrix.Help.Help")]
        public string MatrixHelpHelp { get; set; } = "show this message";

        [JsonProperty("Matrix.Help.Cancel")]
        public string MatrixHelpCancel { get; set; } = "stop the question I am asking you";

        [JsonProperty("Matrix.Help.Ping")]
        public string MatrixHelpPing { get; set; } = "check that I am online";

        [JsonProperty("Matrix.Help.Examples")]
        public string MatrixHelpExamples { get; set; } = "Examples: [Examples]";

        [JsonProperty("Matrix.Help.Access.All")]
        public string MatrixHelpAccessAll { get; set; } = "In this room you can request [Allowed].";

        [JsonProperty("Matrix.Help.Access.Some")]
        public string MatrixHelpAccessSome { get; set; } = "In this room you can request [Allowed]. You cannot request [Denied]; ask a room admin for access.";

        [JsonProperty("Matrix.Help.Access.None")]
        public string MatrixHelpAccessNone { get; set; } = "In this room you cannot request [Denied]. Ask a room admin for access. You can still report problems.";

        [JsonProperty("Matrix.Help.Access.Movies")]
        public string MatrixHelpAccessMovies { get; set; } = "movies";

        [JsonProperty("Matrix.Help.Access.TvShows")]
        public string MatrixHelpAccessTvShows { get; set; } = "tv shows";

        [JsonProperty("Matrix.Help.Access.Music")]
        public string MatrixHelpAccessMusic { get; set; } = "music";

        [JsonProperty("Matrix.Help.And")]
        public string MatrixHelpAnd { get; set; } = "and";

        [JsonProperty("Matrix.Status.Default")]
        public string MatrixStatusDefault { get; set; } = "[CommandPrefix]help for commands";

        [JsonProperty("Matrix.Selection.Prompt")]
        public string MatrixSelectionPrompt { get; set; } = "Tap a number below or reply with a number from 1 to [Count]. Tap ❌ or reply `cancel` to stop.";

        [JsonProperty("Matrix.Confirm.Prompt")]
        public string MatrixConfirmPrompt { get; set; } = "Tap ✅ or reply `yes` to confirm. Tap ❌ or reply `cancel` to stop.";

        [JsonProperty("Matrix.Cancelled")]
        public string MatrixCancelled { get; set; } = "Cancelled.";

        [JsonProperty("Matrix.NotSupported")]
        public string MatrixNotSupported { get; set; } = "This action is not available on Matrix yet.";

        [JsonProperty("Matrix.Command.Movie.Search")]
        public string MatrixCommandMovieSearch { get; set; } = "I found these movies:";

        [JsonProperty("Matrix.Command.Movie.Request.Confirm")]
        public string MatrixCommandMovieRequestConfirm { get; set; } = "Do you want to request this movie?";

        [JsonProperty("Matrix.Command.Movie.NotReleased")]
        public string MatrixCommandMovieNotReleased { get; set; } = "This movie has not been released yet. Do you want to request it anyway?";

        [JsonProperty("Matrix.Command.Movie.Notification.Request")]
        public string MatrixCommandMovieNotificationRequest { get; set; } = "This movie has already been requested. Do you want a notification when it becomes available?";

        [JsonProperty("Matrix.Command.Music.Search")]
        public string MatrixCommandMusicSearch { get; set; } = "I found these artists:";

        [JsonProperty("Matrix.Command.Music.Request.Confirm")]
        public string MatrixCommandMusicRequestConfirm { get; set; } = "Do you want to request music from this artist?";

        [JsonProperty("Matrix.Command.Music.Notification.Request")]
        public string MatrixCommandMusicNotificationRequest { get; set; } = "This artist has already been requested. Do you want a notification when the music becomes available?";

        [JsonProperty("Matrix.Encryption.RoomNotSupported")]
        public string MatrixEncryptionRoomNotSupported { get; set; } = "Hi! You invited me to **[Room]**, but that room uses end-to-end encryption, which I do not support yet. Matrix cannot turn encryption off in an existing room, so please create a new room with encryption turned off and invite me there.";

        [JsonProperty("Matrix.Command.Tv.Search")]
        public string MatrixCommandTvSearch { get; set; } = "I found these tv shows:";

        [JsonProperty("Matrix.Command.Tv.Seasons")]
        public string MatrixCommandTvSeasons { get; set; } = "Which season do you want to request?";

        [JsonProperty("Matrix.Command.Tv.Request.Confirm.Season")]
        public string MatrixCommandTvRequestConfirmSeason { get; set; } = "Do you want to request **season [SeasonNumber]** of this tv show?";

        [JsonProperty("Matrix.Command.Tv.Request.Confirm.FutureSeasons")]
        public string MatrixCommandTvRequestConfirmFutureSeasons { get; set; } = "Do you want to request **future seasons** of this tv show?";

        [JsonProperty("Matrix.Command.Tv.Request.Confirm.AllSeasons")]
        public string MatrixCommandTvRequestConfirmAllSeasons { get; set; } = "Do you want to request **all seasons** of this tv show?";

        [JsonProperty("Matrix.Command.Tv.Notification.Request.Season")]
        public string MatrixCommandTvNotificationRequestSeason { get; set; } = "**Season [SeasonNumber]** has already been requested. Do you want a notification when it becomes available?";

        [JsonProperty("Matrix.Command.Tv.Notification.Request.FutureSeason.Missing")]
        public string MatrixCommandTvNotificationRequestFutureSeasonMissing { get; set; } = "**Future seasons** have already been requested. Do you want a notification when future seasons become available?";

        [JsonProperty("Matrix.Command.Tv.Notification.Request.FutureSeason.Requested")]
        public string MatrixCommandTvNotificationRequestFutureSeasonRequested { get; set; } = "**All seasons** have already been requested. Do you want a notification when future seasons become available?";

        [JsonProperty("Matrix.Command.Tv.Notification.Request.FutureSeason.Available")]
        public string MatrixCommandTvNotificationRequestFutureSeasonAvailable { get; set; } = "**All seasons** are already available. Do you want a notification when future seasons become available?";
    }
}
