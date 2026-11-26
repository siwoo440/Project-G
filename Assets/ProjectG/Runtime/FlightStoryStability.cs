using System;
using System.Collections.Generic;

namespace ProjectG
{
    [Serializable]
    public struct FlightStorySiteReport
    {
        public FlightStoryPhase Phase;
        public string SiteId;
        public long ReportSequence;
        public double ObservedAt;
        public bool HumanAcknowledged;
        public bool LocalAcknowledged;
    }

    [Serializable]
    public struct FlightStorySiteSample
    {
        public FlightStoryPhase Phase;
        public string SiteId;
        public double At;
        public double[] Deviations;
        public bool Equipment;
        public bool Power;
        public bool Personnel;
        public bool Chant;
        public bool Glyph;
        public bool OutgoingMaintainer;
        public bool IncomingMaintainer;
    }

    /// <summary>Revocable local observation and handoff evidence, measured on a monotonic active-mission clock.</summary>
    public sealed class FlightStoryStability
    {
        private sealed class Site
        {
            internal bool HasReport;
            internal bool HasReportHistory;
            internal FlightStorySiteReport Report;
            internal double LastSample = double.NaN;
            internal double SustainedStart = double.NaN;
            internal double BackupStart = double.NaN;
            internal bool HandoffRequested;
            internal double OverlapStart = double.NaN;
            internal bool HandoffComplete;
            internal double HandoffAt = double.NaN;
        }

        // Only scheduling arithmetic gets a microsecond tolerance. Deviation and duration limits stay inclusive and exact.
        private const double SamplePeriod = 0.25d;
        private const double ClockTolerance = 0.000001d;
        private readonly Dictionary<string, Site> sites = new Dictionary<string, Site>(StringComparer.Ordinal);
        private readonly string[] requiredSiteIds;
        private double now;
        private bool hasPhase;
        private FlightStoryPhase phase;

        public FlightStoryStability(string runId, IEnumerable<string> requiredSiteIds)
        {
            if (!FlightStoryValue.Id(runId)) throw new ArgumentException("A sortie ID is required.", nameof(runId));
            RunId = runId;
            this.requiredSiteIds = FlightStoryValue.RequiredIds(requiredSiteIds, nameof(requiredSiteIds));
            foreach (string id in this.requiredSiteIds) sites.Add(id, new Site());
        }

        public string RunId { get; }
        public IReadOnlyList<string> RequiredSiteIds => Array.AsReadOnly(requiredSiteIds);
        public FlightStoryPhase CurrentPhase => phase;

        public bool BeginPhase(FlightStoryPhase next, double at)
        {
            if (!next.IsValid || next.RunId != RunId || !Time(at)) return false;
            if (hasPhase && (next.Equals(phase) || next.Sequence <= phase.Sequence)) return false;
            hasPhase = true;
            phase = next;
            foreach (string id in requiredSiteIds) sites[id] = new Site();
            return true;
        }

        public bool Report(FlightStorySiteReport report, double at)
        {
            if (!hasPhase || report.Phase.RunId != RunId || !FlightStoryValue.Id(report.SiteId) || !sites.TryGetValue(report.SiteId, out Site site)) return false;
            if (!Time(at) || !report.Phase.Equals(phase) || !FlightStoryValue.Finite(report.ObservedAt) || report.ObservedAt < 0d || report.ObservedAt > at || at - report.ObservedAt > 6d || report.ReportSequence < 0 || !report.HumanAcknowledged || !report.LocalAcknowledged)
            {
                Invalidate(site, true);
                return false;
            }
            if (site.HasReportHistory)
            {
                if (report.ReportSequence == site.Report.ReportSequence && report.ObservedAt == site.Report.ObservedAt) return false;
                if (report.ReportSequence <= site.Report.ReportSequence || report.ObservedAt <= site.Report.ObservedAt)
                {
                    Invalidate(site, true);
                    return false;
                }
                if (report.ObservedAt - site.Report.ObservedAt > 6d) Invalidate(site, false);
            }
            site.Report = report;
            site.HasReport = true;
            site.HasReportHistory = true;
            return true;
        }

        public bool Sample(FlightStorySiteSample sample)
        {
            if (!hasPhase || sample.Phase.RunId != RunId || !FlightStoryValue.Id(sample.SiteId) || !sites.TryGetValue(sample.SiteId, out Site site)) return false;
            if (!Time(sample.At) || !sample.Phase.Equals(phase) || !ReportCurrent(site, sample.At) || !sample.Equipment || !sample.Power || !sample.Personnel || !sample.Chant || !sample.Glyph || sample.Deviations == null || sample.Deviations.Length != 3)
            {
                Invalidate(site, false);
                return false;
            }
            double maximum = 0d;
            foreach (double deviation in sample.Deviations)
            {
                if (!FlightStoryValue.Finite(deviation) || deviation < 0d) { Invalidate(site, false); return false; }
                maximum = Math.Max(maximum, deviation);
            }
            if (!double.IsNaN(site.LastSample))
            {
                double gap = sample.At - site.LastSample;
                if (gap < -ClockTolerance) { Invalidate(site, false); return false; }
                if (gap < SamplePeriod - ClockTolerance)
                {
                    // Extra observations cannot earn time, but observed danger cannot be ignored.
                    if (maximum > 0.30d) site.SustainedStart = double.NaN;
                    if (maximum > 0.40d) site.BackupStart = double.NaN;
                    if (!site.HandoffComplete && (maximum > 0.30d || !sample.OutgoingMaintainer || !sample.IncomingMaintainer)) site.OverlapStart = double.NaN;
                    return false;
                }
                if (gap > SamplePeriod + ClockTolerance) Invalidate(site, false);
            }
            site.LastSample = sample.At;
            if (maximum <= 0.30d)
            {
                if (double.IsNaN(site.SustainedStart)) site.SustainedStart = sample.At;
            }
            else site.SustainedStart = double.NaN;
            if (maximum <= 0.40d)
            {
                if (double.IsNaN(site.BackupStart)) site.BackupStart = sample.At;
            }
            else site.BackupStart = double.NaN;
            if (site.HandoffRequested && !site.HandoffComplete && maximum <= 0.30d && sample.OutgoingMaintainer && sample.IncomingMaintainer)
            {
                if (double.IsNaN(site.OverlapStart)) site.OverlapStart = sample.At;
            }
            else if (!site.HandoffComplete) site.OverlapStart = double.NaN;
            return true;
        }

        public bool BeginHandoff(string siteId, double at)
        {
            if (!hasPhase || !FlightStoryValue.Id(siteId) || !sites.TryGetValue(siteId, out Site site) || !Time(at) || site.HandoffRequested || site.HandoffComplete) return false;
            site.HandoffRequested = true;
            site.OverlapStart = double.NaN;
            return true;
        }

        public bool CompleteHandoff(string siteId, double at)
        {
            if (!hasPhase || !FlightStoryValue.Id(siteId) || !sites.TryGetValue(siteId, out Site site) || !Time(at) || site.HandoffComplete || !site.HandoffRequested || !Current(site, at) || double.IsNaN(site.OverlapStart) || site.LastSample - site.OverlapStart < 5d) return false;
            site.HandoffComplete = true;
            site.HandoffAt = at;
            return true;
        }

        public bool IsBackupQualified(double at) => Qualified(at, false);
        public bool IsSustainedQualified(double at) => Qualified(at, true);

        private bool Qualified(double at, bool sustained)
        {
            if (!hasPhase || !Time(at)) return false;
            double commonStart = double.NegativeInfinity;
            double commonEnd = double.PositiveInfinity;
            double finalHandoff = double.NegativeInfinity;
            bool allCurrent = true;
            foreach (Site site in sites.Values)
            {
                if (!Current(site, at)) { allCurrent = false; continue; }
                double start = sustained ? site.SustainedStart : site.BackupStart;
                if (double.IsNaN(start)) allCurrent = false;
                else commonStart = Math.Max(commonStart, start);
                commonEnd = Math.Min(commonEnd, site.LastSample);
                if (sustained)
                {
                    if (!site.HandoffComplete) allCurrent = false;
                    else finalHandoff = Math.Max(finalHandoff, site.HandoffAt);
                }
            }
            if (!allCurrent || commonEnd - commonStart < (sustained ? 30d : 5d)) return false;
            return !sustained || commonEnd - finalHandoff >= 10d;
        }

        private bool Time(double at)
        {
            if (!FlightStoryValue.Finite(at) || at < 0d || at < now - ClockTolerance) return false;
            now = Math.Max(now, at);
            return true;
        }

        private bool ReportCurrent(Site site, double at) => site.HasReport && site.Report.Phase.Equals(phase) && site.Report.HumanAcknowledged && site.Report.LocalAcknowledged && at >= site.Report.ObservedAt && at - site.Report.ObservedAt <= 6d;

        private bool Current(Site site, double at)
        {
            if (!ReportCurrent(site, at) || double.IsNaN(site.LastSample) || at - site.LastSample > SamplePeriod + ClockTolerance || at < site.LastSample - ClockTolerance)
            {
                Invalidate(site, false);
                return false;
            }
            return true;
        }

        private static void Invalidate(Site site, bool report)
        {
            site.LastSample = double.NaN;
            site.SustainedStart = double.NaN;
            site.BackupStart = double.NaN;
            if (!site.HandoffComplete) site.OverlapStart = double.NaN;
            if (report) site.HasReport = false;
        }
    }
}
