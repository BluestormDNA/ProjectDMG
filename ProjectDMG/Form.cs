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
        public Form() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            ProjectDMG dmg = new ProjectDMG(pictureBox);
        }
    }
}
