#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CyanStars.Framework;
using CyanStars.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace CyanStars.Chart.Loading
{
    /// <summary>
    /// 社区谱包顺序的持久化存储
    /// </summary>
    /// <remarks>
    /// 顺序文件保存在持久化数据目录的 <c>Data/PlayerChartPackOrder.json</c>，
    /// 结构为「社区谱包根目录绝对路径 -> 有序的一级文件夹名列表」。
    /// 仅管理社区谱包顺序；内置谱包以 SO 为准，单谱包内的谱面顺序由谱包元数据自身决定。
    /// </remarks>
    public static class ChartPackOrderStore
    {
        private const string DataFolderName = "Data";
        private const string OrderFileName = "PlayerChartPackOrder.json";

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private static string OrderFilePath =>
            PathUtil.Combine(GameRoot.File.Environment.PersistentDataRoot, DataFolderName, OrderFileName);

        /// <summary>
        /// 将本次扫描到的社区谱包文件夹名按持久化记录排序并同步失效/新增项
        /// </summary>
        /// <remarks>
        /// 已记录但磁盘上已不存在的谱包会被移除；新扫描到的谱包按文件夹名排序追加到末尾。
        /// 仅当顺序或成员发生变化时写回文件。
        /// </remarks>
        public static List<string> ApplyPersistedOrder(
            string rootFolderPath,
            IReadOnlyList<string> scannedPackFolderNames
        )
        {
            // 按平台路径大小写规则去重，同时保留磁盘上的实际文件夹名
            var diskNameMap = new Dictionary<string, string>(PathUtil.PathComparer);
            foreach (string name in scannedPackFolderNames)
            {
                if (!string.IsNullOrEmpty(name) && !diskNameMap.ContainsKey(name))
                    diskNameMap[name] = name;
            }

            Dictionary<string, List<string>> orderData = LoadOrderData();
            string? rootKey = FindRootKey(orderData, rootFolderPath);
            List<string>? savedOrder = rootKey == null ? null : orderData[rootKey];

            var orderedNames = new List<string>(diskNameMap.Count);
            var usedNames = new HashSet<string>(PathUtil.PathComparer);

            // 1. 优先保留已记录且仍存在于磁盘的谱包顺序（同时剔除重复/已删除项）
            if (savedOrder != null)
            {
                foreach (string savedName in savedOrder)
                {
                    if (!string.IsNullOrEmpty(savedName) &&
                        diskNameMap.TryGetValue(savedName, out string? actualName) &&
                        usedNames.Add(actualName))
                    {
                        orderedNames.Add(actualName);
                    }
                }
            }

            // 2. 未记录的新谱包按文件夹名排序追加到最后
            var newNames = new List<string>();
            foreach (string actualName in diskNameMap.Values)
            {
                if (usedNames.Add(actualName))
                    newNames.Add(actualName);
            }
            newNames.Sort(StringComparer.OrdinalIgnoreCase);
            orderedNames.AddRange(newNames);

            bool changed = savedOrder == null || !orderedNames.SequenceEqual(savedOrder, PathUtil.PathComparer);
            if (changed && !SaveOrderForRoot(orderData, rootKey, rootFolderPath, orderedNames, out string error))
            {
                Debug.LogWarning(error);
            }

            return orderedNames;
        }

        /// <summary>
        /// 按给定顺序覆盖指定根目录的持久化记录（用于游戏内手动调整谱包顺序）
        /// </summary>
        public static bool TrySaveOrder(
            string rootFolderPath,
            IReadOnlyList<string> orderedPackFolderNames,
            out string error
        )
        {
            Dictionary<string, List<string>> orderData = LoadOrderData();
            string? rootKey = FindRootKey(orderData, rootFolderPath);

            var orderedNames = new List<string>(orderedPackFolderNames.Count);
            var usedNames = new HashSet<string>(PathUtil.PathComparer);
            foreach (string name in orderedPackFolderNames)
            {
                if (!string.IsNullOrEmpty(name) && usedNames.Add(name))
                    orderedNames.Add(name);
            }

            return SaveOrderForRoot(orderData, rootKey, rootFolderPath, orderedNames, out error);
        }

        private static bool SaveOrderForRoot(
            Dictionary<string, List<string>> orderData,
            string? existingRootKey,
            string rootFolderPath,
            List<string> orderedNames,
            out string error
        )
        {
            orderData[existingRootKey ?? PathUtil.Normalize(rootFolderPath)] = orderedNames;
            return TrySaveOrderData(orderData, out error);
        }

        private static Dictionary<string, List<string>> LoadOrderData()
        {
            string orderFilePath = OrderFilePath;
            if (!File.Exists(orderFilePath))
                return new Dictionary<string, List<string>>();

            try
            {
                string json = File.ReadAllText(orderFilePath, Encoding.UTF8);
                var orderData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
                if (orderData == null)
                {
                    Debug.LogWarning($"社区谱包顺序文件内容为空，已视为无记录：{orderFilePath}");
                    return new Dictionary<string, List<string>>();
                }

                return orderData;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"读取社区谱包顺序文件失败，已视为无记录：{orderFilePath}\n{e.Message}");
                return new Dictionary<string, List<string>>();
            }
        }

        private static bool TrySaveOrderData(Dictionary<string, List<string>> orderData, out string error)
        {
            error = string.Empty;
            string orderFilePath = OrderFilePath;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(orderFilePath)!);
                string json = JsonConvert.SerializeObject(orderData, Formatting.Indented);
                File.WriteAllText(orderFilePath, json, Utf8NoBom);
                return true;
            }
            catch (Exception e)
            {
                error = $"保存社区谱包顺序文件失败：{e.Message}";
                Debug.LogError(error);
                return false;
            }
        }

        /// <summary>
        /// 查找与目标根目录指向同一位置的既有键，以保留原始路径写法
        /// </summary>
        private static string? FindRootKey(Dictionary<string, List<string>> orderData, string rootFolderPath)
        {
            foreach (string key in orderData.Keys)
            {
                if (PathUtil.PathEquals(key, rootFolderPath))
                    return key;
            }

            return null;
        }
    }
}
