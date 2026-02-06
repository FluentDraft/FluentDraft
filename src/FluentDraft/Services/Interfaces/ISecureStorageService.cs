namespace FluentDraft.Services.Interfaces
{
    public interface ISecureStorageService
    {
        string Encrypt(string plainText);
        string Decrypt(string encryptedText);
    }
}
