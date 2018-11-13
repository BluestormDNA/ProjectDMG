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
        private const int VBLANK_INTERRUPT = 0; //Bit 0: V-Blank Interrupt Request(INT 40h)  (1=Request)
        private const int LCDSTAT_INTERRUPT = 1; //Bit 1: LCD STAT Interrupt Request (INT 48h)  (1=Request)

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
            byte mode = 0;
            bool interrupt = false;

            if (isLCDEnabled(mmu)) {
                //Accessing OAM - Mode 2 (80 cycles)
                if (scanlineCounter <= OAM_CYCLES) {


                //Accessing VRAM - Mode 3 (172 cycles) Total M2+M3 = 252 Cycles
                } else if (scanlineCounter <= OAM_CYCLES + VRAM_CYCLES) {

                
                //HBLANK - Mode 0 (204 cycles) Total M2+M3+M0 = 456 Cycles
                } else if (scanlineCounter <= OAM_CYCLES + VRAM_CYCLES + HBLANK_CYCLES) {


                //END OF HBLANK AND VBLANK - Mode 1 (4560 cycles) 10 lines
                } else if (scanlineCounter >= SCANLINE_CYCLES) {
                    //Handle Hblank End
                    mmu.LY++;
                    scanlineCounter -= SCANLINE_CYCLES;

                    //handle VBlank
                    //Set Mode 1


                    if (mmu.LY == SCREEN_HEIGHT) {
                        mmu.requestInterrupt(VBLANK_INTERRUPT);
                    } else if (mmu.LY > SCREEN_VBLANK_HEIGHT) {
                        mmu.LY = 0;
                    } else if (mmu.LY < SCREEN_HEIGHT) {
                        drawScanLine(mmu);
                    }

                }
            } else { //LCD Disabled
                scanlineCounter = 0;
                mmu.LY = 0;
                mmu.STAT = (byte)(mmu.STAT & ~0b11111100);
            }
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