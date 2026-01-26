using Domain.Entities;
using Domain.Enums;

namespace web.Services;

public class CompatibilityService
{
    public CompatibilityResult RunPreCompatibilityChecks(
        IReadOnlyDictionary<ComponentType, Component> selected)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var cpu = selected.GetValueOrDefault(ComponentType.CPU);
        var mb = selected.GetValueOrDefault(ComponentType.Motherboard);
        var ram = selected.GetValueOrDefault(ComponentType.RAM);
        var gpu = selected.GetValueOrDefault(ComponentType.GPU);
        var psu = selected.GetValueOrDefault(ComponentType.PSU);
        var cs = selected.GetValueOrDefault(ComponentType.Case);
        var cooler = selected.GetValueOrDefault(ComponentType.Cooling);

        // =========================
        // RAM ↔ Motherboard
        // =========================
        if (ram?.RAMSpec != null && mb?.MotherboardSpec != null)
        {
            if (!string.Equals(
                    ram.RAMSpec.Type,
                    mb.MotherboardSpec.MemoryType,
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("RAM type does not match motherboard.");
            }

            if (mb.MotherboardSpec.MemorySlots.HasValue &&
                ram.RAMSpec.ModuleCount > mb.MotherboardSpec.MemorySlots.Value)
            {
                errors.Add("RAM module count exceeds motherboard slots.");
            }

            if (mb.MotherboardSpec.MaxMemoryCapacityGB.HasValue &&
                ram.RAMSpec.Capacity > mb.MotherboardSpec.MaxMemoryCapacityGB.Value)
            {
                errors.Add("RAM capacity exceeds motherboard maximum.");
            }
        }

        // =========================
        // GPU ↔ Case
        // =========================
        if (gpu?.GPUSpec != null && cs?.CaseSpec != null)
        {
            if (cs.CaseSpec.MaxGPULengthMM.HasValue &&
                gpu.GPUSpec.LengthMM > cs.CaseSpec.MaxGPULengthMM.Value)
            {
                errors.Add("GPU is too long for the selected case.");
            }
        }

        // =========================
        // Cooler ↔ Case / CPU
        // =========================
        if (cooler?.CPUCoolerSpec != null && cs?.CaseSpec != null)
        {
            if (cs.CaseSpec.MaxCoolerHeightMM.HasValue &&
                cooler.CPUCoolerSpec.HeightMM > cs.CaseSpec.MaxCoolerHeightMM.Value)
            {
                errors.Add("CPU cooler is too tall for the case.");
            }
        }


        // =========================
        // PSU ↔ System Power
        // =========================
        if (psu?.PSUSpec?.Wattage is int psuWattage)
        {
            var cpuTdp = cpu?.CPUSpec?.TDP ?? 0;
            var gpuTdp = gpu?.GPUSpec?.TDP ?? 0;

            var estimatedLoad =
                cpuTdp +
                (int)(gpuTdp * 1.5) +
                100;

            if (psuWattage < estimatedLoad)
            {
                errors.Add(
                    $"PSU wattage insufficient: {psuWattage}W < recommended ~{estimatedLoad}W");
            }
        }

        // =========================
        // Motherboard ↔ Case
        // =========================
        if (mb?.MotherboardSpec?.FormFactor != null &&
     cs?.CaseSpec?.FormFactor != null)
        {
            var boardFF = NormalizeMotherboardFormFactor(
                mb.MotherboardSpec.FormFactor);

            var supportedFFs = GetSupportedMotherboardFormFactors(
                cs.CaseSpec.FormFactor);

            if (!supportedFFs.Contains(boardFF))
            {
                errors.Add(
                    $"Motherboard form factor ({mb.MotherboardSpec.FormFactor}) is not supported by the case.");
            }
        }

        return new CompatibilityResult
        {
            Compatible = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
    private static HashSet<string> GetSupportedMotherboardFormFactors(string caseFormFactor)
    {
        var value = caseFormFactor.ToLowerInvariant();

        var supported = new HashSet<string>();

        // Small cases
        if (value.Contains("itx"))
        {
            supported.Add("mini-itx");
            supported.Add("dtx");
        }

        // Midi / Mid tower (MOST common)
        if (value.Contains("midi") || value.Contains("mid"))
        {
            supported.Add("mini-itx");
            supported.Add("micro-atx");
            supported.Add("atx");
        }

        // Full tower
        if (value.Contains("full"))
        {
            supported.Add("mini-itx");
            supported.Add("micro-atx");
            supported.Add("atx");
            supported.Add("e-atx");
        }

        // Explicit listings (best case)
        if (value.Contains("atx"))
            supported.Add("atx");

        if (value.Contains("micro"))
            supported.Add("micro-atx");

        if (value.Contains("mini"))
            supported.Add("mini-itx");

        return supported;
    }

    private static string NormalizeMotherboardFormFactor(string value)
    {
        value = value.ToLowerInvariant();

        if (value.Contains("mini") && value.Contains("itx"))
            return "mini-itx";

        if (value.Contains("micro") || value.Contains("µatx"))
            return "micro-atx";

        if (value.Contains("e-atx") || value.Contains("eatx"))
            return "e-atx";

        if (value.Contains("atx"))
            return "atx";

        return value;
    }

}
