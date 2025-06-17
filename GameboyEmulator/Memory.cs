using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class Memory
    {
        private byte[] memory;

        public Memory()
        {
            memory = new byte[8192];
        }

        public void Clear()
        {
            for(int i = 0; i < memory.Length; i++)
            {
                memory[i] = 0;
            }
        }

        public void SetMemory(byte[] memoryToSet, int startIndex = 0)
        {
            for(int i = 0; i < memoryToSet.Length && i + startIndex < memory.Length; i++)
            {
                memory[i] = memoryToSet[i];
            }
        }

        public byte[] GetRawMemory() => memory;
    }
}
