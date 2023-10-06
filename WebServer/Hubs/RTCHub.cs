using Microsoft.AspNetCore.SignalR;

namespace WebServer.Hubs
{
	public class RTCHub : Hub
	{
		private readonly ILogger<RTCHub> _logger;
		private static Dictionary<string, List<string>> Rooms = new();


		public RTCHub(ILogger<RTCHub> logger)
		{
			_logger = logger;
		}

		public override async Task OnConnectedAsync()
		{
			var senderId = Context.ConnectionId;
			await Clients.Caller.SendAsync("OnConnected", senderId);

			await base.OnConnectedAsync();
		}

		public async ValueTask JoinRoom(string roomName)
		{
			try
			{
				if (!Rooms.ContainsKey(roomName))
				{
					Rooms[roomName] = new List<string>();
				}
				if (Rooms[roomName].Count >= 3)
				{
					throw new Exception($"Room {roomName}: 정원 초과.");
				}
				Rooms[roomName].Add(Context.ConnectionId);
				await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
				_logger.LogInformation($"유저 {Context.ConnectionId}가 {roomName}에 입장하였습니다.");

				await Clients.GroupExcept(roomName, Context.ConnectionId).SendAsync("Welcome");

			}
			catch (Exception ex)
			{
				_logger.LogInformation(ex.Message);
				_logger.LogError(ex.StackTrace);
			}
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var value = Context.ConnectionId;

			foreach (var room in Rooms)
			{
				if (room.Value.Contains(value))
				{
					var roomName = room.Key;
					room.Value.Remove(value);
					await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
					_logger.LogInformation($"유저 {Context.ConnectionId}가 {roomName}에서 퇴장하였습니다.");

					if (room.Value.Count == 0)
					{
						Rooms.Remove(roomName);
						_logger.LogInformation($"{roomName}이 사라졌습니다.");
					}
				}
			}

			await base.OnDisconnectedAsync(exception);
		}

		public async ValueTask SendOffer(string offer, string roomName)
		{
			var senderId = Context.ConnectionId;
			var group = Clients.GroupExcept(roomName, senderId);
			await group.SendAsync("ReceiveOffer", offer);
		}

		public async ValueTask SendAnswer(string answer, string roomName)
		{
			var senderId = Context.ConnectionId;
			var group = Clients.GroupExcept(roomName, senderId);
			await group.SendAsync("ReceiveAnswer", answer);
		}

		public async ValueTask SendIce(string ice, string senderId)
		{
			var roomNames = Rooms.Where(x => x.Value.Contains(senderId)).Select(x => x.Key);
			foreach (var roomName in roomNames)
			{
				var group = Clients.GroupExcept(roomName, senderId);
				await group.SendAsync("ReceiveIce", ice);
				_logger.LogInformation($"{roomName}의 유저 {senderId}가 ICE를 전송하였습니다.");
			}
		}
	}
}
