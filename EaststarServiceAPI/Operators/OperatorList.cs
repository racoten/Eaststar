using EaststarServiceAPI;
using EaststarServiceAPI.Operators;
using Microsoft.EntityFrameworkCore;

namespace EaststarServiceAPI.Operators
{
    public class OperatorList
    {
        private readonly EaststarContext _context = new EaststarContext();

        public async Task<List<OperatorsInfo>> GetOperatorsAsync()
        {
            var operators = await _context.OperatorsInfo
                .Where(o => o.Id != null)
                .ToListAsync();

            return operators.Select(o => new OperatorsInfo
            {
                Id = o.Id,
                Username = o.Username,
                Password = "NULL",
                AgentId = o.AgentId
            }).ToList();
        }
    }
}