using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDMG.Utils {
    public static class Constants {
        public const int DMG_4Mhz = 4194304;
        public const int _60hz = 60;
        public const int CYCLES_PER_UPDATE = DMG_4Mhz / _60hz;
    }
}
