using System.Collections.Generic;

namespace LocalWhisper.Services;

/// <summary>
/// Result of data root validation.
/// </summary>
/// <remarks>
/// Contains validation status, errors, and warnings.
/// See: docs/iterations/iteration-05b-download-repair.md (Task 3)
/// </remarks>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}
