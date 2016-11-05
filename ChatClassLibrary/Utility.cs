using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
        /// <param name="value">Integer value to be converted.</param>
        /// <returns>A byte array of length 8.</returns>
        public static byte[] ToByteArray(long value)
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
        public static int BytesToInt32(byte[] bytes, int startIndex = 0)
        {
            if (BitConverter.IsLittleEndian)
            {   // Create a copy of the int bytes and convert to big endian.
                byte[] newBytes = new byte[4];
                Array.Copy(bytes, startIndex, newBytes, 0, 4);
                Array.Reverse(newBytes);
                bytes = newBytes;
            }
            int value = BitConverter.ToInt32(bytes, 0);
            return value;
        }
        
        /// <summary>
        /// Convert the first 8 bytes of an array to a 64-bit integer.
        /// </summary>
        /// <param name="bytes">Byte array to convert.</param>
        /// <returns>A 64-bit integer.</returns>
        public static long BytesToInt64(byte[] bytes, int startIndex = 0)
        {
            if (BitConverter.IsLittleEndian)
            {   // Create a copy of the long bytes and convert to big endian.
                byte[] newBytes = new byte[8];
                Array.Copy(bytes, startIndex, newBytes, 0, 8);
                Array.Reverse(newBytes);
                bytes = newBytes;
            }
            long value = BitConverter.ToInt64(bytes, 0);
            return value;
        }

        //--------------------------------------------------------------------------------------//

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
        /// <param name="arr">Arrays to be concatenated.</param>
        /// <returns>A new array resulted from concatenating the input arrays in the given order.</returns>
        public static T[] Concat<T>(params T[][] arr)
        {
            int totalLength = 0;
            foreach (T[] a in arr) totalLength += a.Length;

            var z = new T[totalLength];
            int offset = 0;
            foreach (T[] a in arr)
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

        //--------------------------------------------------------------------------------------//

        /// <summary>
        /// Calculate the MD5 checksum of a given stream as a byte array.
        /// </summary>
        /// <param name="stream">The input to compute the hash code for.</param>
        /// <returns>A byte array of length 16 containing the hash.</returns>
        public static byte[] CalculateMD5(Stream stream)
        {
            using (var hasher = MD5.Create())
            {
                byte[] hash = hasher.ComputeHash(stream);
                return hash;
            }
        }

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

        //--------------------------------------------------------------------------------------//

        /// <summary>
        /// Generate a new GUID.
        /// </summary>
        /// <returns>A byte array of length 16 containing the GUID value.</returns>
        public static byte[] NewGuid()
        {
            return Guid.NewGuid().ToByteArray();
        }

        /// <summary>
        /// Convert a byte-array GUID to standard-looking string.
        /// </summary>
        /// <param name="guid">A 16-byte GUID array.</param>
        /// <returns>The string representation of the GUID.</returns>
        public static string ToGuidString(byte[] guid)
        {
            var g = new Guid(guid);
            return g.ToString();
        }

        //--------------------------------------------------------------------------------------//

        public static int FreeTcpPort()
        {
            TcpListener tcp = new TcpListener(IPAddress.Loopback, 0);
            tcp.Start();
            int port = ((IPEndPoint) tcp.LocalEndpoint).Port;
            tcp.Stop();
            return port;
        }

        public static IPAddress GetIPv4Address()
        {
            string localHostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(localHostName);
            List<IPAddress> ipAddressList = ipHostInfo.AddressList.ToList();
            AddressFamily IPv4 = AddressFamily.InterNetwork;
            ipAddressList = ipAddressList.Where(ip => ip.AddressFamily == IPv4).ToList();
            IPAddress ipAddress = ipAddressList[0];
            return ipAddress;
        }
    }
}
