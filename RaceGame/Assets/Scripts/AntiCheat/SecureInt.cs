using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using UnityEngine;

#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
public struct SecureInt
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
{
    private byte[] abbabba;
    private byte[] baabaab;

    public SecureInt(int value)
    {
        baabaab = null;
        abbabba = null;

        abbabba = Encrypt(value);
    }

    public int AsInt()
    {
        return Decrypt();
    }

    public void Set(int value)
    {
        baabaab = null;
        abbabba = null;

        abbabba = Encrypt(value);
    }

    private byte[] Encrypt(int value)
    {
        byte[] buffer = new byte[4];
        IntToBytes(value, buffer);

        baabaab = new byte[16];
        RandomNumberGenerator.Fill(baabaab);

        buffer = XOR(buffer, baabaab);
        return buffer;
    }

    private int Decrypt()
    {
        byte[] buffer = null;
        buffer = XOR(abbabba, baabaab);

        return BytesToInt(buffer);
    }

    private static byte[] XOR(byte[] value, byte[] key)
    {
        int len = value.Length;
        byte[] result = new byte[len];
        for (int i = 0; i < len; i++)
            result[i] = (byte)(value[i] ^ key[i % key.Length]);
        return result;
    }

    private void IntToBytes(int value, Span<byte> dest) => BinaryPrimitives.WriteInt32LittleEndian(dest, value);
    private int BytesToInt(ReadOnlySpan<byte> src) => BinaryPrimitives.ReadInt32LittleEndian(src);

    public static SecureInt operator +(SecureInt a, SecureInt b) => new SecureInt(a.AsInt() + b.AsInt());
    public static SecureInt operator -(SecureInt a, SecureInt b) => new SecureInt(a.AsInt() - b.AsInt());
    public static SecureInt operator *(SecureInt a, SecureInt b) => new SecureInt(a.AsInt() * b.AsInt());
    public static SecureInt operator /(SecureInt a, SecureInt b) => new SecureInt(a.AsInt() / b.AsInt());

    public static bool operator ==(SecureInt a, SecureInt b) => a.AsInt() == b.AsInt();
    public static bool operator !=(SecureInt a, SecureInt b) => a.AsInt() != b.AsInt();

}
