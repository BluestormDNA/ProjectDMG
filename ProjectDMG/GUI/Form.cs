using System;
using System.Windows.Forms;

namespace ProjectDMG {
    public partial class Form : System.Windows.Forms.Form {

        ProjectDMG dmg;

        public Form() {
            InitializeComponent();
        }

        private void Form_Load(object sender, EventArgs e) {
            dmg = new ProjectDMG();
        }

        private void Key_Down(object sender, KeyEventArgs e) {
            if (dmg.power_switch) dmg.joypad.handleKeyDown(e);
        }

        private void Key_Up(object sender, KeyEventArgs e) {
            if (dmg.power_switch) dmg.joypad.handleKeyUp(e);
        }

        private void Drag_Drop(object sender, DragEventArgs e) {
            string[] cartNames = (string[])e.Data.GetData(DataFormats.FileDrop);
            dmg.POWER_ON(cartNames[0]);
        }

        private void Drag_Enter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.All;
            dmg.POWER_OFF();
        }

    }
}
