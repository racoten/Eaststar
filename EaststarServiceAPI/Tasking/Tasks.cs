namespace EaststarServiceAPI.Tasking
{
    public class Tasks
    {
        public string? Id { get; set; }
        public required string Command { get; set; }
        public string? Arguments { get; set; }
        public string? File { get; set; }
        public string? UseTCP { get; set; }
        public string? ActTCP { get; set; }
        public int? Delay { get; set; }
        public int? TimeToExec { get; set; }
        public int? TcpPort { get; set; }
        public string? Date { get; set; }
        public string? AgentId { get; set; }
    }
}
