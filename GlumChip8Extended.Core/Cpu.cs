using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace GlumChip8Extended.Core
{
    public class Cpu
    {
        const int STACK_SZ = 32; // 64 bytes - ushort is 2 bytes

        byte[] _registers = new byte[0x10];
        ushort _rI;
        ushort _rPC;
        ushort _rSP;
        byte _sT;
        byte _dT;
        double _timerAccumulator = 0.0;
        ushort[] _stack = new ushort[STACK_SZ];
        Random _rnd;

        public ushort ProgramCounter { get => _rPC; }
        public ushort StackPointer { get => _rSP; }
        public string RegisterStates
        {
            get
            {
                StringBuilder sb = new();
                sb.Append("Registers:\n");
                for (int i = 0; i < _registers.Length; i++)
                {
                    sb.Append($"R[{i}]: {_registers[i]}\n");
                }
                return sb.ToString();
            }
        }
        public ushort OperationCode
        {
            get;
            private set;
        }

        public Cpu()
        {
            _rnd = new();
            // Temporary enforcement
            _rPC = 0x200;
        }

        void PushStack(ushort value)
        {
            _stack[_rSP++] = value;
        }

        ushort PopStack()
        {
            return _stack[--_rSP];
        }

        void Opperation_RET()
        {
            _rPC = PopStack();
        }

        void Operation_SYS(ushort addr)
        {
            _rPC = addr;
        }

        void Operation_JPnnn(ushort addr)
        {
            _rPC = addr;
        }

        void Operation_CALLnnn(ushort addr)
        {
            PushStack(_rPC);
            _rPC = addr;
        }

        void Operation_SE_Vxkk(byte x, byte val)
        {
            if (_registers[x] == val) _rPC += 2;
        }

        void Operation_SNE_Vxkk(byte x, byte val)
        {
            if (_registers[x] != val) _rPC += 2;
        }

        void Operation_SE_VxVy(byte x, byte y)
        {
            if (_registers[x] == _registers[y]) _rPC += 2;
        }

        void Operation_LD_Vxkk(byte x, byte val)
        {
            _registers[x] = val;
        }

        void Operation_ADD_Vxkk(byte x, byte val)
        {
            _registers[x] += val;
        }

        void Operation_LD_VxVy(byte x, byte y)
        {
            _registers[x] = _registers[y];
        }

        void Operation_OR_VxVy(byte x, byte y)
        {
            _registers[x] |= _registers[y];
        }

        void Operation_AND_VxVy(byte x, byte y)
        {
            _registers[x] &= _registers[y];
        }

        void Operation_XOR_VxVy(byte x, byte y)
        {
            _registers[x] ^= _registers[y];
        }

        void Operation_ADD_VxVy(byte x, byte y)
        {
            ushort sum = (ushort)(_registers[x] + _registers[y]);
            _registers[0xF] = (byte)(sum > 255 ? 1 : 0);
            _registers[x] = (byte)(sum & 0xFF);
        }

        void Operation_SUB_VxVy(byte x, byte y)
        {
            _registers[0xF] = (byte)(_registers[x] > _registers[y] ? 1 : 0);
            _registers[x] -= _registers[y];
        }

        void Operation_SHR_VxVy(byte x, byte y)
        {
            // Get least significant bit of x
            // if bit is 1 then vF is 1 otherwise 0
            // divide vX by 2
            _registers[0xF] = (byte)((_registers[x] & 1) == 1 ? 1 : 0);
            _registers[x] /= 2;
        }

        void Operation_SUBN_VxVy(byte x, byte y)
        {
            _registers[0xF] = (byte)(_registers[y] > _registers[x] ? 1 : 0);
            _registers[x] = (byte)(_registers[y] - _registers[x]);
        }

        void Operation_SHL_VxVy(byte x, byte y)
        {
            // Set VF to the MSB (most significant bit) before shifting
            _registers[0xF] = (byte)((_registers[x] >> 7) & 1);
            _registers[x] <<= 1;
        }

        void Operation_SNE_VxVy(byte x, byte y)
        {
            if (_registers[x] != _registers[y]) _rPC += 2;
        }

        void Operation_LD_Innn(ushort val)
        {
            _rI = val;
        }

        void Operation_JP_V0nnn(ushort val)
        {
            _rPC = (ushort)(val + _registers[0x00]);
        }

        void Operation_RND_Vxkk(byte x, byte val)
        {
            byte random = (byte)_rnd.Next(0, 256);
            _registers[x] = (byte)(random & val);

        }

        void Operation_DRW_VxVy(byte x, byte y, byte n)
        {
            byte startX = _registers[x];
            byte startY = _registers[y];
            int width = (n == 0) ? 16 : 8;
            int height = (n == 0) ? 16 : n;

            _registers[0xF] = 0;

            for (int yline = 0; yline < height; yline++)
            {
                // SCHIP 16x16 sprites take 2 bytes per row
                for (int byteIdx = 0; byteIdx < (width / 8); byteIdx++)
                {
                    ushort memAddr = (ushort)(_rI + (yline * (width / 8)) + byteIdx);
                    byte pixelByte = BusMaster.GlobalBus.ReadMemory(memAddr);

                    for (int xline = 0; xline < 8; xline++)
                    {
                        if ((pixelByte & (0x80 >> xline)) != 0)
                        {
                            int totalX = startX + (byteIdx * 8) + xline;
                            int totalY = startY + yline;

                            // SCHIP behavior: Sprites wrap or clip? 
                            // Most SCHIP ROMS expect wrapping logic:
                            int px = totalX % BusMaster.GlobalBus._display.Width;
                            int py = totalY % BusMaster.GlobalBus._display.Height;

                            if (BusMaster.GlobalBus._display.pixels[px, py] == 1)
                            {
                                _registers[0xF] = 1;
                            }
                            BusMaster.GlobalBus._display.pixels[px, py] ^= 1;
                        }
                    }
                }
            }
            BusMaster.GlobalBus._display._drawFlag = true;
        }

        void Operation_SKP_Vx(byte x)
        {
            byte key = _registers[x];
            if (BusMaster.GlobalBus._keypad._keys[key] == 1)
            {
                _rPC += 2;
            }
        }

        void Operation_SKNP_Vx(byte x)
        {
            byte key = _registers[x];
            if (BusMaster.GlobalBus._keypad._keys[key] == 0)
            {
                _rPC += 2;
            }
        }

        void Operation_LD_VxDt(byte x)
        {
            _registers[x] = _dT;
        }

        void Operation_LD_VxK(byte x)
        {
            int key = BusMaster.GlobalBus._keypad.GetLastPressedHexKey();

            if (key != -1)
            {
                _registers[x] = (byte)key;
            }
            else
            {
                _rPC -= 2;
            }
        }

        void Operation_LD_DtVx(byte x)
        {
            _dT = _registers[x];
        }

        void Operation_LD_VxSt(byte x)
        {
            _registers[x] = _sT;
        }

        void Operation_ADD_IVx(byte x)
        {
            _rI += _registers[x];
        }

        void Operation_LD_FVx(byte x)
        {
            _rI = (ushort)(_registers[x] * 5);
        }

        void Operation_StoreBCD(int vxIndex)
        {
            byte value = _registers[vxIndex];
            // Hundreds 
            BusMaster.GlobalBus.WriteMemory(_rI, (byte)(value / 100));
            // Tens
            BusMaster.GlobalBus.WriteMemory((ushort)(_rI + 1), (byte)((value / 10) % 10));
            // Ones
            BusMaster.GlobalBus.WriteMemory((ushort)(_rI + 2), (byte)(value % 10));
        }

        void Operation_LD_IvX(byte x)
        {
            for (int i = 0; i <= x; i++)
            {
                BusMaster.GlobalBus.WriteMemory((ushort)(_rI + i), _registers[i]);
            }

            // If the quirk is NOT active, we follow the original CHIP-8 spec 
            // and increment the I register.
            if (!BusMaster.Settings.loadStoreQuirk)
            {
                _rI = (ushort)(_rI + x + 1);
            }
        }

        void Operation_LD_VxI(byte x)
        {
            for (int i = 0; i <= x; i++)
            {
                _registers[i] = BusMaster.GlobalBus.ReadMemory((ushort)(_rI + i));
            }

            if (!BusMaster.Settings.loadStoreQuirk)
            {
                _rI = (ushort)(_rI + x + 1);
            }
        }

        public ushort GetNextOpcode()
        {
            ushort opcode = (ushort)(
                (BusMaster.GlobalBus.ReadMemory(_rPC) << 8) |
                 BusMaster.GlobalBus.ReadMemory((ushort)(_rPC + 1))
            );

            _rPC += 2;
            return opcode;
        }

        void Operation_SCD(byte n)
        {
            for (int y = BusMaster.GlobalBus._display.Height - 1; y >= n; y--)
                for (int x = 0; x < BusMaster.GlobalBus._display.Width; x++)
                    BusMaster.GlobalBus._display.pixels[x, y] = BusMaster.GlobalBus._display.pixels[x, y - n];

            for (int y = 0; y < n; y++)
                for (int x = 0; x < BusMaster.GlobalBus._display.Width; x++)
                    BusMaster.GlobalBus._display.pixels[x, y] = 0;
        }

        void Operation_SCR()
        {
            for (int y = 0; y < BusMaster.GlobalBus._display.Height; y++)
            {
                for (int x = BusMaster.GlobalBus._display.Width - 1; x >= 4; x--)
                    BusMaster.GlobalBus._display.pixels[x, y] = BusMaster.GlobalBus._display.pixels[x - 4, y];
                for (int x = 0; x < 4; x++)
                    BusMaster.GlobalBus._display.pixels[x, y] = 0;
            }
        }

        void Operation_SCL()
        {
            for (int y = 0; y < BusMaster.GlobalBus._display.Height; y++)
            {
                for (int x = 0; x < BusMaster.GlobalBus._display.Width - 4; x++)
                    BusMaster.GlobalBus._display.pixels[x, y] = BusMaster.GlobalBus._display.pixels[x + 4, y];
                for (int x = BusMaster.GlobalBus._display.Width - 4; x < BusMaster.GlobalBus._display.Width; x++)
                    BusMaster.GlobalBus._display.pixels[x, y] = 0;
            }
        }

        void Operation_EXIT() => Environment.Exit(0);
        void Operation_LOW() => BusMaster.GlobalBus._display.SetResolution(false);
        void Operation_HIGH() => BusMaster.GlobalBus._display.SetResolution(true);
        void Operation_LD_HFVx(byte x) => _rI = (ushort)(_registers[x] * 10 + 80);

        byte[] _hpFlags = new byte[8];
        void Operation_LD_RVx(byte x) { for (int i = 0; i <= Math.Min(x, (byte)7); i++) _hpFlags[i] = _registers[i]; }
        void Operation_LD_VxR(byte x) { for (int i = 0; i <= Math.Min(x, (byte)7); i++) _registers[i] = _hpFlags[i]; }

        public void RunInstruction(ushort opcode)
        {
            OperationCode = opcode;

            // Extract variables
            ushort nnn = (ushort)(opcode & 0x0FFF);
            byte x = (byte)((opcode & 0x0F00) >> 8);
            byte y = (byte)((opcode & 0x00F0) >> 4);
            byte kk = (byte)(opcode & 0x00FF);
            byte nibble = (byte)(opcode & 0x000F);
            switch (opcode & 0xF000)
            {
                case 0x0000:
                    if (opcode == 0x00E0) Operation_CLS();
                    else if (opcode == 0x00EE) Opperation_RET();
                    else if (opcode == 0x00FB) Operation_SCR();
                    else if (opcode == 0x00FC) Operation_SCL();
                    else if (opcode == 0x00FD) Operation_EXIT();
                    else if (opcode == 0x00FE) Operation_LOW();
                    else if (opcode == 0x00FF) Operation_HIGH();
                    else if ((opcode & 0xFFF0) == 0x00C0) Operation_SCD((byte)(opcode & 0x000F));
                    else Operation_SYS(nnn);
                    break;

                case 0x1000: Operation_JPnnn(nnn); break;
                case 0x2000: Operation_CALLnnn(nnn); break;
                case 0x3000: Operation_SE_Vxkk(x, kk); break;
                case 0x4000: Operation_SNE_Vxkk(x, kk); break;
                case 0x5000: Operation_SE_VxVy(x, y); break;
                case 0x6000: Operation_LD_Vxkk(x, kk); break;
                case 0x7000: Operation_ADD_Vxkk(x, kk); break;

                case 0x8000:
                    switch (nibble)
                    {
                        case 0x0: Operation_LD_VxVy(x, y); break;
                        case 0x1: Operation_OR_VxVy(x, y); break;
                        case 0x2: Operation_AND_VxVy(x, y); break;
                        case 0x3: Operation_XOR_VxVy(x, y); break;
                        case 0x4: Operation_ADD_VxVy(x, y); break;
                        case 0x5: Operation_SUB_VxVy(x, y); break;
                        case 0x6: Operation_SHR_VxVy(x, y); break;
                        case 0x7: Operation_SUBN_VxVy(x, y); break;
                        case 0xE: Operation_SHL_VxVy(x, y); break;
                    }
                    break;

                case 0x9000: Operation_SNE_VxVy(x, y); break;
                case 0xA000: Operation_LD_Innn(nnn); break;
                case 0xB000: Operation_JP_V0nnn(nnn); break;
                case 0xC000: Operation_RND_Vxkk(x, kk); break;
                case 0xD000: Operation_DRW_VxVy(x, y, nibble); break;

                case 0xE000:
                    if (kk == 0x9E) Operation_SKP_Vx(x);
                    else if (kk == 0xA1) Operation_SKNP_Vx(x);
                    break;

                case 0xF000:
                    switch (kk)
                    {
                        case 0x07: Operation_LD_VxDt(x); break;
                        case 0x0A: Operation_LD_VxK(x); break;
                        case 0x15: Operation_LD_DtVx(x); break;
                        case 0x18: Operation_LD_VxSt(x); break;
                        case 0x1E: Operation_ADD_IVx(x); break;
                        case 0x29: Operation_LD_FVx(x); break;
                        case 0x33: Operation_StoreBCD(x); break;
                        case 0x55: Operation_LD_IvX(x); break;
                        case 0x65: Operation_LD_VxI(x); break;
                        case 0x30: Operation_LD_HFVx(x); break;
                        case 0x75: Operation_LD_RVx(x); break;
                        case 0x85: Operation_LD_VxR(x); break;
                    }
                    break;

                default:
                    throw new NotImplementedException($"Opcode {opcode:X4} not supported.");
            }
        }

        public void UpdateTimers()
        {
            _timerAccumulator += Raylib.GetFrameTime();

            double interval = 1.0 / 60;

            while (_timerAccumulator >= interval)
            {
                if (_dT > 0)
                    _dT--;

                if (_sT > 0)
                    _sT--;
                _timerAccumulator -= interval;
            }

        }

        private void Operation_CLS()
        {
            for (int i = 0; i < BusMaster.GlobalBus._display.Width; i++)
            {
                for (int j = 0; j < BusMaster.GlobalBus._display.Height; j++)
                {
                    BusMaster.GlobalBus._display.pixels[i, j] = 0;
                }
            }
        }

        public void Reset()
        {
            _rPC = 0x200;
            Operation_CLS();
            Array.Clear(_registers);
        }
    }
}
