namespace EaststarServiceAPI.Agents
{
    public class AgentHealthCheck
    {
        private readonly EaststarContext context = new EaststarContext();
        public bool HealthCheck(AgentsInfo agentsInfo)
        {

            var agent = context.AgentsInfo.FirstOrDefault(a => a.Id == agentsInfo.Id);

            if (agent == null)
                return false;

            agent.LastPing = DateTime.UtcNow.ToString("o");
            context.SaveChanges();

            return true;
        }
    }
}
