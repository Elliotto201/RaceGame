using System;
using UnityEngine;

public class JoinRequest
{
    public bool IsTest;
    public Loadout Loadout;
    public Guid PlayerMatchGuid;

    public byte[] Serialize()
    {
        byte[] buffer = new byte[1024];
        buffer[0] = EndianBitConverter.GetBytes(IsTest)[0];
        int currentOffset = 1;

        if (IsTest)
        {
            for (int i = 0; i < 4; i++)
            {
                string str = Loadout._Loadout[i];
                byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(str);

                byte[] lengthBytes = EndianBitConverter.GetBytes((ushort)strBytes.Length, true);
                Array.Copy(lengthBytes, 0, buffer, currentOffset, lengthBytes.Length);
                currentOffset += lengthBytes.Length;

                Array.Copy(strBytes, 0, buffer, currentOffset, strBytes.Length);
                currentOffset += strBytes.Length;
            }
        }
        else
        {
            byte[] guidBytes = PlayerMatchGuid.ToByteArray();
            Array.Copy(guidBytes, 0, buffer, currentOffset, guidBytes.Length);
            currentOffset += guidBytes.Length;
        }

        byte[] finalBuffer = new byte[currentOffset];
        Array.Copy(buffer, 0, finalBuffer, 0, currentOffset);
        return finalBuffer;
    }

    public static JoinRequest Deserialize(byte[] buffer)
    {
        bool isTest = EndianBitConverter.ToBoolean(buffer, 0);
        int currentOffset = 1;

        if (isTest)
        {
            string[] loadout = new string[4];
            for (int i = 0; i < 4; i++)
            {
                ushort length = EndianBitConverter.ToUInt16(buffer, currentOffset, true);
                currentOffset += 2;

                loadout[i] = System.Text.Encoding.UTF8.GetString(buffer, currentOffset, length);
                currentOffset += length;
            }

            return new JoinRequest
            {
                IsTest = true,
                Loadout = new Loadout(loadout)
            };
        }
        else
        {
            byte[] guidBytes = new byte[16];
            Array.Copy(buffer, currentOffset, guidBytes, 0, 16);

            return new JoinRequest
            {
                IsTest = false,
                PlayerMatchGuid = new Guid(guidBytes)
            };
        }
    }
}

public static class EndianBitConverter
{
    public static bool IsLittleEndian => BitConverter.IsLittleEndian;

    public static byte[] GetBytes(short value, bool bigEndian = true) => EnsureEndian(BitConverter.GetBytes(value), bigEndian);
    public static byte[] GetBytes(ushort value, bool bigEndian = true) => EnsureEndian(BitConverter.GetBytes(value), bigEndian);
    public static byte[] GetBytes(int value, bool bigEndian = true) => EnsureEndian(BitConverter.GetBytes(value), bigEndian);
    public static byte[] GetBytes(uint value, bool bigEndian = true) => EnsureEndian(BitConverter.GetBytes(value), bigEndian);
    public static byte[] GetBytes(long value, bool bigEndian = true) => EnsureEndian(BitConverter.GetBytes(value), bigEndian);
    public static byte[] GetBytes(ulong value, bool bigEndian = true) => EnsureEndian(BitConverter.GetBytes(value), bigEndian);
    public static byte[] GetBytes(float value, bool bigEndian = true) => EnsureEndian(BitConverter.GetBytes(value), bigEndian);
    public static byte[] GetBytes(double value, bool bigEndian = true) => EnsureEndian(BitConverter.GetBytes(value), bigEndian);
    public static byte[] GetBytes(char value, bool bigEndian = true) => EnsureEndian(BitConverter.GetBytes(value), bigEndian);
    public static byte[] GetBytes(bool value) => BitConverter.GetBytes(value); // bool is 1 byte, no endian issue

    public static short ToInt16(byte[] bytes, int startIndex = 0, bool bigEndian = true) => BitConverter.ToInt16(EnsureEndian(bytes, startIndex, sizeof(short), bigEndian), 0);
    public static ushort ToUInt16(byte[] bytes, int startIndex = 0, bool bigEndian = true) => BitConverter.ToUInt16(EnsureEndian(bytes, startIndex, sizeof(ushort), bigEndian), 0);
    public static int ToInt32(byte[] bytes, int startIndex = 0, bool bigEndian = true) => BitConverter.ToInt32(EnsureEndian(bytes, startIndex, sizeof(int), bigEndian), 0);
    public static uint ToUInt32(byte[] bytes, int startIndex = 0, bool bigEndian = true) => BitConverter.ToUInt32(EnsureEndian(bytes, startIndex, sizeof(uint), bigEndian), 0);
    public static long ToInt64(byte[] bytes, int startIndex = 0, bool bigEndian = true) => BitConverter.ToInt64(EnsureEndian(bytes, startIndex, sizeof(long), bigEndian), 0);
    public static ulong ToUInt64(byte[] bytes, int startIndex = 0, bool bigEndian = true) => BitConverter.ToUInt64(EnsureEndian(bytes, startIndex, sizeof(ulong), bigEndian), 0);
    public static float ToSingle(byte[] bytes, int startIndex = 0, bool bigEndian = true) => BitConverter.ToSingle(EnsureEndian(bytes, startIndex, sizeof(float), bigEndian), 0);
    public static double ToDouble(byte[] bytes, int startIndex = 0, bool bigEndian = true) => BitConverter.ToDouble(EnsureEndian(bytes, startIndex, sizeof(double), bigEndian), 0);
    public static char ToChar(byte[] bytes, int startIndex = 0, bool bigEndian = true) => BitConverter.ToChar(EnsureEndian(bytes, startIndex, sizeof(char), bigEndian), 0);
    public static bool ToBoolean(byte[] bytes, int startIndex = 0) => BitConverter.ToBoolean(bytes, startIndex);

    private static byte[] EnsureEndian(byte[] bytes, bool bigEndian)
    {
        if ((IsLittleEndian && bigEndian) || (!IsLittleEndian && !bigEndian))
            Array.Reverse(bytes);
        return bytes;
    }

    private static byte[] EnsureEndian(byte[] bytes, int startIndex, int length, bool bigEndian)
    {
        var segment = new byte[length];
        Array.Copy(bytes, startIndex, segment, 0, length);
        return EnsureEndian(segment, bigEndian);
    }
}
