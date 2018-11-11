using System;
using System.Windows.Forms;

namespace ProjectDMG {
    public class PPU {

        private const int SCREEN_WIDTH = 160;
        private const int SCREEN_HEIGHT = 144;
        private const int SCANLINE_CYCLES = 456;
        private const int VBLANK_INTERRUPT = 0; //Bit 0: V-Blank Interrupt Request(INT 40h)  (1=Request)

        private DirectBitmap bmp;
        private PictureBox pictureBox;
        private int scanlineCounter;

        public PPU(PictureBox pictureBox) {
            this.pictureBox = pictureBox;
            bmp = new DirectBitmap(SCREEN_WIDTH, SCREEN_HEIGHT);
            pictureBox.Image = bmp.Bitmap;
        }

        public void update(int cycles, MMU mmu) {
            if (isLCDEnabled(mmu)) {
                scanlineCounter += cycles;
                if(scanlineCounter >= SCANLINE_CYCLES) {
                    mmu.LY++;
                    scanlineCounter -= SCANLINE_CYCLES;

                    if(mmu.LY == SCREEN_HEIGHT) { //should this be 145? as 144+1 between 145 and 153???
                        mmu.requestInterrupt(VBLANK_INTERRUPT);
                    } else if (mmu.LY > SCREEN_HEIGHT) {
                        mmu.LY = 0;
                    } else if (mmu.LY < SCREEN_HEIGHT) {
                        drawScanLine(mmu);
                    }

                }
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