
using GlumChip8Extended.Core;
using Raylib_cs;

namespace GlumChip8Extended.Console
{
    internal class Program
    {
        static void RunRom(string file)
        {
            Raylib.InitWindow(800, 600, "Chip8");
            BusMaster.Init(new BusMasterSettings(false));
            BusMaster.GlobalBus.LoadCh8(file);
            BusMaster.Run();
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                System.Console.Write("Please enter the Path of the rom to load: ");
                var f = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(f)) throw new FileNotFoundException();
                var fClean = f.Trim('\"');
                RunRom(fClean);
            }
            else
            {
                RunRom(args[1]);
            }

        }
    }
}
