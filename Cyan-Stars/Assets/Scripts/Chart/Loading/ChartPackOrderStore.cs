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
using IOFile = System.IO.File;

namespace CyanStars.Chart.Loading
{
    /// <summary>
    /// 社区谱包顺序的持久化存储
    /// </summary>
    /// <remarks>
    /// <para>顺序文件固定在应用数据目录的 <c>Data</c> 文件夹下。
    /// 内容是字典：键为社区谱包根目录的绝对路径，值为列表，按游戏内顺序排序的谱包文件夹名。
    /// （相对根目录的一级路径，加载时拼接两者）。</para>
    /// <para>内置谱包不在此记录，每次加载以 SO 顺序为准，必定排在社区谱包之前。</para>
    /// <para>谱面顺序不在此记录，由元数据文件中的顺序定义。</para>
    /// </remarks>
    public static class ChartPackOrderStore
    {
        private const string DataFolderName = "Data";
        private const string OrderFileName = "PlayerChartPackOrder.json";

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);


        /// <summary>
        /// 社区谱包顺序文件的路径
        /// </summary>
        private static string OrderFilePath =>
            PathUtil.Combine(GameRoot.File.Environment.PersistentDataRoot, DataFolderName, OrderFileName);


        /// <summary>
        /// 把本次扫描到的社区谱包文件夹名按持久化记录排序
        /// </summary>
        /// <param name="rootFolderPath">社区谱包根目录的绝对路径</param>
        /// <param name="scannedPackFolderNames">本次从磁盘扫描到的谱包文件夹名，不要求有序</param>
        /// <returns>按游戏内顺序排列、且已去重的谱包文件夹名列表</returns>
        /// <remarks>
        /// <para>已记录但没扫描到的谱包视为丢失，从记录中删去；未记录的新谱包按文件夹名排序追加到最后。</para>
        /// <para>记录有变化时才写回文件。</para>
        /// </remarks>
        public static List<string> ApplyPersistedOrder(
            string rootFolderPath,
            IReadOnlyList<string> scannedPackFolderNames
        )
        {
            // 扫描结果按平台路径大小写敏感性规则去重，保留磁盘上的实际文件夹名
            var scannedNames = new List<string>();
            var knownNames = new HashSet<string>(PathUtil.PathComparer);
            foreach (string name in scannedPackFolderNames)
            {
                if (!string.IsNullOrEmpty(name) && knownNames.Add(name))
                    scannedNames.Add(name);
            }

            Dictionary<string, List<string>> orderData = LoadOrderData();
            string? rootKey = FindRootKey(orderData, rootFolderPath);
            List<string>? savedOrder = rootKey == null ? null : orderData[rootKey];

            var orderedNames = new List<string>();
            var usedNames = new HashSet<string>(PathUtil.PathComparer);

            // 已记录的谱包按记录顺序排前，其中丢失或重复的项在此被剔除
            if (savedOrder != null)
            {
                foreach (string savedName in savedOrder)
                {
                    if (knownNames.Contains(savedName) && usedNames.Add(savedName))
                        orderedNames.Add(savedName);
                }
            }

            // 不在记录中的新谱包按文件夹名排序，追加到最后
            scannedNames.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string scannedName in scannedNames)
            {
                if (usedNames.Add(scannedName))
                    orderedNames.Add(scannedName);
            }

            // 记录缺失、有谱包丢失或新增时写回文件
            bool changed = savedOrder == null || !orderedNames.SequenceEqual(savedOrder, PathUtil.PathComparer);
            if (changed)
            {
                orderData[rootKey ?? PathUtil.Normalize(rootFolderPath)] = orderedNames;
                if (!TrySaveOrderData(orderData, out string error))
                    Debug.LogWarning(error);
            }

            return orderedNames;
        }

        /// <summary>
        /// 按给定顺序覆盖某个根目录的记录并写回文件，见于游戏内调整谱包顺序后的持久化
        /// </summary>
        /// <param name="rootFolderPath">社区谱包根目录的绝对路径</param>
        /// <param name="orderedPackFolderNames">按游戏内顺序排列的谱包文件夹名列表</param>
        /// <param name="error">失败原因；保存成功时为空字符串</param>
        /// <remarks>内置谱包以 SO 顺序为准，不出现在此列表中</remarks>
        public static bool TrySaveOrder(
            string rootFolderPath,
            IReadOnlyList<string> orderedPackFolderNames,
            out string error
        )
        {
            Dictionary<string, List<string>> orderData = LoadOrderData();
            string? rootKey = FindRootKey(orderData, rootFolderPath);

            var orderedNames = new List<string>();
            var usedNames = new HashSet<string>(PathUtil.PathComparer);
            foreach (string name in orderedPackFolderNames)
            {
                if (!string.IsNullOrEmpty(name) && usedNames.Add(name))
                    orderedNames.Add(name);
            }

            orderData[rootKey ?? PathUtil.Normalize(rootFolderPath)] = orderedNames;
            return TrySaveOrderData(orderData, out error);
        }


        /// <summary>
        /// 读取顺序文件；文件缺失、内容为空或解析失败时返回空字典（视为无记录）
        /// </summary>
        private static Dictionary<string, List<string>> LoadOrderData()
        {
            string orderFilePath = OrderFilePath;

            if (!IOFile.Exists(orderFilePath))
                return new Dictionary<string, List<string>>();

            try
            {
                string json = IOFile.ReadAllText(orderFilePath, Encoding.UTF8);
                Dictionary<string, List<string>>? orderData =
                    JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);

                if (orderData == null)
                {
                    Debug.LogWarning($"社区谱包顺序文件内容为空，本次已视为无记录：{orderFilePath}");
                    return new Dictionary<string, List<string>>();
                }

                return orderData;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"读取社区谱包顺序文件失败，本次已视为无记录：{orderFilePath}\n{e.Message}");
                return new Dictionary<string, List<string>>();
            }
        }

        /// <summary>
        /// 保存顺序文件
        /// </summary>
        private static bool TrySaveOrderData(Dictionary<string, List<string>> orderData, out string error)
        {
            error = "";

            string orderFilePath = OrderFilePath;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(orderFilePath)!);

                string json = JsonConvert.SerializeObject(orderData, Formatting.Indented);
                IOFile.WriteAllText(orderFilePath, json, Utf8NoBom);

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
        /// 查找与给定根目录指向同一文件夹的既有键，保留记录中的原始写法
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
