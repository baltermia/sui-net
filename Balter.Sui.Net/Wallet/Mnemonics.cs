using Chaos.NaCl;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Balter.Sui.Net.Wallet;

/// <summary>
/// Based on https://github.com/bmresearch/Solnet/blob/master/src/Solnet.Wallet/Ed25519Bip32.cs
/// </summary>
internal partial class Mnemonics
{
    private const uint HardenedOffset = 0x80000000;

    /// <summary>
    /// The seed for the Ed25519 BIP32 HMAC-SHA512 master key calculation.
    /// </summary>
    private const string Curve = "ed25519 seed";

    [GeneratedRegex("^m(\\/[0-9]+')+$")]
    private static partial Regex PathRegex();

    private static bool IsValidPath(string path)
    {
        if (!PathRegex().IsMatch(path))
            return false;

        bool valid = 
            !path
            .Replace("'", string.Empty)
            .Split('/')
            .Skip(1)
            .Any(a => !int.TryParse(a, out _));

        return valid;
    }

    private static (byte[] Key, byte[] ChainCode) HmacSha512(byte[] keyBuffer, byte[] data)
    {
        byte[] i = new byte[64];
        var digest = new Sha512Digest();
        HMac hmac = new(digest);

        hmac.Init(new KeyParameter(keyBuffer));
        hmac.BlockUpdate(data, 0, data.Length);
        hmac.DoFinal(i, 0);

        byte[] il = i.Slice(0, 32);
        byte[] ir = i.Slice(32);

        return (Key: il, ChainCode: ir);
    }

    private static (byte[] Key, byte[] ChainCode) GetChildKeyDerivation(byte[] key, byte[] chainCode, uint index)
    {
        using MemoryStream buffer = new();

        buffer.Write([0], 0, 1);
        buffer.Write(key, 0, key.Length);
        byte[] indexBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(indexBytes, index);
        buffer.Write(indexBytes, 0, indexBytes.Length);

        return HmacSha512(chainCode, buffer.ToArray());
    }

    /// <summary>
    /// Gets the master key used for key generation from the passed seed.
    /// </summary>
    /// <param name="seed">The seed used to calculate the master key.</param>
    /// <returns>A tuple consisting of the key and corresponding chain code.</returns>
    private static (byte[] Key, byte[] ChainCode) GetMasterKeyFromSeed(byte[] seed)
        => HmacSha512(Encoding.UTF8.GetBytes(Curve), seed);

    public static (byte[] Key, byte[] ChainCode) DerivePath(byte[] seed, string path = "m/44'/784'/0'/0'/0'")
    {
        if (!IsValidPath(path))
            throw new FormatException("Invalid derivation path");

        (byte[] masterKey, byte[] chainCode) = GetMasterKeyFromSeed(seed);

        IEnumerable<uint> segments = path
            .Split('/')
            .Slice(1)
            .Select(a => a.Replace("'", ""))
            .Select(a => Convert.ToUInt32(a, 10));

        (byte[] _masterKey, byte[] _chainCode) results = segments
            .Aggregate(
                (masterKey, chainCode),
                (masterKeyFromSeed, next) =>
                    GetChildKeyDerivation(masterKeyFromSeed.masterKey, masterKeyFromSeed.chainCode, next + HardenedOffset));

        return results;
    }

    public static IKeyPair GetAccountFromMnemonic(string mnemonic, int index, string password = "")
    {
        byte[] seed = MnemonicToSeed(mnemonic, password);

        (byte[] Key, byte[] ChainCode) derived = DerivePath(seed, $"m/44'/784'/{index}'/0'/0'");
        byte[] publicKey = new byte[Ed25519.PublicKeySizeInBytes];
        byte[] privateKey = new byte[Ed25519.ExpandedPrivateKeySizeInBytes];
        Ed25519.KeyPairFromSeed(new ArraySegment<byte>(publicKey), new ArraySegment<byte>(privateKey), new ArraySegment<byte>(derived.Key, 0, 32));
        return new Ed25519KeyPair(publicKey, privateKey);
    }

    #region BIP39    
    // Suinet: original source: https://github.com/elucidsoft/dotnetstandard-bip39
    private static string Salt(string password) =>
        "mnemonic" + password;

    // Suinet: made public
    public static byte[] MnemonicToSeed(string mnemonic, string password)
    {
        byte[] mnemonicBytes = Encoding.UTF8.GetBytes(mnemonic.Normalize(NormalizationForm.FormKD));
        byte[] saltBytes = Encoding.UTF8.GetBytes(Salt(password.Normalize(NormalizationForm.FormKD)));

        Rfc2898DeriveBytesExtended rfc2898DerivedBytes = new(mnemonicBytes, saltBytes, 2048, HashAlgorithmName.SHA512);
        return rfc2898DerivedBytes.GetBytes(64);
    }
    #endregion
}

internal static class Utils
{
    /// <summary>
    /// Slices the array, returning a new array starting at <c>start</c> index and ending at <c>end</c> index.
    /// </summary>
    /// <param name="source">The array to slice.</param>
    /// <param name="start">The starting index of the slicing.</param>
    /// <param name="end">The ending index of the slicing.</param>
    /// <typeparam name="T">The array type.</typeparam>
    /// <returns>The sliced array.</returns>
    internal static T[] Slice<T>(this T[] source, int start, int end)
    {
        if (end < 0)
            end = source.Length;

        var len = end - start;

        // Return new array.
        var res = new T[len];
        for (var i = 0; i < len; i++) res[i] = source[i + start];
        return res;
    }

    /// <summary>
    /// Slices the array, returning a new array starting at <c>start</c>.
    /// </summary>
    /// <param name="source">The array to slice.</param>
    /// <param name="start">The starting index of the slicing.</param>
    /// <typeparam name="T">The array type.</typeparam>
    /// <returns>The sliced array.</returns>
    internal static T[] Slice<T>(this T[] source, int start)
    {
        return Slice(source, start, -1);
    }
}

