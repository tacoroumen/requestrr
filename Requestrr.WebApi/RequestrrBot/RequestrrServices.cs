using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.Extensions;
using Requestrr.WebApi.RequestrrBot.Approvals;
using Requestrr.WebApi.RequestrrBot.ChatClients.Discord;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Ombi;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Overseerr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Radarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Sonarr;
using Requestrr.WebApi.RequestrrBot.Movies;
using Requestrr.WebApi.RequestrrBot.Music;
using Requestrr.WebApi.RequestrrBot.Notifications.Movies;
using Requestrr.WebApi.RequestrrBot.Notifications.Music;
using Requestrr.WebApi.RequestrrBot.Notifications.TvShows;
using Requestrr.WebApi.RequestrrBot.TvShows;

namespace Requestrr.WebApi.RequestrrBot
{
    /// <summary>
    /// Holds the download clients, repositories and workflow factories that every chat client shares.
    /// Registered as a singleton so the Discord and Matrix bots read and write the same notification and approval state.
    /// </summary>
    public class RequestrrServices
    {
        public MovieNotificationsRepository MovieNotificationsRepository { get; } = new MovieNotificationsRepository();
        public TvShowNotificationsRepository TvShowNotificationsRepository { get; } = new TvShowNotificationsRepository();
        public MusicNotificationsRepository MusicNotificationsRepository { get; } = new MusicNotificationsRepository();
        public RequestApprovalRepository RequestApprovalRepository { get; } = new RequestApprovalRepository();

        public OverseerrClient OverseerrClient { get; }
        public OmbiClient OmbiClient { get; }
        public RadarrClient RadarrClient { get; }
        public SonarrClient SonarrClient { get; }
        public LidarrClient LidarrClient { get; }

        public MovieWorkflowFactory MovieWorkflowFactory { get; }
        public TvShowWorkflowFactory TvShowWorkflowFactory { get; }
        public MusicWorkflowFactory MusicWorkflowFactory { get; }

        public RequestrrServices(IServiceProvider serviceProvider, DiscordSettingsProvider discordSettingsProvider)
        {
            OverseerrClient = new OverseerrClient(serviceProvider.Get<IHttpClientFactory>(), serviceProvider.Get<ILogger<OverseerrClient>>(), serviceProvider.Get<OverseerrSettingsProvider>());
            OmbiClient = new OmbiClient(serviceProvider.Get<IHttpClientFactory>(), serviceProvider.Get<ILogger<OmbiClient>>(), serviceProvider.Get<OmbiSettingsProvider>());
            RadarrClient = new RadarrClient(serviceProvider.Get<IHttpClientFactory>(), serviceProvider.Get<ILogger<RadarrClient>>(), serviceProvider.Get<RadarrSettingsProvider>());
            SonarrClient = new SonarrClient(serviceProvider.Get<IHttpClientFactory>(), serviceProvider.Get<ILogger<SonarrClient>>(), serviceProvider.Get<SonarrSettingsProvider>());
            LidarrClient = new LidarrClient(serviceProvider.Get<IHttpClientFactory>(), serviceProvider.Get<ILogger<LidarrClient>>(), serviceProvider.Get<LidarrSettingsProvider>());

            MovieWorkflowFactory = new MovieWorkflowFactory(discordSettingsProvider, MovieNotificationsRepository, RequestApprovalRepository, OverseerrClient, OmbiClient, RadarrClient);
            TvShowWorkflowFactory = new TvShowWorkflowFactory(serviceProvider.Get<TvShowsSettingsProvider>(), discordSettingsProvider, TvShowNotificationsRepository, RequestApprovalRepository, OverseerrClient, OmbiClient, SonarrClient);
            MusicWorkflowFactory = new MusicWorkflowFactory(discordSettingsProvider, MusicNotificationsRepository, LidarrClient);
        }
    }
}
