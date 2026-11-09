using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public class SaveStore
{
    private string filePath;
    private Dictionary<string, string> data = new();
    private string customKey;

    private bool loaded;
    private bool loadFailed;
    private bool dirty;

    public bool UseEncryption = true;
    public bool AutoSave = true;

    public bool IsDirty => dirty;
    public bool HasLoadError => loadFailed;
    public string FilePath => filePath;

    public SaveStore(string path)
    {
        filePath = Path.GetFullPath(path);
    }

    public bool Configure(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) {Report("Configure", filePath, "пустой путь"); return false;}
        string fullPath;
        try {fullPath = Path.GetFullPath(path);}
        catch (Exception error) {Report("Configure", filePath, error.GetType().Name); return false;}
        if (fullPath == filePath) return true;
        if (dirty && !Flush()) return false;
        filePath = fullPath;
        data = new();
        loaded = false;
        loadFailed = false;

        return true;
    }

    public void SetKey(string key)
    {
        if (key == null || Encoding.UTF8.GetByteCount(key) != 32) {Report("SetKey", filePath, "нужно 32 байта UTF8"); return;}
        customKey = key;
    }

    public void Set<T>(string k, T value)
    {
        if (!EnsureLoaded()) return;
        try {data[k] = JsonConvert.SerializeObject(value);}
        catch (Exception error) {Report("Set", filePath, error.GetType().Name); return;}
        Changed();
    }

    public T Get<T>(string k, T defaultValue = default) {return TryGet<T>(k, out var value) ? value : defaultValue;}

    public bool TryGet<T>(string k, out T value)
    {
        value = default;
        if (!EnsureLoaded()) return false;
        if (k == null) {Report("Get", filePath, "пустой ключ записи"); return false;}
        if (!data.TryGetValue(k, out var raw)) return false;
        try {value = JsonConvert.DeserializeObject<T>(raw);}
        catch (Exception error) {Report("Get", filePath, error.GetType().Name); return false;}

        return true;
    }

    public bool Has(string k)
    {
        if (!EnsureLoaded()) return false;
        if (k == null) {Report("Has", filePath, "пустой ключ записи"); return false;}

        return data.ContainsKey(k);
    }

    public void Delete(string k)
    {
        if (!EnsureLoaded()) return;
        if (k == null) {Report("Delete", filePath, "пустой ключ записи"); return;}
        if (data.Remove(k)) Changed();
    }

    public void DeleteAll()
    {
        if (!EnsureLoaded() || data.Count == 0) return;
        data.Clear();
        Changed();
    }

    public bool Reload()
    {
        if (dirty) {Report("Reload", filePath, "есть несохраненные изменения, вызови Flush"); return false;}
        loaded = false;

        return EnsureLoaded();
    }

    public bool Flush()
    {
        if (!EnsureLoaded()) return false;
        if (!dirty) return true;
        string contents;
        try
        {
            var json = JsonConvert.SerializeObject(data);
            contents = UseEncryption ? Encrypt(json) : json;
        }
        catch (Exception error) {Report("Flush", filePath, error.GetType().Name); return false;}
        if (!WriteAtomically(contents, filePath + ".bak")) return false;
        dirty = false;

        return true;
    }

    public bool RestoreBackup()
    {
        if (dirty) {Report("RestoreBackup", filePath, "есть несохраненные изменения, вызови Flush"); return false;}
        var backupPath = filePath + ".bak";
        if (!TryRead(backupPath, out var restored, out var contents)) return false;
        if (!WriteAtomically(contents, filePath + ".corrupt")) return false;
        data = restored;
        loaded = true;
        loadFailed = false;

        return true;
    }

    private void Changed()
    {
        dirty = true;
        if (AutoSave) Flush();
    }

    private bool EnsureLoaded()
    {
        if (loaded) return !loadFailed;
        loaded = true;
        if (!File.Exists(filePath)) {data = new(); loadFailed = false; return true;}
        if (!TryRead(filePath, out var restored, out _)) {loadFailed = true; return false;}
        data = restored;
        loadFailed = false;

        return true;
    }

    private bool TryRead(string path, out Dictionary<string, string> restored, out string contents)
    {
        restored = null;
        contents = null;
        try
        {
            contents = File.ReadAllText(path);
            restored = JsonConvert.DeserializeObject<Dictionary<string, string>>(UseEncryption ? Decrypt(contents) : contents);
            if (restored == null) throw new JsonSerializationException("null root");
        }
        catch (Exception error) {Report("Load", path, error.GetType().Name); return false;}

        return true;
    }

    private bool WriteAtomically(string contents, string backupPath)
    {
        var temporary = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(contents);
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(filePath)) File.Replace(temporary, filePath, backupPath);
            else File.Move(temporary, filePath);
        }
        catch (Exception error) {Report("Flush", filePath, error.GetType().Name); return false;}
        finally
        {
            try {if (File.Exists(temporary)) File.Delete(temporary);}
            catch (Exception error) {Report("Cleanup", temporary, error.GetType().Name);}
        }

        return true;
    }

    private void Report(string operation, string path, string error) {Debug.LogError($"Save {operation}: {path}: {error}");}

    private string GetKey()
    {
        if (customKey != null) return customKey;
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier + "goofy-save"));

        return BitConverter.ToString(hash).Replace("-", "").Substring(0, 32).ToLower();
    }

    private string Encrypt(string plain)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(GetKey());
        aes.GenerateIV();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs)) sw.Write(plain);

        return Convert.ToBase64String(ms.ToArray());
    }

    private string Decrypt(string cipher)
    {
        var bytes = Convert.FromBase64String(cipher);
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(GetKey());
        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(bytes, iv, iv.Length);
        aes.IV = iv;
        using var ms = new MemoryStream(bytes, iv.Length, bytes.Length - iv.Length);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
