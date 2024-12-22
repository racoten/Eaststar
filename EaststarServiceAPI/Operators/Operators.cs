namespace EaststarServiceAPI.Operators
{
    public class OperatorsInfo
    {
        public string? Id { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? AgentId { get; set; }
        public string? JWToken {  get; set; }
    }
}
