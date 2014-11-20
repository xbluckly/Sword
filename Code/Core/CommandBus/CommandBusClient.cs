﻿using Core.Server;
using Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Core.CommandBus
{
    public class CommandBusClient
    {
        private ClientConnectionManager mgr;
        private string SessionID { get; set; }
        private bool started = false;

        public void Start()
        {
            if (started)
                return;

            this.SessionID = Guid.NewGuid().ToString();

            if (mgr == null)
                mgr = new ClientConnectionManager();

            mgr.Connect();
        }

        public void Stop()
        {
            if (mgr != null)
                mgr.Close();

            mgr = null;
        }

        public void Send(Command cmd)
        {
            //validate first, ignored

            //send command
            cmd.SessionID = this.SessionID;
            mgr.Send(cmd);
        }

        public CommandResult WaitForResult()
        {
            return mgr.Receive();
        }
    }
}
