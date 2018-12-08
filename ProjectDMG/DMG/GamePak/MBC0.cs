namespace ProjectDMG.DMG.GamePak {
    class MBC0 : IGamePak {

        byte[] ROM;

        public void Init(byte[] ROM) {
            this.ROM = ROM;
        }

        public byte ReadByte(ushort addr) {
            return ROM[addr];
        }

        public void WriteByte(ushort addr, byte value) {
            //ROM[addr] = value; //MBC0 should ignore writes
        }
    }
}
