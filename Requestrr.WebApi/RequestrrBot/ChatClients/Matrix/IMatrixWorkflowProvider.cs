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
    }
}
