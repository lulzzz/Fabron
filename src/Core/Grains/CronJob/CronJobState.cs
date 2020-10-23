﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TGH.Grains.CronJob
{
    public class CronJobState
    {
#nullable disable
        public CronJobState() { }
#nullable enable
        public CronJobState(string cronExp, JobCommandInfo command)
        {
            CronExp = cronExp;
            Command = command;
        }

        public string CronExp { get; }
        public JobCommandInfo Command { get; }
        private readonly List<ChildJobState> _childJobs = new List<ChildJobState>();
        public ChildJobState? LatestJob => _childJobs.LastOrDefault();
        public IEnumerable<ChildJobState> NotScheduledJobs => _childJobs.Where(job => job.Status == JobStatus.NotCreated);
        public IEnumerable<ChildJobState> EnqueuedJobs => _childJobs.Where(job => !job.IsFinished && job.Status != JobStatus.NotCreated);
        public IEnumerable<ChildJobState> UnFinishedJobs => _childJobs.Where(job => !job.IsFinished);
        public IEnumerable<ChildJobState> FinishedJobs => _childJobs.Where(job => job.IsFinished);
        public string? Reason { get; private set; }
        public JobStatus Status { get; private set; }

        public void Start()
        {
            Status = JobStatus.Running;
        }

        public void Complete()
        {
            Status = JobStatus.RanToCompletion;
        }

        public void Cancel(string reason)
        {
            Status = JobStatus.Canceled;
            Reason = reason;
        }

        public void SetNextReminder()
        {

        }

        public void Schedule(DateTime toTime)
        {
            var cron = Cronos.CronExpression.Parse(CronExp);
            var lastedJob = LatestJob;
            DateTime lastestScheduledAt = lastedJob is null ? DateTime.MinValue : lastedJob.ScheduledAt;
            var nextSchedule = cron.GetNextOccurrence(lastestScheduledAt);
            if (nextSchedule is null) return;

            var nextJob = new ChildJobState(nextSchedule.Value);
            _childJobs.Add(nextJob);

            if (nextSchedule < toTime)
            {
                IEnumerable<DateTime> occurrences = cron.GetOccurrences(nextSchedule.Value, toTime);
                IEnumerable<ChildJobState> jobsToSchedule = occurrences
                    .Select(occ => new ChildJobState(occ));
                _childJobs.AddRange(jobsToSchedule);
            }
        }

    }
}