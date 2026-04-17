using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SilkierQuartz.Example.Jobs
{
    public class LongRunningJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Long Started");
            await Task.Delay(30000);//half min
            Console.WriteLine("Long Running");
            await Task.Delay(30000);//half min
            Console.WriteLine("Long Complete");
            await Task.CompletedTask;
        }
    }
}
