using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EaststarServiceAPI.Agents;
using EaststarServiceAPI.Operators;
using EaststarServiceAPI.Tasking;

namespace EaststarServiceAPI.HttpListeners
{
    public class HttpListenersCreate
    {
        private static readonly Dictionary<string, CancellationTokenSource> _ctsMap = new();
        private static readonly Dictionary<string, Task> _serverTasks = new();
        public bool CreateListener(HttpListenersInfo listener)
        {
            try
            {
                using var context = new EaststarContext();
                context.HttpListenersInfo.Add(listener);
                context.SaveChanges();

                var cts = new CancellationTokenSource();
                HttpListenersCreateForAgents httpListenersCreateForAgents = new HttpListenersCreateForAgents();
                _ctsMap[listener.Id] = cts;
                _serverTasks[listener.Id] = Task.Run(() => httpListenersCreateForAgents.StartListener(listener, cts.Token));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating listener: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StopListenerAsync(string listenerId)
        {
            if (!_ctsMap.ContainsKey(listenerId)) return false;

            var cts = _ctsMap[listenerId];
            var task = _serverTasks[listenerId];

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _ctsMap.Remove(listenerId);
            _serverTasks.Remove(listenerId);

            try
            {
                return RemoveListener(listenerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return true;
        }

        private bool RemoveListener(string id)
        {
            var deleter = new HttpListenersDelete();
            return deleter.DeleteListener(id);
        }
    }
}
