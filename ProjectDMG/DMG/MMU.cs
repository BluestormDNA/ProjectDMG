﻿using ProjectDMG.DMG.GamePak;
using System;
using System.IO;

namespace ProjectDMG {
    public class MMU {

        //GamePak
        private IGamePak gamePak;

        //BootRom
        private byte[] BOOT_ROM = new byte[0x100];
        //DMG Memory Map
        private byte[] VRAM = new byte[0x2000];
        private byte[] WRAM0 = new byte[0x1000];
        private byte[] WRAM1 = new byte[0x1000];
        private byte[] OAM = new byte[0xA0];
        public byte[] IO = new byte[0x80];
        private byte[] HRAM = new byte[0x7F];

        //BootRom Reg
        public byte BR { get { return IO[0x50]; } set { IO[0x50] = value; } }

        //Timer IO Regs
        public byte DIV { get { return IO[0x04]; } set { IO[0x04] = value; } } //FF04 - DIV - Divider Register (R/W)
        public byte TIMA { get { return IO[0x05]; } set { IO[0x05] = value; } } //FF05 - TIMA - Timer counter (R/W)
        public byte TMA { get { return IO[0x06]; } set { IO[0x06] = value; } } //FF06 - TMA - Timer Modulo (R/W)
        public byte TAC { get { return IO[0x07]; } set { IO[0x07] =  value; } } //FF07 - TAC - Timer Control (R/W)
        public bool TAC_ENABLED { get { return (IO[0x07] & 0x4) != 0; } } // Check if byte 2 is 1
        public byte TAC_FREQ { get { return (byte)(IO[0x07] & 0x3); } } // returns byte 0 and 1

        //Interrupt IO Flags
        //Bit 0: V-Blank Interrupt Enable(INT 40h)  (1=Enable)
        //Bit 1: LCD STAT Interrupt Enable(INT 48h)  (1=Enable)
        //Bit 2: Timer Interrupt Enable(INT 50h)  (1=Enable)
        //Bit 3: Serial Interrupt Enable(INT 58h)  (1=Enable)
        //Bit 4: Joypad Interrupt Enable(INT 60h)  (1=Enable)
        public byte IE { get; set; }//FFFF - IE - Interrupt Enable (R/W)
        public byte IF { get { return IO[0x0F]; } set { IO[0x0F] = value; } }//FF0F - IF - Interrupt Flag (R/W)

        //PPU IO Regs
        public byte LCDC { get { return IO[0x40]; } }//FF40 - LCDC - LCD Control (R/W)
        public byte STAT { get { return IO[0x41]; } set { IO[0x41] = value; } }//FF41 - STAT - LCDC Status (R/W)

        public byte SCY { get { return IO[0x42]; } }//FF42 - SCY - Scroll Y (R/W)
        public byte SCX { get { return IO[0x43]; } }//FF43 - SCX - Scroll X (R/W)
        public byte LY { get { return IO[0x44]; } set { IO[0x44] = value; } }//FF44 - LY - LCDC Y-Coordinate (R) bypasses on write always 0
        public byte LYC { get { return IO[0x45]; } }//FF45 - LYC - LY Compare(R/W)
        public byte WY { get { return IO[0x4A]; } }//FF4A - WY - Window Y Position (R/W)
        public byte WX { get { return IO[0x4B]; } }//FF4B - WX - Window X Position minus 7 (R/W)

        public byte BGP { get { return IO[0x47]; } }//FF47 - BGP - BG Palette Data(R/W) - Non CGB Mode Only
        public byte OBP0 { get { return IO[0x48]; } }//FF48 - OBP0 - Object Palette 0 Data (R/W) - Non CGB Mode Only
        public byte OBP1 { get { return IO[0x49]; } }//FF49 - OBP1 - Object Palette 1 Data (R/W) - Non CGB Mode Only

        //public byte DMA { get { return readByte(0xFF46); } }//FF46 - DMA - DMA Transfer and Start Address (R/W)

        public byte JOYP { get { return IO[0x00]; } set { IO[0x00] = value; } }//FF00 - JOYP

        public MMU() {
            //FF4D - KEY1 - CGB Mode Only - Prepare Speed Switch
            //HardCoded to FF to identify DMG as 00 is GBC
            IO[0x4D] = 0xFF;
        }

        public byte readByte(ushort addr) {
            switch (addr) {                                             // General Memory Map 64KB
                case ushort r when addr >= 0x0000 && addr <= 0x3FFF:    //0000-3FFF 16KB ROM Bank 00 (in cartridge, private at bank 00)
                    if (BR == 0 && addr < 0x100) {
                        return BOOT_ROM[addr];
                    } else {
                        return gamePak.ReadLoROM(addr);
                    }
                case ushort r when addr >= 0x4000 && addr <= 0x7FFF:    // 4000-7FFF 16KB ROM Bank 01..NN(in cartridge, switchable bank number)
                    return gamePak.ReadHiROM((ushort)(addr - 0x4000));
                case ushort r when addr >= 0x8000 && addr <= 0x9FFF:    // 8000-9FFF 8KB Video RAM(VRAM)(switchable bank 0-1 in CGB Mode)
                    return VRAM[addr - 0x8000];
                case ushort r when addr >= 0xA000 && addr <= 0xBFFF:    // A000-BFFF 8KB External RAM(in cartridge, switchable bank, if any)
                    return gamePak.ReadERAM((ushort)(addr - 0xA000));
                case ushort r when addr >= 0xC000 && addr <= 0xCFFF:    // C000-CFFF 4KB Work RAM Bank 0(WRAM) <br/>
                case ushort s when addr >= 0xE000 && addr <= 0xEFFF:    // E000-FDFF Same as 0xC000-DDFF(ECHO)  
                    return WRAM0[addr & 0xFFF];
                case ushort r when addr >= 0xD000 && addr <= 0xDFFF:    // D000-DFFF 4KB Work RAM Bank 1(WRAM)(switchable bank 1-7 in CGB Mode) <br/>
                case ushort s when addr >= 0xF000 && addr <= 0xFDFF:    // E000-FDFF Same as 0xC000-DDFF(ECHO)
                    return WRAM1[addr & 0xFFF];
                case ushort r when addr >= 0xFE00 && addr <= 0xFE9F:    // FE00-FE9F Sprite Attribute Table(OAM)
                    return OAM[addr - 0xFE00];
                case ushort r when addr >= 0xFEA0 && addr <= 0xFEFF:    // FEA0-FEFF Not Usable 0
                    return 0x00;
                case ushort r when addr >= 0xFF00 && addr <= 0xFF7F:    // FF00-FF7F IO Ports
                    return IO[addr - 0xFF00];
                case ushort r when addr >= 0xFF80 && addr <= 0xFFFE:    // FF80-FFFE High RAM(HRAM)
                    return HRAM[addr - 0xFF80];
                case 0xFFFF:                                            // FFFF Interrupt Enable Register
                    return IE;
                default:
                    return 0xFF;
            }
        }

        public void writeByte(ushort addr, byte b) {
            switch (addr) {                                              // General Memory Map 64KB
                case ushort r when addr >= 0x0000 && addr <= 0x7FFF:     //0000-3FFF 16KB ROM Bank 00 (in cartridge, private at bank 00) 4000-7FFF 16KB ROM Bank 01..NN(in cartridge, switchable bank number)
                    gamePak.WriteROM(addr, b);
                    break;
                case ushort r when addr >= 0x8000 && addr <= 0x9FFF:    // 8000-9FFF 8KB Video RAM(VRAM)(switchable bank 0-1 in CGB Mode)
                    VRAM[addr - 0x8000] = b;
                    break;
                case ushort r when addr >= 0xA000 && addr <= 0xBFFF:    // A000-BFFF 8KB External RAM(in cartridge, switchable bank, if any) <br/>
                    gamePak.WriteERAM((ushort)(addr - 0xA000), b);
                    break;
                case ushort r when addr >= 0xC000 && addr <= 0xCFFF:    // C000-CFFF 4KB Work RAM Bank 0(WRAM) <br/>
                case ushort s when addr >= 0xE000 && addr <= 0xEFFF:    // E000-FDFF Same as 0xC000-DDFF(ECHO)  
                    WRAM0[addr & 0xFFF] = b;
                    break;
                case ushort r when addr >= 0xD000 && addr <= 0xDFFF:    // D000-DFFF 4KB Work RAM Bank 1(WRAM)(switchable bank 1-7 in CGB Mode) <br/>
                case ushort s when addr >= 0xF000 && addr <= 0xFDFF:    // E000-FDFF Same as 0xC000-DDFF(ECHO)
                    WRAM1[addr & 0xFFF] = b;
                    break;
                case ushort r when addr >= 0xFE00 && addr <= 0xFE9F:    // FE00-FE9F Sprite Attribute Table(OAM)
                    OAM[addr - 0xFE00] = b;
                    break;
                case ushort r when addr >= 0xFEA0 && addr <= 0xFEFF:    // FEA0-FEFF Not Usable
                    //Console.WriteLine("Warning: Tried to write to NOT USABLE space");
                    break;
                case ushort r when addr >= 0xFF00 && addr <= 0xFF7F:    // FF00-FF7F IO Ports
                    switch (addr) {
                        case 0xFF0F: b |= 0xE0; break; // IF returns 1 on first 3 unused bits
                        case 0xFF04:                //DIV on write = 0
                        case 0xFF44: b = 0; break;  //LY on write = 0
                        case 0xFF46: DMA(b); break;
                        //case 0xFF00: b |= 0xC0; break;
                    }
                    //if (addr == 0xFF02 && b == 0x81) { //Temp Serial Link output for debug
                       //Console.Write(Convert.ToChar(readByte(0xFF01)));
                       //Console.ReadLine();
                    //}
                    IO[addr - 0xFF00] = b;
                    break;
                case ushort r when addr >= 0xFF80 && addr <= 0xFFFE:    // FF80-FFFE High RAM(HRAM)
                    HRAM[addr - 0xFF80] = b;
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

        public byte bitSet(byte n, byte v) {
            return v |= (byte)(1 << n);
        }

        public byte bitClear(byte n, byte v) {
            return v &= (byte)~(1 << n);
        }

        public bool isBit(byte n, byte v) {
            return ((v >> n) & 1) == 1;
        }

        public void requestInterrupt(byte b) {
            IF = bitSet(b, IF);
        }

        private void DMA(byte b) {
            ushort addr = (ushort)(b << 8);
            for (byte i = 0; i < OAM.Length; i++) {
                OAM[i] = readByte((ushort)(addr + i));
            }
        }

        public void loadGamePak(String cartName) {
            byte[] rom = File.ReadAllBytes(cartName);
            switch (rom[0x147]) {
                case 0:
                    gamePak = new MBC0();
                    break;
                case 0x01:
                case 0x02:
                case 0x03:
                    gamePak = new MBC1();
                    break;
                case 0x05:
                case 0x06:
                    gamePak = new MBC2();
                    break;
                case 0x0F:
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                    gamePak = new MBC3();
                    break;
                case 0x19:
                case 0x1A:
                case 0x1B:
                    gamePak = new MBC5();
                    break;
                default:
                    Console.WriteLine("Unsupported MBC: " + rom[0x147].ToString("x2"));
                    break;
            }
            gamePak.Init(rom);
        }

        public void loadBootRom() {
            byte[] rom = File.ReadAllBytes("DMG_ROM.bin");
            Array.Copy(rom, 0, BOOT_ROM, 0, rom.Length);
        }

    }
}



