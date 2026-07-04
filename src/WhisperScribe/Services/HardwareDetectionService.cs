using System.Management;
using WhisperScribe.Models;

namespace WhisperScribe.Services;

/// <summary>
/// Detects available compute hardware (CPU always; GPU via WMI video controller inventory)
/// so the user can pick which device Whisper.net should run inference on.
/// </summary>
public class HardwareDetectionService
{
    public List<HardwareOption> GetAvailableHardware()
    {
        var options = new List<HardwareOption>();

        // CPU is always available.
        int threads = Environment.ProcessorCount;
        options.Add(new HardwareOption
        {
            Kind = HardwareKind.Cpu,
            DisplayName = $"CPU ({threads} logical threads)",
            DeviceId = "cpu",
            IsRecommended = false
        });

        // Enumerate GPUs via WMI. NVIDIA cards are highlighted as recommended since
        // Whisper.net.Runtime.Cuda provides the accelerated backend for them.
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "Unknown GPU";

                // Skip virtual/remote display adapters that can't run compute workloads.
                if (name.Contains("Remote", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Basic Render", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool isNvidia = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase);

                options.Add(new HardwareOption
                {
                    Kind = HardwareKind.Gpu,
                    DisplayName = isNvidia ? $"{name} (GPU · CUDA)" : $"{name} (GPU)",
                    DeviceId = isNvidia ? "cuda:0" : "gpu:0",
                    IsRecommended = isNvidia
                });
            }
        }
        catch
        {
            // WMI can be unavailable in locked-down environments — fail gracefully to CPU-only.
        }

        return options;
    }

    public HardwareOption GetRecommendedDefault(IEnumerable<HardwareOption> options) =>
        options.FirstOrDefault(o => o.IsRecommended) ?? options.First(o => o.Kind == HardwareKind.Cpu);
}
