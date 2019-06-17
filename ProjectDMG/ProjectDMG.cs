using ProjectDMG.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProjectDMG {
    public class ProjectDMG {

        Form window;

        public ProjectDMG(Form window) {
            this.window = window;
        }

        private CPU cpu;
        private MMU mmu;
        private PPU ppu;
        private TIMER timer;
        public JOYPAD joypad;

        public bool power_switch;

        public void POWER_ON(string cartName) {
            mmu = new MMU();
            cpu = new CPU(mmu);
            ppu = new PPU(window);
            timer = new TIMER();
            joypad = new JOYPAD();

            mmu.loadGamePak(cartName);

            power_switch = true;

            Task t = Task.Factory.StartNew(EXECUTE, TaskCreationOptions.LongRunning);
        }

        public void POWER_OFF() {
            power_switch = false;
        }

        int fpsCounter;

        public void EXECUTE() {
            // Main Loop Work in progress
            long start = nanoTime();
            long elapsed = 0;
            int cpuCycles = 0;
            int cyclesThisUpdate = 0;

            var timerCounter = new Stopwatch();
            timerCounter.Start();

            while (power_switch) {


                if (timerCounter.ElapsedMilliseconds > 1000) {
                    window.Text = "ProjectDMG | FPS: " + fpsCounter;
                    timerCounter.Restart();
                    fpsCounter = 0;
                }

                //if ((elapsed - start) >= 16740000) { //nanoseconds per frame
                //    start += 16740000;
                    while (cyclesThisUpdate < Constants.CYCLES_PER_UPDATE) {
                        cpuCycles = cpu.Exe();
                        cyclesThisUpdate += cpuCycles;

                        timer.update(cpuCycles, mmu);
                        ppu.update(cpuCycles, mmu);
                        joypad.update(mmu);
                        handleInterrupts();
                    }

                    window.pictureBox.Invalidate();
                    fpsCounter++;
                    cyclesThisUpdate -= Constants.CYCLES_PER_UPDATE;
                //}

                //elapsed = nanoTime();
                //if ((elapsed - start) < 15000000) {
                //    Thread.Sleep(1);
                //}
            }
        }

        private void handleInterrupts() {
            byte IE = mmu.IE;
            byte IF = mmu.IF;
            for (int i = 0; i < 5; i++) {
                if ((((IE & IF) >> i) & 0x1) == 1) {
                    cpu.ExecuteInterrupt(i);
                }
            }

            cpu.UpdateIME();
        }

        private static long nanoTime() {
            long nano = 10000L * Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }

    }
}
