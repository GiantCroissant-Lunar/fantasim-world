namespace FantaSim.Space.Stellar.Contracts.Validation;

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
