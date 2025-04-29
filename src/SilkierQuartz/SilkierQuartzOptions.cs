using CronExpressionDescriptor;
using Quartz;
using SilkierQuartz.TypeHandlers;
using System.Collections.Generic;
using System.IO;
using Number = SilkierQuartz.TypeHandlers.NumberHandler.UnderlyingType;

namespace SilkierQuartz
{
    public class SilkierQuartzOptions
    {
        /// <summary>
        /// Supports any value that is a viable as a img src attribute value: url, or base64
        /// src='data:image/jpeg;base64, LzlqLzRBQ...[end of base64 data]'
        /// Defaults to the SilkierQuartz original logo
        /// </summary>
        public string Logo { get; set; } = "Content/Images/logo.png";

        public string CustomStyleSheet { set; get; } = "";

        public string CustomFavicon { set; get; } = "";

        public string ProductName { get; set; } = "";

        public string VirtualPathRoot { get; set; } = "/";

        public string BasePath { get; set; } = "/";

        public string VirtualPathRootUrlEncode => VirtualPathRoot.Replace("/", "%2F");

        public IScheduler Scheduler { get; set; }

        /// <summary>
        /// Supported value types in job data map.
        /// </summary>
        public List<TypeHandlerBase> StandardTypes { get; } = new List<TypeHandlerBase>();

        /// <summary>
        /// Default type for new job data map item.
        /// </summary>
        public TypeHandlerBase DefaultSelectedType { get; set; }

        public string DefaultDateFormat
        {
            get => DateTimeSettings.DefaultDateFormat;
            set => DateTimeSettings.DefaultDateFormat = value;
        }

        public string DefaultTimeFormat
        {
            get => DateTimeSettings.DefaultTimeFormat;
            set => DateTimeSettings.DefaultTimeFormat = value;
        }

        public bool UseLocalTime
        {
            get => DateTimeSettings.UseLocalTime;
            set => DateTimeSettings.UseLocalTime = value;
        }

        /// <summary>
        /// Set options used by the cron expression description provider
        /// </summary>
        public Options CronExpressionOptions { get; set; } = new Options();

        public SilkierQuartzOptions()
        {
            DefaultSelectedType = new StringHandler() { Name = "String" };
            // order of StandardTypes is important due to TypeHandlerBase.CanHandle evaluation
            StandardTypes.Add(new FileHandler() { Name = "File", DisplayName = "Binary Data" });
            StandardTypes.Add(new BooleanHandler() { Name = "Boolean" });
            StandardTypes.Add(new DateTimeHandler() { Name = "Date", DisplayName = "Date", IgnoreTimeComponent = true });
            StandardTypes.Add(new DateTimeHandler() { Name = "DateTime" });
            StandardTypes.Add(new DateTimeHandler() { Name = "DateTimeUtc", DisplayName = "DateTime (UTC)", IsUtc = true });
            StandardTypes.Add(new NumberHandler(Number.Decimal));
            StandardTypes.Add(new NumberHandler(Number.Double));
            StandardTypes.Add(new NumberHandler(Number.Float));
            StandardTypes.Add(new NumberHandler(Number.Integer));
            StandardTypes.Add(new NumberHandler(Number.Long));
            StandardTypes.Add(DefaultSelectedType); // String
            StandardTypes.Add(new StringHandler() { Name = "MultilineString", DisplayName = "String (Multiline)", IsMultiline = true });
        }

        public bool EnableEdit { get; set; } = true;

        internal string ContentRootDirectory => null;
        internal string ViewsRootDirectory => null;
    }
}
