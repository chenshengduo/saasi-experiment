﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RabbitMQ.Client.Exceptions;

namespace MEMORY_Microservice
{
    class memory
    {
        private static readonly string _rabbitMQHost = "rabbitmq";
        private string id;
        public memory(string id)
        {
            this.id = id;
        }
        static void Main(string[] args)
        {
            // Wait for RabbitMQ to be ready
            Console.WriteLine("================== Waiting for RabbitMQ to start");


            var factory = new ConnectionFactory() { HostName = _rabbitMQHost };
            var connected = false;
            while (!connected)
            {
                try
                {
                    using (var connection = factory.CreateConnection())
                    {
                        Console.WriteLine("================== Connected");
                        connected = true;
                    }

                }
                catch (BrokerUnreachableException e)
                {
                    // not connected
                    Console.WriteLine("================== Not connected, retrying in 500ms");
                }
                Thread.Sleep(500);
            }
            memory mem1 = new memory("1");
            memory mem2 = new memory("2");

            Thread t1 = new Thread(mem1.MemoryProcessing); //listen message
            t1.Start();

        }


        private  void MemoryProcessing()
        {
            var factory = new ConnectionFactory() { HostName = _rabbitMQHost };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "call", type: "direct");
                var queueName = "memory_queue";
                channel.QueueDeclare(queue: queueName,
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
                channel.BasicQos(prefetchSize: 0, prefetchCount: 30, global: false);
                channel.QueueBind(queue: queueName, exchange: "call", routingKey: "memory");
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var order = message.Split(' ');
                    int time = Convert.ToInt16(order[3]);
                    worker w = new worker(Guid.NewGuid().ToString(), time, channel, ea);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(w.Fun));

                };
                channel.BasicConsume(queue: queueName,
                                     noAck: false,
                                     consumer: consumer);

                Console.WriteLine(" Looping...");
                //Console.ReadLine();
                while(true){ Thread.Sleep(10);};
            }
        }

        public class worker
        {
            private string id;
            private IModel channel;
            private BasicDeliverEventArgs ea;
            private int time;

            public worker(string id, int time, IModel channel, BasicDeliverEventArgs ea)
            {
                this.id = id;
                this.time = time;
                this.channel = channel;
                this.ea = ea;
            }

            public void Fun(object state)
            {
                //simulate memory use
                DateTime currentTime = new DateTime();
                currentTime = System.DateTime.Now;
                DateTime finishTime = currentTime.AddSeconds(time);
                Console.WriteLine(this.id + ":Start." + Convert.ToString(currentTime));
                //List<IntPtr> alist = new List<IntPtr>();
                List<byte[]> alist = new List<byte[]>();
                int i = 0;
                while (System.DateTime.Now.CompareTo(finishTime) < 0)
                {
                    byte[] b = new byte[30];
                    alist.Add(b); // Change the size here.

                    i++;
                    if (i == 2000)
                    {
                        Thread.Sleep(200); // Change the wait time here.
                        i = 0;

                    }
                        

                }
                alist.Clear();
                alist = null;
                GC.Collect(); // release memory

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                Console.WriteLine(this.id + ":Done." + Convert.ToString(System.DateTime.Now));


            }
        }
    }
}