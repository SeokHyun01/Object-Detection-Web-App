using Microsoft.AspNetCore.SignalR;
using Models;

namespace WebServer.Hubs
{
	public class ObserverHub : Hub
	{
		private async Task CreateEvent(string userId, int videoId)
		{
			await Clients.All.SendAsync("OnEventCreated", userId, videoId);
		}
	}
}
