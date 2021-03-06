﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using Saasi.Shared.Workload;
using Newtonsoft.Json.Serialization;
using Nexogen.Libraries.Metrics.Prometheus;

namespace Saasi.Monolithic.BusinessWeb.Controllers
{
    [Route("api")]
    public class ValuesController : Controller
    {

        private readonly IMetricsContainer _metrics;
        public ValuesController(IMetricsContainer metrics)
        {
            this._metrics = metrics;
        }

        // GET api/Business
        [HttpGet("Business")]
        /*
         * io: whether to generate Disk I/O load. (yes if io=1, no if io=0)
         * cpu: whether to generate CPU usage. (yes if cpu=1, no if cpu=0)
         * memory: whether to generate Memory usage. (yes if memory=1, no if memory=0)
         * timestart: the Unix timestamp when the request is sent (generated by the user)
         * timetorun: for how long (in seconds) should we generate the CPU/Memory/IO load?
         * timeout: the maximum time (in seconds) allowed for the request to finish.
         */
        public async Task<JsonResult> SimulateBusinessTransaction(int io,
                                                              int cpu,
                                                              int memory,
                                                              long timestart,
                                                              int timetorun,
                                                              int timeout)
        {
            Guid TranscationID = Guid.NewGuid();
            _metrics.GetGauge("bms_active_transactions")
                .Labels(io.ToString(), cpu.ToString(), memory.ToString(), timetorun.ToString())
                .Increment();

            long StartTimestampMs = timestart * 1000;
            long ExpectedFinishTimeMs = (timestart + timeout) * 1000;
            DateTime StartTimeDateTime = new DateTime(StartTimestampMs); //don't know if it's need to add the 1970
            long ReceivedTimeMs = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds; //don't know if it's reasonable
            Console.WriteLine($"Transcation {TranscationID}: Started at {DateTime.Now.ToString()}");

            var tasks = new List<Task<ExecutionResult>>();
            
            if (io == 1)
            {
                var ioWorkload = new IoWorkload();
                tasks.Add(ioWorkload.Run(timetorun));
            }
            if (cpu == 1)
            {
                var cpuWorkload = new CpuWorkload();
                tasks.Add(cpuWorkload.Run(timetorun));
            }
            if (memory == 1)
            {
                var memoryWorkload = new MemoryWorkload();
                tasks.Add(memoryWorkload.Run(timetorun));
            }
            if (tasks.Count > 0) {
                await Task.WhenAll(tasks.ToArray());
            }
            
            var resultList = new Dictionary<string, ExecutionResult>();
            var i = 0;
            if (io == 1) {
                resultList.Add("io", tasks[i].Result);
                ++i;
            }
            if (cpu == 1) {
                resultList.Add("cpu", tasks[i].Result);
                ++i;
            }
            if (memory == 1) {
                resultList.Add("memory", tasks[i].Result);
                ++i;
            }

            Console.WriteLine($"Transcation {TranscationID}: Finished at {DateTime.Now.ToString()}");
            _metrics.GetGauge("bms_active_transactions")
                .Labels(io.ToString(), cpu.ToString(), memory.ToString(), timetorun.ToString())
                .Decrement();

            var finishedTime = DateTime.Now;
            var violated = false;
            if (finishedTime - StartTimeDateTime > new TimeSpan(0,0,timeout)) {
                Console.WriteLine($"Transcation {TranscationID}: Business violation");            
                _metrics.GetCounter("bms_business_violation_total")
                    .Labels(io.ToString(), cpu.ToString(), memory.ToString(), timetorun.ToString())
                    .Increment();
                violated = true;
            } 
            return new JsonResult(new {
                Tasks = resultList,
                StartedAt = StartTimeDateTime.ToString(),
                FinishedAt = finishedTime,
                ExpectedToFinishAt = new DateTime(ExpectedFinishTimeMs).ToString(),
                BusinessViolation = violated
            });
        }

        [HttpGet("status")]
        public string GetStatus() {
            return "OK";
        } 

    }
}
