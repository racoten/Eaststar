using EaststarServiceAPI;
using EaststarServiceAPI.Agents;
using Microsoft.EntityFrameworkCore;

public class AgentList
{
    private readonly EaststarContext _context = new EaststarContext();

    public async Task<List<AgentsInfo>> GetAgentsAsync()
    {
        var agents = await _context.AgentsInfo
            .Where(a => a.Id != null)
            .ToListAsync();

        return agents.Select(a => new AgentsInfo
        {
            Id = a.Id,
            Name = a.Name,
            OperatingSystem = a.OperatingSystem,
            Architecture = a.Architecture,
            Username = a.Username,
            Hostname = a.Hostname,
            Domain = a.Domain,
            LastPing = a.LastPing,
            ProcessName = a.ProcessName,
            ProcessId = a.ProcessId,
            IPv4 = a.IPv4
        }).ToList();
    }
}