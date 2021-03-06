using System;
using System.Collections.Generic;
using System.Text;

namespace LZString
{
    public static class LZString
    {
        private const string KeyStrBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
        private const string KeyStrUriSafe = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-$";

        private static readonly Dictionary<string, Dictionary<char, int>> BaseReverseDic = new Dictionary<string, Dictionary<char, int>>();

        private static int GetBaseValue(string alphabet, char character)
        {
            if (!BaseReverseDic.ContainsKey(alphabet))
            {
                BaseReverseDic[alphabet] = new Dictionary<char, int>();
                for (var i = 0; i < alphabet.Length; i++)
                    BaseReverseDic[alphabet][alphabet[i]] = i;
            }
            return BaseReverseDic[alphabet][character];
        }

        public static string CompressToBase64(string input)
        {
            if (input == null)
                return "";

            var res = Compress(input, 6, a => KeyStrBase64[a]);
            switch (res.Length % 4)
            {
                case 1:
                    return res + "===";
                case 2:
                    return res + "==";
                case 3:
                    return res + "=";
                default:
                    return res;
            }
        }

        public static string DecompressFromBase64(string input)
        {
            if (input == null)
                return "";
            if (input.Length == 0)
                return null;

            return Decompress(input.Length, 32, index => GetBaseValue(KeyStrBase64, input[index]));
        }

        public static string CompressToUTF16(string input)
        {
            if (input == null)
                return "";

            return Compress(input, 15, a => (char)(a + 32)) + " ";
        }


        public static string DecompressFromUTF16(string compressed)
        {
            if (compressed == null)
                return "";
            if (compressed.Length == 0)
                return null;

            return Decompress(compressed.Length, 16384, index => compressed[index] - 32);
        }

        public static byte[] CompressToByteArray(string uncompressed)
        {
            var compressed = Compress(uncompressed);
            var buf = new byte[compressed.Length * 2];

            for (var i = 0; i < compressed.Length; i++)
            {
                var current_value = compressed[i];
                buf[i * 2] = (byte)(current_value >> 8);
                buf[(i * 2) + 1] = (byte)(current_value & 255u);
            }
            return buf;
        }

        public static string DecompressFromByteArray(byte[] compressed)
        {
            if (compressed == null)
                return "";

            var result = new char[compressed.Length / 2];
            for (var i = 0; i < result.Length; i++)
                result[i] = (char)(compressed[i * 2] << 8 | compressed[(i * 2) + 1]);

            return Decompress(new string(result));
        }

        public static string CompressToEncodedUriComponent(string input) => Compress(input, 6, a => KeyStrUriSafe[a]);

        public static string DecompressFromEncodedUriComponent(string input)
        {
            if (input == null)
                return "";
            if (input.Length == 0)
                return null;

            input = input.Replace(' ', '+');
            return Decompress(input.Length, 32, index => GetBaseValue(KeyStrUriSafe, input[index]));
        }

        public static string Compress(string uncompressed) => Compress(uncompressed, 16, i => (char)i);

        private static string Compress(string uncompressed, int bitsPerChar, Func<int, char> getCharFromInt)
        {
            if (uncompressed == null)
                return "";

            int value, context_enlargeIn = 2, context_dictSize = 3, context_numBits = 2, context_data_val = 0, context_data_position = 0;
            var context_dictionaryToCreate = new HashSet<string>();
            var context_dictionary = new Dictionary<string, int>();
            var context_data = new StringBuilder();
            string context_c = "", context_wc = "", context_w = "";

            for (var ii = 0; ii < uncompressed.Length; ii++)
            {
                context_c = uncompressed[ii].ToString();
                if (!context_dictionary.ContainsKey(context_c))
                {
                    context_dictionary[context_c] = context_dictSize++;
                    context_dictionaryToCreate.Add(context_c);
                }
                context_wc = context_w + context_c;
                if (context_dictionary.ContainsKey(context_wc))
                {
                    context_w = context_wc;
                }
                else
                {
                    if (context_dictionaryToCreate.Contains(context_w))
                    {
                        if (context_w[0] < 256)
                        {
                            for (var i = 0; i < context_numBits; i++)
                            {
                                context_data_val <<= 1;
                                if (context_data_position == bitsPerChar - 1)
                                {
                                    context_data_position = 0;
                                    context_data.Append(getCharFromInt(context_data_val));
                                    context_data_val = 0;
                                }
                                else
                                {
                                    context_data_position++;
                                }
                            }
                            value = context_w[0];
                            for (var i = 0; i < 8; i++)
                            {
                                context_data_val = (context_data_val << 1) | (value & 1);
                                if (context_data_position == bitsPerChar - 1)
                                {
                                    context_data_position = 0;
                                    context_data.Append(getCharFromInt(context_data_val));
                                    context_data_val = 0;
                                }
                                else
                                {
                                    context_data_position++;
                                }
                                value >>= 1;
                            }
                        }
                        else
                        {
                            value = 1;
                            for (var i = 0; i < context_numBits; i++)
                            {
                                context_data_val = (context_data_val << 1) | value;
                                if (context_data_position == bitsPerChar - 1)
                                {
                                    context_data_position = 0;
                                    context_data.Append(getCharFromInt(context_data_val));
                                    context_data_val = 0;
                                }
                                else
                                {
                                    context_data_position++;
                                }
                                value = 0;
                            }
                            value = context_w[0];
                            for (var i = 0; i < 16; i++)
                            {
                                context_data_val = (context_data_val << 1) | (value & 1);
                                if (context_data_position == bitsPerChar - 1)
                                {
                                    context_data_position = 0;
                                    context_data.Append(getCharFromInt(context_data_val));
                                    context_data_val = 0;
                                }
                                else
                                {
                                    context_data_position++;
                                }
                                value >>= 1;
                            }
                        }
                        context_enlargeIn--;
                        if (context_enlargeIn == 0)
                        {
                            context_enlargeIn = 1 << context_numBits;
                            context_numBits++;
                        }
                        context_dictionaryToCreate.Remove(context_w);
                    }
                    else
                    {
                        value = context_dictionary[context_w];
                        for (var i = 0; i < context_numBits; i++)
                        {
                            context_data_val = (context_data_val << 1) | (value & 1);
                            if (context_data_position == bitsPerChar - 1)
                            {
                                context_data_position = 0;
                                context_data.Append(getCharFromInt(context_data_val));
                                context_data_val = 0;
                            }
                            else
                            {
                                context_data_position++;
                            }
                            value >>= 1;
                        }
                    }
                    context_enlargeIn--;
                    if (context_enlargeIn == 0)
                    {
                        context_enlargeIn = 1 << context_numBits;
                        context_numBits++;
                    }
                    //Add wc to the dictionary
                    context_dictionary[context_wc] = context_dictSize++;
                    context_w = context_c;
                }
            }
            //Output the code for w
            if (context_w.Length != 0)
            {
                if (context_dictionaryToCreate.Contains(context_w))
                {
                    if (context_w[0] < 256)
                    {
                        for (var i = 0; i < context_numBits; i++)
                        {
                            context_data_val <<= 1;
                            if (context_data_position == bitsPerChar - 1)
                            {
                                context_data_position = 0;
                                context_data.Append(getCharFromInt(context_data_val));
                                context_data_val = 0;
                            }
                            else
                            {
                                context_data_position++;
                            }
                        }
                        value = context_w[0];
                        for (var i = 0; i < 8; i++)
                        {
                            context_data_val = (context_data_val << 1) | (value & 1);
                            if (context_data_position == bitsPerChar - 1)
                            {
                                context_data_position = 0;
                                context_data.Append(getCharFromInt(context_data_val));
                                context_data_val = 0;
                            }
                            else
                            {
                                context_data_position++;
                            }
                            value >>= 1;
                        }
                    }
                    else
                    {
                        value = 1;
                        for (var i = 0; i < context_numBits; i++)
                        {
                            context_data_val = (context_data_val << 1) | value;
                            if (context_data_position == bitsPerChar - 1)
                            {
                                context_data_position = 0;
                                context_data.Append(getCharFromInt(context_data_val));
                                context_data_val = 0;
                            }
                            else
                            {
                                context_data_position++;
                            }
                            value = 0;
                        }
                        value = context_w[0];
                        for (var i = 0; i < 16; i++)
                        {
                            context_data_val = (context_data_val << 1) | (value & 1);
                            if (context_data_position == bitsPerChar - 1)
                            {
                                context_data_position = 0;
                                context_data.Append(getCharFromInt(context_data_val));
                                context_data_val = 0;
                            }
                            else
                            {
                                context_data_position++;
                            }
                            value >>= 1;
                        }
                    }
                    context_enlargeIn--;
                    if (context_enlargeIn == 0)
                    {
                        context_enlargeIn = 1 << context_numBits;
                        context_numBits++;
                    }
                    context_dictionaryToCreate.Remove(context_w);
                }
                else
                {
                    value = context_dictionary[context_w];
                    for (var i = 0; i < context_numBits; i++)
                    {
                        context_data_val = (context_data_val << 1) | (value & 1);
                        if (context_data_position == bitsPerChar - 1)
                        {
                            context_data_position = 0;
                            context_data.Append(getCharFromInt(context_data_val));
                            context_data_val = 0;
                        }
                        else
                        {
                            context_data_position++;
                        }
                        value >>= 1;
                    }
                }
                context_enlargeIn--;
                if (context_enlargeIn == 0)
                {
                    context_enlargeIn = 1 << context_numBits;
                    context_numBits++;
                }
            }
            //Mark the end of the stream
            value = 2;
            for (var i = 0; i < context_numBits; i++)
            {
                context_data_val = (context_data_val << 1) | (value & 1);
                if (context_data_position == bitsPerChar - 1)
                {
                    context_data_position = 0;
                    context_data.Append(getCharFromInt(context_data_val));
                    context_data_val = 0;
                }
                else
                {
                    context_data_position++;
                }
                value >>= 1;
            }

            //Flush the last char
            do
            {
                context_data_val <<= 1;
            } while (context_data_position++ != bitsPerChar - 1);
            context_data.Append(getCharFromInt(context_data_val));

            return context_data.ToString();
        }

        public static string Decompress(string compressed)
        {
            if (compressed == null)
                return "";
            if (compressed.Length == 0)
                return null;

            return Decompress(compressed.Length, 32768, index => compressed[index]);
        }

        private struct DataStruct
        {
            public int val, position, index;
        }

        private static string Decompress(int length, int resetValue, Func<int, int> getNextValue)
        {
            var dictionary = new Dictionary<int, string>();
            int next, enlargeIn = 4, dictSize = 4, numBits = 3, bits, resb, maxpower, power;
            int c = 0;
            string entry = "", w;
            var result = new StringBuilder();
            var data = new DataStruct() { val = getNextValue(0), position = resetValue, index = 1 };

            for (var i = 0; i < 3; i++)
                dictionary[i] = ((char)i).ToString();

            bits = 0;
            maxpower = 4;
            power = 1;
            while (power != maxpower)
            {
                resb = data.val & data.position;
                data.position >>= 1;
                if (data.position == 0)
                {
                    data.position = resetValue;
                    data.val = getNextValue(data.index++);
                }
                bits |= (resb > 0 ? 1 : 0) * power;
                power <<= 1;
            }

            switch (next = bits)
            {
                case 0:
                    bits = 0;
                    maxpower = 256;
                    power = 1;
                    while (power != maxpower)
                    {
                        resb = data.val & data.position;
                        data.position >>= 1;
                        if (data.position == 0)
                        {
                            data.position = resetValue;
                            data.val = getNextValue(data.index++);
                        }
                        bits |= (resb > 0 ? 1 : 0) * power;
                        power <<= 1;
                    }
                    c = bits;
                    break;
                case 1:
                    bits = 0;
                    maxpower = 65536;
                    power = 1;
                    while (power != maxpower)
                    {
                        resb = data.val & data.position;
                        data.position >>= 1;
                        if (data.position == 0)
                        {
                            data.position = resetValue;
                            data.val = getNextValue(data.index++);
                        }
                        bits |= (resb > 0 ? 1 : 0) * power;
                        power <<= 1;
                    }
                    c = bits;
                    break;
                case 2:
                    return "";
            }
            var tmp = (char)c;
            dictionary[3] = w = tmp.ToString();
            result.Append(tmp);
            while (true)
            {
                if (data.index > length)
                    return "";

                bits = 0;
                maxpower = 1 << numBits;
                power = 1;
                while (power != maxpower)
                {
                    resb = data.val & data.position;
                    data.position >>= 1;
                    if (data.position == 0)
                    {
                        data.position = resetValue;
                        data.val = getNextValue(data.index++);
                    }
                    bits |= (resb > 0 ? 1 : 0) * power;
                    power <<= 1;
                }

                switch (c = bits)
                {
                    case 0:
                        bits = 0;
                        maxpower = 256;
                        power = 1;
                        while (power != maxpower)
                        {
                            resb = data.val & data.position;
                            data.position >>= 1;
                            if (data.position == 0)
                            {
                                data.position = resetValue;
                                data.val = getNextValue(data.index++);
                            }
                            bits |= (resb > 0 ? 1 : 0) * power;
                            power <<= 1;
                        }

                        dictionary[dictSize++] = ((char)bits).ToString();
                        c = dictSize - 1;
                        enlargeIn--;
                        break;
                    case 1:
                        bits = 0;
                        maxpower = 65536;
                        power = 1;
                        while (power != maxpower)
                        {
                            resb = data.val & data.position;
                            data.position >>= 1;
                            if (data.position == 0)
                            {
                                data.position = resetValue;
                                data.val = getNextValue(data.index++);
                            }
                            bits |= (resb > 0 ? 1 : 0) * power;
                            power <<= 1;
                        }
                        dictionary[dictSize++] = ((char)bits).ToString();
                        c = dictSize - 1;
                        enlargeIn--;
                        break;
                    case 2:
                        return result.ToString();
                }

                if (enlargeIn == 0)
                {
                    enlargeIn = 1 << numBits;
                    numBits++;
                }

                if (dictionary.ContainsKey(c))
                    entry = dictionary[c];
                else if (c == dictSize)
                    entry = w + w[0].ToString();
                else
                    return null;

                result.Append(entry);

                //Add w+entry[0] to the dictionary.
                dictionary[dictSize++] = w + entry[0].ToString();
                enlargeIn--;
                w = entry;
                if (enlargeIn == 0)
                {
                    enlargeIn = 1 << numBits;
                    numBits++;
                }
            }
        }
    }
}
