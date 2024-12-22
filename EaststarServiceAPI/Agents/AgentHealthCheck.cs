namespace EaststarServiceAPI.Agents
{
    public class AgentHealthCheck
    {
        public bool HealthCheck(AgentsInfo agentsInfo)
        {
            using var context = new EaststarContext();

            var agent = context.AgentsInfo.FirstOrDefault(a => a.Id == agentsInfo.Id);

            if (agent == null)
                return false;

            agent.LastPing = DateTime.UtcNow.ToString("o");
            context.SaveChanges();

            return true;
        }
    }
}
