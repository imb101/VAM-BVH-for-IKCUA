using System;

public class VAMBinaryFileReader
{
    int counter = 0;
    byte[] bytes;

    public VAMBinaryFileReader(byte[] data)
    {
        bytes = data;
        counter = 0;
    }


    public int Position()
    {
        return counter;
    }

    public int Length()
    {
        return bytes.Length;
    }

    public void Seek(int offset)//, int start)
    {
        counter = offset;
    }

    public byte ReadByte()
    {
        byte ret = bytes[counter];
        counter++;
        return ret;
    }

    public byte[] ReadBytes(int count)
    {
        byte[] ret = new byte[count];
        Buffer.BlockCopy(bytes, counter, ret, 0, count);
        counter += count;
        return ret;
    }

    public ushort ReadUInt16()
    {
        ushort ret = BitConverter.ToUInt16(bytes, counter);
        counter += 2;
        return ret;
    }

    public uint ReadUInt32()
    {
        uint ret = BitConverter.ToUInt32(bytes, counter);
        counter += 4;
        return ret;
    }

    public int ReadInt32()
    {
        int ret = BitConverter.ToInt32(bytes, counter);
        counter += 4;
        return ret;
    }
}