using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace GlumChip8Extended.Core
{

    public class Bus
    {
        const ushort RAM_END = 0xFFF;
        const ushort CH8_START = 0x200;
        const int RAM_SZ = 4096;
        byte[] _ram = new byte[RAM_SZ];
        private readonly byte[] _fontset = new byte[]
        {

                0xF0, 0x90, 0x90, 0x90, 0xF0, //0
                0x20, 0x60, 0x20, 0x20, 0x70, //1
                0xF0, 0x10, 0xF0, 0x80, 0xF0, //2
                0xF0, 0x10, 0xF0, 0x10, 0xF0, //3
                0x90, 0x90, 0xF0, 0x10, 0x10, //4
                0xF0, 0x80, 0xF0, 0x10, 0xF0, //5
                0xF0, 0x80, 0xF0, 0x90, 0xF0, //6
                0xF0, 0x10, 0x20, 0x40, 0x40, //7
                0xF0, 0x90, 0xF0, 0x90, 0xF0, //8
                0xF0, 0x90, 0xF0, 0x10, 0xF0, //9
                0xF0, 0x90, 0xF0, 0x90, 0x90, //A
                0xE0, 0x90, 0xE0, 0x90, 0xE0, //B
                0xF0, 0x80, 0x80, 0x80, 0xF0, //C
                0xE0, 0x90, 0x90, 0x90, 0xE0, //D
                0xF0, 0x80, 0xF0, 0x80, 0xF0, //E
                0xF0, 0x80, 0xF0, 0x80, 0x80  //F
        };

        public Cpu _cpu;
        public Display _display;
        public Keypad _keypad;

        public Bus()
        {
            _cpu = new();
            _display = new();
            _keypad = new();
            // Load fontset
            for (int i = 0; i < _fontset.Length; i++)
            {
                WriteMemory((ushort)(0x000 + i), _fontset[i]);
            }
        }

        public void WriteMemory(ushort addr, byte val)
        {
            if (addr < RAM_SZ)
            {
                _ram[addr] = val;
            }
        }

        public byte ReadMemory(ushort addr)
        {
            if (addr < RAM_SZ)
            {
                return _ram[addr];
            }
            return 0x00;
        }

        public void LoadCh8(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(file);

            var bytes = File.ReadAllBytes(file);
            if (bytes.Length > RAM_END)
                throw new OutOfMemoryException();
            for (int i = 0; i < bytes.Length; i++)
            {
                _ram[CH8_START + i] = bytes[i];
            }
        }

        public string GetInstructionDebugLine()
        {
            return new string($"Operation: 0x{_cpu.OperationCode:X4} | PC: 0x{_cpu.ProgramCounter:X4} | SP: 0x{_cpu.StackPointer:X4} | {_cpu.RegisterStates}");
        }

        public void RunNextInstruction()
        {
            _cpu.RunInstruction(_cpu.GetNextOpcode());
        }
    }

    public struct BusMasterSettings
    {
        public bool logAllInstructions;
        public bool loadStoreQuirk;
        public bool drawFPS;
        public bool handleInputExternally;

        public BusMasterSettings(bool logAllInstructions = false, bool loadStoreQuirk = true, bool drawFPS = false, bool handleInputExternally = false)
        {
            this.logAllInstructions = logAllInstructions;
            this.loadStoreQuirk = loadStoreQuirk;
            this.drawFPS = drawFPS;
            this.handleInputExternally = handleInputExternally;
        }
    }

    public static class BusMaster
    {

        public static Bus GlobalBus;
        public static BusMasterSettings Settings;

        public static bool Running { get; set; }
        public static bool IsInitialized { get; set; }

        public static void Init(BusMasterSettings settings = new())
        {
            GlobalBus = new();
            Settings = settings;
        }

        public static void Reset()
        {
            GlobalBus._cpu.Reset();
            GlobalBus._keypad.Reset();
        }

        private static void _cycle()
        {
            if (!Settings.handleInputExternally)
                GlobalBus._keypad.UpdateFromPCKeyBoard();

            for (int i = 0; i < 10; i++)
            {
                GlobalBus.RunNextInstruction();
            }

            GlobalBus._cpu.UpdateTimers();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            GlobalBus._display.Draw(false);
            if (Settings.drawFPS)
            {

                Raylib.DrawFPS(10, 10);
            }
            Raylib.EndDrawing();
        }

        public static void Run(bool fromInternal = false)
        {
            // High-level timing control

            if (fromInternal) // Window closing and main loop handled from outside
            {
                if (!Raylib.WindowShouldClose())
                {
                    _cycle();
                }
                return;
            }

            while (!Raylib.WindowShouldClose())
            {
                _cycle();
            }
            Raylib.CloseWindow();
        }
    }
}
