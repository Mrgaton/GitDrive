﻿using System.Text;
using System.Text.RegularExpressions;

namespace GitDrive.Helpers
{
    /// <summary>
    /// Thx to https://github.com/KvanTTT/BaseNcoding
    /// </summary>
    public abstract class Base
    {
        public uint CharsCount { get; }

        public double BitsPerChars => (double)BlockBitsCount / BlockCharsCount;

        public int BlockBitsCount { get; protected set; }

        public int BlockCharsCount { get; protected set; }

        public string Alphabet { get; }

        public char Special { get; }

        public abstract bool HasSpecial { get; }

        public Encoding Encoding { get; set; }

        public bool Parallel { get; set; }

        protected readonly int[] InvAlphabet;

        public Base(uint charsCount, string alphabet, char special, Encoding encoding = null, bool parallel = false)
        {
            if (alphabet.Length != charsCount)
                throw new ArgumentException($"Base string should contain {charsCount} chars");

            for (int i = 0; i < charsCount; i++)
                for (int j = i + 1; j < charsCount; j++)
                    if (alphabet[i] == alphabet[j])
                        throw new ArgumentException("Base string should contain distinct chars");

            if (alphabet.Contains(special))
                throw new ArgumentException("Base string should not contain special char");

            CharsCount = charsCount;
            Alphabet = alphabet;
            Special = special;
            int bitsPerChar = LogBase2(charsCount);
            BlockBitsCount = Lcm(bitsPerChar, 8);
            BlockCharsCount = BlockBitsCount / bitsPerChar;

            InvAlphabet = new int[Alphabet.Max() + 1];

            for (int i = 0; i < InvAlphabet.Length; i++)
                InvAlphabet[i] = -1;

            for (int i = 0; i < charsCount; i++)
                InvAlphabet[Alphabet[i]] = i;

            Encoding = encoding ?? Encoding.UTF8;
            Parallel = parallel;
        }

        public virtual string EncodeString(string data)
        {
            return Encode(Encoding.GetBytes(data));
        }

        public abstract string Encode(byte[] data);

        public string DecodeToString(string data)
        {
            return Encoding.GetString(Decode(Regex.Replace(data, @"\r\n?|\n", "")));
        }

        public abstract byte[] Decode(string data);

        /// <summary>
        /// From: http://stackoverflow.com/a/600306/1046374
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static bool IsPowerOf2(uint x)
        {
            uint xint = x;
            if (x - xint != 0)
                return false;

            return (xint & (xint - 1)) == 0;
        }

        /// <summary>
        /// From: http://stackoverflow.com/a/13569863/1046374
        /// </summary>
        public static int Lcm(int a, int b)
        {
            int num1, num2;
            if (a > b)
            {
                num1 = a;
                num2 = b;
            }
            else
            {
                num1 = b;
                num2 = a;
            }

            for (int i = 1; i <= num2; i++)
            {
                int mult = num1 * i;
                if (mult % num2 == 0)
                    return mult;
            }

            return num2;
        }

        private static int LogBase2(uint x)
        {
            int r = 0;
            while ((x >>= 1) != 0)
                r++;
            return r;
        }

        private static int LogBaseN(uint x, uint n)
        {
            int r = 0;
            while ((x /= n) != 0)
                r++;
            return r;
        }

        public static int GetOptimalBitsCount2(uint charsCount, out uint charsCountInBits,
            uint maxBitsCount = 64, bool base2BitsCount = false)
        {
            int result = 0;
            charsCountInBits = 0;
            int n1 = LogBase2(charsCount);
            double charsCountLog = Math.Log(2, charsCount);
            double maxRatio = 0;

            for (int n = n1; n <= maxBitsCount; n++)
            {
                if (base2BitsCount && n % 8 != 0)
                    continue;

                uint l1 = (uint)Math.Ceiling(n * charsCountLog);
                double ratio = (double)n / l1;
                if (ratio > maxRatio)
                {
                    maxRatio = ratio;
                    result = n;
                    charsCountInBits = l1;
                }
            }

            return result;
        }

        protected static int GetOptimalBitsCount(uint charsCount, out uint charsCountInBits,
            uint maxBitsCount = 64, uint radix = 2)
        {
            int result = 0;
            charsCountInBits = 0;
            int n0 = LogBaseN(charsCount, radix);
            double charsCountLog = Math.Log(radix, charsCount);
            double maxRatio = 0;

            for (int n = n0; n <= maxBitsCount; n++)
            {
                uint k = (uint)Math.Ceiling(n * charsCountLog);
                double ratio = (double)n / k;
                if (ratio > maxRatio)
                {
                    maxRatio = ratio;
                    result = n;
                    charsCountInBits = k;
                }
            }

            return result;
        }
    }

    public class Base91 : Base
    {
        public const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$%&()*+,./:;<=>?@[]^_`{|}~\"";
        public const char DefaultSpecial = (char)0;

        public override bool HasSpecial => false;

        public Base91(string alphabet = DefaultAlphabet, char special = DefaultSpecial, Encoding textEncoding = null)
            : base(91, alphabet, special, textEncoding)
        {
            BlockBitsCount = 13;
            BlockCharsCount = 2;
        }

        public override string Encode(byte[] data)
        {
            StringBuilder result = new StringBuilder(data.Length);

            int ebq = 0, en = 0;
            for (int i = 0; i < data.Length; ++i)
            {
                ebq |= (data[i] & 255) << en;
                en += 8;
                if (en > 13)
                {
                    int ev = ebq & 8191;

                    if (ev > 88)
                    {
                        ebq >>= 13;
                        en -= 13;
                    }
                    else
                    {
                        ev = ebq & 16383;
                        ebq >>= 14;
                        en -= 14;
                    }

                    int quotient = Math.DivRem(ev, 91, out int remainder);
                    result.Append(Alphabet[remainder]);
                    result.Append(Alphabet[quotient]);
                }
            }

            if (en > 0)
            {
                int quotient = Math.DivRem(ebq, 91, out int remainder);
                result.Append(Alphabet[remainder]);
                if (en > 7 || ebq > 90)
                    result.Append(Alphabet[quotient]);
            }

            return result.ToString();
        }

        public override byte[] Decode(string data)
        {
            unchecked
            {
                int dbq = 0, dn = 0, dv = -1;

                List<byte> result = new List<byte>(data.Length);
                for (int i = 0; i < data.Length; ++i)
                {
                    if (InvAlphabet[data[i]] == -1)
                        continue;
                    if (dv == -1)
                        dv = InvAlphabet[data[i]];
                    else
                    {
                        dv += InvAlphabet[data[i]] * 91;
                        dbq |= dv << dn;
                        dn += (dv & 8191) > 88 ? 13 : 14;
                        do
                        {
                            result.Add((byte)dbq);
                            dbq >>= 8;
                            dn -= 8;
                        }
                        while (dn > 7);
                        dv = -1;
                    }
                }

                if (dv != -1)
                    result.Add((byte)(dbq | dv << dn));

                return result.ToArray();
            }
        }
    }
}