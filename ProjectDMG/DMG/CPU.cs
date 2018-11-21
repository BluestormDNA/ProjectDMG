using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDMG {

    class CPU { // Sharp LR35902 CPU

        private ushort PC;
        private ushort SP;

        private byte A, B, C, D, E, F, H, L;

        private ushort AF { get { return combineRegs(A, F); } set { A = (byte)(value >> 8); F = (byte)value; } }
        private ushort BC { get { return combineRegs(B, C); } set { B = (byte)(value >> 8); C = (byte)value; } }
        private ushort DE { get { return combineRegs(D, E); } set { D = (byte)(value >> 8); E = (byte)value; } }
        private ushort HL { get { return combineRegs(H, L); } set { H = (byte)(value >> 8); L = (byte)value; } }

        private bool FlagZ { get { return (F & 0x80) != 0; } set { F = value ? (byte)(F | 0x80) : (byte)(F & ~0x80); } }
        private bool FlagN { get { return (F & 0x40) != 0; } set { F = value ? (byte)(F | 0x40) : (byte)(F & ~0x40); } }
        private bool FlagH { get { return (F & 0x20) != 0; } set { F = value ? (byte)(F | 0x20) : (byte)(F & ~0x20); } }
        private bool FlagC { get { return (F & 0x10) != 0; } set { F = value ? (byte)(F | 0x10) : (byte)(F & ~0x10); } }

        private bool IME;
        private int cycles;

        public int Exe(MMU mmu) {

            byte opcode = mmu.readByte(PC++);
            debug(mmu, opcode);
            cycles = 0;
                                                                                                                                 
            switch (opcode) {
                case 0x00:                                      break; //NOP        1 4     ----
                case 0x01: BC = mmu.readWord(PC); PC += 2;      break; //LD BC,D16  3 12    ----
                case 0x02: mmu.writeByte(BC, A);                break; //LD (BC),A  1 8     ----
                case 0x03: BC += 1;                             break; //INC BC     1 8     ----
                case 0x04: B = INC(B);                          break; //INC B      1 4     Z0H-
                case 0x05: B = DEC(B);                          break; //DEC B      1 4     Z1H-
                case 0x06: B = mmu.readByte(PC); PC += 1;       break; //LD B,D8    2 8     ----

                case 0x07: //RLCA 1 4 000C
                    F = 0;
                    FlagC = ((A & 0x80) != 0);
                    A = (byte)((A << 1) | (A >> 7));
                    break;

                case 0x08: mmu.writeWord(mmu.readWord(PC), SP); PC += 2; break; //LD (A16),SP 3 20   ----
                case 0x09: DAD(BC);                             break; //ADD HL,BC   1 8    -0HC
                case 0x0A: A = mmu.readByte(BC);                break; //LD A,(BC)   1 8    ----
                case 0x0B: BC -= 1;                             break; //DEC BC      1 8    ----
                case 0x0C: C = INC(C);                          break; //INC C       1 8    Z0H-
                case 0x0D: C = DEC(C);                          break; //DEC C       1 8    Z1H-
                case 0x0E: C = mmu.readByte(PC); PC += 1;       break; //LD C,D8     2 8    ----

                case 0x0F: //RRCA 1 4 000C
                    F = 0;
                    FlagC = ((A & 0x1) != 0);
                    A = (byte)((A >> 1) | (A << 7));
                    break;

                case 0x10: STOP();                              break; //STOP        2 4    ----
                case 0x11: DE = mmu.readWord(PC); PC += 2;      break; //LD DE,D16   3 12   ----
                case 0x12: mmu.writeByte(DE, A);                break; //LD (DE),A   1 8    ----
                case 0x13: DE += 1;                             break; //INC DE      1 8    ----
                case 0x14: D = INC(D);                          break; //INC D       1 8    Z0H-
                case 0x15: D = DEC(D);                          break; //DEC D       1 8    Z1H-
                case 0x16: D = mmu.readByte(PC); PC += 1;       break; //LD D,D8     2 8    ----

                case 0x17://RLA 1 4 000C
                    bool prevC = FlagC;
                    F = 0;
                    FlagC = ((A & 0x80) != 0);
                    A = (byte)((A << 1) | (prevC ? 1 : 0));
                    break;

                case 0x18: JR(mmu, true);                       break; //JR R8       2 12   ----
                case 0x19: DAD(DE);                             break; //ADD HL,DE   1 8    -0HC
                case 0x1A: A = mmu.readByte(DE);                break; //LD A,(DE)   1 8    ----
                case 0x1B: DE -= 1;                             break; //INC DE      1 8    ----
                case 0x1C: E = INC(E);                          break; //INC E       1 8    Z0H-
                case 0x1D: E = DEC(E);                          break; //DEC E       1 8    Z1H-
                case 0x1E: E = mmu.readByte(PC); PC += 1;       break; //LD E,D8     2 8    ----

                case 0x1F://RRA 1 4 000C
                    bool preC = FlagC;
                    F = 0;
                    FlagC = ((A & 0x1) != 0);
                    A = (byte)((A >> 1) | (preC ? 0x80 : 0));
                    break;

                case 0x20: JR(mmu, !FlagZ);                     break; //JR NZ R8    2 12/8 ---- 
                case 0x21: HL = mmu.readWord(PC); PC += 2;      break; //LD HL,D16   3 12   ----
                case 0x22: mmu.writeByte(HL++, A);              break; //LD (HL+),A  1 8    ----
                case 0x23: HL += 1;                             break; //INC HL      1 8    ----
                case 0x24: H = INC(H);                          break; //INC H       1 8    Z0H-
                case 0x25: H = DEC(H);                          break; //DEC H       1 8    Z1H-
                case 0x26: H = mmu.readByte(PC); PC += 1; ;     break; //LD H,D8     2 8    ----

                case 0x27: //DAA    1 4 Z-0C <------------- //TODO PENDING REWRITE
                    int daa = A;
                    if ((daa & 0x0F) > 0x9 || FlagH) {
                        //FlagH = (((daa & 0x0F) + 0x06) & 0xF0) != 0;
                        daa += 0x06;
                        if ((daa & 0xFF00) != 0) {
                            FlagC = true;
                        }
                    }

                    if ((daa & 0xF0) > 0x90 || FlagC) {
                        daa += 0x60;
                        if ((daa & 0xFF00) != 0) {
                            FlagC = true;
                        }
                    }
                    SetFlagZ((byte)daa);
                    FlagH = false;
                    A = (byte)daa;
                    break;

                case 0x28: JR(mmu, FlagZ);                                 break; //JR Z R8    2 12/8  ----
                case 0x29: DAD(HL);                                        break; //ADD HL,HL  1 8     -0HC
                case 0x2A: A = mmu.readByte(HL++);                         break; //LD A (HL+) 1 8     ----
                case 0x2B: HL -= 1;                                        break; //DEC HL     1 4     ----
                case 0x2C: L = INC(L);                                     break; //INC L      1 4     Z0H-
                case 0x2D: L = DEC(L);                                     break; //DEC L      1 4     Z1H-
                case 0x2E: L = mmu.readByte(PC); PC += 1; ;                break; //LD A,D8    2 8     ----
                case 0x2F: A = (byte)~A; FlagN = true; FlagH = true;       break; //CPL	       1 4     -11-

                case 0x30: JR(mmu, !FlagC);                                break; //JR NC R8   2 12/8  ----
                case 0x31: SP = mmu.readWord(PC); PC += 2; ;               break; //LD SP,D16  3 12    ----
                case 0x32: mmu.writeByte(HL--, A);                         break; //LD (HL-),A 1 8     ----
                case 0x33: SP += 1;                                        break; //INC SP     1 8     ----
                case 0x34: mmu.writeByte(HL, INC(mmu.readByte(HL)));       break; //INC (HL)   1 12    Z0H-
                case 0x35: mmu.writeByte(HL, DEC(mmu.readByte(HL)));       break; //DEC (HL)   1 12    Z1H-
                case 0x36: mmu.writeByte(HL, mmu.readByte(PC)); PC += 1;   break; //LD (HL),D8 2 12    ----
                case 0x37: F = 0; FlagC = true;                            break; //SCF	       1 4     0001

                case 0x38: JR(mmu, FlagC);                                 break; //JR C R8    2 12/8  ----
                case 0x39: DAD(SP);                                        break; //ADD HL,SP  1 8     -0HC
                case 0x3A: A = mmu.readByte(HL--);                         break; //LD A (HL-) 1 8     ----
                case 0x3B: SP -= 1;                                        break; //DEC SP     1 8     ----
                case 0x3C: A = INC(A);                                     break; //INC A      1 4     Z0H-
                case 0x3D: A = DEC(A);                                     break; //DEC (HL)   1 4     Z1H-
                case 0x3E: A = mmu.readByte(PC); PC += 1;                  break; //LD A,D8    2 8     ----
                case 0x3F: FlagC = !FlagC; FlagN = false; FlagH = false;   break; //CCF        1 4     -00C

                case 0x40: /*B = B;*/             break; //LD B,B	    1 4    ----
                case 0x41: B = C;                 break; //LD B,C	    1 4	   ----
                case 0x42: B = D;                 break; //LD B,D	    1 4	   ----
                case 0x43: B = E;                 break; //LD B,E	    1 4	   ----
                case 0x44: B = H;                 break; //LD B,H	    1 4	   ----
                case 0x45: B = L;                 break; //LD B,L	    1 4	   ----
                case 0x46: B = mmu.readByte(HL);  break; //LD B,(HL)	1 8	   ----
                case 0x47: B = A;                 break; //LD B,A	    1 4	   ----
                                                 
                case 0x48: C = B;                 break; //LD C,B	    1 4    ----
                case 0x49: /*C = C;*/             break; //LD C,C	    1 4    ----
                case 0x4A: C = D;                 break; //LD C,D   	1 4    ----
                case 0x4B: C = E;                 break; //LD C,E   	1 4    ----
                case 0x4C: C = H;                 break; //LD C,H   	1 4    ----
                case 0x4D: C = L;                 break; //LD C,L   	1 4    ----
                case 0x4E: C = mmu.readByte(HL);  break; //LD C,(HL)	1 8    ----
                case 0x4F: C = A;                 break; //LD C,A   	1 4    ----
                                                                    
                case 0x50: D = B;                 break; //LD D,B	    1 4    ----
                case 0x51: D = C;                 break; //LD D,C	    1 4    ----
                case 0x52: /*D = D;*/             break; //LD D,D	    1 4    ----
                case 0x53: D = E;                 break; //LD D,E	    1 4    ----
                case 0x54: D = H;                 break; //LD D,H	    1 4    ----
                case 0x55: D = L;                 break; //LD D,L	    1 4    ----
                case 0x56: D = mmu.readByte(HL);  break; //LD D,(HL)    1 8    ---- 
                case 0x57: D = A;                 break; //LD D,A	    1 4    ----
                                                                    
                case 0x58: E = B;                 break; //LD E,B   	1 4    ----
                case 0x59: E = C;                 break; //LD E,C   	1 4    ----
                case 0x5A: E = D;                 break; //LD E,D   	1 4    ----
                case 0x5B: /*E = E;*/             break; //LD E,E   	1 4    ----
                case 0x5C: E = H;                 break; //LD E,H   	1 4    ----
                case 0x5D: E = L;                 break; //LD E,L   	1 4    ----
                case 0x5E: E = mmu.readByte(HL);  break; //LD E,(HL)    1 8    ----
                case 0x5F: E = A;                 break; //LD E,A	    1 4    ----
                                                                   
                case 0x60: H = B;                 break; //LD H,B   	1 4    ----
                case 0x61: H = C;                 break; //LD H,C   	1 4    ----
                case 0x62: H = D;                 break; //LD H,D   	1 4    ----
                case 0x63: H = E;                 break; //LD H,E   	1 4    ----
                case 0x64: /*H = H;*/             break; //LD H,H   	1 4    ----
                case 0x65: H = L;                 break; //LD H,L   	1 4    ----
                case 0x66: H = mmu.readByte(HL);  break; //LD H,(HL)    1 8    ----
                case 0x67: H = A;                 break; //LD H,A	    1 4    ----
                                                                  
                case 0x68: L = B;                 break; //LD L,B   	1 4    ----
                case 0x69: L = C;                 break; //LD L,C   	1 4    ----
                case 0x6A: L = D;                 break; //LD L,D   	1 4    ----
                case 0x6B: L = E;                 break; //LD L,E   	1 4    ----
                case 0x6C: L = H;                 break; //LD L,H   	1 4    ----
                case 0x6D: /*L=L;*/               break; //LD L,L	    1 4    ----
                case 0x6E: L = mmu.readByte(HL);  break; //LD L,(HL)	1 8    ----
                case 0x6F: L = A;                 break; //LD L,A	    1 4    ----
                                                 
                case 0x70: mmu.writeByte(HL, B);  break; //LD (HL),B	1 8    ----
                case 0x71: mmu.writeByte(HL, C);  break; //LD (HL),C	1 8	   ----
                case 0x72: mmu.writeByte(HL, D);  break; //LD (HL),D	1 8	   ----
                case 0x73: mmu.writeByte(HL, E);  break; //LD (HL),E	1 8	   ----
                case 0x74: mmu.writeByte(HL, H);  break; //LD (HL),H	1 8	   ----
                case 0x75: mmu.writeByte(HL, L);  break; //LD (HL),L	1 8	   ----
                case 0x76: PC--;                  break; //HLT	        1 4    ----
                case 0x77: mmu.writeByte(HL, A);  break; //LD (HL),A	1 8    ----
                                                 
                case 0x78: A = B;                 break; //LD A,B	    1 4    ----
                case 0x79: A = C;                 break; //LD A,C	    1 4	   ----
                case 0x7A: A = D;                 break; //LD A,D	    1 4	   ----
                case 0x7B: A = E;                 break; //LD A,E	    1 4	   ----
                case 0x7C: A = H;                 break; //LD A,H	    1 4	   ----
                case 0x7D: A = L;                 break; //LD A,L	    1 4	   ----
                case 0x7E: A = mmu.readByte(HL);  break; //LD A,(HL)    1 8    ----
                case 0x7F: /*A = A;*/             break; //LD A,A	    1 4    ----

                case 0x80: ADD(B);                break; //ADD B	    1 4    Z0HC	
                case 0x81: ADD(C);                break; //ADD C	    1 4    Z0HC	
                case 0x82: ADD(D);                break; //ADD D	    1 4    Z0HC	
                case 0x83: ADD(E);                break; //ADD E	    1 4    Z0HC	
                case 0x84: ADD(H);                break; //ADD H	    1 4    Z0HC	
                case 0x85: ADD(L);                break; //ADD L	    1 4    Z0HC	
                case 0x86: ADD(mmu.readByte(HL)); break; //ADD M	    1 8    Z0HC	
                case 0x87: ADD(A);                break; //ADD A	    1 4    Z0HC	

                case 0x88: ADC(B);                break; //ADC B	    1 4    Z0HC	
                case 0x89: ADC(C);                break; //ADC C	    1 4    Z0HC	
                case 0x8A: ADC(D);                break; //ADC D	    1 4    Z0HC	
                case 0x8B: ADC(E);                break; //ADC E	    1 4    Z0HC	
                case 0x8C: ADC(H);                break; //ADC H	    1 4    Z0HC	
                case 0x8D: ADC(L);                break; //ADC L	    1 4    Z0HC	
                case 0x8E: ADC(mmu.readByte(HL)); break; //ADC M	    1 8    Z0HC	
                case 0x8F: ADC(A);                break; //ADC A	    1 4    Z0HC	

                case 0x90: SUB(B);                break; //SUB B	    1 4    Z1HC
                case 0x91: SUB(C);                break; //SUB C	    1 4    Z1HC
                case 0x92: SUB(D);                break; //SUB D	    1 4    Z1HC
                case 0x93: SUB(E);                break; //SUB E	    1 4    Z1HC
                case 0x94: SUB(H);                break; //SUB H	    1 4    Z1HC
                case 0x95: SUB(L);                break; //SUB L	    1 4    Z1HC
                case 0x96: SUB(mmu.readByte(HL)); break; //SUB M	    1 8    Z1HC
                case 0x97: SUB(A);                break; //SUB A	    1 4    Z1HC

                case 0x98: SBC(B);                break; //SBC B	    1 4    Z1HC
                case 0x99: SBC(C);                break; //SBC C	    1 4    Z1HC
                case 0x9A: SBC(D);                break; //SBC D	    1 4    Z1HC
                case 0x9B: SBC(E);                break; //SBC E	    1 4    Z1HC
                case 0x9C: SBC(H);                break; //SBC H	    1 4    Z1HC
                case 0x9D: SBC(L);                break; //SBC L	    1 4    Z1HC
                case 0x9E: SBC(mmu.readByte(HL)); break; //SBC M	    1 8    Z1HC
                case 0x9F: SBC(A);                break; //SBC A	    1 4    Z1HC

                case 0xA0: AND(B);                break; //AND B	    1 4    Z010
                case 0xA1: AND(C);                break; //AND C	    1 4    Z010
                case 0xA2: AND(D);                break; //AND D	    1 4    Z010
                case 0xA3: AND(E);                break; //AND E	    1 4    Z010
                case 0xA4: AND(H);                break; //AND H	    1 4    Z010
                case 0xA5: AND(L);                break; //AND L	    1 4    Z010
                case 0xA6: AND(mmu.readByte(HL)); break; //AND M	    1 8    Z010
                case 0xA7: AND(A);                break; //AND A	    1 4    Z010

                case 0xA8: XOR(B);                break; //XOR B	    1 4    Z000
                case 0xA9: XOR(C);                break; //XOR C	    1 4    Z000
                case 0xAA: XOR(D);                break; //XOR D	    1 4    Z000
                case 0xAB: XOR(E);                break; //XOR E	    1 4    Z000
                case 0xAC: XOR(H);                break; //XOR H	    1 4    Z000
                case 0xAD: XOR(L);                break; //XOR L	    1 4    Z000
                case 0xAE: XOR(mmu.readByte(HL)); break; //XOR M	    1 8    Z000
                case 0xAF: XOR(A);                break; //XOR A	    1 4    Z000

                case 0xB0: OR(B);                 break; //OR B     	1 4    Z000
                case 0xB1: OR(C);                 break; //OR C     	1 4    Z000
                case 0xB2: OR(D);                 break; //OR D     	1 4    Z000
                case 0xB3: OR(E);                 break; //OR E     	1 4    Z000
                case 0xB4: OR(H);                 break; //OR H     	1 4    Z000
                case 0xB5: OR(L);                 break; //OR L     	1 4    Z000
                case 0xB6: OR(mmu.readByte(HL));  break; //OR M     	1 8    Z000
                case 0xB7: OR(A);                 break; //OR A     	1 4    Z000

                case 0xB8: CP(B);                 break; //CP B     	1 4    Z1HC
                case 0xB9: CP(C);                 break; //CP C     	1 4    Z1HC
                case 0xBA: CP(D);                 break; //CP D     	1 4    Z1HC
                case 0xBB: CP(E);                 break; //CP E     	1 4    Z1HC
                case 0xBC: CP(H);                 break; //CP H     	1 4    Z1HC
                case 0xBD: CP(L);                 break; //CP L     	1 4    Z1HC
                case 0xBE: CP(mmu.readByte(HL));  break; //CP M     	1 8    Z1HC
                case 0xBF: CP(A);                 break; //CP A     	1 4    Z1HC

                case 0xC0: RETURN(mmu, !FlagZ);             break; //RET NZ	     1 20/8  ----
                case 0xC1: BC = POP(mmu);                   break; //POP BC      1 12    ----
                case 0xC2: JUMP(mmu, !FlagZ);               break; //JP NZ,A16   3 16/12 ----
                case 0xC3: JUMP(mmu, true);                 break; //JP A16      3 16    ----
                case 0xC4: CALL(mmu, !FlagZ);               break; //CALL NZ A16 3 24/12 ----
                case 0xC5: PUSH(mmu, BC);                   break; //PUSH BC     1 16    ----
                case 0xC6: ADD(mmu.readByte(PC)); PC += 1;  break; //ADD A,D8    2 8     Z0HC
                case 0xC7: RST(mmu, 0x0);                   break; //RST 0       1 16    ----

                case 0xC8: RETURN(mmu, FlagZ);              break; //RET Z       1 20/8  ----
                case 0xC9: RETURN(mmu, true);               break; //RET         1 16    ----
                case 0xCA: JUMP(mmu, FlagZ);                break; //JP Z,A16    3 16/12 ----
                case 0xCB: PREFIX_CB(mmu, mmu.readByte(PC++));      break; //PREFIX CB OPCODE TABLE
                case 0xCC: CALL(mmu, FlagZ);                break; //CALL Z,A16  3 24/12 ----
                case 0xCD: CALL(mmu, true);                 break; //CALL A16    3 24    ----
                case 0xCE: ADC(mmu.readByte(PC)); PC += 1;  break; //ADC A,D8    2 8     ----
                case 0xCF: RST(mmu, 0x8);                   break; //RST 1 08    1 16    ----

                case 0xD0: RETURN(mmu, !FlagC);             break; //RET NC      1 20/8  ----
                case 0xD1: DE = POP(mmu);                   break; //POP DE      1 12    ----
                case 0xD2: JUMP(mmu, !FlagC);               break; //JP NC,A16   3 16/12 ----
                //case 0xD3:                                break; //Illegal Opcode
                case 0xD4: CALL(mmu, !FlagC);               break; //CALL NC,A16 3 24/12 ----
                case 0xD5: PUSH(mmu, DE);                   break; //PUSH DE     1 16    ----
                case 0xD6: SUB(mmu.readByte(PC)); PC += 1;  break; //SUB D8      2 8     ----
                case 0xD7: RST(mmu, 0x10);                  break; //RST 2 10    1 16    ----

                case 0xD8: RETURN(mmu, FlagC);              break; //RET C       1 20/8  ----
                case 0xD9: RETURN(mmu, true); IME = true;   break; //RETI        1 16    ----
                case 0xDA: JUMP(mmu, FlagC);                break; //JP C,A16    3 16/12 ----
                //case 0xDB:                                break; //Illegal Opcode
                case 0xDC: CALL(mmu, FlagC);                break; //Call C,A16  3 24/12 ----
                //case 0xDD:                                break; //Illegal Opcode
                case 0xDE: SBC(mmu.readByte(PC)); PC += 1;  break; //SBC A,A8    2 8     Z1HC
                case 0xDF: RST(mmu, 0x18);                  break; //RST 3 18    1 16    ----

                case 0xE0: mmu.writeByte(combineRegs(0xFF, mmu.readByte(PC)), A); PC += 1;  break; //LDH (A8),A 2 12 ----
                case 0xE1: HL = POP(mmu);                   break; //POP HL      1 12    ----
                case 0xE2: mmu.writeByte(combineRegs(0xFF, mmu.readByte(C)), A);   break; //LD (C),A   1 8  ----
                //case 0xE3:                                break; //Illegal Opcode
                //case 0xE4:                                break; //Illegal Opcode
                case 0xE5: PUSH(mmu, HL);                   break; //PUSH HL     1 16    ----
                case 0xE6: AND(mmu.readByte(PC)); PC += 1;  break; //AND D8      2 8     Z010
                case 0xE7: RST(mmu, 0x20);                  break; //RST 4 20    1 16    ----

                case 0xE8: ADDSP(mmu.readByte(PC)); PC += 1;break; //ADD SP,R8   2 16    00HC
                case 0xE9: PC = mmu.readWord(HL);           break; //JP (HL)     1 4     ----
                case 0xEA: mmu.writeByte(mmu.readWord(PC), A); PC += 2;                     break; //LD (A16),A 3 16 ----
                //case 0xEB:                                break; //Illegal Opcode
                //case 0xEC:                                break; //Illegal Opcode
                //case 0xED:                                break; //Illegal Opcode
                case 0xEE: XOR(mmu.readByte(PC)); PC += 1;  break; //XOR D8      2 8     Z000
                case 0xEF: RST(mmu, 0x28);                  break; //RST 5 28    1 16    ----

                case 0xF0: A = mmu.readByte((ushort)(0xFF00 + mmu.readByte(PC))); PC += 1;  break; //LDH A,(A8)  2 12    ----
                case 0xF1: AF = POP(mmu);                   break; //POP AF      1 12    ZNHC
                case 0xF2: A = mmu.readByte((ushort)(0xFF00 + C));  break; //LD A,(C)    1 8     ----
                case 0xF3: IME = false;                      break; //DI          1 4     ----
                //case 0xF4:                                break; //Illegal Opcode
                case 0xF5: PUSH(mmu, AF);                   break; //PUSH AF     1 16    ----
                case 0xF6: OR(mmu.readByte(PC)); PC += 1;   break; //OR D8       2 8     Z000
                case 0xF7: RST(mmu, 0x30);                  break; //RST 6 30    1 16    ----

                case 0xF8: HL = (ushort)(SP + mmu.readByte(PC)); PC += 1; break; //LD HL,SP+R8 2 12    00HC <----- //TODO FALTAN FLAGS
                case 0xF9: SP = HL;                         break; //LD SP,HL    1 8     ----
                case 0xFA: A = mmu.readByte(mmu.readWord(PC)); PC += 2;   break; //LD A,(A16)  3 16    ----
                case 0xFB: IME = true;                      break; //IME          1 4     ----
                //case 0xFC:                                break; //Illegal Opcode
                //case 0xFD:                                break; //Illegal Opcode
                case 0xFE: CP(mmu.readByte(PC)); PC += 1;   break; //CP D8       2 8     Z1HC
                case 0xFF: RST(mmu, 0x38);                  break; //RST 7 38    1 16    ----

                default: warnUnsupportedOpcode(opcode);     break;
            }
            cycles += Cycles.Value[opcode];
            return cycles;
        }

        private void PREFIX_CB(MMU mmu, byte opcode) {
            switch (opcode) {
                case 0x00: B = RLC(B);                                  break; //RLC B    2   8   Z00C
                case 0x01: C = RLC(C);                                  break; //RLC C    2   8   Z00C
                case 0x02: D = RLC(D);                                  break; //RLC D    2   8   Z00C
                case 0x03: E = RLC(E);                                  break; //RLC E    2   8   Z00C
                case 0x04: H = RLC(H);                                  break; //RLC H    2   8   Z00C
                case 0x05: L = RLC(L);                                  break; //RLC L    2   8   Z00C
                case 0x06: mmu.writeByte(HL, RLC(mmu.readByte(HL)));    break; //RLC (HL) 2   8   Z00C
                case 0x07: A = RLC(A);                                  break; //RLC B    2   8   Z00C
                                                                        
                case 0x08: B = RRC(B);                                  break; //RRC B    2   8   Z00C
                case 0x09: C = RRC(C);                                  break; //RRC C    2   8   Z00C
                case 0x0A: D = RRC(D);                                  break; //RRC D    2   8   Z00C
                case 0x0B: E = RRC(E);                                  break; //RRC E    2   8   Z00C
                case 0x0C: H = RRC(H);                                  break; //RRC H    2   8   Z00C
                case 0x0D: L = RRC(L);                                  break; //RRC L    2   8   Z00C
                case 0x0E: mmu.writeByte(HL, RRC(mmu.readByte(HL)));    break; //RRC (HL) 2   8   Z00C
                case 0x0F: A = RRC(A);                                  break; //RRC B    2   8   Z00C
                                                                           
                case 0x10: B = RL(B);                                   break; //RL B     2   8   Z00C
                case 0x11: C = RL(C);                                   break; //RL C     2   8   Z00C
                case 0x12: D = RL(D);                                   break; //RL D     2   8   Z00C
                case 0x13: E = RL(E);                                   break; //RL E     2   8   Z00C
                case 0x14: H = RL(H);                                   break; //RL H     2   8   Z00C
                case 0x15: L = RL(L);                                   break; //RL L     2   8   Z00C
                case 0x16: mmu.writeByte(HL, RL(mmu.readByte(HL)));     break; //RL (HL)  2   8   Z00C
                case 0x17: A = RL(A);                                   break; //RL B     2   8   Z00C
                                                                                             
                case 0x18: B = RR(B);                                   break; //RR B     2   8   Z00C
                case 0x19: C = RR(C);                                   break; //RR C     2   8   Z00C
                case 0x1A: D = RR(D);                                   break; //RR D     2   8   Z00C
                case 0x1B: E = RR(E);                                   break; //RR E     2   8   Z00C
                case 0x1C: H = RR(H);                                   break; //RR H     2   8   Z00C
                case 0x1D: L = RR(L);                                   break; //RR L     2   8   Z00C
                case 0x1E: mmu.writeByte(HL, RR(mmu.readByte(HL)));     break; //RR (HL)  2   8   Z00C
                case 0x1F: A = RR(A);                                   break; //RR B     2   8   Z00C
                                                                           
                case 0x20: B = SLA(B);                                  break; //SLA B    2   8   Z00C
                case 0x21: C = SLA(C);                                  break; //SLA C    2   8   Z00C
                case 0x22: D = SLA(D);                                  break; //SLA D    2   8   Z00C
                case 0x23: E = SLA(E);                                  break; //SLA E    2   8   Z00C
                case 0x24: H = SLA(H);                                  break; //SLA H    2   8   Z00C
                case 0x25: L = SLA(L);                                  break; //SLA L    2   8   Z00C
                case 0x26: mmu.writeByte(HL, SLA(mmu.readByte(HL)));    break; //SLA (HL) 2   8   Z00C
                case 0x27: A = SLA(A);                                  break; //SLA B    2   8   Z00C
                                                                           
                case 0x28: B = SRA(B);                                  break; //SRA B    2   8   Z00C
                case 0x29: C = SRA(C);                                  break; //SRA C    2   8   Z00C
                case 0x2A: D = SRA(D);                                  break; //SRA D    2   8   Z00C
                case 0x2B: E = SRA(E);                                  break; //SRA E    2   8   Z00C
                case 0x2C: H = SRA(H);                                  break; //SRA H    2   8   Z00C
                case 0x2D: L = SRA(L);                                  break; //SRA L    2   8   Z00C
                case 0x2E: mmu.writeByte(HL, SRA(mmu.readByte(HL)));    break; //SRA (HL) 2   8   Z00C
                case 0x2F: A = SRA(A);                                  break; //SRA B    2   8   Z00C
                                                                          
                case 0x30: B = SWAP(B);                                 break; //SWAP B    2   8   Z00C
                case 0x31: C = SWAP(C);                                 break; //SWAP C    2   8   Z00C
                case 0x32: D = SWAP(D);                                 break; //SWAP D    2   8   Z00C
                case 0x33: E = SWAP(E);                                 break; //SWAP E    2   8   Z00C
                case 0x34: H = SWAP(H);                                 break; //SWAP H    2   8   Z00C
                case 0x35: L = SWAP(L);                                 break; //SWAP L    2   8   Z00C
                case 0x36: mmu.writeByte(HL, SWAP(mmu.readByte(HL)));   break; //SWAP (HL) 2   8   Z00C
                case 0x37: A = SWAP(A);                                 break; //SWAP B    2   8   Z00C
                                                                          
                case 0x38: B = SRL(B);                                  break; //SRL B    2   8   Z00C
                case 0x39: C = SRL(C);                                  break; //SRL C    2   8   Z00C
                case 0x3A: D = SRL(D);                                  break; //SRL D    2   8   Z00C
                case 0x3B: E = SRL(E);                                  break; //SRL E    2   8   Z00C
                case 0x3C: H = SRL(H);                                  break; //SRL H    2   8   Z00C
                case 0x3D: L = SRL(L);                                  break; //SRL L    2   8   Z00C
                case 0x3E: mmu.writeByte(HL, SRL(mmu.readByte(HL)));    break; //SRL (HL) 2   8   Z00C
                case 0x3F: A = SRL(A);                                  break; //SRL B    2   8   Z00C

                case 0x40: BIT(0x1, B);                                 break; //BIT B    2   8   Z01-
                case 0x41: BIT(0x1, C);                                 break; //BIT C    2   8   Z01-
                case 0x42: BIT(0x1, D);                                 break; //BIT D    2   8   Z01-
                case 0x43: BIT(0x1, E);                                 break; //BIT E    2   8   Z01-
                case 0x44: BIT(0x1, H);                                 break; //BIT H    2   8   Z01-
                case 0x45: BIT(0x1, L);                                 break; //BIT L    2   8   Z01-
                case 0x46: BIT(0x1, mmu.readByte(HL));                  break; //BIT (HL) 2   8   Z01-
                case 0x47: BIT(0x1, A);                                 break; //BIT B    2   8   Z01-

                case 0x48: BIT(0x2, B);                                break; //BIT B    2   8   Z01-
                case 0x49: BIT(0x2, C);                                break; //BIT C    2   8   Z01-
                case 0x4A: BIT(0x2, D);                                break; //BIT D    2   8   Z01-
                case 0x4B: BIT(0x2, E);                                break; //BIT E    2   8   Z01-
                case 0x4C: BIT(0x2, H);                                break; //BIT H    2   8   Z01-
                case 0x4D: BIT(0x2, L);                                break; //BIT L    2   8   Z01-
                case 0x4E: BIT(0x2, mmu.readByte(HL));                 break; //BIT (HL) 2   8   Z01-
                case 0x4F: BIT(0x2, A);                                break; //BIT B    2   8   Z01-
                                                                       
                case 0x50: BIT(0x4, B);                                break; //BIT B    2   8   Z01-
                case 0x51: BIT(0x4, C);                                break; //BIT C    2   8   Z01-
                case 0x52: BIT(0x4, D);                                break; //BIT D    2   8   Z01-
                case 0x53: BIT(0x4, E);                                break; //BIT E    2   8   Z01-
                case 0x54: BIT(0x4, H);                                break; //BIT H    2   8   Z01-
                case 0x55: BIT(0x4, L);                                break; //BIT L    2   8   Z01-
                case 0x56: BIT(0x4, mmu.readByte(HL));                 break; //BIT (HL) 2   8   Z01-
                case 0x57: BIT(0x4, A);                                break; //BIT B    2   8   Z01-

                case 0x58: BIT(0x8, B);                                break; //BIT B    2   8   Z01-
                case 0x59: BIT(0x8, C);                                break; //BIT C    2   8   Z01-
                case 0x5A: BIT(0x8, D);                                break; //BIT D    2   8   Z01-
                case 0x5B: BIT(0x8, E);                                break; //BIT E    2   8   Z01-
                case 0x5C: BIT(0x8, H);                                break; //BIT H    2   8   Z01-
                case 0x5D: BIT(0x8, L);                                break; //BIT L    2   8   Z01-
                case 0x5E: BIT(0x8, mmu.readByte(HL));                 break; //BIT (HL) 2   8   Z01-
                case 0x5F: BIT(0x8, A);                                break; //BIT B    2   8   Z01-

                case 0x60: BIT(0x10, B);                               break; //BIT B    2   8   Z01-
                case 0x61: BIT(0x10, C);                               break; //BIT C    2   8   Z01-
                case 0x62: BIT(0x10, D);                               break; //BIT D    2   8   Z01-
                case 0x63: BIT(0x10, E);                               break; //BIT E    2   8   Z01-
                case 0x64: BIT(0x10, H);                               break; //BIT H    2   8   Z01-
                case 0x65: BIT(0x10, L);                               break; //BIT L    2   8   Z01-
                case 0x66: BIT(0x10, mmu.readByte(HL));                break; //BIT (HL) 2   8   Z01-
                case 0x67: BIT(0x10, A);                               break; //BIT B    2   8   Z01-

                case 0x68: BIT(0x20, B);                               break; //BIT B    2   8   Z01-
                case 0x69: BIT(0x20, C);                               break; //BIT C    2   8   Z01-
                case 0x6A: BIT(0x20, D);                               break; //BIT D    2   8   Z01-
                case 0x6B: BIT(0x20, E);                               break; //BIT E    2   8   Z01-
                case 0x6C: BIT(0x20, H);                               break; //BIT H    2   8   Z01-
                case 0x6D: BIT(0x20, L);                               break; //BIT L    2   8   Z01-
                case 0x6E: BIT(0x20, mmu.readByte(HL));                break; //BIT (HL) 2   8   Z01-
                case 0x6F: BIT(0x20, A);                               break; //BIT B    2   8   Z01-

                case 0x70: BIT(0x40, B);                               break; //BIT B    2   8   Z01-
                case 0x71: BIT(0x40, C);                               break; //BIT C    2   8   Z01-
                case 0x72: BIT(0x40, D);                               break; //BIT D    2   8   Z01-
                case 0x73: BIT(0x40, E);                               break; //BIT E    2   8   Z01-
                case 0x74: BIT(0x40, H);                               break; //BIT H    2   8   Z01-
                case 0x75: BIT(0x40, L);                               break; //BIT L    2   8   Z01-
                case 0x76: BIT(0x40, mmu.readByte(HL));                break; //BIT (HL) 2   8   Z01-
                case 0x77: BIT(0x40, A);                               break; //BIT B    2   8   Z01-

                case 0x78: BIT(0x80, B);                               break; //BIT B    2   8   Z01-
                case 0x79: BIT(0x80, C);                               break; //BIT C    2   8   Z01-
                case 0x7A: BIT(0x80, D);                               break; //BIT D    2   8   Z01-
                case 0x7B: BIT(0x80, E);                               break; //BIT E    2   8   Z01-
                case 0x7C: BIT(0x80, H);                               break; //BIT H    2   8   Z01-
                case 0x7D: BIT(0x80, L);                               break; //BIT L    2   8   Z01-
                case 0x7E: BIT(0x80, mmu.readByte(HL));                break; //BIT (HL) 2   8   Z01-
                case 0x7F: BIT(0x80, A);                               break; //BIT B    2   8   Z01-

                case 0x80: B = RES(0x1, B);                               break; //RES B    2   8   ----
                case 0x81: C = RES(0x1, C);                               break; //RES C    2   8   ----
                case 0x82: D = RES(0x1, D);                               break; //RES D    2   8   ----
                case 0x83: E = RES(0x1, E);                               break; //RES E    2   8   ----
                case 0x84: H = RES(0x1, H);                               break; //RES H    2   8   ----
                case 0x85: L = RES(0x1, L);                               break; //RES L    2   8   ----
                case 0x86: mmu.writeByte(HL, RES(0x1, mmu.readByte(HL))); break; //RES (HL) 2   8   ----
                case 0x87: A = RES(0x1, A);                               break; //RES B    2   8   ----

                case 0x88: B = RES(0x2, B);                               break; //RES B    2   8   ----
                case 0x89: C = RES(0x2, C);                               break; //RES C    2   8   ----
                case 0x8A: D = RES(0x2, D);                               break; //RES D    2   8   ----
                case 0x8B: E = RES(0x2, E);                               break; //RES E    2   8   ----
                case 0x8C: H = RES(0x2, H);                               break; //RES H    2   8   ----
                case 0x8D: L = RES(0x2, L);                               break; //RES L    2   8   ----
                case 0x8E: mmu.writeByte(HL, RES(0x2, mmu.readByte(HL))); break; //RES (HL) 2   8   ----
                case 0x8F: A = RES(0x2, A);                               break; //RES B    2   8   ----

                case 0x90: B = RES(0x4, B);                               break; //RES B    2   8   ----
                case 0x91: C = RES(0x4, C);                               break; //RES C    2   8   ----
                case 0x92: D = RES(0x4, D);                               break; //RES D    2   8   ----
                case 0x93: E = RES(0x4, E);                               break; //RES E    2   8   ----
                case 0x94: H = RES(0x4, H);                               break; //RES H    2   8   ----
                case 0x95: L = RES(0x4, L);                               break; //RES L    2   8   ----
                case 0x96: mmu.writeByte(HL, RES(0x4, mmu.readByte(HL))); break; //RES (HL) 2   8   ----
                case 0x97: A = RES(0x4, A);                               break; //RES B    2   8   ----

                case 0x98: B = RES(0x8, B);                               break; //RES B    2   8   ----
                case 0x99: C = RES(0x8, C);                               break; //RES C    2   8   ----
                case 0x9A: D = RES(0x8, D);                               break; //RES D    2   8   ----
                case 0x9B: E = RES(0x8, E);                               break; //RES E    2   8   ----
                case 0x9C: H = RES(0x8, H);                               break; //RES H    2   8   ----
                case 0x9D: L = RES(0x8, L);                               break; //RES L    2   8   ----
                case 0x9E: mmu.writeByte(HL, RES(0x8, mmu.readByte(HL))); break; //RES (HL) 2   8   ----
                case 0x9F: A = RES(0x8, A);                               break; //RES B    2   8   ----

                case 0xA0: B = RES(0x10, B);                               break; //RES B    2   8   ----
                case 0xA1: C = RES(0x10, C);                               break; //RES C    2   8   ----
                case 0xA2: D = RES(0x10, D);                               break; //RES D    2   8   ----
                case 0xA3: E = RES(0x10, E);                               break; //RES E    2   8   ----
                case 0xA4: H = RES(0x10, H);                               break; //RES H    2   8   ----
                case 0xA5: L = RES(0x10, L);                               break; //RES L    2   8   ----
                case 0xA6: mmu.writeByte(HL, RES(0x10, mmu.readByte(HL))); break; //RES (HL) 2   8   ----
                case 0xA7: A = RES(0x10, A);                               break; //RES B    2   8   ----

                case 0xA8: B = RES(0x20, B);                               break; //RES B    2   8   ----
                case 0xA9: C = RES(0x20, C);                               break; //RES C    2   8   ----
                case 0xAA: D = RES(0x20, D);                               break; //RES D    2   8   ----
                case 0xAB: E = RES(0x20, E);                               break; //RES E    2   8   ----
                case 0xAC: H = RES(0x20, H);                               break; //RES H    2   8   ----
                case 0xAD: L = RES(0x20, L);                               break; //RES L    2   8   ----
                case 0xAE: mmu.writeByte(HL, RES(0x20, mmu.readByte(HL))); break; //RES (HL) 2   8   ----
                case 0xAF: A = RES(0x20, A);                               break; //RES B    2   8   ----

                case 0xB0: B = RES(0x40, B);                               break; //RES B    2   8   ----
                case 0xB1: C = RES(0x40, C);                               break; //RES C    2   8   ----
                case 0xB2: D = RES(0x40, D);                               break; //RES D    2   8   ----
                case 0xB3: E = RES(0x40, E);                               break; //RES E    2   8   ----
                case 0xB4: H = RES(0x40, H);                               break; //RES H    2   8   ----
                case 0xB5: L = RES(0x40, L);                               break; //RES L    2   8   ----
                case 0xB6: mmu.writeByte(HL, RES(0x40, mmu.readByte(HL))); break; //RES (HL) 2   8   ----
                case 0xB7: A = RES(0x40, A);                               break; //RES B    2   8   ----

                case 0xB8: B = RES(0x80, B);                               break; //RES B    2   8   ----
                case 0xB9: C = RES(0x80, C);                               break; //RES C    2   8   ----
                case 0xBA: D = RES(0x80, D);                               break; //RES D    2   8   ----
                case 0xBB: E = RES(0x80, E);                               break; //RES E    2   8   ----
                case 0xBC: H = RES(0x80, H);                               break; //RES H    2   8   ----
                case 0xBD: L = RES(0x80, L);                               break; //RES L    2   8   ----
                case 0xBE: mmu.writeByte(HL, RES(0x80, mmu.readByte(HL))); break; //RES (HL) 2   8   ----
                case 0xBF: A = RES(0x80, A);                               break; //RES B    2   8   ----

                case 0xC0: B = SET(0x1, B);                               break; //SET B    2   8   ----
                case 0xC1: C = SET(0x1, C);                               break; //SET C    2   8   ----
                case 0xC2: D = SET(0x1, D);                               break; //SET D    2   8   ----
                case 0xC3: E = SET(0x1, E);                               break; //SET E    2   8   ----
                case 0xC4: H = SET(0x1, H);                               break; //SET H    2   8   ----
                case 0xC5: L = SET(0x1, L);                               break; //SET L    2   8   ----
                case 0xC6: mmu.writeByte(HL, SET(0x1, mmu.readByte(HL))); break; //SET (HL) 2   8   ----
                case 0xC7: A = SET(0x1, A);                               break; //SET B    2   8   ----

                case 0xC8: B = SET(0x2, B);                               break; //SET B    2   8   ----
                case 0xC9: C = SET(0x2, C);                               break; //SET C    2   8   ----
                case 0xCA: D = SET(0x2, D);                               break; //SET D    2   8   ----
                case 0xCB: E = SET(0x2, E);                               break; //SET E    2   8   ----
                case 0xCC: H = SET(0x2, H);                               break; //SET H    2   8   ----
                case 0xCD: L = SET(0x2, L);                               break; //SET L    2   8   ----
                case 0xCE: mmu.writeByte(HL, SET(0x2, mmu.readByte(HL))); break; //SET (HL) 2   8   ----
                case 0xCF: A = SET(0x2, A);                               break; //SET B    2   8   ----

                case 0xD0: B = SET(0x4, B);                               break; //SET B    2   8   ----
                case 0xD1: C = SET(0x4, C);                               break; //SET C    2   8   ----
                case 0xD2: D = SET(0x4, D);                               break; //SET D    2   8   ----
                case 0xD3: E = SET(0x4, E);                               break; //SET E    2   8   ----
                case 0xD4: H = SET(0x4, H);                               break; //SET H    2   8   ----
                case 0xD5: L = SET(0x4, L);                               break; //SET L    2   8   ----
                case 0xD6: mmu.writeByte(HL, SET(0x4, mmu.readByte(HL))); break; //SET (HL) 2   8   ----
                case 0xD7: A = SET(0x4, A);                               break; //SET B    2   8   ----

                case 0xD8: B = SET(0x8, B);                               break; //SET B    2   8   ----
                case 0xD9: C = SET(0x8, C);                               break; //SET C    2   8   ----
                case 0xDA: D = SET(0x8, D);                               break; //SET D    2   8   ----
                case 0xDB: E = SET(0x8, E);                               break; //SET E    2   8   ----
                case 0xDC: H = SET(0x8, H);                               break; //SET H    2   8   ----
                case 0xDD: L = SET(0x8, L);                               break; //SET L    2   8   ----
                case 0xDE: mmu.writeByte(HL, SET(0x8, mmu.readByte(HL))); break; //SET (HL) 2   8   ----
                case 0xDF: A = SET(0x8, A);                               break; //SET B    2   8   ----

                case 0xE0: B = SET(0x10, B);                               break; //SET B    2   8   ----
                case 0xE1: C = SET(0x10, C);                               break; //SET C    2   8   ----
                case 0xE2: D = SET(0x10, D);                               break; //SET D    2   8   ----
                case 0xE3: E = SET(0x10, E);                               break; //SET E    2   8   ----
                case 0xE4: H = SET(0x10, H);                               break; //SET H    2   8   ----
                case 0xE5: L = SET(0x10, L);                               break; //SET L    2   8   ----
                case 0xE6: mmu.writeByte(HL, SET(0x10, mmu.readByte(HL))); break; //SET (HL) 2   8   ----
                case 0xE7: A = SET(0x10, A);                               break; //SET B    2   8   ----

                case 0xE8: B = SET(0x20, B);                               break; //SET B    2   8   ----
                case 0xE9: C = SET(0x20, C);                               break; //SET C    2   8   ----
                case 0xEA: D = SET(0x20, D);                               break; //SET D    2   8   ----
                case 0xEB: E = SET(0x20, E);                               break; //SET E    2   8   ----
                case 0xEC: H = SET(0x20, H);                               break; //SET H    2   8   ----
                case 0xED: L = SET(0x20, L);                               break; //SET L    2   8   ----
                case 0xEE: mmu.writeByte(HL, SET(0x20, mmu.readByte(HL))); break; //SET (HL) 2   8   ----
                case 0xEF: A = SET(0x20, A);                               break; //SET B    2   8   ----

                case 0xF0: B = SET(0x40, B);                               break; //SET B    2   8   ----
                case 0xF1: C = SET(0x40, C);                               break; //SET C    2   8   ----
                case 0xF2: D = SET(0x40, D);                               break; //SET D    2   8   ----
                case 0xF3: E = SET(0x40, E);                               break; //SET E    2   8   ----
                case 0xF4: H = SET(0x40, H);                               break; //SET H    2   8   ----
                case 0xF5: L = SET(0x40, L);                               break; //SET L    2   8   ----
                case 0xF6: mmu.writeByte(HL, SET(0x40, mmu.readByte(HL))); break; //SET (HL) 2   8   ----
                case 0xF7: A = SET(0x40, A);                               break; //SET B    2   8   ----

                case 0xF8: B = SET(0x80, B);                               break; //SET B    2   8   ----
                case 0xF9: C = SET(0x80, C);                               break; //SET C    2   8   ----
                case 0xFA: D = SET(0x80, D);                               break; //SET D    2   8   ----
                case 0xFB: E = SET(0x80, E);                               break; //SET E    2   8   ----
                case 0xFC: H = SET(0x80, H);                               break; //SET H    2   8   ----
                case 0xFD: L = SET(0x80, L);                               break; //SET L    2   8   ----
                case 0xFE: mmu.writeByte(HL, SET(0x80, mmu.readByte(HL))); break; //SET (HL) 2   8   ----
                case 0xFF: A = SET(0x80, A);                               break; //SET B    2   8   ----

                default: warnUnsupportedOpcode(opcode); break;
            }
            cycles += Cycles.CBValue(opcode);
        }

        private byte SET(byte b, byte reg) {//----
            return (byte)(reg | b);
        }

        private byte RES(int b, byte reg) {//----
            return (byte)(reg & ~b);
        }

        private void BIT(byte b, byte reg) {//Z01-
            FlagZ = (reg & b) == 0;
            FlagN = false;
            FlagH = true;
        }

        private byte SRL(byte b) {//Z00C
            byte result = (byte)(b >> 1);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x1) != 0;
            return result;
        }

        private byte SWAP(byte b) {//Z00C
            return (byte)(b & 0xF0 >> 4 | b & 0x0F << 4);
        }

        private byte SRA(byte b) {//Z00C
            byte result = (byte)((b >> 1) | ( b & 0x80));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x1) != 0;
            return result;
        }

        private byte SLA(byte b) {//Z00C
            byte result = (byte)(b << 1);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x80) != 0;
            return result;
        }

        private byte RR(byte b) {//Z00C
            bool prevC = FlagC;
            byte result = (byte)((b >> 1) | (prevC ? 0x80 : 0));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x1) != 0;
            return result;
        }

        private byte RL(byte b) {//Z00C
            bool prevC = FlagC;
            byte result = (byte)((b << 1) | (prevC ? 1 : 0));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x80) != 0;
            return result;
        }

        private byte RRC(byte b) {//Z00C
            byte result = (byte)((b >> 1) | (b << 7));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x1) != 0;
            return result;
        }

        private byte RLC(byte b) {//Z00C
            byte result = (byte)((b << 1) | (b >> 7));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x80) != 0;
            return result;
        }

        private void ADDSP(byte b) {//00HC
            int result = SP + b;
            FlagZ = false;
            FlagN = false;
            //SetFlagH(SP, b); <-- Special
            SetFlagC(result);
            SP = (ushort)result;
        }

        private void JR(MMU mmu, bool flag) {
            if (flag) {
                sbyte sb = (sbyte)mmu.readByte(PC);
                PC = (ushort)(PC + sb);
                PC += 1; //<---- //TODO WHAT?
                cycles += Cycles.JUMP_RELATIVE_TRUE;
            } else {
                PC += 1;
                cycles += Cycles.JUMP_RELATIVE_FALSE;
            }
        }

        private void STOP() {
            throw new NotImplementedException();
        }

        private byte INC(byte b) { //Z0H-
            int result = b + 1;
            SetFlagZ(result);
            FlagN = false;
            SetFlagH(b, 1);
            return (byte)result;
        }

        private byte DEC(byte b) { //Z1H-
            int result = b - 1;
            SetFlagZ(result);
            FlagN = true;
            SetFlagHSub(b, 1);
            return (byte)result;
        }

        private void ADD(byte b) { //Z0HC
            int result = A + b;
            SetFlagZ(result);
            FlagN = false;
            SetFlagH(A, b);
            SetFlagC(result);
            A = (byte)result;
        }

        private void ADC(byte b) { //Z0HC
            int carry = FlagC ? 1 : 0;
            int result = A + b + carry;
            SetFlagZ(result);
            FlagN = false;
            if (FlagC) SetFlagHCarry(A, b);
            else SetFlagH(A, b);
            SetFlagC(result);
            A = (byte)result;
        }

        private void SUB(byte b) {//Z1HC
            int result = A - b;
            SetFlagZ(result);
            FlagN = true;
            SetFlagHSub(A, b);
            SetFlagC(result);
            A = (byte)result;
        }

        private void SBC(byte b) {//Z1HC
            int carry = FlagC ? 1 : 0;
            int result = A - b - carry;
            SetFlagZ(result);
            FlagN = true;
            if (FlagC) SetFlagHSubCarry(A, b);
            else SetFlagHSub(A, b);
            SetFlagC(result);
            A = (byte)result;
        }

        private void AND(byte b) {//Z010
            byte result = (byte)(A & b);
            SetFlagZ(result);
            FlagN = false;
            FlagH = true;
            FlagC = false;
            A = result;
        }

        private void XOR(byte b) {//Z000
            byte result = (byte)(A ^ b);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            A = result;
        }

        private void OR(byte b) {//Z000
            byte result = (byte)(A | b);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            A = result;
        }

        private void CP(byte b) {//Z1HC
            int result = A - b;
            SetFlagZ(result);
            FlagN = true;
            SetFlagHSub(A, b);
            SetFlagC(result);
        }

        private void DAD(ushort w) { //-0HC
            int result = HL + w;
            FlagN = false;
            SetFlagH((byte)HL, (byte)w); //<--- //TODO this is probably wrong
            FlagC = result >> 16 != 0; //Special FlagC as short value involved
            HL = (ushort)result;
        }

        private void RETURN(MMU mmu, bool flag) {
            if (flag) {
                PC = POP(mmu);
                cycles += Cycles.RETURN_TRUE;
            } else {
                cycles += Cycles.RETURN_FALSE;
            }
        }

        private void CALL(MMU mmu, bool flag) {
            if (flag) {
                PUSH(mmu, (ushort)(PC + 2));
                PC = mmu.readWord(PC);
                cycles += Cycles.CALL_TRUE;
            } else {
                PC += 2;
                cycles += Cycles.CALL_FALSE;
            }
        }

        private void JUMP(MMU mmu, bool flag) {
            if (flag) {
                PC = mmu.readWord(PC);
                cycles += Cycles.JUMP_TRUE;
            } else {
                PC += 2;
                cycles += Cycles.JUMP_FALSE;
            }
        }

        private void RST(MMU mmu, byte b) {
            PUSH(mmu, (ushort)(PC + 2));
            PC = b;
        }

        public void ExecuteInterrupt(MMU mmu, byte b) {
            if (IME) {
                PUSH(mmu, PC);
                PC = (ushort)(0x40 + (8 * b));
                IME = false;
                if(b != 0) {
                    Console.WriteLine("CPU: INTERRUPT EXECUTED " + PC.ToString("x4"));
                    Console.ReadLine();
                }

            }
        }

        private void PUSH(MMU mmu, ushort w) {// (SP - 1) < -PC.hi; (SP - 2) < -PC.lo
            SP -= 2;
            mmu.writeWord(SP, w);
            //Console.WriteLine("stack PUSH " + w.ToString("x4"));
            //Console.WriteLine("whats on mem: " + mmu.readWord(SP).ToString("x4"));
            //Console.WriteLine("stack PUSH = " + d16.ToString("x4") + " SP = " + SP.ToString("x4") + " value" + BitConverter.ToUInt16(memory, SP).ToString("x4"));
        }

        private ushort POP(MMU mmu) {
            ushort ret = mmu.readWord(SP);
            //ushort ret = combineRegs(memory[SP + 1], memory[SP]);
            //Console.WriteLine("stack POP = " + ret.ToString("x4") + " SP = " + SP.ToString("x4") + " value" + mmu.readWord(SP).ToString("x4"));
            SP += 2;
            return ret;
        }

        private ushort combineRegs(byte b1, byte b2) {
            return (ushort)(b1 << 8 | b2);
        }

        private void SetFlagZ(int b) {
            FlagZ = (byte)b == 0;
        }

        private void SetFlagC(int i) {
            FlagC = (i >> 8) != 0;
        }

        private void SetFlagH(byte b1, byte b2) {
            FlagH = ((b1 & 0xF) + (b2 & 0xF)) > 0xF;
        }

        private void SetFlagHCarry(byte b1, byte b2) {
            FlagH = ((b1 & 0xF) + (b2 & 0xF)) >= 0xF;
        }

        private void SetFlagHSub(byte b1, byte b2) {
            FlagH = (b2 & 0xF) <= (b1 & 0xF);
        }

        private void SetFlagHSubCarry(byte b1, byte b2) {
            FlagH = (b2 & 0xF) < (b1 & 0xF);
        }

        private void warnUnsupportedOpcode(byte opcode) {
            Console.WriteLine((PC - 1).ToString("x4") + " Unsupported operation " + opcode.ToString("x2"));
        }

        public int dev;
        private void debug(MMU mmu, byte opcode) {
            dev += cycles;
            if (dev >= 23869288) //0x100
                Console.WriteLine("cycle" + dev + " " + (PC - 1).ToString("x4") + " " + SP.ToString("x4") + " AF: " + A.ToString("x2") + "" + F.ToString("x2")
                    + " BC: " + B.ToString("x2") + "" + C.ToString("x2") + " DE: " + D.ToString("x2") + "" + E.ToString("x2") + " HL: " + H.ToString("x2") + "" + L.ToString("x2")
                    + " op " + opcode.ToString("x2") + " next16 " + mmu.readWord(PC).ToString("x4"));
        }


    }
}