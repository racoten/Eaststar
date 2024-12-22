using System;
using EaststarServiceAPI;
using EaststarServiceAPI.HttpListeners;

namespace EaststarServiceAPI.HttpListeners
{
    public class HttpListenersDelete
    {
        public bool DeleteListener(string id)
        {
            try
            {
                using (var context = new EaststarContext())
                {
                    var listener = context.HttpListenersInfo.FirstOrDefault(l => l.Id == id);
                    if (listener != null)
                    {
                        context.HttpListenersInfo.Remove(listener);
                        context.SaveChanges();
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Listener with ID {id} not found.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting listener: {ex.Message}");
                return false;
            }
        }
    }
}
