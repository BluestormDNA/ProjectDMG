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
            //Console.WriteLine("Update PPU STAT:" + mmu.STAT.ToString("x2") + " " + currentMode) ;

            if (isLCDEnabled(mmu)) {
                switch (currentMode) {
                    case 2: //Accessing OAM - Mode 2 (80 cycles)
                        if (scanlineCounter >= OAM_CYCLES) {
                            changeSTATMode(3, mmu);
                            scanlineCounter = 0;
                            //Console.WriteLine("Update PPU INSIDE OAM");
                        }
                        break;
                    case 3: //Accessing VRAM - Mode 3 (172 cycles) Total M2+M3 = 252 Cycles
                        if (scanlineCounter >= VRAM_CYCLES) {
                            changeSTATMode(0, mmu);
                            drawScanLine(mmu);
                            scanlineCounter = 0;
                            // Console.WriteLine("Update PPU INSIDE VRAM");
                        }
                        break;
                    case 0: //HBLANK - Mode 0 (204 cycles) Total M2+M3+M0 = 456 Cycles
                        if (scanlineCounter >= HBLANK_CYCLES) {
                            //Console.WriteLine("Update PPU INSIDE HBLANK" + mmu.LY);
                            //mmu.debugIO();

                            mmu.LY++;
                            scanlineCounter = 0;

                            if (mmu.LY == SCREEN_HEIGHT) { //check if we arrived Vblank
                                changeSTATMode(1, mmu);
                                //we should draw frame here
                            } else { //not arrived yet so return to 2
                                changeSTATMode(2, mmu);
                            }
                        }
                        break;
                    case 1: //VBLANK - Mode 1 (4560 cycles - 10 lines)
                        if (scanlineCounter >= SCANLINE_CYCLES) {
                            mmu.LY++;
                            scanlineCounter = 0;

                            //Console.WriteLine("Update PPU INSIDE VBLANK");

                            if (mmu.LY > SCREEN_VBLANK_HEIGHT) { //check end of VBLANK
                                changeSTATMode(2, mmu);
                                mmu.LY = 0;
                            }
                        }
                        break;
                }

            } else { //LCD Disabled
                scanlineCounter = 0;
                mmu.LY = 0;
                mmu.STAT = (byte)(mmu.STAT & ~0b11111100);
            }
        }

        private void changeSTATMode(int mode, MMU mmu) {
            switch (mode) {
                case 2: //Accessing OAM - Mode 2 (80 cycles)
                    mmu.STAT = mmu.bitSet(1, mmu.STAT);
                    mmu.STAT = mmu.bitClear(0, mmu.STAT);
                    if (mmu.isBit(5, mmu.STAT)) { // Bit 5 - Mode 2 OAM Interrupt         (1=Enable) (Read/Write)
                        requestPPUInterrupt(5);
                    }
                    //Console.WriteLine("Inside Changestat 2:" + mmu.STAT.ToString("x2"));
                    break;
                case 3: //Accessing VRAM - Mode 3 (172 cycles) Total M2+M3 = 252 Cycles
                    mmu.STAT = mmu.bitSet(1, mmu.STAT);
                    mmu.STAT = mmu.bitSet(0, mmu.STAT);
                    break;
                case 0: //HBLANK - Mode 0 (204 cycles) Total M2+M3+M0 = 456 Cycles
                    mmu.STAT = mmu.bitClear(1, mmu.STAT);
                    mmu.STAT = mmu.bitClear(0, mmu.STAT);
                    if (mmu.isBit(3, mmu.STAT)) { // Bit 3 - Mode 0 H-Blank Interrupt     (1=Enable) (Read/Write)
                        requestPPUInterrupt(3);
                    }
                    break;
                case 1: //VBLANK - Mode 1 (4560 cycles - 10 lines)
                    mmu.STAT = mmu.bitClear(1, mmu.STAT);
                    mmu.STAT = mmu.bitSet(0, mmu.STAT);
                    if (mmu.isBit(4, mmu.STAT)) { // Bit 4 - Mode 1 V-Blank Interrupt     (1=Enable) (Read/Write)
                        requestPPUInterrupt(4);
                    }
                    break;
            }
        }

        private void requestPPUInterrupt(int v) {
            //throw new NotImplementedException();
        }

        private void drawScanLine(MMU mmu) {
            if (mmu.isBit(0, mmu.LCDC)) {
                renderTiles(mmu);
            }
            if (mmu.isBit(1, mmu.LCDC)) {
                renderSprites(mmu);
            }
        }

        private void renderTiles(MMU mmu) {
            //TODO WINDOW
            int y = mmu.SCY + mmu.LY;
            byte tileRow = (byte)(y / 8 * 32);

            for (int p = 0; p < H_PIXELS; p++) {
                int x = p + mmu.SCX;

                byte tileCol = (byte)(x / 8);
                ushort tileAdress = (ushort)(getTileMapAdress(mmu) + tileRow + tileCol);

                Console.WriteLine("TileRow: " + tileRow.ToString("x2"));
                Console.WriteLine("TileCol: " + tileCol.ToString("x2"));
                Console.WriteLine("TileAdress: " + tileAdress.ToString("x4"));
                Console.WriteLine("TileMapAdress: " + (getTileMapAdress(mmu) + tileRow + tileCol).ToString("x4"));
                Console.WriteLine("TileDataAdress: " + (getTileDataAdress(mmu) + mmu.readByte(tileAdress) * 16).ToString("x4"));
                Console.WriteLine("8010 " + mmu.readByte(0x8010).ToString("x2"));
                Console.WriteLine(mmu.readByte(tileAdress));


                ushort tileLoc;
                if (isSignedAdress(mmu)) {
                    tileLoc = 0x8190;//(ushort)(getTileDataAdress(mmu) + mmu.readByte(tileAdress) * 16);
                } else {
                    tileLoc = (ushort)(getTileDataAdress(mmu) + ((sbyte)mmu.readByte(tileAdress) + 128) * 16);
                }

                byte tileLine = (byte)(y % 8 * 2);

                byte b1 = mmu.readByte((ushort)(tileLoc + tileLine));
                byte b2 = mmu.readByte((ushort)(tileLoc + tileLine + 1));

                byte colorBit = (byte)((x % 8 - 7) * -1);

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
                default:
                    return Color.Red;
            }
        }

        private byte GetColorIdBits(byte colorBit, byte b1, byte b2) {
            int hi = (b2 >> colorBit) & 0x1;
            int lo = (b1 >> colorBit) & 0x1;
            return (byte)(hi << 1 | lo);
        }

        public byte GetColorIdThroughtPalette(MMU mmu, byte colorId) {
            Console.WriteLine("BGP: "+ mmu.BGP.ToString("x2") + "ColorId " + colorId);
            switch (colorId) {
                case 0b00:
                    return (byte)(mmu.BGP & 0b00000011);
                case 0b01:
                    return (byte)((mmu.BGP & 0b00001100) >> 2);
                case 0b10:
                    return (byte)((mmu.BGP & 0b00110000) >> 4);
                case 0b11:
                    return (byte)((mmu.BGP & 0b11000000) >> 6);
                default:
                    return 0xFF; // TODO
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
            throw new NotImplementedException();
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
            return mmu.isBit(7, mmu.LCDC);
        }
    }
}