﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SilkierQuartz
{
    /// <summary>
    /// Marks a Quartz job type for automatic SilkierQuartz registration and optional trigger creation.
    /// </summary>
    public class SilkierQuartzAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute without creating a predefined schedule.
        /// </summary>
        public SilkierQuartzAttribute()
        {
        }

        /// <summary>
        /// Initializes the attribute with a simple schedule interval and optional job metadata.
        /// </summary>
        /// <param name="days">The day component of the interval.</param>
        /// <param name="hours">The hour component of the interval.</param>
        /// <param name="minutes">The minute component of the interval.</param>
        /// <param name="seconds">The second component of the interval.</param>
        /// <param name="milliseconds">The millisecond component of the interval.</param>
        /// <param name="_identity">The optional Quartz job identity.</param>
        /// <param name="_group">The optional Quartz job group.</param>
        /// <param name="_description">The optional job description.</param>
        public SilkierQuartzAttribute(double days, double hours, double minutes, double seconds, double milliseconds, string? _identity, string? _group = null, string? _description = null) : this(days, hours, minutes, seconds, milliseconds, 0, _identity, _group, _description)
        {
        }

        /// <summary>
        /// Initializes the attribute with an hour-based simple schedule interval and optional job metadata.
        /// </summary>
        /// <param name="hours">The hour component of the interval.</param>
        /// <param name="minutes">The minute component of the interval.</param>
        /// <param name="seconds">The second component of the interval.</param>
        /// <param name="_identity">The optional Quartz job identity.</param>
        /// <param name="_group">The optional Quartz job group.</param>
        /// <param name="_description">The optional job description.</param>
        public SilkierQuartzAttribute(double hours, double minutes, double seconds, string? _identity, string? _group = null, string? _description = null) : this(0, hours, minutes, seconds, 0, 0, _identity, _group, _description)
        {
        }

        /// <summary>
        /// Initializes the attribute with a minute-based simple schedule interval and optional job metadata.
        /// </summary>
        /// <param name="minutes">The minute component of the interval.</param>
        /// <param name="seconds">The second component of the interval.</param>
        /// <param name="_identity">The optional Quartz job identity.</param>
        /// <param name="_group">The optional Quartz job group.</param>
        /// <param name="_description">The optional job description.</param>
        public SilkierQuartzAttribute(double minutes, double seconds, string? _identity, string? _group = null, string? _description = null) : this(0, 0, minutes, seconds, 0, 0, _identity, _group, _description)
        {
        }
		
        /// <summary>
        /// Initializes the attribute with a second-based simple schedule interval and optional job metadata.
        /// </summary>
        /// <param name="seconds">The second component of the interval.</param>
        /// <param name="_identity">The optional Quartz job identity.</param>
        /// <param name="_group">The optional Quartz job group.</param>
        /// <param name="_description">The optional job description.</param>
        public SilkierQuartzAttribute(double seconds, string? _identity, string? _group = null, string? _description = null) : this(0, 0, 0, seconds, 0, 0, _identity, _group, _description)
        {
        }

        /// <summary>
        /// Initializes the attribute with a simple schedule interval.
        /// </summary>
        /// <param name="days">The day component of the interval.</param>
        /// <param name="hours">The hour component of the interval.</param>
        /// <param name="minutes">The minute component of the interval.</param>
        /// <param name="seconds">The second component of the interval.</param>
        /// <param name="milliseconds">The millisecond component of the interval.</param>
        public SilkierQuartzAttribute(double days, double hours, double minutes, double seconds, double milliseconds) : this(days, hours, minutes, seconds, milliseconds, 0, null, null, null)
        {
        }

        /// <summary>
        /// Initializes the attribute with an hour-based simple schedule interval.
        /// </summary>
        /// <param name="hours">The hour component of the interval.</param>
        /// <param name="minutes">The minute component of the interval.</param>
        /// <param name="seconds">The second component of the interval.</param>
        public SilkierQuartzAttribute(double hours, double minutes, double seconds) : this(0, hours, minutes, seconds, 0, 0, null, null, null)
        {
        }

        /// <summary>
        /// Initializes the attribute with a minute-based simple schedule interval.
        /// </summary>
        /// <param name="minutes">The minute component of the interval.</param>
        /// <param name="seconds">The second component of the interval.</param>
        public SilkierQuartzAttribute(double minutes, double seconds) : this(0, 0, minutes, seconds, 0, 0, null, null, null)
        {
        }
		
        /// <summary>
        /// Initializes the attribute with a second-based simple schedule interval.
        /// </summary>
        /// <param name="seconds">The second component of the interval.</param>
        public SilkierQuartzAttribute(double seconds) : this(0, 0, 0, seconds, 0, 0, null, null, null)
        {
        }

        /// <summary>
        /// Initializes the attribute for manual execution only without an automatic trigger.
        /// </summary>
        /// <param name="Manual">Ignored value used to select the manual constructor overload.</param>
        public SilkierQuartzAttribute(bool Manual) : this(0, 0, 0, 0, 0, 0, null, null, null)
        {
            this.Manual = true;
        }

        /// <summary>
        /// Initializes the attribute with the full simple schedule configuration.
        /// </summary>
        /// <param name="days">The day component of the interval.</param>
        /// <param name="hours">The hour component of the interval.</param>
        /// <param name="minutes">The minute component of the interval.</param>
        /// <param name="seconds">The second component of the interval.</param>
        /// <param name="milliseconds">The millisecond component of the interval.</param>
        /// <param name="ticks">Additional ticks added to the interval.</param>
        /// <param name="_identity">The optional Quartz job identity.</param>
        /// <param name="_group">The optional Quartz job group.</param>
        /// <param name="_description">The optional job description.</param>
        public SilkierQuartzAttribute(double days, double hours, double minutes, double seconds, double milliseconds, long ticks, string? _identity, string? _group, string? _description)
        {
            Identity = _identity;
            Group = _group;
            Description = _description;

            WithInterval = TimeSpan.FromTicks(ticks + (long)(days * TimeSpan.TicksPerDay
                                             + hours * TimeSpan.TicksPerHour
                                             + minutes * TimeSpan.TicksPerMinute
                                             + seconds * TimeSpan.TicksPerSecond
                                             + milliseconds + TimeSpan.TicksPerMillisecond));
        }
		
        /// <summary>
        /// Gets or sets the optional Quartz job description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the optional Quartz job identity.
        /// </summary>
        public string? Identity { get; set; }

        /// <summary>
        /// Gets or sets the optional Quartz job group.
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// Gets or sets the interval used for the generated simple trigger.
        /// </summary>
        public TimeSpan WithInterval { get; set; }

        /// <summary>
        /// Gets or sets the trigger start time. <see cref="DateTimeOffset.MinValue"/> means start immediately.
        /// </summary>
        public DateTimeOffset StartAt { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        /// Gets or sets the number of repeats for the generated simple trigger. Zero or less repeats forever.
        /// </summary>
        public int RepeatCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the optional generated trigger name.
        /// </summary>
        public string TriggerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional generated trigger group.
        /// </summary>
        public string TriggerGroup { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional generated trigger description.
        /// </summary>
        public string TriggerDescription { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional trigger priority.
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether the job should be registered without an automatic trigger.
        /// </summary>
        public bool Manual { get; set; } = false;
    }
}
