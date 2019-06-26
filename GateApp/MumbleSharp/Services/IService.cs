using System.Collections;
using System.Collections.Generic;

namespace MumbleSharp.Services
{
    public interface IService
    {
        IEnumerable<PacketProcessor> GetProcessors();
    }
}