// Command response models have been moved to SecurityScanner.Core.Models.CommandModels
// This file is kept for backwards compatibility but all models are now in Core
using SecurityScanner.Core.Models;

namespace SecurityScanner.Commands.Models
{
    // Re-export the models from Core for backwards compatibility
    using CommandResponse = SecurityScanner.Core.Models.CommandResponse;
    using ScanCommandResponse = SecurityScanner.Core.Models.ScanCommandResponse;
    using ScanSummary = SecurityScanner.Core.Models.ScanSummary;
    using ReportCommandResponse = SecurityScanner.Core.Models.ReportCommandResponse;
    using ConfigCommandResponse = SecurityScanner.Core.Models.ConfigCommandResponse;
    using HealthCommandResponse = SecurityScanner.Core.Models.HealthCommandResponse;
    using ScannerHealthStatus = SecurityScanner.Core.Models.ScannerHealthStatus;
}