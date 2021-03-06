﻿#region License
/* Copyright (c) 2017-2018 Wes Hampson
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
#endregion

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using WHampson.Cascara.Interpreter;

namespace WHampson.Cascara
{
    /// <summary>
    /// Represents a file containing binary (non human-readable) data.
    /// </summary>
    public class BinaryData : IDisposable
    {
        /// <summary>
        /// The maximum file size that can be loaded.
        /// </summary>
        public const int MaxFileSize = int.MaxValue;

        private const int BufferSize = 4096;

        /// <summary>
        /// Creates a new little-endian <see cref="BinaryData"/> object from the data
        /// stored inside a file.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <returns>The newly-created <see cref="BinaryData"/> object.</returns>
        /// /// <exception cref="ArgumentException">
        /// Thrown if the path is malformatted.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown if a problem occurs while reading the file.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if the process does not have permission to access to the specified path.
        /// </exception>
        public static BinaryData Load(string path)
        {
            return Load(path, Endianness.Little);
        }

        /// <summary>
        /// Creates a new <see cref="BinaryData"/> object from the data
        /// stored inside a file.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <param name="endianness">The byte order for primitive types.</param>
        /// <returns>The newly-created <see cref="BinaryData"/> object.</returns>
        /// /// <exception cref="ArgumentException">
        /// Thrown if the path is malformatted or if the endianness is not specified.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown if a problem occurs while reading the file.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if the process does not have permission to access to the specified path.
        /// </exception>
        public static BinaryData Load(string path, Endianness endianness)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                string msg = Resources.ArgumentExceptionEmptyString;
                throw new ArgumentException(msg, nameof(path));
            }

            using (FileStream fStream = new FileStream(path, FileMode.Open))
            {
                if (fStream.Length > MaxFileSize)
                {
                    throw new IOException("File is too large.");
                }

                // Create file data buffer
                int len = (int) fStream.Length;
                IntPtr data = Marshal.AllocHGlobal(len);
                byte[] buffer = new byte[BufferSize];

                // Read the file in chunks
                int offset = 0;
                int bytesRead;
                while ((bytesRead = fStream.Read(buffer, 0, BufferSize)) > 0)
                {
                    Marshal.Copy(buffer, 0, data + offset, bytesRead);
                    offset += bytesRead;
                }

                return new BinaryData(data, len, endianness);
            }
        }

        private bool hasBeenDisposed;
        private IntPtr dataPtr;
        private Structure fileStructure;

        private BinaryData(Endianness endianness)
        {
            hasBeenDisposed = false;
            dataPtr = IntPtr.Zero;
            fileStructure = new Structure(this, SymbolTable.CreateRootSymbolTable());
            Endianness = endianness;
        }

        /// <summary>
        /// Initializes a new <see cref="BinaryData"/> object of the specified length
        /// with little-endian byte order.
        /// </summary>
        /// <param name="length">The number of bytes to allocate.</param>
        /// <exception cref="OutOfMemoryException">
        /// Thrown if there is not enough memory to create the <see cref="BinaryData"/>
        /// with the length provided.
        /// </exception>
        public BinaryData(int length)
            : this(length, Endianness.Little)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="BinaryData"/> object of the specified length.
        /// </summary>
        /// <param name="length">The number of bytes to allocate.</param>
        /// <param name="endianness">The byte order for primitive types.</param>
        /// <exception cref="OutOfMemoryException">
        /// Thrown if there is not enough memory to create the <see cref="BinaryData"/>
        /// with the length provided.
        /// </exception>
        public BinaryData(int length, Endianness endianness)
            : this(endianness)
        {
            if (length < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            dataPtr = Marshal.AllocHGlobal(length);
            Length = length;
        }

        /// <summary>
        /// Initializes a new <see cref="BinaryData"/> object with data
        /// from the specified byte array with little-endian byte order.
        /// </summary>
        /// <param name="data">The array to initialize the file with.</param>
        /// <exception cref="OutOfMemoryException">
        /// Thrown if there is not enough memory to create the <see cref="BinaryData"/>
        /// with the length provided.
        /// </exception>
        public BinaryData(byte[] data)
            : this(data, Endianness.Little)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="BinaryData"/> object with data
        /// from the specified byte array.
        /// </summary>
        /// <param name="data">The array to initialize the file with.</param>
        /// <param name="endianness">The byte order for primitive types.</param>
        /// <exception cref="OutOfMemoryException">
        /// Thrown if there is not enough memory to create the <see cref="BinaryData"/>
        /// with the length provided.
        /// </exception>
        public BinaryData(byte[] data, Endianness endianness)
            : this(endianness)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.Length < 1)
            {
                throw new ArgumentException(Resources.ArgumentExceptionEmptyCollection, nameof(data));
            }

            dataPtr = Marshal.AllocHGlobal(data.Length);
            Length = data.Length;

            Set<byte>(0, data);
        }

        private BinaryData(IntPtr data, int length, Endianness endianness)
            : this(endianness)
        {
            dataPtr = data;
            Length = length;
        }

        /// <summary>
        /// Gets or sets the byte order.
        /// The value of this property affects the values of primitive types
        /// returned by <see cref="Get{T}(int)"/> and set by
        /// <see cref="Set{T}(int, T)"/>.
        /// </summary>
        public Endianness Endianness
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the length of the <see cref="BinaryData"/> in bytes.
        /// </summary>
        public int Length
        {
            get;
        }

        /// <summary>
        /// Gets or sets a byte in the file data.
        /// </summary>
        /// <param name="index">The position in the file data of the byte to get or set.</param>
        /// <returns>The value of the byte at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the index is out of range.
        /// </exception>
        public byte this[int index]
        {
            get { return Get<byte>(index); }
            set { Set<byte>(index, value); }
        }

        /// <summary>
        /// Gets a value from the file data.
        /// </summary>
        /// <remarks>
        /// If <typeparamref name="T"/> is a multi-byte type, the bytes will be retrieved
        /// in the order specified by the <see cref="Endianness"/> property.
        /// </remarks>
        /// <typeparam name="T">The type of the value to get.</typeparam>
        /// <param name="index">The position in the file data of the value to get.</param>
        /// <returns>The value at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the index is out of range.
        /// </exception>
        public T Get<T>(int index)
            where T : struct
        {
            return Get<T>(index, 1)[0];
        }

        /// <summary>
        /// Gets an array of values from the file data.
        /// </summary>
        /// <remarks>
        /// If <typeparamref name="T"/> is a multi-byte type, the bytes of each element
        /// will be retrieved in the order specified by the <see cref="Endianness"/> property.
        /// </remarks>
        /// <typeparam name="T">The type of the values to get.</typeparam>
        /// <param name="index">The position in the file data of the first value to get.</param>
        /// <param name="count">The number of elements to get.</param>
        /// <returns>
        /// An array of <paramref name="count"/> values retrieved from the binary file data at the specified index.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the index is out of range or if the count results in an index that is out of range.
        /// </exception>
        public T[] Get<T>(int index, int count)
            where T : struct
        {
            if (!PrimitiveTypeUtils.IsPrimitiveType<T>())
            {
                throw new ArgumentException(Resources.ArgumentExceptionPrimitiveType, nameof(T));
            }

            int elemSize = PrimitiveTypeUtils.SizeOf<T>();
            int numBytes = elemSize * count;

            RangeCheck(index);
            RangeCheck(index + numBytes - 1);

            // Get bytes from binary file data
            byte[] b = new byte[numBytes];
            Marshal.Copy(dataPtr + index, b, 0, numBytes);

            // Create array of values
            T[] values = new T[count];
            byte[] valueBytes = new byte[elemSize];
            for (int i = 0; i < count; i++)
            {
                Buffer.BlockCopy(b, i * elemSize, valueBytes, 0, elemSize);
                if (Endianness == Endianness.Big)
                {
                    valueBytes = valueBytes.Reverse().ToArray();
                }

                values[i] = PrimitiveTypeUtils.GetValue<T>(valueBytes);
            }

            return values;
        }

        /// <summary>
        /// Sets a value in the file data.
        /// </summary>
        /// /// <remarks>
        /// If <typeparamref name="T"/> is a multi-byte type, the bytes will be written
        /// in the order specified by the <see cref="Endianness"/> property.
        /// </remarks>
        /// <typeparam name="T">The type of the value to set.</typeparam>
        /// <param name="index">The position in the file data of the value to set.</param>
        /// <param name="value">tThe value to write.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the index is out of range.
        /// </exception>
        public void Set<T>(int index, T value)
            where T : struct
        {
            Set<T>(index, new T[] { value });
        }

        /// <summary>
        /// Sets an array of values in the file data.
        /// </summary>
        /// <remarks>
        /// If <typeparamref name="T"/> is a multi-byte type, the bytes of each element
        /// will be written in the order specified by the <see cref="Endianness"/> property.
        /// </remarks>
        /// <typeparam name="T">The type of the values to set.</typeparam>
        /// <param name="index">The position in the file data of the first value to get.</param>
        /// <param name="values">The values to be written to the binary file data starting at the index.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the index is out of range or if the count results in an index that is out of range.
        /// </exception>
        public void Set<T>(int index, T[] values)
            where T : struct
        {
            if (!PrimitiveTypeUtils.IsPrimitiveType<T>())
            {
                throw new ArgumentException(Resources.ArgumentExceptionPrimitiveType, nameof(T));
            }

            int elemSize = PrimitiveTypeUtils.SizeOf<T>();
            int numBytes = elemSize * values.Length;

            RangeCheck(index);
            RangeCheck(index + numBytes - 1);

            // Convert values into a single array of bytes
            byte[] b = new byte[numBytes];
            for (int i = 0; i < values.Length; i++)
            {
                byte[] valueBytes = PrimitiveTypeUtils.GetBytes<T>(values[i]);
                if (Endianness == Endianness.Big)
                {
                    valueBytes = valueBytes.Reverse().ToArray();
                }

                Buffer.BlockCopy(valueBytes, 0, b, i * elemSize, elemSize);
            }

            // Copy byte array to binary file data
            Marshal.Copy(b, 0, dataPtr + index, numBytes);
        }

        /// <summary>
        /// Returns a copy of the data of this <see cref="BinaryData"/> as a byte array.
        /// </summary>
        /// <returns>A copy of the file data as a byte array.</returns>
        public byte[] ToArray()
        {
            byte[] buf = new byte[Length];
            Marshal.Copy(dataPtr, buf, 0, Length);

            return buf;
        }

        /// <summary>
        /// Converts the data in this <see cref="BinaryData"/> into an object
        /// by setting properties or fields using the names specified in a
        /// <see cref="LayoutScript"/>. The <see cref="RunLayoutScript(LayoutScript)"/>
        /// method must have been called prior for this to work properly.
        /// <see cref="DeserializationFlags"/> for this method are set to
        /// <see cref="DeserializationFlags.Public"/> and
        /// <see cref="DeserializationFlags.Properties"/>.
        /// </summary>
        public T Deserialize<T>() where T : new()
        {
            return fileStructure.Deserialize<T>();
        }

        /// <summary>
        /// Converts the data in this <see cref="BinaryData"/> into an object
        /// by setting properties or fields using the names specified in a
        /// <see cref="LayoutScript"/>. The <see cref="RunLayoutScript(LayoutScript)"/>
        /// method must have been called prior for this to work properly.
        /// </summary>
        /// <param name="flags">
        /// A <see cref="DeserializationFlags"/> bitfield that controls the
        /// behavior of deserialization.
        /// </param>
        public T Deserialize<T>(DeserializationFlags flags) where T : new()
        {
            return fileStructure.Deserialize<T>(flags);
        }

        /// <summary>
        /// Writes the binary data to a file.
        /// If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="path">The path to the file to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the path is malformatted.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown if a problem occurs while writing to the file.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if the process does not have permission to access to the specified path.
        /// </exception>
        public void Store(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                string msg = Resources.ArgumentExceptionEmptyString;
                throw new ArgumentException(msg, nameof(path));
            }

            // Create directory if needed
            DirectoryInfo dirInfo = Directory.GetParent(path);
            if (dirInfo != null)
            {
                Directory.CreateDirectory(dirInfo.FullName);
            }

            // Write data to file
            using (FileStream fStream = new FileStream(path, FileMode.Create))
            {
                byte[] buffer = new byte[BufferSize];
                int offset = 0;
                int bytesRead;

                while (offset < Length)
                {
                    bytesRead = Math.Min(BufferSize, Length - offset);
                    Marshal.Copy(dataPtr + offset, buffer, 0, bytesRead);

                    fStream.Write(buffer, 0, bytesRead);
                    offset += bytesRead;
                }
            }
        }

        /// <summary>
        /// Ensures the specified index falls within the binary file data.
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the index is out of range.
        /// </summary>
        /// <param name="index">The index to check.</param>
        private void RangeCheck(int index)
        {
            if (index < 0 || index >= Length)
            {
                string msg = Resources.ArgumentExceptionBinaryDataIndexOutOfRange;
                throw new ArgumentOutOfRangeException(null, index, msg);
            }
        }

        /// <summary>
        /// Gets a <see cref="Structure"/> by name. If no match exists,
        /// <c>null</c> is returned.
        /// </summary>
        /// <param name="name">The name of the <see cref="Structure"/> to get.</param>
        /// <returns>The <see cref="Structure"/> object, if found. <c>null</c> otherwise.</returns>
        public Structure GetStructure(string name)
        {
            return fileStructure.GetStructure(name);
        }

        /// <summary>
        /// Gets a value indicating whether a <see cref="Structure"/> with
        /// the specified name exists.
        /// </summary>
        /// <param name="name">The name of the <see cref="Structure"/> to check for.</param>
        /// <returns><c>True</c> if the structure exists.</returns>
        public bool HasStructure(string name)
        {
            return GetStructure(name) != null;
        }

        /// <summary>
        /// Gets a <see cref="Primitive{T}"/> by name and type. If no match is
        /// exists,  <c>null</c> is returned.
        /// </summary>
        /// <typeparam name="T">The type of primitive to get.</typeparam>
        /// <param name="name">The name of the <see cref="Primitive{T}"/> to get.</param>
        /// <returns>The <see cref="Primitive{T}"/> object, if found. <c>null</c> otherwise.</returns>
        public Primitive<T> GetPrimitive<T>(string name)
            where T : struct
        {
            return fileStructure.GetPrimitive<T>(name);
        }

        /// <summary>
        /// Gets a value indicating whether a <see cref="Primitive{T}"/> with
        /// the specified name and type exists.
        /// </summary>
        /// <typeparam name="T">The type of primitive to check for.</typeparam>
        /// <param name="name">The name of the <see cref="Primitive{T}"/> to check for.</param>
        /// <returns><c>True</c> if the primitive type exists.</returns>
        public bool HasPrimitive<T>(string name)
            where T : struct
        {
            return GetPrimitive<T>(name) != null;
        }

        /// <summary>
        /// Executes a <see cref="LayoutScript"/> on the binary data.
        /// Any textual output from the script will be written to
        /// <see cref="Console.Out"/>.
        /// <param name="script">The layout script to run.</param>
        /// </summary>
        public void RunLayoutScript(LayoutScript script)
        {
            RunLayoutScript(script, Console.Out);
        }

        /// <summary>
        /// Executes a <see cref="LayoutScript"/> on the binary data.
        /// <param name="script">The layout script to run.</param>
        /// <param name="scriptOutputWriter">A <see cref="TextWriter"/> to which
        /// textual output from the script will be written.</param>
        /// </summary>
        public void RunLayoutScript(LayoutScript script, TextWriter scriptOutputWriter)
        {
            LayoutInterpreter interpreter = new LayoutInterpreter(script, scriptOutputWriter);
            interpreter.Execute(fileStructure.Symbol, this);
        }

        /// <summary>
        /// Frees all unmanaged memory and disposes this object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!hasBeenDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                if (dataPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(dataPtr);
                    dataPtr = IntPtr.Zero;
                }

                hasBeenDisposed = true;
            }
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~BinaryData()
        {
            Dispose(false);
        }

        /// <summary>
        /// Frees all unmanaged memory and disposes this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a string that represents this object.
        /// </summary>
        public override string ToString()
        {
            JObject o = new JObject();
            o.Add(nameof(Length), Length);
            o.Add(nameof(Endianness), Endianness.ToString());
            o.Add("SymbolTable", JObject.Parse(fileStructure.ToString()));
            return o.ToString(Formatting.None);
        }
    }

    /// <summary>
    /// Specifies the byte order for multi-byte primitive data types.
    /// </summary>
    public enum Endianness
    {
        /// <summary>
        /// The most significant byte goes first.
        /// </summary>
        Big,

        /// <summary>
        /// The least significant byte goes first.
        /// </summary>
        Little
    }
}
