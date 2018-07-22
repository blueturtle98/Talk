﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TalkLib
{
    public enum ClientCommand
    {
        Disconnect = 1,
        SendMessage = 2,
    }

    public enum ServerCommand
    {
        NotifyConnected = 1,
        ClientAlreadyConnected = 2,
        ServerShutdown = 3,
        ChannelList = 4,
        MessageFromClient = 5,
    }
}
