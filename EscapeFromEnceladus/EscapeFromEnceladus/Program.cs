namespace Enceladus
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (EnceladusGame game = new EnceladusGame())
            {
                game.Run();
            }
        }
    }
#endif
}

