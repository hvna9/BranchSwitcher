namespace BranchSwitcher.Models;

public sealed record GitOperationResult(bool Success, string Repository, string Branch, string Message);
