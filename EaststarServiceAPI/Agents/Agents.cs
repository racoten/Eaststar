namespace EaststarServiceAPI.Agents
{
    public class AgentsInfo
    {
        public string? Id { get; set; }
        public required string Name { get; set; }
        public string? OperatingSystem { get; set; }
        public string? Architecture { get; set; }
        public string? Username { get; set; }
        public string? Hostname { get; set; }
        public string? Domain { get; set; }
        public string? LastPing { get; set; }
        public string? ProcessName { get; set; }
        public string? ProcessId { get; set; }
        public string? IPv4 { get; set; }
        public string? JWToken { get; set; }
    }
}
