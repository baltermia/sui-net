namespace Balter.Sui.Net.Wallet;

internal interface IKeyPair
{
    public string PublicKeyBase64 { get; }
    public string PrivateKeyBase64 { get; }
    public string PublicKeyAsSuiAddress { get; }

    public byte[] PublicKey { get; }
    public byte[] PrivateKey { get; }

    public string ToSuiAddress(byte[] publicKeyBytes);
    public string Sign(string base64message);
    public byte[] Sign(byte[] message);
}