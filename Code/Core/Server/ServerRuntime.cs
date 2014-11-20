﻿using Core.CommandBus;
using Core.Server.Cleaner;
using Core.Server.Pipes;
using Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Server
{
    public static class ServerRuntime
    {
        internal static ConnectionMaster master;
        private static IncomingQueueRepository incomingQueueRepository = new IncomingQueueRepository();
        private static OutgoingQueueRepository outgoingQueueRepository = new OutgoingQueueRepository();
        private static OfflineConnectionCleanWorker offlineConnectionCleanWorker = new OfflineConnectionCleanWorker();
        private static PipeProcessorPool pipeProcessorPool = new PipeProcessorPool(1);

        public static void Start(int port)
        {
            if (master != null)
                throw new Exception("已经Start了");

            master = new ConnectionMaster(port);
            master.Start();

            pipeProcessorPool.PrepareIdlePipeProcessors();

            Task.Factory.StartNew(() => {
                StartDispatchIncomeQueue();
            }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(() =>
            {
                StartDispatchOutgoingQueue();
            }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(() =>
            {
                StartCleanWorker();
            }, TaskCreationOptions.LongRunning);
        }

        private static void StartCleanWorker()
        {
            while (true)
            {
                offlineConnectionCleanWorker.DetectAndTagInactiveConnectionWorkers();

                offlineConnectionCleanWorker.CleanTaggedConnectionWorkers();
            }
        }

        internal static void StartDispatchIncomeQueue()
        {
            while (true)
            {
                var incomingMsg = incomingQueueRepository.DequeueBlock();

                Console.WriteLine("Incoming: "+ incomingMsg.Method2Invoke);

                PipeProcessor pipe = pipeProcessorPool.PickOneIdle();

                CommandResult result =pipe.Process(incomingMsg);

                result.ConnectionWorker = incomingMsg.ConnectionWorker;
                ServerRuntime.AddCommandResultToOutgoingQueueRepository(result);
            }
        }

        internal static void StartDispatchOutgoingQueue()
        {
            while (true)
            {
                var outgoingMsg = outgoingQueueRepository.DequeueBlock();

                Console.WriteLine("Outgoing: " + outgoingMsg.Result.Length);

                outgoingMsg.ConnectionWorker.SendResponse(outgoingMsg);
            }
        }

        internal static void AddCommandToIncomingQueueRepository(Command cmd)
        {
            incomingQueueRepository.Enqueue(cmd);
        }

        internal static void AddCommandResultToOutgoingQueueRepository(CommandResult cmdResult)
        {
            outgoingQueueRepository.Enqueue(cmdResult);
        }
    }
}
