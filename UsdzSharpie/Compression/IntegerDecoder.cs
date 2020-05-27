using System;
using System.Collections.Generic;
using System.Text;

namespace UsdzSharpie.Compression
{
    public class IntegerDecoder
    {
        private static void DecodeHelper(int count, int common, byte[] codesIn, ref int codeInOffset, byte[] vintsIn, ref int vintsOffset, ref int preVal, ref List<int> results)
        {
            var codeByte = codesIn[codeInOffset];
            codeInOffset++;
            for (var i = 0; i < count; i++)
            {
                var x = (codeByte & (3 << (2 * i))) >> (2 * i);
                if (x == 0)
                {
                    preVal += common;
                }
                else if (x == 1)
                {
                    preVal += unchecked((sbyte)vintsIn[vintsOffset]);
                    vintsOffset += 1;
                }
                else if (x == 2)
                {
                    preVal += BitConverter.ToInt16(vintsIn, vintsOffset);
                    vintsOffset += 2;
                }
                else if (x == 4)
                {
                    preVal += BitConverter.ToInt32(vintsIn, vintsOffset);
                    vintsOffset += 4;
                }
                results.Add((int)preVal);
            }
        }

        public static int[] DecodeIntegers(byte[] buffer, ulong count)
        {
            var commonValue = BitConverter.ToInt32(buffer, 0);
            var numcodesBytes = (count * 2 + 7) / 8;

            var codesIn = new byte[(int)numcodesBytes];
            Array.Copy(buffer, 4, codesIn, 0, codesIn.Length);
            var vintsIn = new byte[buffer.Length - (int)numcodesBytes - 4];
            Array.Copy(buffer, 4 + codesIn.Length, vintsIn, 0, vintsIn.Length);

            var vintsOffset = 0;
            var codeInOffset = 0;

            var results = new List<int>();

            int preVal = 0;
            int intsLeft = (int)count;
            while (intsLeft > 0)
            {
                var toProcess = Math.Min(intsLeft, 4);
                DecodeHelper(toProcess, commonValue, codesIn, ref codeInOffset, vintsIn, ref vintsOffset, ref preVal, ref results);
                intsLeft -= toProcess;
            }

            return results.ToArray();
        }

    }
}
