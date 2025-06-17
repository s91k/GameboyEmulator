using GameboyEmulator;

namespace GameboyEmulatorTests
{
    public class CPUTests
    {
        CPU cpu;
        Memory memory;

        [SetUp]
        public void Setup()
        {
            memory = new Memory();
            cpu = new CPU(memory);
        }

        [Test]
        public void TestNOP()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000000,    // nop
                0b_01110110     // halt
            ]);

            cpu.Run();

            Assert.Pass();
        }

        [Test]
        public void TestLdR8Imm8()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000110, 0b_01010101,   // ld r8[0] imm8
                0b_00111110, 0b_10101010,   // ld r8[7] imm8
                0b_01110110                 // halt
            ]);

            cpu.Run();

            Assert.That(cpu.B, Is.EqualTo(0b_01010101));
            Assert.That(cpu.A, Is.EqualTo(0b_10101010));
        }

        [Test]
        public void TestLdR8R8()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000110, 0x12,  // ld r8[0] imm8
                0b_01111000,        // ld r8[7] r8[0]
                0b_01110110         // halt
            ]);

            cpu.Run();

            Assert.That(cpu.A, Is.EqualTo(0x12));
        }

        [Test]
        public void TestLdR16Imm16()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000001, 0b_01010101, 0b_10101010,  // ld r16[0] imm16
                0b_00010001, 0b_11110000, 0b_00001111,  // ld r16[1] imm16
                0b_01110110                             // halt
            ]);

            cpu.Run();

            Assert.That(cpu.BC, Is.EqualTo(CPU.BytesToUShort(0b_10101010, 0b_01010101)));
            Assert.That(cpu.DE, Is.EqualTo(CPU.BytesToUShort(0b_00001111, 0b_11110000)));
        }

        [Test]
        public void TestLdR16memA()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00111110, 0b_10101010,               // ld r8[7] imm8
                0b_00000001, 0b_00000110, 0b_00000000,  // ld r16[0] imm16
                0b_00000010,                            // ld [r16mem][0], a
                0b_01110110                             // halt
            ]);

            cpu.Run();

            Assert.That(memory.GetRawMemory()[6], Is.EqualTo(0b_10101010));
        }

        [Test]
        public void TestLdAR16mem()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000001, 0b_00000101, 0b_00000000,  // ld r16[0] imm16
                0b_00001010,                            // ld [r16mem][0], a
                0b_01110110,                            // halt
                0b_10101010,
            ]);

            cpu.Run();

            Assert.That(cpu.A, Is.EqualTo(0b_10101010));
        }

        [Test]
        public void TestIncR16AndDecR16()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000001, 0x02, 0x00,    // ld r16[0] imm16
                0b_00010001, 0x02, 0x00,    // ld r16[1] imm16
                0b_00000011,                // inc r16[0]
                0b_00011011,                // dec r16[1]
                0b_01110110                 // halt
            ]);

            cpu.Run();

            Assert.That(cpu.BC, Is.EqualTo(0x03));
            Assert.That(cpu.DE, Is.EqualTo(0x01));
        }

        [Test]
        public void TestAddHLR16()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000001, 0x03, 0x00,    // ld r16[0] imm16
                0b_00100001, 0x02, 0x00,    // ld r16[2] imm16
                0b_00001001,                // add hl r16[0]
                0b_01110110                 // halt
            ]);

            cpu.Run();

            Assert.That(cpu.HL, Is.EqualTo(0x05));
            Assert.That(cpu.CarryFlag, Is.False);
            Assert.That(cpu.HalfCarryFlag, Is.False);
        }

        [Test]
        public void TestAddHLR16HalfCarry()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000001, 0xFF, 0x0F,    // ld r16[0] imm16
                0b_00100001, 0xFF, 0x0F,    // ld r16[2] imm16
                0b_00001001,                // add hl r16[0]
                0b_01110110                 // halt
            ]);

            cpu.Run();

            Assert.That(cpu.CarryFlag, Is.False);
            Assert.That(cpu.HalfCarryFlag, Is.True);
        }

        [Test]
        public void TestAddHLR16Carry()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000001, 0x00, 0xF0,    // ld r16[0] imm16
                0b_00100001, 0x00, 0xF0,    // ld r16[2] imm16
                0b_00001001,                // add hl r16[0]
                0b_01110110                 // halt
            ]);

            cpu.Run();

            Assert.That(cpu.CarryFlag, Is.True);
            Assert.That(cpu.HalfCarryFlag, Is.False);
        }

        [Test]
        public void TestIncH8()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000100,    // inc r8[0]
                0b_00000100,    // inc r8[0]
                0b_01110110     // halt
            ]);

            cpu.Run();

            Assert.That(cpu.B, Is.EqualTo(2));
            Assert.That(cpu.ZeroFlag, Is.False);
            Assert.That(cpu.HalfCarryFlag, Is.False);
            Assert.That(cpu.SubtractionFlag, Is.False);
        }

        [Test]
        public void TestDecH8()
        {
            memory.Clear();

            memory.SetMemory([
                0b_00000101,    // dec r8[0]
                0b_00000101,    // dec r8[0]
                0b_01110110     // halt
            ]);

            cpu.Run();

            Assert.That(cpu.B, Is.EqualTo(Byte.MaxValue - 1));
            Assert.That(cpu.ZeroFlag, Is.False);
            Assert.That(cpu.HalfCarryFlag, Is.False);
            Assert.That(cpu.SubtractionFlag, Is.True);
        }

        [Test]
        public void TestBytesToUShort()
        {
            Assert.That(CPU.BytesToUShort(0b_10000000, 0b_00000001), Is.EqualTo(0b_1000000000000001));
            Assert.That(CPU.BytesToUShort(0b_11111111, 0b_00000000), Is.EqualTo(0b_1111111100000000));
        }

    }
}