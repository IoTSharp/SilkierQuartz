using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SilkierQuartz.Example.Jobs
{
    public class HelloJobSingle : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Hello  Single");
            return Task.CompletedTask;
        }
    }
}
