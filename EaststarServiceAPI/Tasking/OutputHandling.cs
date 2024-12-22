using EaststarServiceAPI.Agents;
using System;
using System.Linq;
using System.Text.Json;

namespace EaststarServiceAPI.Tasking
{
    public class OutputHandling
    {
        EaststarContext context = new EaststarContext();
        public string PostOutputForOperator(string agentId, string output)
        {
            
            var guid = Guid.NewGuid();

            var outputTask = new TaskOutput
            {
                Id = guid.ToString(),
                Output = output,
                AgentId = agentId
            };

            context.TaskOutput.Add(outputTask);
            context.SaveChanges();

            return JsonSerializer.Serialize(outputTask);
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
