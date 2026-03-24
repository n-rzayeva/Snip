using Microsoft.AspNetCore.SignalR;

namespace Snip.LinkService.Hubs;

public class ClickHub : Hub
{
    public async Task JoinLinkGroup(string slug)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, slug);
    }
}