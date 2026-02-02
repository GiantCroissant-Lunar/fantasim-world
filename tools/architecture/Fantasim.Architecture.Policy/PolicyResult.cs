namespace Fantasim.Architecture.Policy
{
    public enum PolicyDecision
    {
        Allow,      // No diagnostic
        Warn,       // Emit as Warning
        Error,      // Emit as Error
        Suppress,   // Explicitly suppress (if allowed)
        Hidden      // Diagnostic exists but is hidden
    }

    public class PolicyResult
    {
        public PolicyDecision Decision { get; set; }
        public string MessageOverride { get; set; } = string.Empty;
        public bool IsConstitutional { get; set; }

        public static PolicyResult DefaultError(string message = "") => new PolicyResult { Decision = PolicyDecision.Error, MessageOverride = message, IsConstitutional = true };
        public static PolicyResult DefaultWarning(string message = "") => new PolicyResult { Decision = PolicyDecision.Warn, MessageOverride = message };
    }
}
