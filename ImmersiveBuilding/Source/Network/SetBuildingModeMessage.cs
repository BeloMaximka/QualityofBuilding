using ProtoBuf;

namespace ImmersiveBuilding.Source.Network;

[ProtoContract]
public class SetBuildingModeMessage
{
    [ProtoMember(1)]
    public int Mode { get; set; }
}
