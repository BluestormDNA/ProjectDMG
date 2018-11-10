using System;

namespace ProjectDMG {
    public class TIMER {

        private int divCounter;
        private int timerCounter;

        //private const int IO_INTERRUPT_TIMER = 2;

        private const int DMG_DIV_FREQ = 16384; //16384Hz
        private const int CGB_DIV_FREQ = DMG_DIV_FREQ * 2; //32768Hz
        private const int TAC_FREQ_00_4096 = 4096; //00: CPU Clock / 1024 (DMG, CGB:   4096 Hz, SGB:   ~4194 Hz)
        private const int TAC_FREQ_01_262144 = 262144; //01: CPU Clock / 16   (DMG, CGB: 262144 Hz, SGB: ~268400 Hz)
        private const int TAC_FREQ_10_65536 = 65536; //10: CPU Clock / 64   (DMG, CGB:  65536 Hz, SGB:  ~67110 Hz)
        private const int TAC_FREQ_11_16384 = 16384; //11: CPU Clock / 256  (DMG, CGB:  16384 Hz, SGB:  ~16780 Hz)
        private int CURRENT_TAC_FREQ = TAC_FREQ_00_4096; //default

        public void update(int cycles, MMU mmu) {
            divCounter += cycles;
            timerCounter += cycles;
            handleDivider(mmu);
            handleTimer(mmu);
        }

        private void handleDivider(MMU mmu) {
            if(divCounter >= DMG_DIV_FREQ) { //TODO is this 256? CPU SPEED / DIV RATE?
                mmu.DIV++;
                divCounter -= DMG_DIV_FREQ;
            }
        }

        private void handleTimer(MMU mmu) {
            if (mmu.TAC_ENABLED) {
                if(timerCounter >= CURRENT_TAC_FREQ) {
                    mmu.TIMA++;
                    timerCounter -= CURRENT_TAC_FREQ;
                }
                if(mmu.TIMA == 0xFF) {
                    requestTimerInterrupt(mmu);
                }
            }
        }

        private void requestTimerInterrupt(MMU mmu) {
            mmu.IF = mmu.bitSet(2, mmu.IF);
        }
    }
}