using ProtoBuf;

namespace QualityOfBuilding.Source.Network;

[ProtoContract]
public class SetBuildingModeMessage
{
    [ProtoMember(1)]
    public required string ToolModeCode { get; set; }
}
