namespace ProjectDMG.DMG.GamePak {
    interface IGamePak {
        byte ReadByte(ushort addr);
        void WriteByte(ushort addr, byte value);
        void Init(byte[] rom);
    }
}
