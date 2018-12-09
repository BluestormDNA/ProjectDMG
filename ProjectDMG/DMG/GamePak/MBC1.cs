using System;

namespace ProjectDMG.DMG.GamePak {
    class MBC1 : IGamePak {

        private byte[] ROM;
        private byte[] ERAM = new byte [0x8000]; //MBC1 MAX ERAM on 4 banks
        private int ROM_BANK = 1; //default as 0 is 0x000 - 0x3FFF fixed
        private const int ROM_OFFSET = 0x4000;
        private int RAM_BANK;

        public void Init(byte[] ROM) {
            this.ROM = ROM;
        }

        public byte ReadERAM(ushort addr) {
            return ERAM[addr - 0xA000];
        }

        public byte ReadROM(ushort addr) {
            switch (addr) {
                case ushort r when addr >= 0x0000 && addr <= 0x3FFF:
                    return ROM[addr];
                case ushort r when addr >= 0x4000 && addr <= 0x7FFF:
                    return ROM[(ROM_OFFSET * ROM_BANK) + (addr - ROM_OFFSET)];
                default:
                    return 0xFF;
            }
        }

        public void WriteERAM(ushort addr, byte value) {
            //throw new System.NotImplementedException();
        }

        public void WriteROM(ushort addr, byte value) {
            switch (addr) {
                case ushort r when addr >= 0x2000 && addr <= 0x3FFF:
                    ROM_BANK = value & 0x1F;
                    break;
            }
        }
    }
}
