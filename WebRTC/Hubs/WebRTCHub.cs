using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace webrtc_dotnetframework.Hubs
{
    public class WebRTCHub : Hub
    {
        private static RoomManager roomManager = new RoomManager();

        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            roomManager.DeleteRoom(Context.ConnectionId);
            NotifyRoomInfoAsync(false);
            return base.OnDisconnected(stopCalled);
        }

        public async Task CreateRoom(string name)
        {
            RoomInfo roomInfo = roomManager.CreateRoom(Context.ConnectionId, name);
            if (roomInfo != null)
            {
                await Groups.Add(Context.ConnectionId, roomInfo.RoomId);
                Clients.Caller.created(roomInfo.RoomId);
                NotifyRoomInfoAsync(false);
            }
            else
            {
                Clients.Caller.error("Error occurred when creating a new room.");
            }
        }

        public async Task Join(string roomId)
        {
            await Groups.Add(Context.ConnectionId, roomId);
            Clients.Caller.joined(roomId);
            Clients.Group(roomId).ready();

            // Remove the room from the room list
            if (int.TryParse(roomId, out int id))
            {
                roomManager.DeleteRoom(id);
                NotifyRoomInfoAsync(false);
            }
        }

        public void LeaveRoom(string roomId)
        {
            Clients.Group(roomId).bye();
        }

        public void GetRoomInfo()
        {
            NotifyRoomInfoAsync(true);
        }

        public void SendMessage(string roomId, object message)
        {
            Clients.OthersInGroup(roomId).message(message);
        }

        public void NotifyRoomInfoAsync(bool notifyOnlyCaller)
        {
            List<RoomInfo> roomInfos = roomManager.GetAllRoomInfo();
            var list = from room in roomInfos
                       select new
                       {
                           RoomId = room.RoomId,
                           Name = room.Name,
                           Button = "<button class=\"joinButton\">Join!</button>"
                       };
            var data = JsonConvert.SerializeObject(list);

            if (notifyOnlyCaller)
            {
                Clients.Caller.updateRoom(data);
            }
            else
            {
                Clients.All.updateRoom(data);
            }
        }
    }

    public class RoomManager
    {
        private int nextRoomId;
        private ConcurrentDictionary<int, RoomInfo> rooms;

        public RoomManager()
        {
            nextRoomId = 1;
            rooms = new ConcurrentDictionary<int, RoomInfo>();
        }

        public RoomInfo CreateRoom(string connectionId, string name)
        {
            rooms.TryRemove(nextRoomId, out _);

            var roomInfo = new RoomInfo
            {
                RoomId = nextRoomId.ToString(),
                Name = name,
                HostConnectionId = connectionId
            };

            bool result = rooms.TryAdd(nextRoomId, roomInfo);

            if (result)
            {
                nextRoomId++;
                return roomInfo;
            }
            else
            {
                return null;
            }
        }

        public void DeleteRoom(int roomId)
        {
            rooms.TryRemove(roomId, out _);
        }

        public void DeleteRoom(string connectionId)
        {
            int? correspondingRoomId = null;
            foreach (var pair in rooms)
            {
                if (pair.Value.HostConnectionId.Equals(connectionId))
                {
                    correspondingRoomId = pair.Key;
                }
            }

            if (correspondingRoomId.HasValue)
            {
                rooms.TryRemove(correspondingRoomId.Value, out _);
            }
        }

        public List<RoomInfo> GetAllRoomInfo()
        {
            return rooms.Values.ToList();
        }
    }

    public class RoomInfo
    {
        public string RoomId { get; set; }
        public string Name { get; set; }
        public string HostConnectionId { get; set; }
    }
}
