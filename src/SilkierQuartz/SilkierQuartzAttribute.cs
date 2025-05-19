using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SilkierQuartz
{
    public class SilkierQuartzAttribute : Attribute
    {
        public SilkierQuartzAttribute()
        {
        }

        public SilkierQuartzAttribute(double days, double hours, double minutes, double seconds, double milliseconds, string _identity, string? _group = null, string _description = null) : this(days, hours, minutes, seconds, milliseconds, 0, _identity, _group, _description)
        {
        }

        public SilkierQuartzAttribute(double hours, double minutes, double seconds, string _identity, string? _group = null, string _description = null) : this(0, hours, minutes, seconds, 0, 0, _identity, _group, _description)
        {
        }

        public SilkierQuartzAttribute(double minutes, double seconds, string _identity, string? _group = null, string _description = null) : this(0, 0, minutes, seconds, 0, 0, _identity, _group, _description)
        {
        }
		
        public SilkierQuartzAttribute(double seconds, string _identity, string? _group = null, string _description = null) : this(0, 0, 0, seconds, 0, 0, _identity, _group, _description)
        {
        }

        public SilkierQuartzAttribute(double days, double hours, double minutes, double seconds, double milliseconds) : this(days, hours, minutes, seconds, milliseconds, 0, null, null, null)
        {
        }

        public SilkierQuartzAttribute(double hours, double minutes, double seconds) : this(0, hours, minutes, seconds, 0, 0, null, null, null)
        {
        }

        public SilkierQuartzAttribute(double minutes, double seconds) : this(0, 0, minutes, seconds, 0, 0, null, null, null)
        {
        }
		
        public SilkierQuartzAttribute(double seconds) : this(0, 0, 0, seconds, 0, 0, null, null, null)
        {
        }

        public SilkierQuartzAttribute(bool Manual) : this(0, 0, 0, 0, 0, 0, null, null, null)
        {
            this.Manual = true;
        }

        public SilkierQuartzAttribute(double days, double hours, double minutes, double seconds, double milliseconds, long ticks, string _identity, string? _group, string _description)
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
		
        public string Description { get; set; } = null;
        public string Identity { get; set; } = null;
        public string Group { get; set; } = null;
        public TimeSpan WithInterval { get; set; }
        public DateTimeOffset StartAt { get; set; } = DateTimeOffset.MinValue;
        public int RepeatCount { get; set; } = 0;
        public string TriggerName { get; set; } = string.Empty;
        public string TriggerGroup { get; set; } = string.Empty;
        public string TriggerDescription { get; set; } = string.Empty;
        public int Priority { get; set; } = 0;
        public bool Manual { get; set; } = false;
    }
}
