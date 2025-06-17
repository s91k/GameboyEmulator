using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class CPU
    {
        struct OpCode
        {
            public Action Action;
            public byte Cycles;
        }

        Dictionary<byte, OpCode> opCodes = new Dictionary<byte, OpCode>();

        // 8-bit Registers
        public byte A { get; private set; }
        public byte B { get; private set; }
        public byte C { get; private set; }
        public byte D { get; private set; }
        public byte E { get; private set; }
        public byte F {
            get
            {
                byte flags = 0b_00000000;

                if (ZeroFlag)
                {
                    flags &= 0b_00000001;
                }

                if (SubtractionFlag)
                {
                    flags &= 0b_00000010;
                }

                if (HalfCarryFlag)
                {
                    flags &= 0b_00000100;
                }

                if (CarryFlag)
                {
                    flags &= 0b_00001000;
                }

                return flags;
            }
        }
        public byte H { get; private set; }
        public byte L { get; private set; }

        // 16-bit registers
        public ushort AF { get => BytesToUShort(A, F); }
        public ushort BC { get => BytesToUShort(B, C); }
        public ushort DE { get => BytesToUShort(D, E); }
        public ushort HL { get => BytesToUShort(H, L); private set => SetR16(value, 2); }

        // Flags
        public bool ZeroFlag { get; private set; }
        public bool SubtractionFlag { get; private set; }
        public bool HalfCarryFlag { get; private set; }
        public bool CarryFlag { get; private set; }

        ushort stackPointer;
        ushort programCounter;

        byte remainingInstructionCycles;

        bool isHalted;

        // Memory
        private byte[] memory;

        public CPU(Memory memory)
        {
            this.memory = memory.GetRawMemory();

            // nop	0	0	0	0	0	0	0	0
            opCodes.Add(0b_00000000, new OpCode { Action = OpNop, Cycles = 1 });

            // ld r16, imm16	0	0	Dest (r16)	0	0	0	1
            // ld [r16mem], a	0	0	Dest (r16mem)	0	0	1	0
            // ld a, [r16mem]	0	0	Source (r16mem)	1	0	1	0
            for (byte b = 0; b < 4; b++)
            {
                byte r16 = b;
                opCodes.Add((byte)(0b_00000001 | (r16 << 4)), new OpCode { Action = () => OpLdR16Imm16(r16), Cycles = 3 });
                opCodes.Add((byte)(0b_00000010 | (r16 << 4)), new OpCode { Action = () => OpLdR16memA(r16), Cycles = 2 });
                opCodes.Add((byte)(0b_00001010 | (r16 << 4)), new OpCode { Action = () => OpLdAR16mem(r16), Cycles = 2 });
            }

            // ld [imm16], sp	0	0	0	0	1	0	0	0
            opCodes.Add(0b_00001000, new OpCode { Action = () => OpLdR16Imm16(3), Cycles = 3 });

            // inc r16	0	0	Operand (r16)	0	0	1	1
            // dec r16	0	0	Operand (r16)	1	0	1	1
            // add hl, r16	0	0	Operand (r16)	1	0	0	1
            for (byte b = 0; b < 4; b++)
            {
                byte r16 = b;
                opCodes.Add((byte)(0b_00000011 | (r16 << 4)), new OpCode { Action = () => OpIncR16(r16), Cycles = 2 });
                opCodes.Add((byte)(0b_00001011 | (r16 << 4)), new OpCode { Action = () => OpDecR16(r16), Cycles = 2 });
                opCodes.Add((byte)(0b_00001001 | (r16 << 4)), new OpCode { Action = () => OpAddHLR16(r16), Cycles = 2 });
            }

            // inc r8	0	0	Operand (r8)	1	0	0
            // dec r8	0	0	Operand (r8)	1	0	1
            // ld r8, imm8	0	0	Dest (r8)	1	1	0
            for (byte b = 0; b < 8; b++)
            {
                byte r8 = b;
                opCodes.Add((byte)(0b_00000100 | (r8 << 3)), new OpCode { Action = () => OpIncR8(r8), Cycles = 1 });
                opCodes.Add((byte)(0b_00000101 | (r8 << 3)), new OpCode { Action = () => OpDecR8(r8), Cycles = 1 });
                opCodes.Add((byte)(0b_00000110 | (r8 << 3)), new OpCode { Action = () => OpLdR8Imm8(r8), Cycles = 2 });
            }

            // rlca	0	0	0	0	0	1	1	1
            // rrca	0	0	0	0	1	1	1	1
            // rla	0	0	0	1	0	1	1	1
            // rra	0	0	0	1	1	1	1	1
            // daa	0	0	1	0	0	1	1	1
            // cpl	0	0	1	0	1	1	1	1
            // scf	0	0	1	1	0	1	1	1
            // ccf	0	0	1	1	1	1	1	1

            // jr imm8	0	0	0	1	1	0	0	0
            // jr cond, imm8	0	0	1	Condition (cond)	0	0	0

            // stop	0	0	0	1	0	0	0	0 

            // ld r8, r8	0	1	Dest (r8)	Source (r8)
            for (byte i = 0; i < 8; i++)
            {
                for (byte j = 0; j < 8; j++)
                {
                    if(i != 6 && j != 6)
                    {
                        byte src = i;
                        byte dest = j;

                        opCodes.Add((byte)((0b_01000000 | (dest << 3)) | src), new OpCode { Action = () => OpLdR8R8(dest, src), Cycles = 1 });
                    }
                }
            }

            // halt	0	1	1	1	0	1	1	0
            opCodes.Add(0b_01110110, new OpCode { Action = OpHalt, Cycles = 1 });

            // add a, r8	1	0	0	0	0	Operand (r8)
            // adc a, r8	1	0	0	0	1	Operand (r8)
            // sub a, r8	1	0	0	1	0	Operand (r8)
            // sbc a, r8	1	0	0	1	1	Operand (r8)
            // and a, r8	1	0	1	0	0	Operand (r8)
            // xor a, r8	1	0	1	0	1	Operand (r8)
            // or a, r8	1	0	1	1	0	Operand (r8)
            // cp a, r8	1	0	1	1	1	Operand (r8)

            // add a, imm8	1	1	0	0	0	1	1	0
            // adc a, imm8	1	1	0	0	1	1	1	0
            // sub a, imm8	1	1	0	1	0	1	1	0
            // sbc a, imm8	1	1	0	1	1	1	1	0
            // and a, imm8	1	1	1	0	0	1	1	0
            // xor a, imm8	1	1	1	0	1	1	1	0
            // or a, imm8	1	1	1	1	0	1	1	0
            // cp a, imm8	1	1	1	1	1	1	1	0

            // ret cond	1	1	0	Condition (cond)	0	0	0
            // ret	1	1	0	0	1	0	0	1
            // reti	1	1	0	1	1	0	0	1
            // jp cond, imm16	1	1	0	Condition (cond)	0	1	0
            // jp imm16	1	1	0	0	0	0	1	1
            // jp hl	1	1	1	0	1	0	0	1
            // call cond, imm16	1	1	0	Condition (cond)	1	0	0
            // call imm16	1	1	0	0	1	1	0	1
            // rst tgt3	1	1	Target (tgt3)	1	1	1

            // pop r16stk	1	1	Register (r16stk)	0	0	0	1
            // push r16stk	1	1	Register (r16stk)	0	1	0	1

            // Prefix (see block below)	1	1	0	0	1	0	1	1

            // ldh [c], a	1	1	1	0	0	0	1	0
            // ldh [imm8], a	1	1	1	0	0	0	0	0
            // ld [imm16], a	1	1	1	0	1	0	1	0
            // ldh a, [c]	1	1	1	1	0	0	1	0
            // ldh a, [imm8]	1	1	1	1	0	0	0	0
            // ld a, [imm16]	1	1	1	1	1	0	1	0

            // add sp, imm8	1	1	1	0	1	0	0	0
            // ld hl, sp + imm8	1	1	1	1	1	0	0	0
            // ld sp, hl	1	1	1	1	1	0	0	1

            // di	1	1	1	1	0	0	1	1
            // ei	1	1	1	1	1	0	1	1

            // rlc r8	0	0	0	0	0	Operand (r8)
            // rrc r8	0	0	0	0	1	Operand (r8)
            // rl r8	0	0	0	1	0	Operand (r8)
            // rr r8	0	0	0	1	1	Operand (r8)
            // sla r8	0	0	1	0	0	Operand (r8)
            // sra r8	0	0	1	0	1	Operand (r8)
            // swap r8	0	0	1	1	0	Operand (r8)
            // srl r8	0	0	1	1	1	Operand (r8)// 

            // bit b3, r8	0	1	Bit index (b3)	Operand (r8)
            // res b3, r8	1	0	Bit index (b3)	Operand (r8)
            // set b3, r8	1	1	Bit index (b3)	Operand (r8)
        }

        public void Run()
        {
            isHalted = false;

            while(!isHalted && programCounter < memory.Length)
            {
                if (remainingInstructionCycles > 0)
                {
                    remainingInstructionCycles--;
                } else
                {
                    var instruction = memory[programCounter++];

                    if (opCodes.TryGetValue(instruction, out var instructionAction))
                    {
                        instructionAction.Action.Invoke();
                        remainingInstructionCycles = instructionAction.Cycles--;
                    }
                    else
                    {
                        Console.WriteLine($"Invalid instruction, {Convert.ToString(instruction, 2).PadLeft(8, '0')}");
                    }
                }
            }
        }

        private ushort GetR16(byte r16) => r16 switch
        {
            0 => BC,
            1 => DE,
            2 => HL,
            3 => stackPointer,
            _ => throw new ArgumentException()
        };

        public static ushort BytesToUShort(byte high, byte low) => (ushort)(low + (high << 8));

        private void SetR16(ushort value, byte r16)
        {
            switch (r16)
            {
                case 0:
                    SetR16(value, 0, 1);    // BC
                    break;
                case 1:
                    SetR16(value, 2, 3);    // DE
                    break;
                case 2:
                    SetR16(value, 4, 5);    // HL
                    break;
                case 3:
                    stackPointer = value;
                    break;
            }
        }

        private void SetR16(ushort value, byte highR8, byte lowR8)
        {
            SetR8(highR8, (byte)(value >> 8));
            SetR8(lowR8, (byte)(value & 0b_0000000011111111));
        }

        public byte GetR8(byte r8) => r8 switch
        {
            0 => B,
            1 => C,
            2 => D,
            3 => E,
            4 => H,
            5 => L,
            6 => memory[HL],
            7 => A,
            _ => throw new ArgumentException()
        };

        public void SetR8(byte r8, byte value)
        {
            switch (r8)
            {
                case 0:
                    B = value;
                    break;
                case 1:
                    C = value;
                    break;
                case 2:
                    D = value;
                    break;
                case 3:
                    E = value;
                    break;
                case 4:
                    H = value;
                    break;
                case 5:
                    L = value;
                    break;
                case 6:
                    memory[HL] = value;
                    break;
                case 7:
                    A = value;
                    break;
            }
        }

        private void OpNop()
        {
            // Do nothing
        }

        private void OpLdR8Imm8(byte r8) => SetR8(r8, memory[programCounter++]);

        private void OpLdR16Imm16(byte r16)
        {
            byte low = memory[programCounter++];
            byte high = memory[programCounter++];

            SetR16(BytesToUShort(high, low), r16);
        }

        private void OpLdR16memA(byte r16) => memory[GetR16(r16)] = A; 

        private void OpLdAR16mem(byte r16) => A = memory[GetR16(r16)];

        private void OpLdR8R8(byte dest, byte src) => SetR8(dest, GetR8(src));

        private void OpIncR16(byte r16) => SetR16((ushort)(GetR16(r16) + 1), r16);

        private void OpDecR16(byte r16) => SetR16((ushort)(GetR16(r16) - 1), r16);

        private void OpAddHLR16(byte r16)
        {
            ushort r16Value = GetR16(r16);

            SubtractionFlag = false;
            HalfCarryFlag = (r16Value & 0b_0000111111111111) + (HL & 0b_0000111111111111) > 0b_0000111111111111;
            CarryFlag = r16Value + HL > 0xFFFF;

            HL = (ushort)(r16Value + HL);
        }

        private void OpIncR8(byte r8)
        {
            byte r8Value = GetR8(r8);

            ZeroFlag = (r8Value + 1) == 0;
            SubtractionFlag = false;
            HalfCarryFlag = (r8Value & 0b_00001111) == 0b_00001111;

            SetR8(r8, (byte)(r8Value + 1));
        }

        private void OpDecR8(byte r8)
        {
            byte r8Value = GetR8(r8);

            ZeroFlag = (r8Value - 1) == 0;
            SubtractionFlag = true;
            HalfCarryFlag = (r8Value & 0b_00001111) == 0;

            SetR8(r8, (byte)(r8Value - 1));
        }

        private void OpHalt() => isHalted = true;

        private void OpAdd(byte r8) => A += r8;
    }
}
