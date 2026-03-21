using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.DataAccess.Repositories;
using Imate.API.Models.Enums;

namespace Imate.API.Business.Services.Recruiters
{
	public class AutoCloseJobServices : BackgroundService
	{
		private readonly IServiceScopeFactory _scopeFactory;

		public AutoCloseJobServices(IServiceScopeFactory scopeFactory)
		{
			_scopeFactory = scopeFactory;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{


			while (!stoppingToken.IsCancellationRequested)
			{
				var now = DateTime.UtcNow;
				var nextRun = now.Date.AddDays(1);

				var delay = nextRun - now;

				await Task.Delay(delay, stoppingToken);
				try
				{
					using (var scope = _scopeFactory.CreateScope())
					{
						var jobRepo = scope.ServiceProvider.GetRequiredService<IRecruiterRepository>();
						var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

						var jobs = await jobRepo.GetJobsToCloseAsync();

						if (jobs.Any())
						{
							foreach (var job in jobs)
							{
								job.Status = JobStatus.Closed;
							}

							await unitOfWork.SaveChangesAsync();
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"AutoCloseJob Error: {ex.Message}");
				}

			}
		}
	}
}
