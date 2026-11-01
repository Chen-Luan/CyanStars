#nullable enable

using System.Collections.Generic;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面内容指纹
    /// </summary>
    /// <remarks>
    /// <para>只收录参与谱面身份判定的关键字段：bpm 组、预备拍、变速组、音符与排列、特效轨道。
    /// 标题、曲绘、谱包介绍、难度定数等展示信息不在其中</para>
    /// <para>字段一律为整数、布尔或字符串：浮点数与颜色由 <see cref="ChartFingerprintBuilder"/> 转成固定文本，
    /// 使指纹文本不随序列化器的数字写出格式变化，避免程序升级导致已有哈希与成绩键失配</para>
    /// <para>指纹结构发生变化时递增 <see cref="FingerprintFormat"/>，并同步递增 <see cref="ChartHash.VersionPrefix"/></para>
    /// </remarks>
    public class ChartFingerprintData
    {
        /// <summary>
        /// 指纹结构版本
        /// </summary>
        public string FingerprintFormat = FingerprintFormatValue;

        /// <summary>
        /// 当前指纹结构版本
        /// </summary>
        public const string FingerprintFormatValue = "fp0";

        /// <summary>
        /// bpm 组，顺序承载语义（相邻组共同决定每拍的时长）
        /// </summary>
        public List<ChartBpmFingerprint> BpmGroup = new List<ChartBpmFingerprint>();

        /// <summary>
        /// 第一个 bpm 组开始前的预备拍次数
        /// </summary>
        public long ReadyBeat;

        /// <summary>
        /// 变速组，顺序承载语义（音符按下标引用变速组）
        /// </summary>
        public List<ChartSpeedFingerprint> SpeedGroups = new List<ChartSpeedFingerprint>();

        /// <summary>
        /// 音符，每项是一张音符的紧凑文本，按文本排序后写出
        /// </summary>
        /// <remarks>音符在加载与编辑时都会按判定拍重排，因此书写顺序不参与哈希</remarks>
        public List<string> Notes = new List<string>();

        /// <summary>
        /// 特效轨道，顺序承载语义（轨道按下标被引用）
        /// </summary>
        public List<ChartTrackFingerprint> Tracks = new List<ChartTrackFingerprint>();
    }

    /// <summary>
    /// bpm 组单项指纹
    /// </summary>
    public class ChartBpmFingerprint
    {
        /// <summary>
        /// 每分钟拍数，固定文本
        /// </summary>
        public string Bpm = "";

        /// <summary>
        /// 本组生效起始拍
        /// </summary>
        public ChartBeatFingerprint StartBeat = new ChartBeatFingerprint();
    }

    /// <summary>
    /// 变速组单项指纹
    /// </summary>
    /// <remarks>不含变速组名称，名称只是制谱器内的备注</remarks>
    public class ChartSpeedFingerprint
    {
        /// <summary>
        /// 相对速度或绝对速度
        /// </summary>
        public int Type;

        /// <summary>
        /// 速度曲线关键帧，顺序承载语义（曲线按时间单调递增）
        /// </summary>
        public List<ChartBezierPointFingerprint> Points = new List<ChartBezierPointFingerprint>();
    }

    /// <summary>
    /// 贝塞尔曲线关键帧指纹
    /// </summary>
    public class ChartBezierPointFingerprint
    {
        /// <summary>
        /// 位置点时间，单位 ms
        /// </summary>
        public int PositionMsTime;

        /// <summary>
        /// 位置点速度，固定文本
        /// </summary>
        public string PositionValue = "";

        /// <summary>
        /// 左控制点时间，单位 ms
        /// </summary>
        public int LeftMsTime;

        /// <summary>
        /// 左控制点速度，固定文本
        /// </summary>
        public string LeftValue = "";

        /// <summary>
        /// 右控制点时间，单位 ms
        /// </summary>
        public int RightMsTime;

        /// <summary>
        /// 右控制点速度，固定文本
        /// </summary>
        public string RightValue = "";
    }

    /// <summary>
    /// 音符指纹
    /// </summary>
    /// <remarks>
    /// 各类音符共用一个形状，由 <see cref="Type"/> 判别；与该类型无关的字段保持默认值，
    /// 这样新增音符类型时只需补充判别取值，不必改动指纹形状
    /// </remarks>
    public class ChartNoteFingerprint
    {
        /// <summary>
        /// 音符类型
        /// </summary>
        public int Type;

        /// <summary>
        /// 头判定引用的变速组下标
        /// </summary>
        public int SpeedGroupIndex;

        /// <summary>
        /// 变速偏移
        /// </summary>
        public int SpeedGroupOffset;

        /// <summary>
        /// 尾判定引用的变速组下标，仅长按音符有意义
        /// </summary>
        public int HoldEndSpeedGroupIndex;

        /// <summary>
        /// 所在轨道，仅 Break 音符有意义
        /// </summary>
        public int BreakNotePos;

        /// <summary>
        /// 水平轨道位置比例，固定文本，非常规音符为空
        /// </summary>
        public string? Pos;

        /// <summary>
        /// 判定拍
        /// </summary>
        public ChartBeatFingerprint JudgeBeat = new ChartBeatFingerprint();

        /// <summary>
        /// 结束判定拍，仅长按音符有意义
        /// </summary>
        public ChartBeatFingerprint? EndJudgeBeat;

        /// <summary>
        /// 是否可被判定
        /// </summary>
        public bool JudgeAble;

        /// <summary>
        /// 是否可被展示
        /// </summary>
        public bool ViewAble;

        /// <summary>
        /// 正解提示音
        /// </summary>
        public string? CorrectAudioName;

        /// <summary>
        /// 打击音
        /// </summary>
        public string? HitAudioName;
    }

    /// <summary>
    /// 特效轨道指纹
    /// </summary>
    public class ChartTrackFingerprint
    {
        /// <summary>
        /// 轨道类型键
        /// </summary>
        public string TrackKey = "";

        /// <summary>
        /// 轨道数据投影，属性名为键、固定文本为值，按属性名升序
        /// </summary>
        /// <remarks>新出现的轨道类型无需改动本类即可参与哈希，无法投影的属性类型会让计算抛出异常</remarks>
        public Dictionary<string, string> TrackData = new Dictionary<string, string>();
    }

    /// <summary>
    /// 节拍指纹
    /// </summary>
    /// <remarks>约分后的三元组，同一拍位的不同分数写法得到相同指纹</remarks>
    public class ChartBeatFingerprint
    {
        /// <summary>
        /// 整数部分
        /// </summary>
        public int IntegerPart;

        /// <summary>
        /// 分数部分分子
        /// </summary>
        public int Numerator;

        /// <summary>
        /// 分数部分分母
        /// </summary>
        public int Denominator;
    }
}
