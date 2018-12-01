using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProjectDMG {
    public class PPU {

        private const int SCREEN_WIDTH = 160;
        private const int SCREEN_HEIGHT = 144;
        private const int SCREEN_VBLANK_HEIGHT = 153;
        private const int OAM_CYCLES = 80;
        private const int VRAM_CYCLES = 172;
        private const int HBLANK_CYCLES = 204;
        private const int SCANLINE_CYCLES = 456;
        private const int H_PIXELS = 160;

        private const int VBLANK_INTERRUPT = 0;
        private const int LCD_INTERRUPT = 1;

        private DirectBitmap bmp;
        private PictureBox pictureBox;
        private int scanlineCounter;

        public PPU(PictureBox pictureBox) {
            this.pictureBox = pictureBox;
            bmp = new DirectBitmap(SCREEN_WIDTH, SCREEN_HEIGHT);
            pictureBox.Image = bmp.Bitmap;
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
                                //we should draw frame here
                            } else { //not arrived yet so return to 2
                                changeSTATMode(2, mmu);
                            }
                        }
                        break;
                    case 1: //VBLANK - Mode 1 (4560 cycles - 10 lines)
                        if (scanlineCounter >= SCANLINE_CYCLES) {
                            mmu.LY++;
                            scanlineCounter -= SCANLINE_CYCLES;

                            //Console.WriteLine("Update PPU INSIDE VBLANK");

                            if (mmu.LY > SCREEN_VBLANK_HEIGHT) { //check end of VBLANK
                                changeSTATMode(2, mmu);
                                mmu.LY = 0;
                            }
                        }
                        break;
                }

                //handle coincidence Flag //TODO REWRITE?
                if (mmu.LY == mmu.LYC) {
                    mmu.STAT = mmu.bitSet(2, mmu.STAT);
                    if (mmu.isBit(6, mmu.STAT)) {
                        mmu.requestInterrupt(LCD_INTERRUPT);
                    }
                } else {
                    mmu.STAT = mmu.bitClear(2, mmu.STAT);
                }

            } else { //LCD Disabled
                //Console.WriteLine("LCD DISABLED");
                scanlineCounter = 0;
                mmu.LY = 0;
                mmu.STAT = (byte)(mmu.STAT & ~0b11111100);
                mmu.STAT = (byte)(mmu.STAT | 0x2); //Forces Mode 2 OAM Start
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
            if (mmu.isBit(0, mmu.LCDC)) { //Bit 0 - BG Display  (0=Off, 1=On)
                renderTiles(mmu);
            }
            if (mmu.isBit(1, mmu.LCDC)) { //Bit 1 - OBJ (Sprite) Display Enable
                renderSprites(mmu);
            }
        }

        private void renderTiles(MMU mmu) {
            //TODO WINDOW
            int y = mmu.SCY + mmu.LY;
            ushort tileRow = (ushort)(y / 8 * 32);

            for (int p = 0; p < H_PIXELS; p++) {
                int x = p + mmu.SCX;

                ushort tileCol = (ushort)(x / 8);
                ushort tileAdress = (ushort)(getTileMapAdress(mmu) + ((tileRow + tileCol) & 0x3FF));

                ushort tileLoc;
                if (isSignedAdress(mmu)) {
                    tileLoc = (ushort)(getTileDataAdress(mmu) + mmu.readByte(tileAdress) * 16);
                } else {
                    tileLoc = (ushort)(getTileDataAdress(mmu) + ((sbyte)mmu.readByte(tileAdress) + 128) * 16);
                }

                byte tileLine = (byte)(y % 8 * 2);

                byte b1 = mmu.readByte((ushort)(tileLoc + tileLine));
                byte b2 = mmu.readByte((ushort)(tileLoc + tileLine + 1));

                byte colorBit = (byte)(7 - x % 8); //inversed

                byte colorId = GetColorIdBits(colorBit, b1, b2);
                byte colorIdThroughtPalette = GetColorIdThroughtPalette(mmu, colorId);
                Color color = GetColor(colorIdThroughtPalette);

                bmp.SetPixel(p, mmu.LY, color);
            }

        }

        private Color GetColor(byte colorIdThroughtPalette) {
            switch (colorIdThroughtPalette) {
                case 0b00:
                    return Color.White;
                case 0b01:
                    return Color.Gray;
                case 0b10:
                    return Color.DarkGray;
                case 0b11:
                    return Color.Black;
                default: //Just in case something is wrong
                    return Color.Red;
            }
        }

        private byte GetColorIdBits(byte colorBit, byte b1, byte b2) {
            int hi = (b2 >> colorBit) & 0x1;
            int lo = (b1 >> colorBit) & 0x1;
            return (byte)(hi << 1 | lo);
        }

        public byte GetColorIdThroughtPalette(MMU mmu, byte colorId) {
            switch (colorId) {
                case 0b00:
                    return (byte)(mmu.BGP & 0b00000011);
                case 0b01:
                    return (byte)((mmu.BGP & 0b00001100) >> 2);
                case 0b10:
                    return (byte)((mmu.BGP & 0b00110000) >> 4);
                case 0b11:
                    return (byte)((mmu.BGP & 0b11000000) >> 6);
                default: //Just in case something is wrong
                    return 0xFF;
            }
        }

        private bool isSignedAdress(MMU mmu) {
            //Bit 4 - BG & Window Tile Data Select   (0=8800-97FF, 1=8000-8FFF)
            return mmu.isBit(4, mmu.LCDC);
        }

        private ushort getTileMapAdress(MMU mmu) {
            //Bit 3 - BG Tile Map Display Select     (0=9800-9BFF, 1=9C00-9FFF)
            if (mmu.isBit(3, mmu.LCDC)) {
                return 0x9C00;
            } else {
                return 0x9800;
            }
        }

        private ushort getTileDataAdress(MMU mmu) {
            //Bit 4 - BG & Window Tile Data Select   (0=8800-97FF, 1=8000-8FFF)
            if (mmu.isBit(4, mmu.LCDC)) {
                return 0x8000;
            } else {
                //unsig
                return 0x8800;
            }
        }

        private void renderSprites(MMU mmu) {
            //40 sprites
            for (int i = 0; i < 0x9F; i += 4) { //0x9F OAM Size, 4 bytes per Sprite:
                //Byte0 - Y Position
                byte y = (byte)(mmu.readByte((ushort)(0xFF00 + i)) - 16); //needs 16 offset
                //Byte1 - X Position
                byte x = (byte)(mmu.readByte((ushort)(0xFF00 + i + 1)) - 8); //needs 8 offset
                //Byte2 - Tile/Pattern Number
                byte tile = mmu.readByte((ushort)(0xFF00 + i + 2));
                //Byte3 - Attributes/Flags:
                byte attr = mmu.readByte((ushort)(0xFF00 + i + 3));

                if ((mmu.LY >= y) && (mmu.LY < (y + 8))) {
                    byte palette = mmu.isBit(4, attr) ? mmu.OBP1 : mmu.OBP0; //Bit4   Palette number  **Non CGB Mode Only** (0=OBP0, 1=OBP1)

                    byte tileRow;
                    if(isYFlipped(attr, mmu)) {
                        tileRow = (byte)(7 - mmu.LY - y);
                    } else {
                        tileRow = (byte)(mmu.LY - y);
                    }

                    for (int p = 0; p < 8; p++) {
                        //
                    }
                }
            }
        }

        public void RenderFrame(MMU mmu, PictureBox pictureBox) {
            if (pictureBox.InvokeRequired) {
                pictureBox.Invoke(new MethodInvoker(
                delegate () {
                    pictureBox.Refresh();
                }));
            } else {
                pictureBox.Refresh();
            }
        }

        private bool isLCDEnabled(MMU mmu) {
            //Bit 7 - LCD Display Enable
            return mmu.isBit(7, mmu.LCDC);
        }

        private int spriteSize(MMU mmu) {
            //Bit 2 - OBJ (Sprite) Size (0=8x8, 1=8x16)
            return mmu.isBit(2, mmu.LCDC) ? 16 : 8;
        }

        private bool isXFlipped(byte attr, MMU mmu) {
            //Bit5   X flip(0 = Normal, 1 = Horizontally mirrored)
            return mmu.isBit(5, attr);
        }

        private bool isYFlipped(byte attr, MMU mmu) {
            //Bit6 Y flip(0 = Normal, 1 = Vertically mirrored)
            return mmu.isBit(6, attr);
        }
    }
}