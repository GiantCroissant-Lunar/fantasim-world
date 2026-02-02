using FantaSim.Geosphere.Plate.Cache.Contracts.Models;

namespace FantaSim.Geosphere.Plate.Cache.Contracts;

public interface IDerivedProductAuditSink
{
    void Record(DerivedProductAuditRecord record);
}
