using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClassLibrary
{
    public static class Utility
    {
        /// <summary>
        /// Convert a 32-bit integer to an array of 4 bytes (using big endian).
        /// </summary>
        /// <param name="value">Integer value to be converted.</param>
        /// <returns>A byte array of length 4.</returns>
        public static byte[] IntToBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// Convert the first 4 bytes of an array to a 32-bit integer.
        /// </summary>
        /// <param name="bytes">Byte array to convert.</param>
        /// <returns>A 32-bit integer.</returns>
        public static int BytesToInt(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            int value = BitConverter.ToInt32(bytes, 0);
            return value;
        }

        /// <summary>
        /// Concatenate two arrays of same type.
        /// </summary>
        /// <typeparam name="T">Can be any type.</typeparam>
        /// <param name="x">The first array.</param>
        /// <param name="y">The second array.</param>
        /// <returns>A new array resulted from concatenating y to x.</returns>
        public static T[] Concat<T>(T[] x, T[] y)
        {
            var z = new T[x.Length + y.Length];
            x.CopyTo(z, 0);
            y.CopyTo(z, x.Length);
            return z;
        }
    }
}
