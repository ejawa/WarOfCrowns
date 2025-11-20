using UnityEngine;
using System.IO; // Библиотека для работы с файлами
using System.Text;

namespace WarOfCrowns.Data
{
    public static class SaveSystem
    {
        // Имя папки сохранения. Позже можно сделать динамическим (Slot_1, Slot_2)
        private const string SAVE_FOLDER = "SaveSlot_1";

        // Главный метод инициализации (создает папку, если ее нет)
        public static void Init()
        {
            string path = GetSaveFolderPath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        // Метод СОХРАНЕНИЯ (Универсальный)
        public static void SaveData<T>(T data, string fileName)
        {
            // 1. Превращаем данные (класс) в текст (JSON)
            string json = JsonUtility.ToJson(data, true); // true = красивое форматирование

            // 2. Определяем полный путь к файлу
            string path = Path.Combine(GetSaveFolderPath(), fileName);

            // 3. Записываем на диск
            File.WriteAllText(path, json, Encoding.UTF8);

            Debug.Log($"[SaveSystem] Saved {fileName} to {path}");
        }

        // Метод ЗАГРУЗКИ (Универсальный)
        public static T LoadData<T>(string fileName)
        {
            string path = Path.Combine(GetSaveFolderPath(), fileName);

            if (File.Exists(path))
            {
                // 1. Читаем текст из файла
                string json = File.ReadAllText(path);

                // 2. Превращаем текст обратно в класс
                T data = JsonUtility.FromJson<T>(json);
                return data;
            }
            else
            {
                Debug.LogWarning($"[SaveSystem] File not found: {path}");
                return default(T); // Возвращаем пустоту, если файла нет
            }
        }

        // Вспомогательный метод получения пути
        private static string GetSaveFolderPath()
        {
            // persistentDataPath - это специальная папка, которая сохраняется между запусками
            // Windows: %userprofile%\AppData\LocalLow\CompanyName\ProductName
            return Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        }
    }
}