using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectDMG {
    public class ProjectDMG {

        CPU cpu;
        MMU mmu;
        PPU ppu;

        public ProjectDMG(PictureBox pictureBox) {

            cpu = new CPU();
            mmu = new MMU();
            ppu = new PPU();

            mmu.loadBootRom();

            Thread cpuThread = new Thread(new ThreadStart(exe));
            cpuThread.IsBackground = true;
            cpuThread.Start();
        }

        public void exe() {
            while (true) {
                cpu.Exe(mmu);
            }
        }


    }

        
}
