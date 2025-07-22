namespace Wrappers;

public class FileWrapper : IFileWrapper
{
    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public async Task<string> ReadAllTextAsync(string path)
    {
        return await File.ReadAllTextAsync(path);
    }

    public string[] ReadAllLines(string path)
    {
        return File.ReadAllLines(path);
    }

    public async Task<string[]> ReadAllLinesAsync(string path)
    {
        return await File.ReadAllLinesAsync(path);
    }

    public byte[] ReadAllBytes(string path)
    {
        return File.ReadAllBytes(path);
    }

    public async Task<byte[]> ReadAllBytesAsync(string path)
    {
        return await File.ReadAllBytesAsync(path);
    }

    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }

    public async Task WriteAllTextAsync(string path, string contents)
    {
        await File.WriteAllTextAsync(path, contents);
    }

    public void WriteAllLines(string path, string[] contents)
    {
        File.WriteAllLines(path, contents);
    }

    public async Task WriteAllLinesAsync(string path, string[] contents)
    {
        await File.WriteAllLinesAsync(path, contents);
    }

    public void WriteAllBytes(string path, byte[] bytes)
    {
        File.WriteAllBytes(path, bytes);
    }

    public async Task WriteAllBytesAsync(string path, byte[] bytes)
    {
        await File.WriteAllBytesAsync(path, bytes);
    }

    public void AppendAllText(string path, string contents)
    {
        File.AppendAllText(path, contents);
    }

    public async Task AppendAllTextAsync(string path, string contents)
    {
        await File.AppendAllTextAsync(path, contents);
    }

    public void Copy(string sourceFileName, string destFileName)
    {
        File.Copy(sourceFileName, destFileName);
    }

    public void Copy(string sourceFileName, string destFileName, bool overwrite)
    {
        File.Copy(sourceFileName, destFileName, overwrite);
    }

    public void Move(string sourceFileName, string destFileName)
    {
        File.Move(sourceFileName, destFileName);
    }

    public void Delete(string path)
    {
        File.Delete(path);
    }

    public DateTime GetCreationTime(string path)
    {
        return File.GetCreationTime(path);
    }

    public DateTime GetLastWriteTime(string path)
    {
        return File.GetLastWriteTime(path);
    }

    public long GetFileSize(string path)
    {
        var fileInfo = new FileInfo(path);
        return fileInfo.Length;
    }

    public FileAttributes GetAttributes(string path)
    {
        return File.GetAttributes(path);
    }

    public void SetAttributes(string path, FileAttributes fileAttributes)
    {
        File.SetAttributes(path, fileAttributes);
    }
}