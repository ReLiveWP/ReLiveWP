using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public record CandidateRevision(
    long RevisionId,
    Guid UpdateId,
    int RevisionNumber,
    UpdateType UpdateType,
    bool IsLeaf,
    string DeploymentAction,
    DateTime? LastChangeTime);
