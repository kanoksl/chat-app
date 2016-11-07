using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace ChatClassLibrary
{
    public static class Utility
    {
        #region Byte-Array Converters

        /// <summary>
        /// Convert a 32-bit integer to an array of 4 bytes (using big endian).
        /// </summary>
        /// <param name="value">Integer value to be converted.</param>
        /// <returns>A byte array of length 4.</returns>
        public static byte[] ToByteArray(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// Convert a 64-bit integer to an array of 8 bytes (using big endian).
        /// </summary>
        /// <param name="value">Long integer value to be converted.</param>
        /// <returns>A byte array of length 8.</returns>
        public static byte[] ToByteArray(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// Convert 4 bytes of an array to a 32-bit integer.
        /// </summary>
        /// <param name="bytes">Byte array to convert.</param>
        /// <param name="startIndex">The position of the 4 bytes to convert.</param>
        /// <returns>A 32-bit integer.</returns>
        public static int BytesToInt32(byte[] bytes, int startIndex = 0)
        {
            if (BitConverter.IsLittleEndian)
            {   // Create a copy of the int bytes and convert to big endian.
                byte[] newBytes = new byte[4];
                Array.Copy(bytes, startIndex, newBytes, 0, 4);
                Array.Reverse(newBytes);
                bytes = newBytes;
                startIndex = 0;
            }
            return BitConverter.ToInt32(bytes, startIndex);
        }

        /// <summary>
        /// Convert the first 8 bytes of an array to a 64-bit integer.
        /// </summary>
        /// <param name="bytes">Byte array to convert.</param>
        /// <param name="startIndex">The position of the 8 bytes to convert.</param>
        /// <returns>A 64-bit integer.</returns>
        public static long BytesToInt64(byte[] bytes, int startIndex = 0)
        {
            if (BitConverter.IsLittleEndian)
            {   // Create a copy of the long bytes and convert to big endian.
                byte[] newBytes = new byte[8];
                Array.Copy(bytes, startIndex, newBytes, 0, 8);
                Array.Reverse(newBytes);
                bytes = newBytes;
                startIndex = 0;
            }
            return BitConverter.ToInt64(bytes, startIndex);
        }

        #endregion

        //--------------------------------------------------------------------------------------//

        #region Array Manipulations

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

        /// <summary>
        /// Concatenate multiple arrays of same type.
        /// </summary>
        /// <typeparam name="T">Can be any type.</typeparam>
        /// <param name="arrays">Arrays to be concatenated.</param>
        /// <returns>A new array resulted from concatenating the input arrays in the given order.</returns>
        public static T[] Concat<T>(params T[][] arrays)
        {
            int totalLength = arrays.Sum(a => a.Length);

            var z = new T[totalLength];
            int offset = 0;
            foreach (var a in arrays)
            {
                a.CopyTo(z, offset);
                offset += a.Length;
            }
            return z;
        }

        /// <summary>
        /// Make a sub-array (slice) of some length starting at specified index.
        /// (The source array is not modified).
        /// </summary>
        /// <typeparam name="T">Can be any type.</typeparam>
        /// <param name="source">The source array.</param>
        /// <param name="index">Starting index of the slice.</param>
        /// <param name="length">Length of the slice.</param>
        /// <returns>An array of type T[].</returns>
        public static T[] Slice<T>(T[] source, int index, int length)
        {
            var slice = new T[length];
            Array.Copy(source, index, slice, 0, length);
            return slice;
        }

        #endregion

        //--------------------------------------------------------------------------------------//

        #region File Hashing (MD5)

        /// <summary>
        /// Calculate the MD5 checksum of a file as a byte array.
        /// </summary>
        /// <param name="filePath">Path of the file.</param>
        /// <returns>A byte array of length 16 containing the hash.</returns>
        public static byte[] CalculateMD5(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var hasher = MD5.Create())
            {
                byte[] hash = hasher.ComputeHash(stream);
                return hash;
            }
        }

        /// <summary>
        /// Convert an array of hash to standard-looking string.
        /// </summary>
        /// <param name="hash">Array returned from <code>ComputeHash()</code>.</param>
        /// <returns>A lowecase hexadecimal string of the hash value.</returns>
        public static string ToHashString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        #endregion

        //--------------------------------------------------------------------------------------//

        #region Networking (Ports and IP Addresses)

        /// <summary>
        /// Find a free TCP/IP port tthat can be used with TcpListener or similar.
        /// </summary>
        /// <returns>An integer value that is the free port number.</returns>
        public static int FreeTcpPort()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint) tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        /// <summary>
        /// Get the IPv4 address of this computer (for addressing in LAN).
        /// </summary>
        /// <returns>An IPv4 address.</returns>
        public static IPAddress GetIPv4Address()
        {
            var localHostName = Dns.GetHostName();
            var ipHostInfo = Dns.GetHostEntry(localHostName);
            var ipAddressList = ipHostInfo.AddressList.ToList();
            ipAddressList = ipAddressList.Where(ip =>
                    ip.AddressFamily == AddressFamily.InterNetwork).ToList();
            return ipAddressList[0];
        }

        #endregion

        public static string[] RandomNames =
        {
            "Alice",
            "Bob",
            "Dorothea",
            "Caroline",
            "Nicolasa",
            "Byron",
            "Sharika",
            "Tiffanie",
            "Nguyet",
            "Kristel",
            "Tierra",
            "Stanford",
            "Alesha",
            "Kandice",
            "Aurelio",
            "Coralee",
            "Darla",
            "Spencer",
            "Therese",
            "Micaela",
            "Carlota",
            "Starr",
            "Clyde",
            "Brinda",
            "Lakeesha",
            "Nilda",
            "Clay",
            "Mamie",
            "Jonah",
            "Ceola",
            "Estrella",
            "Tracy",
            "Lolicon"
        };

        public static string GetRandomName()
        {
            Random random = new Random();
            return RandomNames[random.Next(RandomNames.Length)];
        }
    }
}
