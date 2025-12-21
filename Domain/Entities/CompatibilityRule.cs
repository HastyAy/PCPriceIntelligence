using Domain.Enums;

namespace Domain.Entities;

public class CompatibilityRule : BaseEntity
{
    public ComponentType SourceType { get; set; }  // e.g., PSU
    public ComponentType TargetType { get; set; }  // e.g., GPU

    public string RuleName { get; set; } = string.Empty;  // "PSU_Wattage_Check"

    // Store rule logic as JSON
    // Example: {"minWattage": 850, "gpuTDP": 450}
    public string ConditionJson { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;  // "650W PSU too weak for RTX 4090 (requires 850W)"

    // 1=Info, 2=Warning, 3=Error
    public int Severity { get; set; } = 2;

    public bool IsActive { get; set; } = true;
}