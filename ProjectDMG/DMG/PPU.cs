using System;
using System.Windows.Forms;

namespace ProjectDMG {
    public class PPU {

        private DirectBitmap bmp;

        public PPU() {
            bmp = new DirectBitmap(160, 144);
        }

        public void RenderFrame(MMU mmu, PictureBox pictureBox) {

            Console.WriteLine("Updating");

        }

        internal void update(int cycles, MMU mmu) {
           
        }

    }
}