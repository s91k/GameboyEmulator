using GameboyEmulator;

namespace GameboyEmulatorTests
{
    public class CPUTests
    {
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public void TestNOP()
        {
            var memory = new byte[1];

            memory[0] = 0b_00000000;    // nop

            CPU cpu = new CPU(memory);

            cpu.Run(1);

            Assert.Pass();
        }

        [Test]
        public void TestLdR8Imm8()
        {
            var memory = new byte[4];

            memory[0] = 0b_00000110;    // ld r8[0] imm8
            memory[1] = 0b_01010101;

            memory[2] = 0b_00111110;    // ld r8[7] imm8
            memory[3] = 0b_10101010;

            CPU cpu = new CPU(memory);

            cpu.Run(4);

            Assert.That(memory[1], Is.EqualTo(cpu.B));
            Assert.That(memory[3], Is.EqualTo(cpu.A));
        }

        [Test]
        public void TestLdR8R8()
        {
            var memory = new byte[3];

            memory[0] = 0b_00000110;    // ld r8[0] imm8
            memory[1] = 0b_01010101;

            memory[2] = 0b_01111000;    // ld r8[7] r8[0]

            CPU cpu = new CPU(memory);

            cpu.Run(3);

            Assert.That(memory[1], Is.EqualTo(cpu.A));
        }

        [Test]
        public void TestLdR16Imm16()
        {
            var memory = new byte[6];

            memory[0] = 0b_00000001;    // ld r16[0] imm16
            memory[1] = 0b_01010101;
            memory[2] = 0b_10101010;

            memory[3] = 0b_00010001;    // ld r16[1] imm16
            memory[4] = 0b_11110000;
            memory[5] = 0b_00001111;

            CPU cpu = new CPU(memory);

            cpu.Run(6);

            Assert.That(cpu.BC, Is.EqualTo(CPU.BytesToUShort(memory[2], memory[1])));
            Assert.That(cpu.DE, Is.EqualTo(CPU.BytesToUShort(memory[5], memory[4])));
        }

        [Test]
        public void TestLdR16memA()
        {
            var memory = new byte[7];

            memory[0] = 0b_00111110;    // ld r8[7] imm8
            memory[1] = 0b_10101010;

            memory[2] = 0b_00000001;    // ld r16[0] imm16
            memory[3] = 0b_00000110;
            memory[4] = 0b_00000000;

            memory[5] = 0b_00000010;    // ld [r16mem][0], a

            CPU cpu = new CPU(memory);

            cpu.Run(6);

            Assert.That(memory[6], Is.EqualTo(0b_10101010));
        }

        [Test]
        public void TestLdAR16mem()
        {
            var memory = new byte[7];

            memory[0] = 0b_00000000;    // nop
            memory[1] = 0b_00000000;    // nop

            memory[2] = 0b_00000001;    // ld r16[0] imm16
            memory[3] = 0b_00000110;
            memory[4] = 0b_00000000;

            memory[5] = 0b_00001010;    // ld [r16mem][0], a

            memory[6] = 0b_10101010;

            CPU cpu = new CPU(memory);

            cpu.Run(6);

            Assert.That(cpu.A, Is.EqualTo(0b_10101010));
        }

        [Test]
        public void TestIncR16AndDecR16()
        {
            var memory = new byte[8];

            memory[0] = 0b_00000001;    // ld r16[0] imm16
            memory[1] = 0x02;
            memory[2] = 0x00;

            memory[3] = 0b_00010001;    // ld r16[1] imm16
            memory[4] = 0x02;
            memory[5] = 0x00;

            memory[6] = 0b_00000011;    // inc r16[0]
            memory[7] = 0b_00011011;    // dec r16[1]

            CPU cpu = new CPU(memory);

            cpu.Run(8);

            Assert.That(cpu.BC, Is.EqualTo(0x03));
            Assert.That(cpu.DE, Is.EqualTo(0x01));
        }

        [Test]
        public void TestAddHLR16()
        {
            var memory = new byte[7];

            memory[0] = 0b_00000001;    // ld r16[0] imm16
            memory[1] = 0x03;
            memory[2] = 0x00;

            memory[3] = 0b_00100001;    // ld r16[2] imm16
            memory[4] = 0x02;
            memory[5] = 0x00;

            memory[6] = 0b_00001001;    // add hl r16[0]

            CPU cpu = new CPU(memory);

            cpu.Run(7);

            Assert.That(cpu.HL, Is.EqualTo(0x05));
            Assert.That(cpu.CarryFlag, Is.False);
            Assert.That(cpu.HalfCarryFlag, Is.False);
        }

        [Test]
        public void TestAddHLR16HalfCarry()
        {
            var memory = new byte[7];

            memory[0] = 0b_00000001;    // ld r16[0] imm16
            memory[1] = 0xFF;
            memory[2] = 0x0F;

            memory[3] = 0b_00100001;    // ld r16[2] imm16
            memory[4] = 0xFF;
            memory[5] = 0x0F;

            memory[6] = 0b_00001001;    // add hl r16[0]

            CPU cpu = new CPU(memory);

            cpu.Run(7);

            Assert.That(cpu.CarryFlag, Is.False);
            Assert.That(cpu.HalfCarryFlag, Is.True);
        }

        [Test]
        public void TestAddHLR16Carry()
        {
            var memory = new byte[7];

            memory[0] = 0b_00000001;    // ld r16[0] imm16
            memory[1] = 0x00;
            memory[2] = 0xF0;

            memory[3] = 0b_00100001;    // ld r16[2] imm16
            memory[4] = 0x00;
            memory[5] = 0xF0;

            memory[6] = 0b_00001001;    // add hl r16[0]

            CPU cpu = new CPU(memory);

            cpu.Run(7);

            Assert.That(cpu.CarryFlag, Is.True);
            Assert.That(cpu.HalfCarryFlag, Is.False);
        }

        [Test]
        public void TestIncH8()
        {
            var memory = new byte[2];

            memory[0] = 0b_00000100;    // inc r8[0]
            memory[1] = 0b_00000100;    // inc r8[0]

            CPU cpu = new CPU(memory);

            cpu.Run(2);

            Assert.That(cpu.B, Is.EqualTo(2));
            Assert.That(cpu.ZeroFlag, Is.False);
            Assert.That(cpu.HalfCarryFlag, Is.False);
            Assert.That(cpu.SubtractionFlag, Is.False);
        }

        [Test]
        public void TestDecH8()
        {
            var memory = new byte[2];

            memory[0] = 0b_00000101;    // inc r8[0]
            memory[1] = 0b_00000101;    // inc r8[0]

            CPU cpu = new CPU(memory);

            cpu.Run(2);

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