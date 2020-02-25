namespace SurviveCoreServer
{
    static class Program
    {
        static void Main(string[] args)
        {
            using (Server server = new Server())
            {
                server.Run();
            }
        }
    }
}