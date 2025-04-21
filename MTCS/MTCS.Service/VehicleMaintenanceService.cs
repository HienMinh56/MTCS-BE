using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MTCS.Data;
using MTCS.Service.Services;

namespace MTCS.Service
{
    public class VehicleMaintenanceService : BackgroundService
    {
        private readonly ILogger<VehicleMaintenanceService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const string MAINTENANCE_DUE_ALERT_KEY = "Maintenance_Due_Alert";

        public VehicleMaintenanceService(
            ILogger<VehicleMaintenanceService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Vehicle Maintenance Check Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckVehicleMaintenances(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking vehicle maintenance schedules.");
                }

                var nextRunTime = DateTime.Today.AddDays(1);
                var delay = nextRunTime - DateTime.Now;

                if (delay.TotalMilliseconds <= 0)
                {
                    delay = TimeSpan.FromMinutes(10);
                }

                _logger.LogInformation($"Next maintenance check scheduled for {DateTime.Now.Add(delay):yyyy-MM-dd HH:mm:ss}");
                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task CheckVehicleMaintenances(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Checking vehicle maintenance schedules...");

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var config = await unitOfWork.SystemConfigurationRepository.GetConfigByKey(MAINTENANCE_DUE_ALERT_KEY);

            int alertDays = 7; // Default value

            if (config != null && int.TryParse(config.ConfigValue, out int configuredDays))
            {
                alertDays = configuredDays;
                _logger.LogInformation($"Using configured alert threshold of {alertDays} days for maintenance due alerts.");
            }
            else
            {
                _logger.LogWarning($"{MAINTENANCE_DUE_ALERT_KEY} configuration not found or invalid. Using default value of {alertDays} days.");
            }

            await CheckTractorMaintenances(unitOfWork, notificationService, alertDays, stoppingToken);
            await CheckTrailerMaintenances(unitOfWork, notificationService, alertDays, stoppingToken);

            _logger.LogInformation("Finished checking vehicle maintenance schedules.");
        }

        private async Task CheckTractorMaintenances(
            UnitOfWork unitOfWork,
            INotificationService notificationService,
            int alertDays,
            CancellationToken stoppingToken)
        {
            var activeTractors = await unitOfWork.TractorRepository.GetActiveTractorsAsync();
            var today = DateTime.Today;

            foreach (var tractor in activeTractors)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                if (tractor.NextMaintenanceDate.HasValue)
                {
                    var maintenanceDate = tractor.NextMaintenanceDate.Value;
                    var daysUntilMaintenance = (maintenanceDate - today).Days;

                    if (daysUntilMaintenance <= alertDays && daysUntilMaintenance > 0)
                    {
                        bool shouldNotify =
                            daysUntilMaintenance == alertDays || // First day in alert period
                            daysUntilMaintenance == 7 ||         // 7 days before
                            daysUntilMaintenance == 1;           // 1 day before

                        if (shouldNotify)
                        {
                            _logger.LogInformation($"Tractor {tractor.TractorId} ({tractor.LicensePlate}) maintenance due in {daysUntilMaintenance} days. Sending notification.");

                            await NotifyStaff(
                                unitOfWork,
                                notificationService,
                                "Đầu kéo sắp đến hạn bảo dưỡng",
                                $"Đầu kéo {tractor.LicensePlate} (ID: {tractor.TractorId}) cần được bảo dưỡng trong {daysUntilMaintenance} ngày nữa vào ngày {maintenanceDate:dd/MM/yyyy}."
                            );
                        }
                    }
                    else if (daysUntilMaintenance <= 0)
                    {
                        _logger.LogInformation($"Tractor {tractor.TractorId} ({tractor.LicensePlate}) maintenance is overdue by {Math.Abs(daysUntilMaintenance)} days. Sending notification.");

                        await NotifyStaff(
                            unitOfWork,
                            notificationService,
                            "Đầu kéo đã QUÁ HẠN bảo dưỡng",
                            $"Đầu kéo {tractor.LicensePlate} (ID: {tractor.TractorId}) đã QUÁ HẠN bảo dưỡng {Math.Abs(daysUntilMaintenance)} ngày (từ {maintenanceDate:dd/MM/yyyy}). Vui lòng xử lý ngay!"
                        );
                    }
                }
            }
        }

        private async Task CheckTrailerMaintenances(
            UnitOfWork unitOfWork,
            INotificationService notificationService,
            int alertDays,
            CancellationToken stoppingToken)
        {
            var activeTrailers = await unitOfWork.TrailerRepository.GetActiveTrailersAsync();
            var today = DateTime.Today;

            foreach (var trailer in activeTrailers)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                if (trailer.NextMaintenanceDate.HasValue)
                {
                    var maintenanceDate = trailer.NextMaintenanceDate.Value;
                    var daysUntilMaintenance = (maintenanceDate - today).Days;

                    if (daysUntilMaintenance <= alertDays && daysUntilMaintenance > 0)
                    {
                        bool shouldNotify =
                            daysUntilMaintenance == alertDays || // First day in alert period
                            daysUntilMaintenance == 7 ||         // 7 days before
                            daysUntilMaintenance == 1;           // 1 day before

                        if (shouldNotify)
                        {
                            _logger.LogInformation($"Trailer {trailer.TrailerId} ({trailer.LicensePlate}) maintenance due in {daysUntilMaintenance} days. Sending notification.");

                            await NotifyStaff(
                                unitOfWork,
                                notificationService,
                                "Rơ-móoc sắp đến hạn bảo dưỡng",
                                $"Rơ-móoc {trailer.LicensePlate} (ID: {trailer.TrailerId}) cần được bảo dưỡng trong {daysUntilMaintenance} ngày nữa vào ngày {maintenanceDate:dd/MM/yyyy}."
                            );
                        }
                    }
                    else if (daysUntilMaintenance <= 0)
                    {
                        _logger.LogInformation($"Trailer {trailer.TrailerId} ({trailer.LicensePlate}) maintenance is overdue by {Math.Abs(daysUntilMaintenance)} days. Sending notification.");

                        await NotifyStaff(
                            unitOfWork,
                            notificationService,
                            "Rơ-móoc đã QUÁ HẠN bảo dưỡng",
                            $"Rơ-móoc {trailer.LicensePlate} (ID: {trailer.TrailerId}) đã QUÁ HẠN bảo dưỡng {Math.Abs(daysUntilMaintenance)} ngày (từ {maintenanceDate:dd/MM/yyyy}). Vui lòng xử lý ngay!"
                        );
                    }
                }
            }
        }

        private async Task NotifyStaff(
            UnitOfWork unitOfWork,
            INotificationService notificationService,
            string title,
            string body)
        {
            var staffs = await unitOfWork.InternalUserRepository.GetStaffList();
            foreach (var staff in staffs)
            {
                await notificationService.SendNotificationAsync(staff.UserId, title, body, "System");
            }
        }
    }
}
