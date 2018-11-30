using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectDMG {
    public class JOYPAD {

        private byte PAD_BYTE_MASK = 0x10;
        private byte BUTTON_BYTE_MASK = 0x20;
        private byte pad = 0xF;
        private byte buttons = 0xF;

        internal void handleKeyDown(KeyEventArgs e) {
            byte b = GetKeyBit(e);
            if ((b & PAD_BYTE_MASK) == PAD_BYTE_MASK) {
                pad = (byte)(pad & ~(b & 0xF));
            } else if((b & BUTTON_BYTE_MASK) == BUTTON_BYTE_MASK) {
                buttons = (byte)(buttons & ~(b & 0xF));
            }
            Console.WriteLine("Down: Buttons = " + buttons.ToString("x2") + " Pad = " + pad.ToString("x2"));
        }

        internal void handleKeyUp(KeyEventArgs e) {
            byte b = GetKeyBit(e);
            if ((b & PAD_BYTE_MASK) == PAD_BYTE_MASK) {
                pad = (byte)(pad | (b & 0xF));
            } else if ((b & BUTTON_BYTE_MASK) == BUTTON_BYTE_MASK) {
                buttons = (byte)(buttons | (b & 0xF));
            }
            Console.WriteLine("UP: Buttons = " + buttons.ToString("x2") + " Pad = " + pad.ToString("x2"));
        }

        public void update(MMU mmu) {
            if(!mmu.isBit(4, mmu.JOYP)) {
                mmu.JOYP = (byte)(0xCF & pad);
            }
            if (!mmu.isBit(5, mmu.JOYP)) {
                mmu.JOYP = (byte)(0xCF & buttons);
            }
        }

        private byte GetKeyBit(KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.D:
                case Keys.Right:
                    return 0x11;

                case Keys.A:
                case Keys.Left:
                    return 0x12;

                case Keys.W:
                case Keys.Up:
                    return 0x14;

                case Keys.S:
                case Keys.Down:
                    return 0x18;

                case Keys.J:
                case Keys.Z:
                    return 0x21;

                case Keys.K:
                case Keys.X:
                    return 0x22;

                case Keys.Space:
                case Keys.C:
                    return 0x24;

                case Keys.Enter:
                case Keys.V:
                    return 0x28;
            }
            return 0;
        }
    }
}
