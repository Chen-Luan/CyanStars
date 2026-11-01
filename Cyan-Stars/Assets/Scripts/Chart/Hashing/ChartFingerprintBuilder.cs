#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using CyanStars.Chart.BezierCurve;
using CyanStars.Chart.Tracks;
using CyanStars.Framework.File.Serialization;
using UnityEngine;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面内容指纹的构建
    /// </summary>
    /// <remarks>
    /// <para>所有的内容等价规则集中在本类：节拍约分、音符书写顺序无关、变速组备注名不参与、
    /// 浮点与颜色转成固定文本</para>
    /// <para>构建只读取领域对象，不读取谱面文件的文本，因此 json 的键顺序、缩进与数字写法都不影响指纹</para>
    /// </remarks>
    internal static class ChartFingerprintBuilder
    {
        /// <summary>
        /// 构建指纹
        /// </summary>
        /// <param name="source">谱面哈希的输入内容</param>
        /// <returns>可直接序列化的谱面指纹</returns>
        /// <exception cref="NotSupportedException">特效轨道出现无法投影的属性类型</exception>
        public static ChartFingerprintData Build(ChartHashSource source)
        {
            ChartFingerprintData fingerprint = new ChartFingerprintData
            {
                FingerprintFormat = ChartFingerprintData.FingerprintFormatValue,
                ReadyBeat = source.ChartData.ReadyBeat,
                BpmGroup = BuildBpmGroup(source.ChartPackData),
                SpeedGroups = BuildSpeedGroups(source.ChartData),
                Notes = BuildNotes(source.ChartData),
                Tracks = BuildTracks(source.ChartData)
            };

            return fingerprint;
        }

        private static List<ChartBpmFingerprint> BuildBpmGroup(ChartPackData chartPackData)
        {
            List<ChartBpmFingerprint> group = new List<ChartBpmFingerprint>();

            foreach (BpmGroupItem item in chartPackData.BpmGroup)
            {
                group.Add(new ChartBpmFingerprint
                {
                    Bpm = FormatFloat(item.Bpm),
                    StartBeat = BuildBeat(item.StartBeat)
                });
            }

            return group;
        }

        private static List<ChartSpeedFingerprint> BuildSpeedGroups(ChartData chartData)
        {
            List<ChartSpeedFingerprint> groups = new List<ChartSpeedFingerprint>();

            foreach (SpeedTemplateData speedGroupData in chartData.SpeedGroupDatas)
            {
                ChartSpeedFingerprint speedGroup = new ChartSpeedFingerprint
                {
                    Type = (int)speedGroupData.Type
                };

                if (speedGroupData.BezierCurves != null)
                {
                    foreach (BezierPoint point in speedGroupData.BezierCurves.Points)
                    {
                        speedGroup.Points.Add(new ChartBezierPointFingerprint
                        {
                            PositionMsTime = point.PositionPoint.MsTime,
                            PositionValue = FormatFloat(point.PositionPoint.Value),
                            LeftMsTime = point.LeftControlPoint.MsTime,
                            LeftValue = FormatFloat(point.LeftControlPoint.Value),
                            RightMsTime = point.RightControlPoint.MsTime,
                            RightValue = FormatFloat(point.RightControlPoint.Value)
                        });
                    }
                }

                groups.Add(speedGroup);
            }

            return groups;
        }

        /// <summary>
        /// 构建音符指纹文本列表并按文本排序
        /// </summary>
        /// <remarks>书写顺序不参与哈希，但重复的音符逐项保留，音符总数仍参与摘要</remarks>
        private static List<string> BuildNotes(ChartData chartData)
        {
            List<string> notes = new List<string>();

            foreach (BaseChartNoteData noteData in chartData.Notes)
            {
                if (!JsonSerializer.TrySerializeCompact(BuildNote(noteData), out string noteText))
                    throw new InvalidOperationException($"音符无法投影为指纹文本：{noteData?.GetType().Name}");

                notes.Add(noteText);
            }

            notes.Sort(StringComparer.Ordinal);
            return notes;
        }

        private static ChartNoteFingerprint BuildNote(BaseChartNoteData noteData)
        {
            ChartNoteFingerprint note = new ChartNoteFingerprint
            {
                Type = (int)noteData.Type,
                SpeedGroupIndex = noteData.SpeedGroupIndex,
                SpeedGroupOffset = noteData.SpeedGroupOffset,
                JudgeBeat = BuildBeat(noteData.JudgeBeat),
                JudgeAble = noteData.JudgeAble,
                ViewAble = noteData.ViewAble,
                CorrectAudioName = noteData.CorrectAudioName,
                HitAudioName = noteData.HitAudioName
            };

            if (noteData is IChartNoteNormalPos normalPos)
                note.Pos = FormatFloat(normalPos.Pos);

            switch (noteData)
            {
                case HoldChartNoteData hold:
                    note.HoldEndSpeedGroupIndex = hold.HoldEndSpeedGroupIndex;
                    note.EndJudgeBeat = BuildBeat(hold.EndJudgeBeat);
                    break;
                case BreakChartNoteData breakNote:
                    note.BreakNotePos = (int)breakNote.BreakNotePos;
                    break;
            }

            return note;
        }

        private static List<ChartTrackFingerprint> BuildTracks(ChartData chartData)
        {
            List<ChartTrackFingerprint> tracks = new List<ChartTrackFingerprint>();

            foreach (ChartTrackData trackData in chartData.TrackDatas)
            {
                ChartTrackFingerprint track = new ChartTrackFingerprint
                {
                    TrackKey = trackData.TrackKey,
                    TrackData = BuildTrackProperties(trackData.TrackData)
                };

                tracks.Add(track);
            }

            return tracks;
        }

        /// <summary>
        /// 把轨道数据对象的公共成员投影为有序文本字典
        /// </summary>
        /// <remarks>
        /// <para>公共属性与公共字段都参与投影，与谱面文件里轨道数据的 json 形状一致；
        /// 新出现的轨道类型无需改动本类即可参与哈希</para>
        /// <para>出现无法投影的成员类型时抛出异常，避免轨道内容被静默忽略</para>
        /// </remarks>
        private static Dictionary<string, string> BuildTrackProperties(IChartTrackData? trackData)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>(StringComparer.Ordinal);

            if (trackData == null)
                return properties;

            Type ownerType = trackData.GetType();

            List<MemberInfo> members =
                new List<MemberInfo>(ownerType.GetProperties(BindingFlags.Public | BindingFlags.Instance));
            members.AddRange(ownerType.GetFields(BindingFlags.Public | BindingFlags.Instance));
            members.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));

            foreach (MemberInfo member in members)
            {
                if (member is PropertyInfo property)
                {
                    if (!property.CanRead || property.GetIndexParameters().Length > 0)
                        continue;

                    properties[member.Name] = FormatPropertyValue(
                        property.GetValue(trackData, null), ownerType, member.Name);
                    continue;
                }

                properties[member.Name] = FormatPropertyValue(
                    ((FieldInfo)member).GetValue(trackData), ownerType, member.Name);
            }

            return properties;
        }

        private static string FormatPropertyValue(object? value, Type ownerType, string propertyName)
        {
            switch (value)
            {
                case null:
                    return "";
                case Beat beat:
                    ChartBeatFingerprint simplified = BuildBeat(beat);
                    return $"{simplified.IntegerPart}+{simplified.Numerator}/{simplified.Denominator}";
                case Color color:
                    return $"{FormatFloat(color.r)}|{FormatFloat(color.g)}|{FormatFloat(color.b)}|{FormatFloat(color.a)}";
                case string text:
                    return text;
                case bool flag:
                    return flag ? "true" : "false";
                case float floatValue:
                    return FormatFloat(floatValue);
                case double doubleValue:
                    return FormatFloat((float)doubleValue);
                case Enum enumeration:
                    return Convert.ToInt64(enumeration, CultureInfo.InvariantCulture)
                        .ToString(CultureInfo.InvariantCulture);
                case int integerValue:
                    return integerValue.ToString(CultureInfo.InvariantCulture);
                case long longValue:
                    return longValue.ToString(CultureInfo.InvariantCulture);
            }

            throw new NotSupportedException(
                $"特效轨道 {ownerType.Name} 的成员 {propertyName} 类型为 {value!.GetType().Name}，" +
                "无法纳入谱面哈希，需要为其定义固定的文本投影形式。");
        }

        /// <summary>
        /// 节拍约分后的指纹
        /// </summary>
        private static ChartBeatFingerprint BuildBeat(Beat beat)
        {
            Beat simplified = beat.Simplify();

            return new ChartBeatFingerprint
            {
                IntegerPart = simplified.IntegerPart,
                Numerator = simplified.Numerator,
                Denominator = simplified.Denominator
            };
        }

        /// <summary>
        /// 浮点值的固定文本
        /// </summary>
        /// <remarks>
        /// 用最短可往返格式并按不变文化写出，负零与非数一并归一，
        /// 使指纹文本只取决于数值本身，与 json 里的原始写法和序列化器的数字格式无关
        /// </remarks>
        private static string FormatFloat(float value)
        {
            if (float.IsNaN(value))
                return "NaN";

            if (float.IsInfinity(value))
                return value > 0 ? "Infinity" : "-Infinity";

            return (value == 0f ? 0f : value).ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
