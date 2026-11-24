using System.Threading.Tasks;
using Requestrr.WebApi.RequestrrBot.Movies;
using Requestrr.WebApi.RequestrrBot.TvShows;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// Creates request workflows bound to a Matrix room and user.
    /// User interfaces call it when a requester answers a prompt, the same way Discord recreates a workflow per button press.
    /// </summary>
    public interface IMatrixWorkflowProvider
    {
        MovieRequestingWorkflow CreateMovieWorkflow(MatrixCommandContext context, int categoryId);
        IMovieNotificationWorkflow CreateMovieNotificationWorkflow(MatrixCommandContext context);
        IMovieSearcher GetMovieSearcher();
        TvShowRequestingWorkflow CreateTvShowWorkflow(MatrixCommandContext context, int categoryId);
        ITvShowNotificationWorkflow CreateTvShowNotificationWorkflow(MatrixCommandContext context);

        /// <summary>
        /// Remembers a "pending approval" message so it can be edited once the request is approved or declined.
        /// </summary>
        /// Also posts the request to the approval rooms, where moderators can approve or deny it with a reaction.
        /// </summary>
        Task TrackPendingRequestAsync(MatrixCommandContext context, int requestId, string eventId, string title, MatrixMessage details);
    }
}
