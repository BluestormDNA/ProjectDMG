using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectDMG {
    public partial class Form : System.Windows.Forms.Form {

        ProjectDMG dmg;

        public Form() {
            InitializeComponent();
        }

        private void Form_Load(object sender, EventArgs e) {
            dmg = new ProjectDMG(pictureBox);
        }

        private void Key_Down(object sender, KeyEventArgs e) {
            dmg.joypad.handleKeyDown(e);
        }

        private void Key_Up(object sender, KeyEventArgs e) {
            dmg.joypad.handleKeyUp(e);
        }
    }
}
