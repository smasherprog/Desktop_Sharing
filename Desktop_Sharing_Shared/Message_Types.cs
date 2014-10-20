using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Desktop_Sharing_Shared
{
    public enum Message_Types
    {
        RESOLUTION_CHANGE,
        UPDATE_REGION,
        MOUSE_EVENT,
        PING,
        PING_ACK,
        KEY_EVENT,
        FOLDER,
        FILE,
        MOUSE_IMAGE_EVENT,
        MOUSE_POSITION_EVENT
    }
}
