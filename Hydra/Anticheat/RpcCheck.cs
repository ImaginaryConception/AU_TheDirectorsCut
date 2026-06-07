using Hazel;
using System;

namespace AU_TheDirectorsCut.Hydra.Anticheat
{
    internal abstract class RpcCheck
    {
        public virtual bool Enabled { get; set; } = true;

        public virtual void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc) { }

        public abstract RpcCalls GetRpcCall();

        public virtual bool IsHostOnly()
        {
            return false;
        }

        public virtual Type GetExpectedNetObject()
        {
            return typeof(PlayerControl);
        }
    }
}
