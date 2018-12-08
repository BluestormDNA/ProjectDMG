namespace ProjectDMG.DMG.GamePak {
    interface IGamePak {
        byte ReadROM(ushort addr);
        void WriteROM(ushort addr, byte value);
        byte ReadERAM(ushort addr);
        void WriteERAM(ushort addr, byte value);
        void Init(byte[] rom);
    }
}
