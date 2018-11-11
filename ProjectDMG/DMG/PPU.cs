using System;
using System.Windows.Forms;

namespace ProjectDMG {
    public class PPU {

        private const int SCREEN_WIDTH = 160;
        private const int SCREEN_HEIGHT = 144;
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
            if (isLCDEnabled(mmu)) {
                scanlineCounter += cycles;
                if(scanlineCounter >= SCANLINE_CYCLES) {
                    mmu.LY++;
                    scanlineCounter -= SCANLINE_CYCLES;

                    if(mmu.LY == SCREEN_HEIGHT) { //should this be 145? as 144+1 between 145 and 153???

                    }

                }
            }
        }

        public void RenderFrame(MMU mmu, PictureBox pictureBox) {

            Console.WriteLine("Rendering Frame");

        }

        private bool isLCDEnabled(MMU mmu) {
            return mmu.isBit(7, mmu.STAT);
        }
    }
}