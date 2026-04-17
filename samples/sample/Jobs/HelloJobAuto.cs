using Quartz;
using SilkierQuartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SilkierQuartz.Example.Jobs
{

    [SilkierQuartz(5, Group = "example", Description = "this e sq test", TriggerDescription = "_hellojobauto", TriggerGroup = "SilkierQuartz")]
    public class HelloJobAuto : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine($"Hello {DateTime.Now}");
            return Task.CompletedTask;
        }
    }
}
