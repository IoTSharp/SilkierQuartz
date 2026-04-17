using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SilkierQuartz.Example.Jobs
{
    public class InjectSampleJobSingle : IJob
    {
        InjectProperty _options;

        public InjectSampleJobSingle(IOptions<InjectProperty> options)
        {
            _options = options.Value;
        }
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine($"InjectSampleJobSingle {_options.WriteText} ");
            return Task.CompletedTask;
        }
    }
}
