using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Datasets.Contracts.Ingest;

[UnifyModel]
public sealed record PlatesDatasetIngestAudit(
    [property: UnifyProperty(0)] string DatasetId,
    [property: UnifyProperty(1)] string BodyId,
    [property: UnifyProperty(2)] string ManifestFileName,
    [property: UnifyProperty(3)] string ManifestFileSha256,
    [property: UnifyProperty(4)] string ManifestCanonicalSha256,
    [property: UnifyProperty(5)] string StableIdPolicyId,
    [property: UnifyProperty(6)] string AssetOrderingPolicyId,
    [property: UnifyProperty(7)] string QuantizationPolicyId,
    [property: UnifyProperty(8)] PlatesDatasetIngestAssetAudit[] Assets,
    [property: UnifyProperty(9)] PlatesDatasetIngestTargetAudit[] Targets,
    [property: UnifyProperty(10)] PlatesDatasetIngestStreamAudit[] Streams,
    [property: UnifyProperty(11)] string AuditSha256);

[UnifyModel]
public sealed record PlatesDatasetIngestAssetAudit(
    [property: UnifyProperty(0)] string AssetId,
    [property: UnifyProperty(1)] string Kind,
    [property: UnifyProperty(2)] string RelativePath,
    [property: UnifyProperty(3)] string Format,
    [property: UnifyProperty(4)] string FileSha256);

[UnifyModel]
public sealed record PlatesDatasetIngestTargetAudit(
    [property: UnifyProperty(0)] string AssetId,
    [property: UnifyProperty(1)] string Kind,
    [property: UnifyProperty(2)] string StreamKey);

[UnifyModel]
public sealed record PlatesDatasetIngestStreamAudit(
    [property: UnifyProperty(0)] string StreamKey,
    [property: UnifyProperty(1)] int EventCount,
    [property: UnifyProperty(2)] long FirstSequence,
    [property: UnifyProperty(3)] long LastSequence,
    [property: UnifyProperty(4)] string EventIdDigestSha256);
