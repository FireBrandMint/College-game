using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Bindings
{
    public class PacketBuffer : IDisposable
    {
        List <byte> bufferList;
        byte [] readbuffer;

        int readpos;

        bool buffupdate = false;

        public PacketBuffer (byte[] bytes = null)
        {
            bufferList = new List<byte>();
            readpos = 0;

            if (bytes != null) WriteBytes(bytes);
        }

        public int GetReadPosition ()
        {
            return readpos;
        }

        public byte [] ToArray () => bufferList.ToArray();

        public int Count ()
        {
            return bufferList.Count;
        }

        public int Length ()
        {
            return Count() - readpos;
        }

        public void Clear ()
        {
            bufferList.Clear();

            buffupdate = true;

            readpos = 0;
        }
    
        public void WriteBytes (byte[] input)
        {
            if (input.Length == 0) return;

            bufferList.AddRange(input);
            buffupdate = true;
        }

        public void WriteByte (byte input)
        {
            bufferList.Add(input);
            buffupdate = true;
        }
    
        public void WriteInteger (int input)
        {
            bufferList.AddRange(BitConverter.GetBytes(input));
            buffupdate = true;
        }

        public void WriteFloat (float input)
        {
            bufferList.AddRange(BitConverter.GetBytes(input));
            buffupdate = true;
        }

        public void WriteString (string input)
        {
            bufferList.AddRange(BitConverter.GetBytes(input.Length));
            bufferList.AddRange(Encoding.ASCII.GetBytes(input));
            buffupdate = true;
        }

        //Read data

        public int ReadInteger (bool peek = true)
        {
            if (bufferList.Count > readpos)
            {
                if (buffupdate)
                {
                    readbuffer = bufferList.ToArray();
                    buffupdate = false;
                }

                int value = BitConverter.ToInt32(readbuffer, readpos);

                if (peek & bufferList.Count > readpos)
                {
                    readpos += 4;
                }

                return value;
            }

            throw new Exception ("Buffer is past its limit!");
        }

        public float ReadFloat (bool peek = true)
        {
            if (bufferList.Count > readpos)
            {
                if (buffupdate)
                {
                    readbuffer = bufferList.ToArray();
                    buffupdate = false;
                }

                float value = BitConverter.ToSingle(readbuffer, readpos);

                if (peek & bufferList.Count > readpos)
                {
                    readpos += 4;
                }

                return value;
            }

            throw new Exception ("Buffer is past its limit!");
        }

        public byte ReadByte (bool peek = true)
        {
            if (bufferList.Count > readpos)
            {
                if (buffupdate)
                {
                    readbuffer = bufferList.ToArray();
                    buffupdate = false;
                }

                byte value = readbuffer[readpos];

                if (peek & bufferList.Count > readpos)
                {
                    readpos += 1;
                }

                return value;
            }

            throw new Exception ("Buffer is past its limit!");
        }

        public byte[] ReadBytes (int length, bool peek = true)
        {
            if (bufferList.Count > readpos)
            {
                if (buffupdate)
                {
                    readbuffer = bufferList.ToArray();
                    buffupdate = false;
                }

                byte[] value = bufferList.GetRange(readpos, length).ToArray();

                if (peek & bufferList.Count > readpos)
                {
                    readpos += length;
                }

                return value;
            }

            throw new Exception ("Buffer is past its limit!");
        }

        public string ReadString (bool peek = true)
        {

            int length = ReadInteger(true);
            if (buffupdate)
            {
                readbuffer = bufferList.ToArray();
                buffupdate = false;
            }

            string value = Encoding.ASCII.GetString(readbuffer, readpos, length);

            if (peek & bufferList.Count > readpos)
            {
                readpos += length;
            }

            return value;
        }
    
        //IDisposable
        private bool disposedValue = false;
        protected virtual void Dispose (bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    bufferList.Clear();
                }
                readpos = 0;
            }

            disposedValue = true;
        }

        public void Dispose ()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}