﻿using System;
using System.Collections.Generic;
using System.Text;
using Docker.DotNet;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Monitor
{
    public class MemoryMicroservice : Microservice
    {
        private static double MemoryViolationThreshold = 40.0;
        private int MemoryViolationCounter = 0;
        public MemoryMicroservice(DockerClient dockerClient) : base(ContainerType.MemoryMicroservice, dockerClient)
        {
             
        }

        /*
         * scale container
         */
        public override async Task DoScale()
        {
            await base.DoScale();
            Console.WriteLine("scaleout memory");
            await RunScriptOnHost("scalemem1.sh", this.ScaleTarget);
        }

        public override void CheckResourceUtilisation()
        {
            foreach (var pair in Containers)
            {
                var container = pair.Value;
                if (container.MemoryUsage > MemoryViolationThreshold)
                {

                    MemoryViolationCounter++;
                    Console.WriteLine($"Memory violation: {container.Id} Total {MemoryViolationCounter}");
                }
            }

            if (MemoryViolationCounter >= 3 * ActualScale)
            {
                if (LastScaleTime.AddSeconds(30).CompareTo(DateTime.Now) < 0) //A container can scale one time in 30 seconds.
                {
                    LastScaleTime = DateTime.Now;
                    ScaleTarget++;
                    Console.WriteLine($"Memory -> {ScaleTarget}");
                    WriteScaleOutRecord();
                    DoScale();
                }
                MemoryViolationCounter = 0;
            }
        }


    }
}
