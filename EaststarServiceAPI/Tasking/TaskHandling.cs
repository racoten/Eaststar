using System.Text.Json;

namespace EaststarServiceAPI.Tasking
{
    public class TaskHandling
    {
        public bool PostTaskForAgent(Tasks task)
        {
            try
            {
                EaststarContext context = new EaststarContext();
                Guid guid = Guid.NewGuid();

                task.Date = DateTime.UtcNow.ToString();
                task.Id = guid.ToString();

                context.Add(task);
                context.SaveChanges();

                return true;
            }
            catch (Exception _)
            {
                return false;
            }

            
        }

        public string GetTaskForAgent(string agentId)
        {
            using var ctx = new EaststarContext();
            var tasks = ctx.Set<Tasks>()
                           .Where(t => t.AgentId == agentId)
                           .ToList();

            return JsonSerializer.Serialize(tasks);
        }
    }
}
