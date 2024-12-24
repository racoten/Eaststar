using EaststarServiceAPI.Agents;

namespace EaststarServiceAPI.Operators
{
    public class AgentRegister
    {
        private readonly EaststarContext _context = new EaststarContext();
        public bool Register(AgentsInfo agentsInfo)
        {

            try
            {
                Guid guid = Guid.NewGuid();

                var existingUser = _context.AgentsInfo
                    .FirstOrDefault(o => o.Id == agentsInfo.Id);

                if (existingUser != null)
                {
                    return false;
                }

                agentsInfo.Id = guid.ToString();

                _context.AgentsInfo.Add(agentsInfo);
                _context.SaveChanges();

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
