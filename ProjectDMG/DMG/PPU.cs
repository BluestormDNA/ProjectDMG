using System;
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
            byte currentMode = (byte)(mmu.STAT & 0b00000011);
            //Console.WriteLine("Update PPU STAT:" + mmu.STAT.ToString("x2") + " " + currentMode) ;

            //if (isLCDEnabled(mmu)) {
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
            //} else { //LCD Disabled
            //    scanlineCounter = 0;
            //    mmu.LY = 0;
            //    mmu.STAT = (byte)(mmu.STAT & ~0b11111100);
            //}
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
            //throw new NotImplementedException();
        }

        public void RenderFrame(MMU mmu, PictureBox pictureBox) {
            Console.WriteLine("Rendering Frame");
        }

        private bool isLCDEnabled(MMU mmu) {
            return mmu.isBit(7, mmu.STAT);
        }
    }
}