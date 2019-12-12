// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Beatmaps.Formats
{
    public class LegacyBeatmapEncoder
    {
        public const int LATEST_VERSION = 14234;

        private readonly IBeatmap beatmap;

        public LegacyBeatmapEncoder(IBeatmap beatmap)
        {
            this.beatmap = beatmap;

            if (beatmap.BeatmapInfo.RulesetID < 0 || beatmap.BeatmapInfo.RulesetID > 3)
                throw new ArgumentException("Only beatmaps in the osu, taiko, catch, or mania rulesets can be encoded to the legacy beatmap format.", nameof(beatmap));
        }

        public void Encode(TextWriter writer)
        {
            writer.WriteLine($"osu file format v{LATEST_VERSION}");

            writer.WriteLine();
            handleGeneral(writer);

            writer.WriteLine();
            handleEditor(writer);

            writer.WriteLine();
            handleMetadata(writer);

            writer.WriteLine();
            handleDifficulty(writer);

            writer.WriteLine();
            handleEvents(writer);

            writer.WriteLine();
            handleTimingPoints(writer);

            writer.WriteLine();
            handleHitObjects(writer);
        }

        private void handleGeneral(TextWriter writer)
        {
            writer.WriteLine("[General]");

            writer.WriteLine(FormattableString.Invariant($"AudioFilename: {Path.GetFileName(beatmap.Metadata.AudioFile)}"));
            writer.WriteLine(FormattableString.Invariant($"AudioLeadIn: {beatmap.BeatmapInfo.AudioLeadIn}"));
            writer.WriteLine(FormattableString.Invariant($"PreviewTime: {beatmap.Metadata.PreviewTime}"));
            // Todo: Not all countdown types are supported by lazer yet
            writer.WriteLine(FormattableString.Invariant($"Countdown: {(beatmap.BeatmapInfo.Countdown ? "1" : "0")}"));
            writer.WriteLine(FormattableString.Invariant($"SampleSet: {toLegacySampleBank(beatmap.ControlPointInfo.SamplePoints[0].SampleBank)}"));
            writer.WriteLine(FormattableString.Invariant($"StackLeniency: {beatmap.BeatmapInfo.StackLeniency}"));
            writer.WriteLine(FormattableString.Invariant($"Mode: {beatmap.BeatmapInfo.RulesetID}"));
            writer.WriteLine(FormattableString.Invariant($"LetterboxInBreaks: {(beatmap.BeatmapInfo.LetterboxInBreaks ? "1" : "0")}"));
            // if (beatmap.BeatmapInfo.UseSkinSprites)
            //     writer.WriteLine(@"UseSkinSprites: 1");
            // if (b.AlwaysShowPlayfield)
            //     writer.WriteLine(@"AlwaysShowPlayfield: 1");
            // if (b.OverlayPosition != OverlayPosition.NoChange)
            //     writer.WriteLine(@"OverlayPosition: " + b.OverlayPosition);
            // if (!string.IsNullOrEmpty(b.SkinPreference))
            //     writer.WriteLine(@"SkinPreference:" + b.SkinPreference);
            // if (b.EpilepsyWarning)
            //     writer.WriteLine(@"EpilepsyWarning: 1");
            // if (b.CountdownOffset > 0)
            //     writer.WriteLine(@"CountdownOffset: " + b.CountdownOffset.ToString());
            if (beatmap.BeatmapInfo.RulesetID == 3)
                writer.WriteLine(FormattableString.Invariant($"SpecialStyle: {(beatmap.BeatmapInfo.SpecialStyle ? "1" : "0")}"));
            writer.WriteLine(FormattableString.Invariant($"WidescreenStoryboard: {(beatmap.BeatmapInfo.WidescreenStoryboard ? "1" : "0")}"));
            // if (b.SamplesMatchPlaybackRate)
            //     writer.WriteLine(@"SamplesMatchPlaybackRate: 1");
        }

        private void handleEditor(TextWriter writer)
        {
            writer.WriteLine("[Editor]");

            if (beatmap.BeatmapInfo.Bookmarks.Length > 0)
                writer.WriteLine(FormattableString.Invariant($"Bookmarks: {string.Join(',', beatmap.BeatmapInfo.Bookmarks)}"));
            writer.WriteLine(FormattableString.Invariant($"DistanceSpacing: {beatmap.BeatmapInfo.DistanceSpacing}"));
            writer.WriteLine(FormattableString.Invariant($"BeatDivisor: {beatmap.BeatmapInfo.BeatDivisor}"));
            writer.WriteLine(FormattableString.Invariant($"GridSize: {beatmap.BeatmapInfo.GridSize}"));
            writer.WriteLine(FormattableString.Invariant($"TimelineZoom: {beatmap.BeatmapInfo.TimelineZoom}"));
        }

        private void handleMetadata(TextWriter writer)
        {
            writer.WriteLine("[Metadata]");

            writer.WriteLine(FormattableString.Invariant($"Title: {beatmap.Metadata.Title}"));
            writer.WriteLine(FormattableString.Invariant($"TitleUnicode: {beatmap.Metadata.TitleUnicode}"));
            writer.WriteLine(FormattableString.Invariant($"Artist: {beatmap.Metadata.Artist}"));
            writer.WriteLine(FormattableString.Invariant($"ArtistUnicode: {beatmap.Metadata.ArtistUnicode}"));
            writer.WriteLine(FormattableString.Invariant($"Creator: {beatmap.Metadata.AuthorString}"));
            writer.WriteLine(FormattableString.Invariant($"Version: {beatmap.Metadata.Artist}"));
            writer.WriteLine(FormattableString.Invariant($"Source: {beatmap.Metadata.Source}"));
            writer.WriteLine(FormattableString.Invariant($"Tags: {beatmap.Metadata.Tags}"));
            writer.WriteLine(FormattableString.Invariant($"BeatmapID: {beatmap.BeatmapInfo.OnlineBeatmapID ?? 0}"));
            writer.WriteLine(FormattableString.Invariant($"BeatmapSetID: {beatmap.BeatmapInfo.BeatmapSet.OnlineBeatmapSetID ?? 0}"));
        }

        private void handleDifficulty(TextWriter writer)
        {
            writer.WriteLine("[Difficulty]");

            writer.WriteLine(FormattableString.Invariant($"HPDrainRate: {beatmap.BeatmapInfo.BaseDifficulty.DrainRate}"));
            writer.WriteLine(FormattableString.Invariant($"CircleSize: {beatmap.BeatmapInfo.BaseDifficulty.CircleSize}"));
            writer.WriteLine(FormattableString.Invariant($"OverallDifficulty: {beatmap.BeatmapInfo.BaseDifficulty.OverallDifficulty}"));
            writer.WriteLine(FormattableString.Invariant($"ApproachRate: {beatmap.BeatmapInfo.BaseDifficulty.ApproachRate}"));
            writer.WriteLine(FormattableString.Invariant($"SliderMultiplier: {beatmap.BeatmapInfo.BaseDifficulty.SliderMultiplier}"));
            writer.WriteLine(FormattableString.Invariant($"SliderTickRate: {beatmap.BeatmapInfo.BaseDifficulty.SliderTickRate}"));
        }

        private void handleEvents(TextWriter writer)
        {
            // Todo: Storyboard events
        }

        private void handleTimingPoints(TextWriter writer)
        {
            if (beatmap.ControlPointInfo.Groups.Count == 0)
                return;

            writer.WriteLine("[TimingPoints]");

            foreach (var group in beatmap.ControlPointInfo.Groups)
            {
                var timingPoint = group.ControlPoints.OfType<TimingControlPoint>().FirstOrDefault();
                var difficultyPoint = beatmap.ControlPointInfo.DifficultyPointAt(group.Time);
                var samplePoint = beatmap.ControlPointInfo.SamplePointAt(group.Time);
                var effectPoint = beatmap.ControlPointInfo.EffectPointAt(group.Time);

                // Convert beat length the legacy format
                double beatLength;
                if (timingPoint != null)
                    beatLength = timingPoint.BeatLength;
                else
                    beatLength = -100 / difficultyPoint.SpeedMultiplier;

                // Apply the control point to a hit sample to uncover legacy properties (e.g. suffix)
                HitSampleInfo tempHitSample = samplePoint.ApplyTo(new HitSampleInfo());

                // Convert effect flags to the legacy format
                LegacyEffectFlags effectFlags = LegacyEffectFlags.None;
                if (effectPoint.KiaiMode)
                    effectFlags |= LegacyEffectFlags.Kiai;
                if (effectPoint.OmitFirstBarLine)
                    effectFlags |= LegacyEffectFlags.OmitFirstBarLine;

                writer.Write(FormattableString.Invariant($"{group.Time},"));
                writer.Write(FormattableString.Invariant($"{beatLength},"));
                writer.Write(FormattableString.Invariant($"{(int)beatmap.ControlPointInfo.TimingPointAt(group.Time).TimeSignature},"));
                writer.Write(FormattableString.Invariant($"{(int)toLegacySampleBank(tempHitSample.Bank)},"));
                writer.Write(FormattableString.Invariant($"{toLegacyCustomSampleBank(tempHitSample.Suffix)},"));
                writer.Write(FormattableString.Invariant($"{tempHitSample.Volume},"));
                writer.Write(FormattableString.Invariant($"{(timingPoint != null ? "1" : "0")},"));
                writer.Write(FormattableString.Invariant($"{(int)effectFlags}"));
                writer.Write("\n");
            }
        }

        private void handleHitObjects(TextWriter writer)
        {
            if (beatmap.HitObjects.Count == 0)
                return;

            writer.WriteLine("[HitObjects]");

            foreach (var h in beatmap.HitObjects)
            {
                switch (beatmap.BeatmapInfo.RulesetID)
                {
                    case 0:
                        handleOsuHitObject(writer, h);
                        break;

                    case 1:
                        handleTaikoHitObject(writer, h);
                        break;

                    case 2:
                        handleCatchHitObject(writer, h);
                        break;

                    case 3:
                        handleManiaHitObject(writer, h);
                        break;
                }
            }
        }

        private void handleOsuHitObject(TextWriter writer, HitObject hitObject)
        {
            var positionData = hitObject as IHasPosition;
            var comboData = hitObject as IHasCombo;

            Debug.Assert(positionData != null);
            Debug.Assert(comboData != null);

            LegacyHitObjectType hitObjectType = (LegacyHitObjectType)(comboData.ComboOffset << 4);
            if (comboData.NewCombo)
                hitObjectType |= LegacyHitObjectType.NewCombo;

            if (hitObject is IHasCurve _)
                hitObjectType |= LegacyHitObjectType.Slider;
            else if (hitObject is IHasEndTime _)
                hitObjectType |= LegacyHitObjectType.Spinner;
            else
                hitObjectType |= LegacyHitObjectType.Circle;

            LegacyHitSoundType soundType = LegacyHitSoundType.Normal;
            HitSampleInfo firstAdditionSound = hitObject.Samples.FirstOrDefault(s => s.Name != HitSampleInfo.HIT_NORMAL);
            if (firstAdditionSound != null)
                soundType |= toLegacyHitSound(firstAdditionSound.Name);

            writer.Write(FormattableString.Invariant($"{positionData.X},"));
            writer.Write(FormattableString.Invariant($"{positionData.Y},"));
            writer.Write(FormattableString.Invariant($"{hitObject.StartTime},"));
            writer.Write(FormattableString.Invariant($"{(int)hitObjectType},"));
            writer.Write(FormattableString.Invariant($"{(int)soundType},"));

            if (hitObject is IHasCurve curveData)
            {
                for (int i = 0; i < curveData.Path.ControlPoints.Count; i++)
                {
                    PathControlPoint point = curveData.Path.ControlPoints[i];

                    switch (point.Type.Value)
                    {
                        case PathType.Bezier:
                            writer.Write("B|");
                            break;

                        case PathType.Catmull:
                            writer.Write("C|");
                            break;

                        case PathType.PerfectCurve:
                            writer.Write("P|");
                            break;

                        case PathType.Linear:
                            writer.Write("L|");
                            break;
                    }

                    writer.Write(FormattableString.Invariant($"{positionData.X + point.Position.Value.X}:{positionData.Y + point.Position.Value.Y}"));
                    writer.Write(i != curveData.Path.ControlPoints.Count - 1 ? "|" : ",");
                }

                writer.Write(FormattableString.Invariant($"{curveData.RepeatCount - 1},"));
                writer.Write(FormattableString.Invariant($"{curveData.Path.Distance},"));

                for (int i = 0; i < curveData.NodeSamples.Count; i++)
                {
                    LegacyHitSoundType type = LegacyHitSoundType.None;

                    foreach (var sample in curveData.NodeSamples[i])
                        type |= toLegacyHitSound(sample.Name);

                    writer.Write(FormattableString.Invariant($"{(int)type}"));
                    writer.Write(i != curveData.NodeSamples.Count - 1 ? "|" : ",");
                }

                for (int i = 0; i < curveData.NodeSamples.Count; i++)
                {
                    writer.Write(getSampleBank(curveData.NodeSamples[i], true));
                    writer.Write(i != curveData.NodeSamples.Count - 1 ? "|" : ",");
                }
            }
            else if (hitObject is IHasEndTime endTimeData)
                writer.Write(FormattableString.Invariant($"{endTimeData.EndTime},"));

            writer.Write(getSampleBank(hitObject.Samples));
            writer.Write(Environment.NewLine);
        }

        private void handleTaikoHitObject(TextWriter writer, HitObject hitObject)
        {
        }

        private void handleCatchHitObject(TextWriter writer, HitObject hitObject)
        {
        }

        private void handleManiaHitObject(TextWriter writer, HitObject hitObject)
        {
        }

        private string getSampleBank(IList<HitSampleInfo> samples, bool banksOnly = false)
        {
            LegacySampleBank normalBank = toLegacySampleBank(samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL)?.Bank);
            LegacySampleBank addBank = toLegacySampleBank(samples.FirstOrDefault(s => !string.IsNullOrEmpty(s.Name) && s.Name != HitSampleInfo.HIT_NORMAL)?.Bank);

            if (addBank == LegacySampleBank.None)
                addBank = normalBank;

            string customSampleBank = toLegacyCustomSampleBank(samples.FirstOrDefault()?.Suffix);
            string sampleFilename = samples.FirstOrDefault(s => string.IsNullOrEmpty(s.Name))?.LookupNames.First() ?? string.Empty;

            int volume = samples.First().Volume;

            StringBuilder sb = new StringBuilder();

            sb.Append(FormattableString.Invariant($"{(int)normalBank}:"));
            sb.Append(FormattableString.Invariant($"{(int)addBank}:"));

            if (!banksOnly)
            {
                sb.Append(FormattableString.Invariant($"{customSampleBank}:"));
                sb.Append(FormattableString.Invariant($"{volume}:"));
                sb.Append(FormattableString.Invariant($"{sampleFilename}"));
            }

            return sb.ToString();
        }

        private LegacyHitSoundType toLegacyHitSound(string hitSoundName)
        {
            switch (hitSoundName)
            {
                case HitSampleInfo.HIT_NORMAL:
                    return LegacyHitSoundType.Normal;

                case HitSampleInfo.HIT_WHISTLE:
                    return LegacyHitSoundType.Whistle;

                case HitSampleInfo.HIT_FINISH:
                    return LegacyHitSoundType.Finish;

                case HitSampleInfo.HIT_CLAP:
                    return LegacyHitSoundType.Clap;

                default:
                    return LegacyHitSoundType.None;
            }
        }

        private LegacySampleBank toLegacySampleBank(string sampleBank)
        {
            switch (sampleBank?.ToLower())
            {
                case "normal":
                    return LegacySampleBank.Normal;

                case "soft":
                    return LegacySampleBank.Soft;

                case "drum":
                    return LegacySampleBank.Drum;

                default:
                    return LegacySampleBank.None;
            }
        }

        private string toLegacyCustomSampleBank(string sampleSuffix) => string.IsNullOrEmpty(sampleSuffix) ? "0" : sampleSuffix;
    }
}
