using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    internal class Memory
    {
        private byte[] memory;

        Memory()
        {
            memory = new byte[8192];
        }
    }
}
