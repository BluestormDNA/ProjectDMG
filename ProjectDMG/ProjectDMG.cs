using ProjectDMG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectDMG {
    public class ProjectDMG {

        PictureBox pictureBox;
        CPU cpu;
        MMU mmu;
        PPU ppu;
        TIMER timer;

        public ProjectDMG(PictureBox pictureBox) {

            this.pictureBox = pictureBox;
            cpu = new CPU();
            mmu = new MMU();
            ppu = new PPU(pictureBox);
            timer = new TIMER();

            mmu.loadGamePak();
            mmu.loadBootRom();

            Thread cpuThread = new Thread(new ThreadStart(Exe));
            cpuThread.IsBackground = true;
            cpuThread.Start();
        }

        public void Exe() {
            DateTime start = DateTime.Now;
            DateTime elapsed = DateTime.Now;
            int cpuCycles = 0;
            int cyclesThisUpdate = 0;
            while (true) {

                while ((elapsed - start).TotalMilliseconds >= Constants.MILLIS_PER_FRAME) {

                    while (cyclesThisUpdate < Constants.CYCLES_PER_UPDATE) {
                        cpuCycles = cpu.Exe(mmu);
                        cyclesThisUpdate += cpuCycles;

                        timer.update(cpuCycles, mmu);
                        ppu.update(cpuCycles, mmu);
                        handleInterrupts(mmu, cpu);
                    }

                    ppu.RenderFrame(mmu, pictureBox);
                    cyclesThisUpdate -= Constants.CYCLES_PER_UPDATE;
                    start = DateTime.Now;
                }

                elapsed = DateTime.Now;
            }
        }

        private void handleInterrupts(MMU mmu, CPU cpu) {
            if (mmu.IF != 0) {
                for(byte i = 0; i < 5; i++) {
                    if(mmu.isBit(i, mmu.IE) && mmu.isBit(i, mmu.IF)) {
                        //Console.WriteLine("Exe Interrupt " +i);
                        //Console.ReadLine();
                        cpu.ExecuteInterrupt(mmu, i);
                        mmu.bitClear(i, mmu.IF);
                    }
                }
            }
        }
    }


}
