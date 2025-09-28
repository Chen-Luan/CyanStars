using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace CyanStars.Chart
{
    /// <summary>
    /// 运行时谱面数据
    /// </summary>
    public class Chart
    {
        /// <summary>
        /// 谱面数据
        /// </summary>
        public ChartData ChartData;

        /// <summary>
        /// 谱面引用的元数据
        /// </summary>
        public ChartMetadata ChartMetadata;

        /// <summary>
        /// 谱面哈希，用于校验成绩
        /// </summary>
        public string ChartHash;


        public Chart(ChartData chartData, ChartMetadata chartMetadata)
        {
            ChartData = chartData;
            ChartMetadata = chartMetadata;
            ChartHash = CalculateHash(chartData);
        }

        private static string CalculateHash(ChartData data)
        {
            if (data == null)
            {
                return null;
            }

            var contentToHash = new
            {
                data.DataVersion,
                data.ReadyBeat,
                data.BpmGroup,
                data.SpeedGroupDatas,
                data.Notes,
                data.TrackDatas
            };
            var json = JsonConvert.SerializeObject(contentToHash,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Formatting = Formatting.None
                });

            using var sha256 = SHA256.Create();
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] hashBytes = sha256.ComputeHash(jsonBytes);

            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
