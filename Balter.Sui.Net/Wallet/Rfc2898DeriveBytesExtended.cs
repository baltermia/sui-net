﻿using System.Diagnostics;
using System.Security.Cryptography;

namespace Balter.Sui.Net.Wallet;

/// <summary>
/// This is a copy of Rfc28989DeiveBytes from corefx, since .NET Standard 2.0 leaves out the version
/// where you can specify the HMAC to use I had to copy this version in and use it instead.
/// </summary>
public class Rfc2898DeriveBytesExtended : DeriveBytes
{
    private const int MinimumSaltSize = 8;

    private readonly byte[] _password;
    private byte[] _salt;
    private uint _iterations;
    private HMAC _hmac;
    private int _blockSize;

    private byte[]? _buffer;
    private uint _block;
    private int _startIndex;
    private int _endIndex;

    public HashAlgorithmName HashAlgorithm { get; }

    public Rfc2898DeriveBytesExtended(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm)
    {
        if (salt == null)
            throw new ArgumentNullException(nameof(salt));
        if (salt.Length < MinimumSaltSize)
            throw new ArgumentException("Salt is not at least eight bytes.", nameof(salt));
        if (iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(iterations), "Positive number required.");
        if (password == null)
            throw new NullReferenceException();  // This "should" be ArgumentNullException but for compat, we throw NullReferenceException.

        _salt = salt.CloneByteArray();
        _iterations = (uint)iterations;
        _password = password.CloneByteArray();
        HashAlgorithm = hashAlgorithm;
        _hmac = OpenHmac();
        // _blockSize is in bytes, HashSize is in bits.
        _blockSize = _hmac.HashSize >> 3;

        Initialize();
    }

    public int IterationCount
    {
        get
        {
            return (int)_iterations;
        }

        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Positive number required.");
            _iterations = (uint)value;
            Initialize();
        }
    }

    public byte[] Salt
    {
        get
        {
            return _salt.CloneByteArray();
        }

        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length < MinimumSaltSize)
                throw new ArgumentException("Salt is not at least eight bytes.");
            _salt = value.CloneByteArray();
            Initialize();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hmac.Dispose();

            if (_buffer != null)
                Array.Clear(_buffer, 0, _buffer.Length);
            if (_password != null)
                Array.Clear(_password, 0, _password.Length);
            if (_salt != null)
                Array.Clear(_salt, 0, _salt.Length);
        }
        base.Dispose(disposing);
    }

    public override byte[] GetBytes(int cb)
    {
        Debug.Assert(_blockSize > 0);

        if (cb <= 0)
            throw new ArgumentOutOfRangeException(nameof(cb), "Positive number required.");
        byte[] password = new byte[cb];

        int offset = 0;
        int size = _endIndex - _startIndex;
        if (size > 0)
        {
            if (cb >= size)
            {
                Buffer.BlockCopy(_buffer!, _startIndex, password, 0, size);
                _startIndex = _endIndex = 0;
                offset += size;
            }
            else
            {
                Buffer.BlockCopy(_buffer!, _startIndex, password, 0, cb);
                _startIndex += cb;
                return password;
            }
        }

        Debug.Assert(_startIndex == 0 && _endIndex == 0, "Invalid start or end index in the internal buffer.");

        while (offset < cb)
        {
            byte[] T_block = Func();
            int remainder = cb - offset;
            if (remainder > _blockSize)
            {
                Buffer.BlockCopy(T_block, 0, password, offset, _blockSize);
                offset += _blockSize;
            }
            else
            {
                Buffer.BlockCopy(T_block, 0, password, offset, remainder);
                offset += remainder;
                Buffer.BlockCopy(T_block, remainder, _buffer!, _startIndex, _blockSize - remainder);
                _endIndex += (_blockSize - remainder);
                return password;
            }
        }
        return password;
    }

    private HMAC OpenHmac()
    {
        Debug.Assert(_password != null);

        HashAlgorithmName hashAlgorithm = HashAlgorithm;

        if (string.IsNullOrEmpty(hashAlgorithm.Name))
            throw new CryptographicException("The hash algorithm name cannot be null or empty.");

        if (hashAlgorithm == HashAlgorithmName.SHA1)
            return new HMACSHA1(_password);
        if (hashAlgorithm == HashAlgorithmName.SHA256)
            return new HMACSHA256(_password);
        if (hashAlgorithm == HashAlgorithmName.SHA384)
            return new HMACSHA384(_password);
        if (hashAlgorithm == HashAlgorithmName.SHA512)
            return new HMACSHA512(_password);

        throw new CryptographicException($"{hashAlgorithm.Name} is not a known hash algorithm.");
    }

    private void Initialize()
    {
        if (_buffer != null)
            Array.Clear(_buffer, 0, _buffer.Length);
        _buffer = new byte[_blockSize];
        _block = 1;
        _startIndex = _endIndex = 0;
    }

    // This function is defined as follows:
    // Func (S, i) = HMAC(S || i) ^ HMAC2(S || i) ^ ... ^ HMAC(iterations) (S || i) 
    // where i is the block number.
    private byte[] Func()
    {
        byte[] temp = new byte[_salt.Length + sizeof(uint)];
        Buffer.BlockCopy(_salt, 0, temp, 0, _salt.Length);
        Helpers.WriteInt(_block, temp, _salt.Length);

        temp = _hmac.ComputeHash(temp);

        byte[] ret = temp;
        for (int i = 2; i <= _iterations; i++)
        {
            temp = _hmac.ComputeHash(temp);

            for (int j = 0; j < _blockSize; j++)
            {
                ret[j] ^= temp[j];
            }
        }

        // increment the block count.
        _block++;
        return ret;
    }

    public override void Reset()
    {
        Initialize();
    }
}

internal static class Helpers
{
    public static byte[] CloneByteArray(this byte[] src) =>
        (byte[])src.Clone();

    public static void WriteInt(uint i, byte[] arr, int offset)
    {
        unchecked
        {
            Debug.Assert(arr != null);
            Debug.Assert(arr.Length >= offset + sizeof(uint));

            arr[offset] = (byte)(i >> 24);
            arr[offset + 1] = (byte)(i >> 16);
            arr[offset + 2] = (byte)(i >> 8);
            arr[offset + 3] = (byte)i;
        }
    }
}

