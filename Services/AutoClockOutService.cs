using Data.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class AutoClockOutService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<AutoClockOutService> _logger;

        public AutoClockOutService(IServiceProvider services, ILogger<AutoClockOutService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var timeEntryService = scope.ServiceProvider.GetRequiredService<ITimeEntryService>();
                        await timeEntryService.ProcessAutoClockOut();
                        await timeEntryService.ProcessAbsentees();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoClockOutService");
                }

                // Run every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
