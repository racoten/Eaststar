using EaststarServiceAPI.Agents;
using System;
using System.Linq;
using System.Text.Json;

namespace EaststarServiceAPI.Tasking
{
    public class OutputHandling
    {
        EaststarContext context = new EaststarContext();
        public void PostOutputForOperator(AgentsInfo agentsInfo, string output)
        {
            
            var guid = Guid.NewGuid();

            var outputTask = new TaskOutput
            {
                Id = guid.ToString(),
                Output = output,
                AgentId = agentsInfo.Id
            };

            context.TaskOutput.Add(outputTask);
            context.SaveChanges();
        }
        public string GetOutputFromAgent(string agentId)
        {
            var taskOutput = context.Set<TaskOutput>()
                           .Where(t => t.AgentId == agentId)
                           .ToList();

            return JsonSerializer.Serialize(taskOutput);
        }
    }
}
