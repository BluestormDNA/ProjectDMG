using System;
using System.Windows.Forms;

namespace ProjectDMG {
    public class PPU {

        private DirectBitmap bmp;
        private PictureBox pictureBox;

        public PPU(PictureBox pictureBox) {
            this.pictureBox = pictureBox;
            bmp = new DirectBitmap(160, 144);
            pictureBox.Image = bmp.Bitmap;
        }

        public void update(int cycles, MMU mmu) {
           
        }

        public void RenderFrame(MMU mmu, PictureBox pictureBox) {

            Console.WriteLine("Rendering Frame");

        }
    }
}