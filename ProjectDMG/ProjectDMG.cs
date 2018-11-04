using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectDMG {
    public class ProjectDMG {

        public ProjectDMG(PictureBox pictureBox) {

            CPU cpu = new CPU();
            MMU mmu = new MMU();
            PPU ppu = new PPU();

            mmu.loadBootRom();

            while (true) {
                cpu.Exe(mmu);
            }
        }
    }
}
