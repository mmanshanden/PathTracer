using System;

namespace PathTracer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Window game = new Window(512, 512))
            {
                game.Run();
            }
        }
    }
}
