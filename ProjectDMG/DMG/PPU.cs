using System;
using System.Runtime.CompilerServices;

namespace ProjectDMG {
    public class PPU {

        private const int SCREEN_WIDTH = 160;
        private const int SCREEN_HEIGHT = 144;
        private const int SCREEN_VBLANK_HEIGHT = 153;
        private const int OAM_CYCLES = 80;
        private const int VRAM_CYCLES = 172;
        private const int HBLANK_CYCLES = 204;
        private const int SCANLINE_CYCLES = 456;

        private const int VBLANK_INTERRUPT = 0;
        private const int LCD_INTERRUPT = 1;

        private int[] color = new int[]{0x00FFFFFF, 0x00808080, 0x00404040, 0};

        public DirectBitmap bmp;
        private int scanlineCounter;

        private Form window;

        public PPU(Form window)
        {
            this.window = window;
            bmp = new DirectBitmap();
            window.pictureBox.Image = bmp.Bitmap;
        }

        public void update(int cycles, MMU mmu) {
            scanlineCounter += cycles;
            byte currentMode = (byte)(mmu.STAT & 0b00000011); //Current Mode Mask

            if (isLCDEnabled(mmu)) {
                switch (currentMode) {
                    case 2: //Accessing OAM - Mode 2 (80 cycles)
                        if (scanlineCounter >= OAM_CYCLES) {
                            changeSTATMode(3, mmu);
                            scanlineCounter -= OAM_CYCLES;
                        }
                        break;
                    case 3: //Accessing VRAM - Mode 3 (172 cycles) Total M2+M3 = 252 Cycles
                        if (scanlineCounter >= VRAM_CYCLES) {
                            changeSTATMode(0, mmu);
                            drawScanLine(mmu);
                            scanlineCounter -= VRAM_CYCLES;
                        }
                        break;
                    case 0: //HBLANK - Mode 0 (204 cycles) Total M2+M3+M0 = 456 Cycles
                        if (scanlineCounter >= HBLANK_CYCLES) {
                            mmu.LY++;
                            scanlineCounter -= HBLANK_CYCLES;

                            if (mmu.LY == SCREEN_HEIGHT) { //check if we arrived Vblank
                                changeSTATMode(1, mmu);
                                mmu.requestInterrupt(VBLANK_INTERRUPT);
                                RenderFrame();
                            } else { //not arrived yet so return to 2
                                changeSTATMode(2, mmu);
                            }
                        }
                        break;
                    case 1: //VBLANK - Mode 1 (4560 cycles - 10 lines)
                        if (scanlineCounter >= SCANLINE_CYCLES) {
                            mmu.LY++;
                            scanlineCounter -= SCANLINE_CYCLES;

                            if (mmu.LY > SCREEN_VBLANK_HEIGHT) { //check end of VBLANK
                                changeSTATMode(2, mmu);
                                mmu.LY = 0;
                            }
                        }
                        break;
                }

                if (mmu.LY == mmu.LYC) { //handle coincidence Flag
                    mmu.STAT = mmu.bitSet(2, mmu.STAT);
                    if (mmu.isBit(6, mmu.STAT)) {
                        mmu.requestInterrupt(LCD_INTERRUPT);
                    }
                } else {
                    mmu.STAT = mmu.bitClear(2, mmu.STAT);
                }

            } else { //LCD Disabled
                scanlineCounter = 0;
                mmu.LY = 0;
                mmu.STAT = (byte)(mmu.STAT & 0b11111100);
                //mmu.STAT = (byte)(mmu.STAT | 0x2); //Forces Mode 2 OAM Start
            }
        }

        private void changeSTATMode(int mode, MMU mmu) {
            switch (mode) {
                case 2: //Accessing OAM - Mode 2 (80 cycles)
                    mmu.STAT = mmu.bitSet(1, mmu.STAT);
                    mmu.STAT = mmu.bitClear(0, mmu.STAT);
                    if (mmu.isBit(5, mmu.STAT)) { // Bit 5 - Mode 2 OAM Interrupt         (1=Enable) (Read/Write)
                        mmu.requestInterrupt(LCD_INTERRUPT);
                    }
                    break;
                case 3: //Accessing VRAM - Mode 3 (172 cycles) Total M2+M3 = 252 Cycles
                    mmu.STAT = mmu.bitSet(1, mmu.STAT);
                    mmu.STAT = mmu.bitSet(0, mmu.STAT);
                    break;
                case 0: //HBLANK - Mode 0 (204 cycles) Total M2+M3+M0 = 456 Cycles
                    mmu.STAT = mmu.bitClear(1, mmu.STAT);
                    mmu.STAT = mmu.bitClear(0, mmu.STAT);
                    if (mmu.isBit(3, mmu.STAT)) { // Bit 3 - Mode 0 H-Blank Interrupt     (1=Enable) (Read/Write)
                        mmu.requestInterrupt(LCD_INTERRUPT);
                    }
                    break;
                case 1: //VBLANK - Mode 1 (4560 cycles - 10 lines)
                    mmu.STAT = mmu.bitClear(1, mmu.STAT);
                    mmu.STAT = mmu.bitSet(0, mmu.STAT);
                    if (mmu.isBit(4, mmu.STAT)) { // Bit 4 - Mode 1 V-Blank Interrupt     (1=Enable) (Read/Write)
                        mmu.requestInterrupt(LCD_INTERRUPT);
                    }
                    break;
            }
        }

        private void drawScanLine(MMU mmu) {
            if (mmu.isBit(0, mmu.LCDC)) { //Bit 0 - BG Display (0=Off, 1=On)
                renderBG(mmu);
            }
            if (mmu.isBit(1, mmu.LCDC)) { //Bit 1 - OBJ (Sprite) Display Enable
                renderSprites(mmu);
            }
        }

        private void renderBG(MMU mmu) {
            byte WX = (byte)(mmu.WX - 7); //WX needs -7 Offset
            byte WY = mmu.WY;
            byte LY = mmu.LY;
            byte LCDC = mmu.LCDC;
            byte SCY = mmu.SCY;
            byte SCX = mmu.SCX;
            bool isWin = isWindow(mmu, LCDC, WY, LY);

            byte y = isWin ? (byte)(LY - WY) : (byte)(SCY + LY);
            byte tileLine = (byte)((y & 7) * 2);

            ushort tileRow = (ushort)(y / 8 * 32);
            ushort tileMap = isWin ? getWindowTileMapAdress(mmu, LCDC) : getBGTileMapAdress(mmu, LCDC);

            byte hi = 0;
            byte lo = 0;

            for (int p = 0; p < SCREEN_WIDTH; p++) {
                byte x = isWin && p >= WX ? (byte)(p - WX) : (byte)(p + SCX);
                if((p & 0x7 )== 0 || ((p + SCX) & 0x7) == 0) {
                    ushort tileCol = (ushort)(x / 8);
                    ushort tileAdress = (ushort)(tileMap + tileRow + tileCol);

                    ushort tileLoc;
                    if (isSignedAdress(mmu, LCDC)) {
                        tileLoc = (ushort)(getTileDataAdress(mmu, LCDC) + mmu.readByte(tileAdress) * 16);
                    } else {
                        tileLoc = (ushort)(getTileDataAdress(mmu, LCDC) + ((sbyte)mmu.readByte(tileAdress) + 128) * 16);
                    }

                    lo = mmu.readByte((ushort)(tileLoc + tileLine));
                    hi = mmu.readByte((ushort)(tileLoc + tileLine + 1));
                }

                int colorBit = 7 - (x & 7); //inversed
                int colorId = GetColorIdBits(colorBit, lo, hi);
                int colorIdThroughtPalette = GetColorIdThroughtPalette(mmu.BGP, colorId);

                bmp.SetPixel(p, LY, color[colorIdThroughtPalette]);
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetColorIdBits(int colorBit, byte l, byte h) {
            int hi = (h >> colorBit) & 0x1;
            int lo = (l >> colorBit) & 0x1;
            return (hi << 1 | lo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetColorIdThroughtPalette(int palette, int colorId) {
            return (palette >> colorId * 2) & 0x3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isSignedAdress(MMU mmu, byte LCDC) {
            //Bit 4 - BG & Window Tile Data Select   (0=8800-97FF, 1=8000-8FFF)
            return mmu.isBit(4, LCDC);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort getBGTileMapAdress(MMU mmu, byte LCDC) {
            //Bit 3 - BG Tile Map Display Select     (0=9800-9BFF, 1=9C00-9FFF)
            if (mmu.isBit(3, LCDC)) {
                return 0x9C00;
            } else {
                return 0x9800;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort getWindowTileMapAdress(MMU mmu, byte LCDC) {
            //Bit 6 - Window Tile Map Display Select(0 = 9800 - 9BFF, 1 = 9C00 - 9FFF)
            if (mmu.isBit(6, LCDC)) {
                return 0x9C00;
            } else {
                return 0x9800;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort getTileDataAdress(MMU mmu, byte LCDC) {
            //Bit 4 - BG & Window Tile Data Select   (0=8800-97FF, 1=8000-8FFF)
            if (mmu.isBit(4, LCDC)) {
                return 0x8000;
            } else {
                return 0x8800; //Signed Area
            }
        }

        private void renderSprites(MMU mmu) {
            byte LY = mmu.LY;
            byte LCDC = mmu.LCDC;
            for (int i = 0x9C; i >= 0; i -= 4) { //0x9F OAM Size, 40 Sprites x 4 bytes:
                //Byte0 - Y Position
                int y = mmu.readByte((ushort)(0xFE00 + i)) - 16; //needs 16 offset
                //Byte1 - X Position
                int x = mmu.readByte((ushort)(0xFE00 + i + 1)) - 8; //needs 8 offset
                //Byte2 - Tile/Pattern Number
                byte tile = mmu.readByte((ushort)(0xFE00 + i + 2));
                //Byte3 - Attributes/Flags:
                byte attr = mmu.readByte((ushort)(0xFE00 + i + 3));

                if ((LY >= y) && (LY < (y + spriteSize(mmu, LCDC)))) {
                    byte palette = mmu.isBit(4, attr) ? mmu.OBP1 : mmu.OBP0; //Bit4   Palette number  **Non CGB Mode Only** (0=OBP0, 1=OBP1)

                    byte tileRow = isYFlipped(attr, mmu) ? (byte)(spriteSize(mmu, LCDC) - 1 - (LY - y)) : (byte)(LY - y);

                    ushort tileddress = (ushort)(0x8000 + (tile * 16) + (tileRow * 2));
                    byte lo = mmu.readByte(tileddress);
                    byte hi = mmu.readByte((ushort)(tileddress + 1));

                    for (int p = 0; p < 8; p++) {
                        int IdPos = isXFlipped(attr, mmu) ? p : 7 - p;
                        int colorId = GetColorIdBits(IdPos, lo, hi);
                        byte colorIdThroughtPalette = (byte)GetColorIdThroughtPalette(palette, colorId);

                        if ((x + p) >= 0 && (x + p) < SCREEN_WIDTH) {
                            if (!isTransparent(colorId) && (isAboveBG(attr) || isBGWhite(mmu.BGP, x + p, LY))) {
                                bmp.SetPixel(x + p, LY, color[colorIdThroughtPalette]);
                            }

                        }

                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isBGWhite(byte BGP, int x, int y) {
            int id = BGP & 0x3;
            return bmp.GetPixel(x, y) == color[id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isAboveBG(byte attr) {
            //Bit7 OBJ-to - BG Priority(0 = OBJ Above BG, 1 = OBJ Behind BG color 1 - 3)
            return attr >> 7 == 0;
        }

        public void RenderFrame() {
            //window.pictureBox.Refresh();
            window.pictureBox.Invalidate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isLCDEnabled(MMU mmu) {
            //Bit 7 - LCD Display Enable
            return mmu.isBit(7, mmu.LCDC);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int spriteSize(MMU mmu, byte LCDC) {
            //Bit 2 - OBJ (Sprite) Size (0=8x8, 1=8x16)
            return mmu.isBit(2, LCDC) ? 16 : 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isXFlipped(int attr, MMU mmu) {
            //Bit5   X flip(0 = Normal, 1 = Horizontally mirrored)
            return mmu.isBit(5, attr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isYFlipped(byte attr, MMU mmu) {
            //Bit6 Y flip(0 = Normal, 1 = Vertically mirrored)
            return mmu.isBit(6, attr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isTransparent(int b) {
            return b == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isWindow(MMU mmu, byte LCDC, byte WY, byte LY) {
            //Bit 5 - Window Display Enable (0=Off, 1=On)
            return mmu.isBit(5, LCDC) && WY <= LY;
        }
    }
}