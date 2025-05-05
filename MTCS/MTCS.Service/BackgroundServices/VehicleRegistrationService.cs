// MTCS.Service/VehicleRegistrationService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MTCS.Data;
using MTCS.Data.Enums;
using MTCS.Service.Services;

public class VehicleRegistrationService : BackgroundService
{
    private readonly ILogger<VehicleRegistrationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const string REGISTRATION_EXPIRY_ALERT_KEY = "Registration_Expiry_Alert";

    public VehicleRegistrationService(
        ILogger<VehicleRegistrationService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Vehicle Registration Check Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckVehicleRegistrations(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking vehicle registrations.");
            }

            var nextRunTime = DateTime.Today.AddDays(1);
            var delay = nextRunTime - DateTime.Now;

            if (delay.TotalMilliseconds <= 0)
            {
                delay = TimeSpan.FromMinutes(10);
            }

            _logger.LogInformation($"Next check scheduled for {DateTime.Now.Add(delay):yyyy-MM-dd HH:mm:ss}");
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task CheckVehicleRegistrations(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Checking vehicle registrations...");

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var config = await unitOfWork.SystemConfigurationRepository.GetConfigByKey(REGISTRATION_EXPIRY_ALERT_KEY);

        int alertDays = 7; // Default value

        if (config != null && int.TryParse(config.ConfigValue, out int configuredDays))
        {
            alertDays = configuredDays;
            _logger.LogInformation($"Using configured alert threshold of {alertDays} days for registration expiry alerts.");
        }
        else
        {
            _logger.LogWarning($"{REGISTRATION_EXPIRY_ALERT_KEY} configuration not found or invalid. Using default value of {alertDays} days.");
        }

        await CheckTractorRegistrations(unitOfWork, notificationService, alertDays, stoppingToken);
        await CheckTrailerRegistrations(unitOfWork, notificationService, alertDays, stoppingToken);

        _logger.LogInformation("Finished checking vehicle registrations.");
    }

    private async Task CheckTractorRegistrations(
    UnitOfWork unitOfWork,
    INotificationService notificationService,
    int alertDays,
    CancellationToken stoppingToken)
    {
        var activeTractors = await unitOfWork.TractorRepository.GetActiveTractorsAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var tractor in activeTractors)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            if (tractor.RegistrationExpirationDate.HasValue)
            {
                var expirationDate = tractor.RegistrationExpirationDate.Value;
                var daysUntilExpiration = (expirationDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;

                // Only deactivate tractors with 'Active' status
                if (daysUntilExpiration <= 0 && tractor.Status == TractorStatus.Active.ToString())
                {
                    _logger.LogInformation($"Tractor {tractor.TractorId} ({tractor.LicensePlate}) registration has expired. Setting to Inactive.");

                    tractor.Status = TractorStatus.Inactive.ToString();
                    tractor.ModifiedDate = DateTime.Now;
                    tractor.ModifiedBy = "System";

                    await unitOfWork.TractorRepository.UpdateAsync(tractor);

                    await NotifyStaff(
                        unitOfWork,
                        notificationService,
                        "Đầu kéo HẾT HẠN đăng kiểm",
                        $"Đầu kéo {tractor.LicensePlate} (ID: {tractor.TractorId}) đăng kiểm đã HẾT HẠN. Đã vô hiệu hoá Đầu kéo. Vui lòng đăng kiểm phương tiện!"
                    );
                }
                else if (daysUntilExpiration <= alertDays && daysUntilExpiration > 0 &&
                        (tractor.Status == TractorStatus.Active.ToString() || tractor.Status == TractorStatus.Onduty.ToString()))
                {
                    bool shouldNotify =
                    daysUntilExpiration == alertDays ||
                    daysUntilExpiration == 7 ||           // 7 days before
                    daysUntilExpiration == 3 ||
                    daysUntilExpiration == 1;             // 1 day before

                    if (shouldNotify)
                    {
                        _logger.LogInformation($"Tractor {tractor.TractorId} ({tractor.LicensePlate}) registration expires in {daysUntilExpiration} days. Sending notification.");

                        await NotifyStaff(
                            unitOfWork,
                            notificationService,
                            "Đầu kéo sắp đến hạn đăng kiểm",
                            $"Đầu kéo {tractor.LicensePlate} (ID: {tractor.TractorId}) còn đăng kiểm {daysUntilExpiration} ngày và HẾT HẠN vào {expirationDate}."
                        );
                    }
                }
            }
        }
    }

    private async Task CheckTrailerRegistrations(
    UnitOfWork unitOfWork,
    INotificationService notificationService,
    int alertDays,
    CancellationToken stoppingToken)
    {
        var activeTrailers = await unitOfWork.TrailerRepository.GetActiveTrailersAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var trailer in activeTrailers)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            if (trailer.RegistrationExpirationDate.HasValue)
            {
                var expirationDate = trailer.RegistrationExpirationDate.Value;
                var daysUntilExpiration = (expirationDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;

                // Only deactivate trailers with 'Active' status
                if (daysUntilExpiration <= 0 && trailer.Status == TrailerStatus.Active.ToString())
                {
                    _logger.LogInformation($"Trailer {trailer.TrailerId} ({trailer.LicensePlate}) registration has expired. Setting to Inactive.");

                    trailer.Status = TrailerStatus.Inactive.ToString();
                    trailer.ModifiedDate = DateTime.Now;
                    trailer.ModifiedBy = "System";

                    await unitOfWork.TrailerRepository.UpdateAsync(trailer);

                    await NotifyStaff(
                        unitOfWork,
                        notificationService,
                        "Rơ-móoc HẾT HẠN đăng kiểm",
                        $"Rơ-móoc {trailer.LicensePlate} (ID: {trailer.TrailerId}) đăng kiểm đã HẾT HẠN. Đã vô hiệu hoá Rơ-móoc. Vui lòng đăng kiểm phương tiện!"
                    );
                }
                else if (daysUntilExpiration <= alertDays && daysUntilExpiration > 0 &&
                        (trailer.Status == TrailerStatus.Active.ToString() || trailer.Status == TrailerStatus.Onduty.ToString()))
                {
                    bool shouldNotify =
                   daysUntilExpiration == alertDays ||
                   daysUntilExpiration == 7 ||           // 7 days before
                   daysUntilExpiration == 3 ||
                   daysUntilExpiration == 1;             // 1 day before

                    if (shouldNotify)
                    {
                        _logger.LogInformation($"Trailer {trailer.TrailerId} ({trailer.LicensePlate}) registration expires in {daysUntilExpiration} days.");

                        await NotifyStaff(
                            unitOfWork,
                            notificationService,
                            "Rơ-móoc sắp đến hạn đăng kiểm",
                            $"Rơ-móoc {trailer.LicensePlate} (ID: {trailer.TrailerId}) còn đăng kiểm {daysUntilExpiration} ngày và HẾT HẠN vào {expirationDate}."
                        );
                    }
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
