using System.IO;
using UnityEngine;

public static class Save
{
    private static SaveStore defaultStore = new(Path.Combine(Application.persistentDataPath, "save.dat"));

    public static bool UseEncryption = true;
    public static bool AutoSave = true;

    public static bool IsDirty => defaultStore.IsDirty;
    public static bool HasLoadError => defaultStore.HasLoadError;
    public static string FilePath => defaultStore.FilePath;

    private static SaveStore Store
    {
        get
        {
            defaultStore.UseEncryption = UseEncryption;
            defaultStore.AutoSave = AutoSave;

            return defaultStore;
        }
    }

    public static bool Configure(string path) {return Store.Configure(path);}
    public static void SetKey(string key) {Store.SetKey(key);}
    public static void Set<T>(string k, T value) {Store.Set(k, value);}
    public static T Get<T>(string k, T defaultValue = default) {return Store.Get(k, defaultValue);}
    public static bool TryGet<T>(string k, out T value) {return Store.TryGet(k, out value);}
    public static bool Has(string k) {return Store.Has(k);}
    public static void Delete(string k) {Store.Delete(k);}
    public static void DeleteAll() {Store.DeleteAll();}
    public static bool Reload() {return Store.Reload();}
    public static bool Flush() {return Store.Flush();}
    public static bool RestoreBackup() {return Store.RestoreBackup();}
}
