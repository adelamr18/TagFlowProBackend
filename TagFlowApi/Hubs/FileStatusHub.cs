using Microsoft.AspNetCore.SignalR;

namespace TagFlowApi.Hubs
{
    public class FileStatusHub : Hub
    {
        public async Task NotifyFileStatusUpdated(int fileId, string downloadLink)
        {
            await Clients.All.SendAsync("FileStatusUpdated", fileId, downloadLink);
        }
    }
}
