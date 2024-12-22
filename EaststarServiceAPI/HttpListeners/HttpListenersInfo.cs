namespace EaststarServiceAPI.HttpListeners
{
    public class HttpListenersInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public required string Host {  get; set; }
        public required string Port { get; set; }
        public string? Headers { get; set; }
        public bool Active { get; set; }
    }
}
