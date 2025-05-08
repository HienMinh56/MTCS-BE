using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MTCS.Data;
using MTCS.Service.Services;

namespace MTCS.Service.BackgroundServices
{
    public class ContractExpirationService : BackgroundService
    {
        private readonly ILogger<ContractExpirationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const string CONTRACT_EXPIRY_ALERT_KEY = "Contract_Expiry_Alert";

        public ContractExpirationService(
            ILogger<ContractExpirationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Contract Expiration Check Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckContractExpirations(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking contract expirations.");
                }

                var nextRunTime = DateTime.Today.AddDays(1);
                var delay = nextRunTime - DateTime.Now;

                if (delay.TotalMilliseconds <= 0)
                {
                    delay = TimeSpan.FromHours(1);
                }

                _logger.LogInformation($"Next contract check scheduled for {DateTime.Now.Add(delay):yyyy-MM-dd HH:mm:ss}");
                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task CheckContractExpirations(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Checking contract expirations...");

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var config = await unitOfWork.SystemConfigurationRepository.GetConfigByKey(CONTRACT_EXPIRY_ALERT_KEY);

            int alertDays = 30;

            if (config != null && int.TryParse(config.ConfigValue, out int configuredDays))
            {
                alertDays = configuredDays;
                _logger.LogInformation($"Using configured alert threshold of {alertDays} days for contract expiry alerts.");
            }
            else
            {
                _logger.LogWarning($"{CONTRACT_EXPIRY_ALERT_KEY} configuration not found or invalid. Using default value of {alertDays} days.");
            }

            var contracts = await unitOfWork.ContractRepository.GetContractsWithCustomerAsync();
            var activeContracts = contracts.Where(c => c.Status == 1).ToList();
            var today = DateTime.Today;

            foreach (var contract in activeContracts)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                if (contract.EndDate.HasValue)
                {
                    var expirationDate = contract.EndDate.Value;
                    var daysUntilExpiration = (expirationDate.Date - today).Days;

                    if (daysUntilExpiration <= 0)
                    {
                        _logger.LogInformation($"Contract {contract.ContractId} for customer {contract.Customer?.CompanyName} has expired. Setting status to Inactive.");

                        contract.Status = 0;
                        await unitOfWork.ContractRepository.UpdateAsync(contract);

                        await NotifyStaff(
                            unitOfWork,
                            notificationService,
                            "Hợp đồng đã HẾT HẠN",
                            $"Hợp đồng {contract.ContractId} với khách hàng: {contract.Customer?.CompanyName} đã HẾT HẠN vào ngày {expirationDate:dd/MM/yyyy}."
                        );
                    }
                    else if (daysUntilExpiration <= alertDays)
                    {
                        bool shouldNotify =
                            daysUntilExpiration == alertDays ||
                            daysUntilExpiration == 30 ||
                            daysUntilExpiration == 7 ||
                            daysUntilExpiration == 3;

                        if (shouldNotify)
                        {
                            _logger.LogInformation($"Contract {contract.ContractId} for customer {contract.Customer?.CompanyName} expires in {daysUntilExpiration} days. Sending notification.");

                            // Notify staff and admin
                            await NotifyStaffAndAdmin(
                                unitOfWork,
                                notificationService,
                                "Hợp đồng sắp hết hạn",
                                $"Hợp đồng {contract.ContractId} với khách hàng: {contract.Customer?.CompanyName} sẽ HẾT HẠN sau {daysUntilExpiration} ngày, vào ngày {expirationDate:dd/MM/yyyy}."
                            );

                            await NotifyCustomerViaEmail(
                                emailService,
                                contract,
                                daysUntilExpiration
                            );
                        }
                    }
                }
            }
            _logger.LogInformation("Finished checking contract expirations.");
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

        private async Task NotifyStaffAndAdmin(
            UnitOfWork unitOfWork,
            INotificationService notificationService,
            string title,
            string body)
        {
            var users = await unitOfWork.InternalUserRepository.GetActiveInternalList();
            foreach (var user in users)
            {
                await notificationService.SendNotificationAsync(user.UserId, title, body, "System");
            }
        }

        private async Task NotifyCustomerViaEmail(
    IEmailService emailService,
    Data.Models.Contract contract,
    int daysUntilExpiration)
        {
            if (contract.Customer != null && !string.IsNullOrEmpty(contract.Customer.Email))
            {
                try
                {
                    var contractDate = contract.StartDate?.ToString("dd/MM/yyyy") ?? "N/A";
                    var expirationDate = contract.EndDate?.ToString("dd/MM/yyyy") ?? "N/A";

                    _logger.LogInformation($"Sending email notification to customer {contract.Customer.CompanyName} about contract expiration in {daysUntilExpiration} days");

                    await emailService.SendEmailContractExpirationAsync(
                        contract.Customer.Email,
                        contractDate,
                        expirationDate,
                        contract.Customer.CompanyName,
                        contract.ContractId,
                        contract.SignedTime
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email notification to customer {contract.Customer.CompanyName}");
                }
            }
            else
            {
                _logger.LogWarning($"Cannot send email notification for contract {contract.ContractId}: Customer email not available");
            }
        }
    }
}
