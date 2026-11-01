using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CyanStars.Framework.File.Serialization;
using CyanStars.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace CyanStars.GameSave
{
    /// <summary>
    /// 管理游戏存档的保存和加载操作
    /// </summary>
    /// <remarks>
    /// json 的格式参数与解析实现由 <see cref="JsonSerializer"/> 统一持有，本类不再自己指定序列化参数，
    /// 也不设置全局默认参数（那会外溢到其他调用方）
    /// </remarks>
    public static class GameSaveManager
    {
        /// <summary>
        /// 保存游戏存档
        /// </summary>
        /// <param name="gameSaveData">存档数据</param>
        /// <param name="saveFilePath">存档文件路径</param>
        public static void SaveGameSave(GameSaveData gameSaveData, string saveFilePath)
        {
            try
            {
                gameSaveData.Verification = CalculateVerification(gameSaveData);

                if (!JsonSerializer.TrySerialize(gameSaveData, out string json))
                {
                    Debug.LogError($"存档序列化失败，已放弃写入：{saveFilePath}");
                    return;
                }

                File.WriteAllText(saveFilePath, json);
                Debug.Log($"存档保存成功：{saveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存存档时发生错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 尝试读取游戏存档
        /// </summary>
        /// <param name="saveFilePath">文档文件路径</param>
        /// <param name="gameSaveData">存档数据</param>
        /// <returns>读取操作的结果</returns>
        public static GameSaveLoadResult LoadGameSave(string saveFilePath, [CanBeNull] out GameSaveData gameSaveData)
        {
            if (!File.Exists(saveFilePath))
            {
                Debug.LogWarning("存档文件不存在，需要先手动创建。");
                gameSaveData = null;
                return GameSaveLoadResult.FileNotFound;
            }

            try
            {
                string json = File.ReadAllText(saveFilePath);
                if (!JsonSerializer.TryDeserialize(json, out gameSaveData) || gameSaveData == null)
                {
                    Debug.LogError($"存档内容无法解析，已备份原文件：{saveFilePath}");
                    BackupSaveFile(saveFilePath);
                    gameSaveData = null;
                    return GameSaveLoadResult.InvalidData;
                }

                Debug.Log("存档读取成功，进行校验。");

                // 计算存档数据的校验值并验证
                string verification = CalculateVerification(gameSaveData);
                if (verification != gameSaveData.Verification)
                {
                    Debug.LogWarning("存档校验失败，存档可能被篡改或版本不匹配。");
                    BackupSaveFile(saveFilePath);
                    gameSaveData = null;
                    return GameSaveLoadResult.InvalidData;
                }

                Debug.Log("存档校验通过。");
                return GameSaveLoadResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载存档时发生错误：{ex.Message}");
                BackupSaveFile(saveFilePath);
                gameSaveData = null;
                return GameSaveLoadResult.UnknownError;
            }
        }

        /// <summary>
        /// 创建一个默认的游戏存档数据，不会自动保存
        /// </summary>
        /// <returns>默认的游戏存档数据</returns>
        public static GameSaveData CreateDefaultGameSave()
        {
            return new GameSaveData
            {
                Version = 1,
                CreateTime = DateTime.Now,
                SaveTime = DateTime.Now,
                Verification = null, // 校验码暂时为空
                MusicGameData = new GameSaveData.MusicGameSaveData()
            };
        }

        /// <summary>
        /// 计算存档数据的校验值，用于验证存档是否未被篡改
        /// </summary>
        /// <param name="gameSaveData">要计算校验值的存档数据</param>
        /// <returns>存档数据的校验值</returns>
        /// <remarks>校验文本取单行序列化结果：只取决于数据本身，与存档文件的缩进写法无关</remarks>
        /// <exception cref="InvalidOperationException">存档数据无法序列化</exception>
        private static string CalculateVerification(GameSaveData gameSaveData)
        {
            // 克隆存档数据，但不包含Verification属性
            GameSaveData clonedGameSaveData = new GameSaveData
            {
                Version = gameSaveData.Version,
                CreateTime = gameSaveData.CreateTime,
                SaveTime = gameSaveData.SaveTime,
                MusicGameData = gameSaveData.MusicGameData
            };

            if (!JsonSerializer.TrySerializeCompact(clonedGameSaveData, out string json))
                throw new InvalidOperationException("存档数据无法序列化，校验值计算失败。");

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
                StringBuilder hashString = new StringBuilder();

                foreach (byte b in hashBytes)
                {
                    hashString.Append(b.ToString("x2"));
                }

                return hashString.ToString();
            }
        }

        /// <summary>
        /// 重命名原存档并备份（在存档校验失败时）
        /// </summary>
        private static void BackupSaveFile(string saveFilePath)
        {
            string saveDirectory = Path.GetDirectoryName(saveFilePath);
            if (saveDirectory == null)
            {
                throw new ArgumentNullException(nameof(saveFilePath), "存档文件路径无效，无法获取目录。");
            }

            // 查找一个可用的备份文件名
            int backupNumber = 1;
            string backupFileName = $"CyanStarsGameSaveBackup{backupNumber}.json";
            string backupFilePath = PathUtil.Combine(saveDirectory, backupFileName);

            while (File.Exists(backupFilePath))
            {
                backupNumber++;
                backupFileName = $"CyanStarsGameSaveBackup{backupNumber}.json";
                backupFilePath = PathUtil.Combine(saveDirectory, backupFileName);
            }

            File.Move(saveFilePath, backupFilePath);
            Debug.Log($"存档已备份，备份文件名为：{backupFileName}");
        }

        // Todo: 清理备份文件（低优先级）
    }
}
