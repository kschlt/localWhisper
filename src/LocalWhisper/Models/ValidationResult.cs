namespace LocalWhisper.Models;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
/// <remarks>
/// Used by validators (DataRootValidator, ModelValidator, etc.)
/// to return validation status with detailed error/warning messages.
/// Iteration 5: Repair flow validation.
/// Iteration 6: Settings window validation.
/// </remarks>
public class ValidationResult
{
    /// <summary>
    /// Whether validation passed (no errors).
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// List of error messages (validation failures).
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of warning messages (non-critical issues).
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Helper method to add an error and mark validation as invalid.
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Helper method to add a warning (doesn't affect IsValid).
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Check if there are any errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Check if there are any warnings.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;
}
