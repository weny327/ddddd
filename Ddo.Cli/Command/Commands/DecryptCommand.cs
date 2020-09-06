﻿using System;
using Arrowgene.Services.Buffers;
using Ddo.Cli.Argument;
using Ddo.Server.Common;
using Ddo.Server.Crypto;
using Buffer = Arrowgene.Services.Buffers.Buffer;

namespace Ddo.Cli.Command.Commands
{
    public class DecryptCommand : ConsoleCommand
    {
        private static void DumpBuffer(IBuffer buffer)
        {
            int pos = buffer.Position;
            buffer.SetPositionStart();
            Console.WriteLine(Util.ToAsciiString(buffer.GetAllBytes(), true));
            while (buffer.Size > buffer.Position)
            {
                byte[] row = buffer.ReadBytes(16);
                Console.WriteLine(Util.ToHexString(row, ' '));
            }

            buffer.Position = pos;
        }

        public override CommandResultType Handle(ConsoleParameter parameter)
        {
            string data1 =
                "0130b71a9b05ff4c4f2cbfc63bbf8b955587b1d42764a984c71c5b710fd4b351e98ebc90d3be3dc9d49ebfb981b7c4f01b0b3944f294f0a114b35b44bb24084ee16471d1f4d9d13c784f434af0ef35f17505557a4a0a5c1a25b8013cee0bb55d4645effa115785c5e480e84ffc32f82c9c1f2f3e658723ba2794c238cd5f51655c5d64ba2f3ccf2fb7ea43546f9aa87122e9c6e9e85598e0c8926d13f5ef1481a47a5fbeb9f34978337b0c475d2a730f3370306275b02d1c456633e3180a6c3734338b1dbdc68c21a9039e3c8c8d2634147641f6a7aacf88f3df1bd439517d82c9d53f6ac1fd69549357963e0f4762390c64674009c10dee3f2fca1b415bef5f0bec821f794e9a6a6917e61d600977e8945cf899a803c975048b3faf20021a839e163169ad17d3270b7e2986bd237fb53209";

            string data2 =
                "0150066b608643985002409e7be9541a39c4658cde84ce0f290dd91ebc1c7d4054864bfcaa80279c06d06b149f350c320029f8329c6e837b07608d4bd7aa13ecea46b77f3673e05da5a4b6877f27922701d333c89f10171c3b5f9482924f38b572ef68a598323f5091d0b1d572678437bd10f1dad2b112decf9c801f193188c0cd7c881918220bd851d8372c73f87b66e78462792e53d2cc5868e768941b6011fe9d65300b8841153afb06e73e9419f057656c9f30e836c17e0489537cdf91d17bf6a8d24e889a6b8c414a456d06410bca38a15aba22109841d523911b716ee7eca07970e1392857dc646d9adfecb26d0f877cd19cd27857aaf098cd73d0cf18597286eb6b06c1e4a9934046e369d8c619d3af94867aa106789d43ef76427dbaaf728d06d668b7e252ca95bfa6f99c5f515d7334218a51a761eb7b25977c574f8ce878daeac7be1945b1c00f954b5cab9810";


            string data3 =
                "0150BC0E48543D92BE6DF03E8B73EDC7D5F5D8775C5150864BFAD79AADE10E0E88F66DB61E47678F66506E06297463D637FEB1671039B4E90791F6439248B9633316D4FA8DC191D314F43339685ACD6B568EC169FE58BAAF447743A40A003B8BFC5E82BC5C0CDE6490DCE6C5062DA8FD43EB837CE5EA439D06AF6467690ECBA512C4484A3CF48C2DC3E9578A2CB729138843EE83BBB4BBF3BEAE284F95A14A372B127D7918476439CE3743ED0F49A7C73E1494311B594703BE42C9B51A8C0612E0F80B74E06723F76BB7CD7FABF81D0947BA410FF63C2445D570C67FCBC47434E2F22A792574E80429C93BBC5B0D01EAB00682DC74C299E260D944083B4A6EB8D8B77874F023DD825DE2F96A2DFE777539D9D1E45C02612DA64EA10A11DE8706009ECFFF0B84FE7D538B312D022BE6768C7F3798F637914A04EB62C3FD5F7D78CCCAB48FC002273DBC263C3A803F58830993";
            byte[] decrypted = Decrypt(Util.FromHexString(data2));


            return CommandResultType.Exit;
        }

        public override string Key => "decrypt";
        public override string Description => "Decrypt packet data";

        private byte[] Decrypt(byte[] input)
        {
            GenerateKey(RootCryptoKey);


            DdoPacketCrypto crypto = new DdoPacketCrypto();

            byte[] decrypted = crypto.Decrypt(input);
            IBuffer output = new StreamBuffer(decrypted);

            Console.WriteLine("Decrypted:");
            DumpBuffer(output);
            Console.WriteLine();

            // Inflate output for hash calculation
            IBuffer inflated = Inflate(output);
            Console.WriteLine("Inflated:");
            DumpBuffer(inflated);
            Console.WriteLine();

            // Parse expected Hash
            IBuffer hashBytes = output.Clone(272, 20);
            byte[] expectedHash = ReadHash(hashBytes.GetAllBytes());
            Console.WriteLine("Expected Hash:");
            DumpBuffer(new StreamBuffer(expectedHash));
            Console.WriteLine();

            // Calculate hash
            DdoPacketHash ddoPacketHash = new DdoPacketHash();
            byte[] calculatedHash = ddoPacketHash.ComputeHash(inflated.GetAllBytes());
            Console.WriteLine("Calculated Hash:");
            DumpBuffer(new StreamBuffer(calculatedHash));
            Console.WriteLine();

            // Reverse decrypted
            Console.WriteLine("Reversed:");
            byte[] reversed = output.GetBytes(0, 256);
            Array.Reverse(reversed);
            DumpBuffer(new StreamBuffer(reversed));
            Console.WriteLine();

            // Transform reversed
            byte[] transformed = new byte[reversed.Length];
            uint c = 0x204D276C;
            for (int i = 0; i < reversed.Length; i++)
            {
                uint current = reversed[i];
                uint j = (uint) (i % 4);
                uint j1 = j << 3; // 0369164A | C1E2 03 | shl edx,3 
                byte j2 = (byte) j1;
                uint j3 = (uint) (0xFF << j2);
                uint j4 = c & j3;
                uint j6 = j << 3;
                uint j7 = j4 >> (int) j6;
                byte res = (byte) (j7 ^ current);
                transformed[i] = res;
            }

            Console.WriteLine("Transformed:");
            DumpBuffer(new StreamBuffer(transformed));
            Console.WriteLine();

            // Reversed
            // 0x85 (*4) - size? = 0x3F
            // 0x3F


            return output.GetAllBytes();
        }


        private byte[] GenerateKey(byte[] inputBytes)
        {
            IBuffer input = new StreamBuffer(inputBytes);
            input.SetPositionStart();
            IBuffer output = new StreamBuffer();
            output.SetPositionStart();


            IBuffer out5 = new StreamBuffer();
            out5.SetPositionStart();
            out5.WriteBytes(input.GetBytes(16,16));
            
            for (int i = 0; i < 4; i++)
            {
                uint a = input.ReadUInt32();
                uint b = input.GetUInt32((i * 4) + 16);
                uint c = a ^ b;
                output.WriteInt32(c);
            }

            DumpBuffer(output);
            byte[] outp = output.GetAllBytes();
            GenerateA(0, outp, 0);
            DumpBuffer(new StreamBuffer(outp));
            GenerateA(8, outp, 8);
            DumpBuffer(new StreamBuffer(outp));


            StreamBuffer sbi = new StreamBuffer(outp);
            IBuffer output2 = new StreamBuffer();
            sbi.SetPositionStart();
            input.Position = 0;
            for (int i = 0; i < 4; i++)
            {
                uint a = input.ReadUInt32();
                uint b = sbi.ReadUInt32();
                uint c = a ^ b;
                output2.WriteInt32(c);
            }

            DumpBuffer(output2);
            outp = output2.GetAllBytes();
            GenerateA(16, outp, 0);
            DumpBuffer(new StreamBuffer(outp));
            GenerateA(24, outp, 8);
            DumpBuffer(new StreamBuffer(outp));

            out5.WriteBytes(outp);

            byte[] outpO = new StreamBuffer(outp).GetAllBytes();
            byte[] outp1 = new StreamBuffer(outp).GetAllBytes();

            // 0355EAEF | 0FB64C24 0C | movzx ecx,byte ptr ss:[esp+C]
            for (int i = 0; i < 4; i++)
            {
                outp[(i * 4) + 0] = inputBytes[(i * 4) + 3];
                outp[(i * 4) + 1] = inputBytes[(i * 4) + 2];
                outp[(i * 4) + 2] = inputBytes[(i * 4) + 1];
                outp[(i * 4) + 3] = inputBytes[(i * 4) + 0];
            }

            DumpBuffer(new StreamBuffer(outp));

            for (int i = 0; i < 4; i++)
            {
                outpO[(i * 4) + 0] = outp1[(i * 4) + 3];
                outpO[(i * 4) + 1] = outp1[(i * 4) + 2];
                outpO[(i * 4) + 2] = outp1[(i * 4) + 1];
                outpO[(i * 4) + 3] = outp1[(i * 4) + 0];
            }

            DumpBuffer(new StreamBuffer(outpO));
            
            
            IBuffer output1 = new StreamBuffer();
            output1.SetPositionStart();
            DumpBuffer(out5);
            out5.SetPositionStart();
            for (int i = 0; i < 4; i++)
            {
                uint a = out5.ReadUInt32();
                uint b = out5.GetUInt32((i * 4) + 16);
                uint c = a ^ b;
                output1.WriteInt32(c);
            }
            DumpBuffer(output1);

            
            
            
            
            // 0355EC27   | 81FE 80000000         | cmp esi,80 

            
            
            
            

            return output.GetAllBytes();
        }

        private void GenerateA(int lookupIdx, byte[] outp, int outpIdx)
        {
            // 0343B8F6 | 0FB60B | movzx ecx,byte ptr ds:[ebx] | keygen - 1st byte

            byte a1 = outp[outpIdx + 0];
            byte b1 = (byte) (a1 ^ K_20DBD30[lookupIdx + 0]); // 02334600  | 0FB607 | movzx eax,byte ptr ds:[edi]
            byte c1 = K_2179DD0[b1]; // 0349A933  | 8A81 D09D1702 | mov al,byte ptr ds:[ecx+2179DD0]
            //0349A93D  | 884424 14 | mov byte ptr ss:[esp+14],al

            byte d1 = outp[outpIdx + 1];
            byte e1 = (byte) (d1 ^ K_20DBD30[lookupIdx + 1]);
            byte f1 = K_2179DD0[e1];
            byte g1 = (byte) (f1 >> 0x07);
            byte h1 = (byte) (f1 + f1); // 0363E1A | 02C0 | add al,al
            byte i1 = (byte) (g1 ^ h1);
            // 0256B5DA  | 884C24 0F | mov byte ptr ss:[esp+F],cl

            byte j1 = outp[outpIdx + 2];
            byte k1 = (byte) (j1 ^ K_20DBD30[lookupIdx + 2]);
            byte l1 = K_2179DD0[k1];
            byte m1 = (byte) (l1 >> 0x01);
            byte n1 = (byte) (l1 << 0x07);
            byte o1 = (byte) (m1 ^ n1);
            // 03B15411  | 884C24 0D | mov byte ptr ss:[esp+D],cl

            byte p1 = outp[outpIdx + 3]; // 0366CCAA  | C1EA 07 | shr edx,7
            byte q1 = (byte) (p1 >> 0x07); // 0366CCAA  | C1EA 07 | shr edx,7
            byte r1 = (byte) (K_20DBD30[lookupIdx + 3] >> 0x07); // 03A56D87  | C1F8 07 | sar eax,7 
            byte s1 = (byte) (q1 ^ r1); // 0251E753 | 33D0 | xor edx,eax
            byte t1 = (byte) (p1 + p1);

            byte u1 = outp[outpIdx + 4];
            byte v1 = (byte) (s1 ^ t1);
            byte w1 = (byte) (v1 & 0xFF);
            byte x1 = (byte) (K_20DBD30[lookupIdx + 3] & 0x7F);
            byte y1 = (byte) (x1 + x1);
            byte z1 = (byte) (w1 ^ y1);
            byte a2 = K_2179DD0[z1]; // 023B6F89  | 8A82 D09D1702 | mov al,byte ptr ds:[edx+2179DD0]
            // 023B6F8F  | 884424 0C | mov byte ptr ss:[esp+C],al

            byte b2 = (byte) (u1 ^ K_20DBD30[lookupIdx + 4]); // 02EC4FE1  | 33C8 | xor ecx,eax 
            byte c2 = K_2179DD0[b2];
            byte d2 = (byte) (c2 >> 0x07);
            byte e2 = (byte) (c2 + c2);
            byte f2 = (byte) (d2 ^ e2);
            // 03523162  | 884C24 0E | mov byte ptr ss:[esp+E],cl

            byte g2 = outp[outpIdx + 5];
            byte h2 = (byte) (g2 ^ K_20DBD30[lookupIdx + 5]);
            byte i2 = K_2179DD0[h2];
            byte j2 = (byte) (i2 >> 0x01);
            byte k2 = (byte) (i2 << 0x07);
            byte l2 = (byte) (j2 ^ k2);
            // 03441B02  | 884C24 18 | mov byte ptr ss:[esp+18],cl 

            byte m2 = outp[outpIdx + 6];
            byte n2 = (byte) (m2 >> 0x07);
            byte o2 = (byte) (K_20DBD30[lookupIdx + 6] >> 0x07);
            byte p2 = (byte) (n2 ^ o2);
            byte q2 = (byte) (m2 + m2);
            byte r2 = (byte) (p2 ^ q2);
            byte s2 = (byte) (K_20DBD30[lookupIdx + 6] & 0x7F);
            byte t2 = (byte) (r2 & 0xFF);
            byte u2 = (byte) (s2 + s2);
            byte v2 = (byte) (t2 ^ u2);
            byte w2 = K_2179DD0[v2];

            byte x2 = outp[outpIdx + 7]; // 03A21C2B  | 0FB64B 07 | movzx ecx,byte ptr ds:[ebx+7] 
            byte y2 = (byte) (x2 ^ K_20DBD30[lookupIdx + 7]);


            int idx = 0;
            if (outpIdx == 0)
            {
                idx = 8;
            }

            byte z2 = outp[idx + 0]; // 02519BA0  | 8A06 | mov al,byte ptr ds:[esi] 
            byte a3 = K_2179DD0[y2];
            byte b3 = (byte) (a3 ^ z2);
            byte c3 = (byte) (w2 ^ b3);
            byte d3 = (byte) (c3 ^ l2);
            byte e3 = (byte) (d3 ^ a2);
            byte f3 = (byte) (e3 ^ o1);
            byte g3 = (byte) (f3 ^ c1);
            outp[idx + 0] = g3; // 0409B4AC | 8806 | mov byte ptr ds:[esi],al

            byte h3 = (byte) (a3 ^ w2);
            byte i3 = (byte) (h3 ^ f2);
            byte j3 = (byte) (i3 ^ a2);
            byte k3 = (byte) (j3 ^ i1);
            byte l3 = (byte) (k3 ^ c1);
            byte m3 = (byte) (l3 ^ outp[idx + 1]); // 04041189  | 3046 01 | xor byte ptr ds:[esi+1],al
            outp[idx + 1] = m3;

            byte
                n3 = outp[idx + 2]; //0376CF65  | 8A46 02               | mov al,byte ptr ds:[esi+2                                     
            byte o3 = (byte) (a3 ^ n3);
            byte p3 = (byte) (l2 ^ o3);
            byte q3 = (byte) (f2 ^ p3);
            byte r3 = (byte) (q3 ^ o1);
            byte s3 = (byte) (r3 ^ i1);
            byte t3 = (byte) (s3 ^ c1);
            outp[idx + 2] = t3; // 036633FD  | 8A46 03 | mov al,byte ptr ds:[esi+3]  

            byte u3 = outp[idx + 3];
            byte v3 = (byte) (u3 ^ w2);
            byte w3 = (byte) (v3 ^ l2);
            byte x3 = (byte) (w3 ^ f2);
            byte y3 = (byte) (x3 ^ a2);
            byte z3 = (byte) (y3 ^ o1);
            byte a4 = (byte) (z3 ^ i1);
            outp[idx + 3] = a4;

            byte b4 = outp[idx + 4];
            byte c4 = (byte) (b4 ^ a3);
            byte d4 = (byte) (c4 ^ w2);
            byte e4 = (byte) (d4 ^ l2);
            byte f4 = (byte) (e4 ^ i1);
            byte g4 = (byte) (f4 ^ c1);
            outp[idx + 4] = g4;

            byte h4 = outp[idx + 5]; // 0364EFC2 | 8A46 05 | mov al,byte ptr ds:[esi+5] 
            byte i4 = (byte) (h4 ^ a3);
            byte j4 = (byte) (i4 ^ w2);
            byte k4 = (byte) (j4 ^ f2);
            byte l4 = (byte) (k4 ^ o1);
            byte m4 = (byte) (l4 ^ i1);
            outp[idx + 5] = m4;

            byte n4 = outp[idx + 6]; // 0251F2F0 | 8846 05 | mov byte ptr ds:[esi+5],al
            byte o4 = (byte) (n4 ^ a3);
            byte p4 = (byte) (o4 ^ l2);
            byte q4 = (byte) (p4 ^ f2);
            byte r4 = (byte) (q4 ^ a2);
            byte s4 = (byte) (r4 ^ o1);
            outp[idx + 6] = s4;

            byte t4 = outp[idx + 7];
            byte u4 = (byte) (t4 ^ w2);
            byte v4 = (byte) (u4 ^ l2);
            byte w4 = (byte) (v4 ^ f2);
            byte x4 = (byte) (w4 ^ a2);
            byte y4 = (byte) (x4 ^ c1);
            outp[idx + 7] = y4; // 0409A0E0 | 8846 07 | mov byte ptr ds:[esi+7],al
        }


        private static byte[] K_20DBD30 = new byte[]
        {
            0xA0, 0x9E, 0x66, 0x7F, 0x3B, 0xCC, 0x90, 0x8B, 0xB6, 0x7A, 0xE8, 0x58, 0x4C, 0xAA, 0x73, 0xB2,
            0xC6, 0xEF, 0x37, 0x2F, 0xE9, 0x4F, 0x82, 0xBE, 0x54, 0xFF, 0x53, 0xA5, 0xF1, 0xD3, 0x6F, 0x1C,
            0x10, 0xE5, 0x27, 0xFA, 0xDE, 0x68, 0x2D, 0x1D, 0xB0, 0x56, 0x88, 0xC2, 0xB3, 0xE6, 0xC1, 0xFD,
        };

        private static byte[] K_2179DD0 = new byte[]
        {
            0x70, 0x82, 0x2C, 0xEC, 0xB3, 0x27, 0xC0, 0xE5, 0xE4, 0x85, 0x57, 0x35, 0xEA, 0x0C, 0xAE, 0x41,
            0x23, 0xEF, 0x6B, 0x93, 0x45, 0x19, 0xA5, 0x21, 0xED, 0x0E, 0x4F, 0x4E, 0x1D, 0x65, 0x92, 0xBD,
            0x86, 0xB8, 0xAF, 0x8F, 0x7C, 0xEB, 0x1F, 0xCE, 0x3E, 0x30, 0xDC, 0x5F, 0x5E, 0xC5, 0x0B, 0x1A,
            0xA6, 0xE1, 0x39, 0xCA, 0xD5, 0x47, 0x5D, 0x3D, 0xD9, 0x01, 0x5A, 0xD6, 0x51, 0x56, 0x6C, 0x4D,
            0x8B, 0x0D, 0x9A, 0x66, 0xFB, 0xCC, 0xB0, 0x2D, 0x74, 0x12, 0x2B, 0x20, 0xF0, 0xB1, 0x84, 0x99,
            0xDF, 0x4C, 0xCB, 0xC2, 0x34, 0x7E, 0x76, 0x05, 0x6D, 0xB7, 0xA9, 0x31, 0xD1, 0x17, 0x04, 0xD7,
            0x14, 0x58, 0x3A, 0x61, 0xDE, 0x1B, 0x11, 0x1C, 0x32, 0x0F, 0x9C, 0x16, 0x53, 0x18, 0xF2, 0x22,
            0xFE, 0x44, 0xCF, 0xB2, 0xC3, 0xB5, 0x7A, 0x91, 0x24, 0x08, 0xE8, 0xA8, 0x60, 0xFC, 0x69, 0x50,
            0xAA, 0xD0, 0xA0, 0x7D, 0xA1, 0x89, 0x62, 0x97, 0x54, 0x5B, 0x1E, 0x95, 0xE0, 0xFF, 0x64, 0xD2,
            0x10, 0xC4, 0x00, 0x48, 0xA3, 0xF7, 0x75, 0xDB, 0x8A, 0x03, 0xE6, 0xDA, 0x09, 0x3F, 0xDD, 0x94,
            0x87, 0x5C, 0x83, 0x02, 0xCD, 0x4A, 0x90, 0x33, 0x73, 0x67, 0xF6, 0xF3, 0x9D, 0x7F, 0xBF, 0xE2,
            0x52, 0x9B, 0xD8, 0x26, 0xC8, 0x37, 0xC6, 0x3B, 0x81, 0x96, 0x6F, 0x4B, 0x13, 0xBE, 0x63, 0x2E,
            0xE9, 0x79, 0xA7, 0x8C, 0x9F, 0x6E, 0xBC, 0x8E, 0x29, 0xF5, 0xF9, 0xB6, 0x2F, 0xFD, 0xB4, 0x59,
            0x78, 0x98, 0x06, 0x6A, 0xE7, 0x46, 0x71, 0xBA, 0xD4, 0x25, 0xAB, 0x42, 0x88, 0xA2, 0x8D, 0xFA,
            0x72, 0x07, 0xB9, 0x55, 0xF8, 0xEE, 0xAC, 0x0A, 0x36, 0x49, 0x2A, 0x68, 0x3C, 0x38, 0xF1, 0xA4,
            0x40, 0x28, 0xD3, 0x7B, 0xBB, 0xC9, 0x43, 0xC1, 0x15, 0xE3, 0xAD, 0xF4, 0x77, 0xC7, 0x80, 0x9E
        };

        private IBuffer Inflate(IBuffer output)
        {
            IBuffer processedA = Process64(output.Clone(0, 64));
            IBuffer processedB = Process64(output.Clone(64, 64));
            IBuffer processedC = Process64(output.Clone(128, 64));
            IBuffer processedD = Process64(output.Clone(192, 64));

            // Inflate last bytes
            IBuffer lastRow = output.Clone(256, 16);
            lastRow.WriteBytes(new byte[48]
                {
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
                    0x17, 0x18, 0x19, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30,
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46,
                    0x47, 0x48,
                }
            );
            byte[] final = lastRow.GetAllBytes();


            // possible hash init
            uint c1 = 0xD9AAD30C;
            uint c2 = 0x51225B84;
            uint c3 = 0x26552CF3;
            uint c4 = 0xAEDDA47B;
            uint c5 = 0x7D3D11FD;

            uint ad = c1 ^ 0xBEEFF00D;
            uint a1 = c2 ^ 0xBEEFF00D;
            uint a3 = c3 ^ 0xBEEFF00D;
            uint a4 = c4 ^ 0xBEEFF00D;
            uint a5 = c5 ^ 0xBEEFF00D;


            //TODO extract how the last bytes are inflated.
            final[17] = (byte) (final[16] | 0x80); //025E0593 | 804C2C 3C 80 | or byte ptr ss:[esp+ebp+3C],80

            uint a00 = 0;
            uint a01 = 0;
            uint
                a = a00 & 0xFF00; // 0333166B | 81E1 00FF0000            | and ecx,FF00                                                            |
            uint
                b = a01 << 0x10; // 025F34AA | C1E0 10                  | shl eax,10                                                              |
            uint result = a & b; // 025F34AD | 03C8 | add ecx,eax
            uint r1 = result << 0x8;
            uint
                r2 = b >> 0x8; // 02EC44EC | C1E8 08                  | shr eax,8                                                               |
            uint r3 = r2 & 0xFF00;


            // IBuffer processedE = Process64(lastRow);


            // TODO REMOVE THIS BLOCK ONCE ALGO COMPLETED
            IBuffer lastRowStatic = output.Clone(256, 16);
            lastRowStatic.WriteByte(0x80);
            lastRowStatic.WriteBytes(new byte[15]);
            lastRowStatic.WriteBytes(new byte[16]);
            lastRowStatic.WriteBytes(new byte[14]);
            lastRowStatic.WriteByte(0x08);
            lastRowStatic.WriteByte(0x80);
            IBuffer processedE = Process64(lastRowStatic);
            // TODO REMOVE THIS BLOCK ONCE ALGO COMPLETED

            IBuffer processed = new StreamBuffer();
            processed.WriteBuffer(processedA);
            processed.WriteBuffer(processedB);
            processed.WriteBuffer(processedC);
            processed.WriteBuffer(processedD);
            processed.WriteBuffer(processedE);

            return processed;
        }

        private byte[] ReadHash(byte[] input)
        {
            int hashSize = 20;
            byte[] output = new byte[hashSize];
            int srcIndex = -1;
            int srcOffset = 0;
            for (int dstIndex = 0; dstIndex < 20; dstIndex++)
            {
                if (srcIndex < srcOffset)
                {
                    srcIndex = dstIndex + 3;
                    srcOffset = dstIndex;
                }

                output[dstIndex] = input[srcIndex];
                srcIndex--;
            }

            return output;
        }

        private IBuffer Process64(IBuffer input)
        {
            input.SetPositionStart();
            IBuffer output = new StreamBuffer();
            for (int i = 0; i < 4; i++)
            {
                uint a1 = input.ReadUInt32();
                uint a2 = input.ReadUInt32();
                uint a3 = input.ReadUInt32();
                uint a4 = input.ReadUInt32();
                uint a11 = Buffer.SwapBytes(a1);
                uint a22 = Buffer.SwapBytes(a2);
                uint a33 = Buffer.SwapBytes(a3);
                uint a44 = Buffer.SwapBytes(a4);
                output.WriteInt32(a11);
                output.WriteInt32(a22);
                output.WriteInt32(a33);
                output.WriteInt32(a44);
            }

            for (int i = 0; i < 64; i++)
            {
                output.Position = 0x34 + (i * 4);
                uint e = output.ReadUInt32();
                output.Position = 0x20 + (i * 4);
                uint f = output.ReadUInt32();

                uint ef = e ^ f; // 013ED0B3 | 3341 18 | xor eax,dword ptr ds:[ecx+18]

                output.Position = 0 + (i * 4);
                uint g = output.ReadUInt32();
                uint efg = ef ^ g;

                output.Position = 0x8 + (i * 4);
                uint h = output.ReadUInt32();
                uint efgh = efg ^ h;

                uint rol = RotateLeft(efgh, 1);
                output.Position = 0x40 + (i * 4);
                output.WriteInt32(rol);
            }

            return output;
        }

        public static uint RotateLeft(uint x, int n)
        {
            return (((x) << (n)) | ((x) >> (32 - (n))));
        }

        private static byte[] RootCryptoKey = new byte[]
        {
            0x66, 0x32, 0x33, 0x65, 0x39, 0x38, 0x48, 0x61, 0x66, 0x4A, 0x64, 0x53, 0x6F, 0x61, 0x6A, 0x38,
            0x30, 0x51, 0x42, 0x6A, 0x68, 0x68, 0x32, 0x33, 0x6F, 0x61, 0x6A, 0x67, 0x6B, 0x6C, 0x53, 0x61,
            0x64, 0x72, 0x68, 0x6F, 0x67, 0x68, 0x32, 0x49, 0x4A, 0x6E, 0x77, 0x4A, 0x45, 0x46, 0x35, 0x38,
        };
    }
}
