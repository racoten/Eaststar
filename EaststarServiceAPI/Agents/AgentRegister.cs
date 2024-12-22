using EaststarServiceAPI.Agents;

namespace EaststarServiceAPI.Operators
{
    public class AgentRegister
    {
        public bool Register(AgentsInfo agentsInfo)
        {
            using var context = new EaststarContext();

            try
            {
                Guid guid = Guid.NewGuid();

                var existingUser = context.AgentsInfo
                    .FirstOrDefault(o => o.Id == agentsInfo.Id);

                if (existingUser != null)
                {
                    return false;
                }

                agentsInfo.Id = guid.ToString();

                context.AgentsInfo.Add(agentsInfo);
                context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
