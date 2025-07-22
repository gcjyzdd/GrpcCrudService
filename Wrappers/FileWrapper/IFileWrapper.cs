namespace Wrappers;

public interface IFileWrapper
{
    bool Exists(string path);
    string ReadAllText(string path);
    Task<string> ReadAllTextAsync(string path);
    string[] ReadAllLines(string path);
    Task<string[]> ReadAllLinesAsync(string path);
    byte[] ReadAllBytes(string path);
    Task<byte[]> ReadAllBytesAsync(string path);
    void WriteAllText(string path, string contents);
    Task WriteAllTextAsync(string path, string contents);
    void WriteAllLines(string path, string[] contents);
    Task WriteAllLinesAsync(string path, string[] contents);
    void WriteAllBytes(string path, byte[] bytes);
    Task WriteAllBytesAsync(string path, byte[] bytes);
    void AppendAllText(string path, string contents);
    Task AppendAllTextAsync(string path, string contents);
    void Copy(string sourceFileName, string destFileName);
    void Copy(string sourceFileName, string destFileName, bool overwrite);
    void Move(string sourceFileName, string destFileName);
    void Delete(string path);
    DateTime GetCreationTime(string path);
    DateTime GetLastWriteTime(string path);
    long GetFileSize(string path);
    FileAttributes GetAttributes(string path);
    void SetAttributes(string path, FileAttributes fileAttributes);
}