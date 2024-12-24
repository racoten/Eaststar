using EaststarServiceAPI.HttpListeners;

namespace EaststarServiceAPI.Testing
{
    public class Listeners
    {
        public static void Start()
        {
            var testListener = new HttpListenersInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Listener",
                Host = "127.0.0.1",
                Port = "5005",
                Headers = null,
                Active = true
            };

            var creator = new HttpListenersCreate();
            if (creator.CreateListener(testListener))
            {
                Console.WriteLine("Test listener created successfully.");
            }
            else
            {
                Console.WriteLine("Failed to create listener.");
            }
        }
    }
}
