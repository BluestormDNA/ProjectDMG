using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDMG {
    public class MMU {

        private byte[] ROM = new byte[0x8000];
        private byte[] VRAM = new byte[0x2000];
        private byte[] ERAM = new byte[0x2000];
        private byte[] WRAM0 = new byte[0x1000];
        private byte[] WRAM1 = new byte[0x1000];
        private byte[] WRAM_MIRROR = new byte[0x1E00];
        private byte[] OAM = new byte[0xA0];
        //private byte[] NOT_USABLE = new byte[0x60];
        private byte[] IO = new byte[0x80];
        private byte[] HRAM = new byte[0x7F];
        private byte IE;

        public byte readByte(ushort addr) {
            switch (addr) {                                             // General Memory Map 64KB
                case ushort r when addr >= 0x0000 && addr <= 0x7FFF:    //0000-3FFF 16KB ROM Bank 00 (in cartridge, private at bank 00) 4000-7FFF 16KB ROM Bank 01..NN(in cartridge, switchable bank number)
                    return ROM[addr];
                case ushort r when addr >= 0x8000 && addr <= 0x9FFF:    // 8000-9FFF 8KB Video RAM(VRAM)(switchable bank 0-1 in CGB Mode)
                    return VRAM[addr & 0x1FFF];
                case ushort r when addr >= 0xA000 && addr <= 0xBFFF:    // A000-BFFF 8KB External RAM(in cartridge, switchable bank, if any) <br/>
                    return ERAM[addr & 0x1FFF];
                case ushort r when addr >= 0xC000 && addr <= 0xCFFF:    // C000-CFFF 4KB Work RAM Bank 0(WRAM) <br/>
                    return WRAM0[addr & 0xFFF];
                case ushort r when addr >= 0xD000 && addr <= 0xDFFF:    // D000-DFFF 4KB Work RAM Bank 1(WRAM)(switchable bank 1-7 in CGB Mode) <br/>
                    return WRAM1[addr & 0xFFF];
                case ushort r when addr >= 0xE000 && addr <= 0xFDFF:    // E000-FDFF Same as 0xC000-DDFF(ECHO)(typically not used) <br/>
                    return WRAM_MIRROR[addr & 0x1DFF];
                case ushort r when addr >= 0xFE00 && addr <= 0xFE9F:    // FE00-FE9F Sprite Attribute Table(OAM)
                    return OAM[addr & 0x9F];
                case ushort r when addr >= 0xFEA0 && addr <= 0xFEFF:    // FEA0-FEFF Not Usable
                    return 0;
                case ushort r when addr >= 0xFF00 && addr <= 0xFF7F:    // FF00-FF7F IO Ports
                    return IO[addr & 0x80];
                case ushort r when addr >= 0xFF80 && addr <= 0xFFFE:    // FF80-FFFE High RAM(HRAM)
                    return HRAM[addr & 0x7F];
                case 0xFFFF:                                            // FFFF Interrupt Enable Register.
                    return IE;
                default:
                    return 0;
            }
        }

        public void writeByte(ushort addr, byte b) {
            switch (addr) {                                             // General Memory Map 64KB
                case ushort r when addr >= 0x0000 && addr <= 0x7FFF:     //0000-3FFF 16KB ROM Bank 00 (in cartridge, private at bank 00) 4000-7FFF 16KB ROM Bank 01..NN(in cartridge, switchable bank number)
                    Console.WriteLine("Warning: Tried to write to ROM space " + addr.ToString("x4") + " " + b.ToString("x2"));
                    break;
                case ushort r when addr >= 0x8000 && addr <= 0x9FFF:    // 8000-9FFF 8KB Video RAM(VRAM)(switchable bank 0-1 in CGB Mode)
                    VRAM[addr & 0x1FFF] = b;
                    break;
                case ushort r when addr >= 0xA000 && addr <= 0xBFFF:    // A000-BFFF 8KB External RAM(in cartridge, switchable bank, if any) <br/>
                    ERAM[addr & 0x1FFF] = b;
                    break;
                case ushort r when addr >= 0xC000 && addr <= 0xCFFF:    // C000-CFFF 4KB Work RAM Bank 0(WRAM) <br/>
                    WRAM0[addr & 0xFFF] = b;
                    break;
                case ushort r when addr >= 0xD000 && addr <= 0xDFFF:    // D000-DFFF 4KB Work RAM Bank 1(WRAM)(switchable bank 1-7 in CGB Mode) <br/>
                    WRAM1[addr & 0xFFF] = b;
                    break;
                case ushort r when addr >= 0xE000 && addr <= 0xFDFF:    // E000-FDFF Same as 0xC000-DDFF(ECHO)(typically not used) <br/>
                    WRAM_MIRROR[addr & 0x1DFF] = b;
                    break;
                case ushort r when addr >= 0xFE00 && addr <= 0xFE9F:    // FE00-FE9F Sprite Attribute Table(OAM)
                    OAM[addr & 0x9F] = b;
                    break;
                case ushort r when addr >= 0xFEA0 && addr <= 0xFEFF:    // FEA0-FEFF Not Usable
                    Console.WriteLine("Warning: Tried to write to NOT USABLE space");
                    break;
                case ushort r when addr >= 0xFF00 && addr <= 0xFF7F:    // FF00-FF7F IO Ports
                    IO[addr & 0x80] = b;
                    break;
                case ushort r when addr >= 0xFF80 && addr <= 0xFFFE:    // FF80-FFFE High RAM(HRAM)
                    HRAM[addr & 0x7F] = b;
                    break;
                case 0xFFFF: // FFFF Interrupt Enable Register.
                    IE = b;
                    break;
            }
        }

        public ushort readWord(ushort addr) {
            return (ushort)(readByte((ushort)(addr + 1)) << 8 | readByte(addr));
        }

        public void writeWord(ushort addr, ushort w) {
            writeByte((ushort)(addr + 1), (byte)(w >> 8));
            writeByte(addr, (byte)w);
        }

        public void loadBootRom() {
            byte[] rom = File.ReadAllBytes("DMG_ROM.bin");
            Array.Copy(rom, 0, ROM, 0, rom.Length);
        }

    }
}



