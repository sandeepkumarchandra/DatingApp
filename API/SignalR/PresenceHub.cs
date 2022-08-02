using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extentions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly PresenceTracker _tracker;
        public PresenceHub(PresenceTracker tracker)
        {
            _tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            var isOnline = await _tracker.UserConnected(Context.User.getUserName(), Context.ConnectionId);
            if(isOnline)
                await Clients.Others.SendAsync("UserIsOnline", Context.User.getUserName());

            var currentUsers = await _tracker.GetOnlieUsers();
            await Clients.Caller.SendAsync("GetOnlineUsers",currentUsers);
        }
 
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var isOffline = await _tracker.UserDisconnected(Context.User.getUserName(), Context.ConnectionId);
            if(isOffline)
                await  Clients.Others.SendAsync("UserIsOffline", Context.User.getUserName());
            
            await base.OnDisconnectedAsync(exception);
        }
        
    }
}