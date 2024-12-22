using System;
using System.Collections.Generic;
using System.Linq;
using EaststarServiceAPI;
using EaststarServiceAPI.HttpListeners;

namespace EaststarServiceAPI.HttpListeners
{
    public class HttpListenersGet
    {
        public List<HttpListenersInfo> GetAllListeners()
        {
            try
            {
                using (var context = new EaststarContext())
                {
                    return context.HttpListenersInfo.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching listeners: {ex.Message}");
                return new List<HttpListenersInfo>();
            }
        }

        public HttpListenersInfo GetListenerById(string id)
        {
            try
            {
                using (var context = new EaststarContext())
                {
                    return context.HttpListenersInfo.FirstOrDefault(listener => listener.Id == id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching listener by ID: {ex.Message}");
                return null;
            }
        }
    }
}
