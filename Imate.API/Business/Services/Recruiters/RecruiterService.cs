using System.Linq;
using System.Text.Json.Serialization;
using Amazon.Runtime.Internal;
using Azure;
using Azure.Core;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Interfaces.Recruiters;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.JobApplications;
using Imate.API.Presentation.RequestModels.Recruiters;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.JobApplications;
using Imate.API.Presentation.ResponseModels.Recruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using static Imate.API.Common.Router.APIConfig;

namespace Imate.API.Business.Services.Recruiters
{
	public class RecruiterService : IRecruiterService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAuditLogService _auditLogService;
		private readonly IEmailService _emailService;
		private readonly IAwsS3StorageService _s3StorageService;

		public RecruiterService(IUnitOfWork unitOfWork, IAuditLogService auditLogService, IEmailService emailService, IAwsS3StorageService s3StorageService)
		{
			_unitOfWork = unitOfWork;
			_auditLogService = auditLogService;
			_emailService = emailService;
			_s3StorageService = s3StorageService;
		}

		public async Task<PagedList<GetJobRecruiterResponse>> GetListJobRecruiterAsync(int accountId, RecruiterJobSearchFilterRequest filterRequest)
		{
			try
			{
				var query = _unitOfWork.Recruiters.GetJobsByRecruiterId(accountId);
				// Search
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.SearchTerm))
				{
					query = query.Where(j => j.Title.Contains(filterRequest.SearchTerm));
				}

				// Filter location
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.Location))
				{
					query = query.Where(j => j.Location.Contains(filterRequest.Location));
				}

				// Filter employment type
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.EmploymentType))
				{
					query = query.Where(j => j.EmploymentType == filterRequest.EmploymentType);
				}

				// Filter status
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.Status))
				{
					query = query.Where(j => j.Status.ToString() == filterRequest.Status);
				}

				var jobs = query
					.Select(job => new GetJobRecruiterResponse
					{
						Id = job.Id,
						Title = job.Title,
						JobDescription = job.JobDescription,
						EmploymentType = job.EmploymentType,
						Location = job.Location,
						MinSalary = job.MinSalary,
						MaxSalary = job.MaxSalary,
						ApplicationDeadline = job.ApplicationDeadline,
						Status = job.Status,

						JobSkills = job.JobSkills.Select(s => new JobSkillResponse
						{
							Id = s.SkillId,
							SkillName = s.Skill.Name
						}).ToList(),

						JobPositions = job.JobPositions.Select(p => new JobPositionResponse
						{
							Id = p.PositionId,
							PositionName = p.Position.Name
						}).ToList()
					});
				return await PagedList<GetJobRecruiterResponse>.CreateAsync(jobs, filterRequest.PageNumber, filterRequest.PageSize);
			}
			catch (Exception ex)
			{
				throw new ApplicationException("An error occurred while retrieving Jobs.", ex);
			}
		}

		public async Task SubmitRecruiterProfileAsync(int accountId, SubmitRecruiterProfileRequest request)
		{
			if (request == null)
				throw new BadRequestException("Dữ liệu hồ sơ Recruiter không hợp lệ.");

			// Lấy account kèm theo navigation Recruiter
			var account = await _unitOfWork.Accounts.GetByIdRecruiter(accountId)
				?? throw new NotFoundException("Không tìm thấy tài khoản.");

			// Chỉ cho phép tài khoản role Recruiter nộp hồ sơ
			var primaryRole = account.AccountRoles.FirstOrDefault()?.Role.Name;
			if (primaryRole != RoleName.Recruiter)
			{
				throw new BadRequestException("Chỉ tài khoản Recruiter mới có thể nộp hồ sơ Recruiter.");
			}

			// Validate bắt buộc
			if (string.IsNullOrWhiteSpace(request.CompanyName))
				throw new BadRequestException("Tên công ty không được để trống.");
			if (string.IsNullOrWhiteSpace(request.Phone))
				throw new BadRequestException("Số điện thoại không được để trống.");

			if (account.Recruiter == null)
			{
				// Tạo mới hồ sơ Recruiter
				var recruiter = new Models.Entities.Recruiter
				{
					AccountId = account.Id,
					CompanyName = request.CompanyName.Trim(),
					Industry = request.Industry?.Trim() ?? "General",
					CompanySize = request.CompanySize?.Trim(),
					Website = request.CompanyWebsite?.Trim(),
					CompanyLogo = request.CompanyLogo,
					Address = request.CompanyAddress?.Trim(),
					Phone = request.Phone.Trim(),
					VerificationStatus = VerificationStatus.Pending
				};

				_unitOfWork.Recruiters.Create(recruiter);
			}
			else
			{
				// Cập nhật hồ sơ đã có
				account.Recruiter.CompanyName = request.CompanyName.Trim();
				account.Recruiter.Industry = request.Industry?.Trim() ?? account.Recruiter.Industry;
				account.Recruiter.CompanySize = request.CompanySize?.Trim() ?? account.Recruiter.CompanySize;
				account.Recruiter.Website = request.CompanyWebsite?.Trim();
				account.Recruiter.CompanyLogo = request.CompanyLogo ?? account.Recruiter.CompanyLogo;
				account.Recruiter.Address = request.CompanyAddress?.Trim();
				account.Recruiter.Phone = request.Phone.Trim();
				account.Recruiter.VerificationStatus = VerificationStatus.Pending;

				_unitOfWork.Recruiters.Update(account.Recruiter);
			}

			// Đảm bảo trạng thái account là PendingVerification sau khi nộp hồ sơ
			if (account.Status != AccountStatus.PendingVerification)
			{
				account.Status = AccountStatus.PendingVerification;
				await _unitOfWork.Accounts.UpdateAsync(account);
			}

			await _unitOfWork.SaveChangesAsync();
		}

		public async Task UpdataRecruiterrProfileAsync(int accountId, UpdateRecruiterProfileRequest request)
		{
			try
			{
				var recruiter = await _unitOfWork.Recruiters.GetRecruiterByIdAsync(accountId)
					?? throw new NotFoundException("Không tìm thấy hồ sơ Recruiter.");
				if (request.CompanyLogo != null)
				{
					if (request.CompanyLogo.Length == 0)
					{
						throw new BadRequestException("File ảnh không được rỗng.");
					}

					var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var extension = Path.GetExtension(request.CompanyLogo.FileName).ToLowerInvariant();

                    if (!request.CompanyLogo.ContentType.StartsWith("image/") || !allowedExtensions.Contains(extension))
                    {
                        throw new BadRequestException($"File không hợp lệ. Chỉ chấp nhận các định dạng ảnh: {string.Join(", ", allowedExtensions)}.");
                    }

                    const long maxFileSize = 5 * 1024 * 1024; // 5MB
                    if (request.CompanyLogo.Length > maxFileSize)
                    {
                        throw new BadRequestException("Dung lượng ảnh quá lớn. Vui lòng tải lên ảnh nhỏ hơn 5MB.");
                    }

                    if (!string.IsNullOrEmpty(recruiter.CompanyLogo))
					{
						await _s3StorageService.DeleteFileAsync(recruiter.CompanyLogo);
					}
					recruiter.CompanyLogo = await _s3StorageService.UploadFileAsync(request.CompanyLogo, "company-logos");

				}

				recruiter.CompanyName = request.CompanyName;
				recruiter.Website = request.Website;
				recruiter.Industry = request.Industry;
				recruiter.CompanySize = request.CompanySize;
				recruiter.Address = request.Address;
				recruiter.Phone = request.Phone;

				await _unitOfWork.Recruiters.UpdateRecruiterAsync(recruiter);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (NotFoundException)
			{
				throw;
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
			{
				throw new ApplicationException("An error occurred while Updating Recruiter profile.", ex);

			}

		}

		public async Task<Job> CreateJobPostAsync(int accountId, CreateUpdateJobRequest request)
		{
			try
			{
				if (request == null || request.ApplicationDeadline < DateTime.UtcNow.Date || request.MinSalary > request.MaxSalary)
					throw new BadRequestException("Dữ liệu hồ sơ Đăng tuyển không hợp lệ.");

				// Lấy account kèm theo navigation Recruiter
				var account = await _unitOfWork.Accounts.GetByIdRecruiter(accountId)
					?? throw new NotFoundException("Không tìm thấy tài khoản.");

				// Chỉ cho phép tài khoản role Recruiter
				var primaryRole = account.AccountRoles.FirstOrDefault()?.Role.Name;
				if (primaryRole != RoleName.Recruiter)
				{
					throw new BadRequestException("Chỉ tài khoản Recruiter mới có thể tạo Job.");
				}
				// Validate bắt buộc
				// Tạo mới Job
				var job = new Job
				{
					RecruiterId = account.Id,
					Title = request.Title,
					EmploymentType = request.EmploymentType,
					Location = request.Location,
					MinSalary = request.MinSalary,
					MaxSalary = request.MaxSalary,
					JobDescription = request.Description,
					ApplicationDeadline = request.ApplicationDeadline,
					CreatedAt = DateTime.UtcNow
				};

				job.JobPositions = request.JobPositions
				.Select(id => new JobPosition
				{
					PositionId = id
				})
				.ToList();

				job.JobSkills = request.JobSkills
				.Select(id => new JobSkill
				{
					SkillId = id
				})
				.ToList();
				var result = await _unitOfWork.Recruiters.CreateJobPostAsync(job);


				await _unitOfWork.SaveChangesAsync();
				var exsitingJob = await _unitOfWork.Recruiters.GetPostedJobByIdAsync(result.Id);
				var newData = new
				{
					exsitingJob.Title,
					exsitingJob.Location,
					exsitingJob.EmploymentType,
					exsitingJob.MinSalary,
					exsitingJob.MaxSalary,
					exsitingJob.JobDescription,
					exsitingJob.Status,
					exsitingJob.ApplicationDeadline,
					JobSkills = exsitingJob.JobSkills.Select(id => new JobSkillResponse
					{
						Id = id.SkillId,
						SkillName = id.Skill.Name,
					}),
					JobPosition = exsitingJob.JobPositions.Select(id => new JobPositionResponse
					{
						Id = id.PositionId,
						PositionName = id.Position.Name,

					}),
				};
				var (oldChanges, newChanges) = AuditHelper.GetChanges(new { }, newData);

				await _auditLogService.CreateAuditLogAsync(accountId, AuditAction.Create, "Job", result.Id, oldChanges, newChanges);
				return result;
			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}", ex);

			}
		}

		public async Task<Job> UpdateJobPostAsync(int accountId, CreateUpdateJobRequest request)
		{
			try
			{
				var exsitingJob = await _unitOfWork.Recruiters.GetPostedJobByIdAsync(request.Id);
				if (exsitingJob == null)
				{
					throw new NotFoundException($"Job with Id {request.Id} not found");
				}
				var oldData = new
				{
					exsitingJob.Title,
					exsitingJob.Location,
					exsitingJob.EmploymentType,
					exsitingJob.MinSalary,
					exsitingJob.MaxSalary,
					exsitingJob.JobDescription,
					exsitingJob.ApplicationDeadline,
					exsitingJob.Status,
					JobSkills = exsitingJob.JobSkills.Select(id => new JobSkillResponse
					{
						Id = id.SkillId,
						SkillName = id.Skill.Name,
					}).ToList(),
					JobPosition = exsitingJob.JobPositions.Select(id => new JobPositionResponse
					{
						Id = id.PositionId,
						PositionName = id.Position.Name,

					}).ToList(),
				};


				if (request == null || request.MinSalary > request.MaxSalary)
					throw new BadRequestException("Dữ liệu hồ sơ Đăng tuyển không hợp lệ.");
				// Lấy account kèm theo navigation Recruiter
				var account = await _unitOfWork.Accounts.GetByIdRecruiter(accountId)
					?? throw new NotFoundException("Không tìm thấy tài khoản.");

				// Chỉ cho phép tài khoản role Recruiter
				var primaryRole = account.AccountRoles.FirstOrDefault()?.Role.Name;
				if (primaryRole != RoleName.Recruiter)
				{
					throw new BadRequestException("Chỉ tài khoản Recruiter mới có thể cập nhật Job.");
				}
				// Validate bắt buộc
				// Tạo mới Job

				exsitingJob.RecruiterId = account.Id;
				exsitingJob.Title = request.Title;
				exsitingJob.EmploymentType = request.EmploymentType;
				exsitingJob.Location = request.Location;
				exsitingJob.MinSalary = request.MinSalary;
				exsitingJob.MaxSalary = request.MaxSalary;
				exsitingJob.JobDescription = request.Description;
				exsitingJob.ApplicationDeadline = request.ApplicationDeadline;
				exsitingJob.Status = request.Status;
				exsitingJob.UpdatedAt = DateTime.UtcNow;

				exsitingJob.JobPositions.Clear();

				exsitingJob.JobPositions = request.JobPositions
				.Select(id => new JobPosition
				{
					JobId = exsitingJob.Id,
					PositionId = id
				})
				.ToList();
				exsitingJob.JobSkills.Clear();

				exsitingJob.JobSkills = request.JobSkills
				.Select(id => new JobSkill
				{
					JobId = exsitingJob.Id,
					SkillId = id
				})
				.ToList();
				var result = await _unitOfWork.Recruiters.UpdateJobPostAsync(exsitingJob);


				await _unitOfWork.SaveChangesAsync();
				var jobUpdate = await _unitOfWork.Recruiters.GetPostedJobByIdAsync(result.Id);
				var newData = new
				{
					jobUpdate.Title,
					jobUpdate.Location,
					jobUpdate.EmploymentType,
					jobUpdate.MinSalary,
					jobUpdate.MaxSalary,
					jobUpdate.JobDescription,
					jobUpdate.Status,
					jobUpdate.ApplicationDeadline,
					JobSkills = jobUpdate.JobSkills.Select(id => new JobSkillResponse
					{
						Id = id.SkillId,
						SkillName = id.Skill?.Name,
					}).ToList(),
					JobPosition = jobUpdate.JobPositions.Select(id => new JobPositionResponse
					{
						Id = id.PositionId,
						PositionName = id.Position?.Name,

					}).ToList(),
				};
				var (oldChanges, newChanges) = AuditHelper.GetChanges(oldData, newData);
				await _auditLogService.CreateAuditLogAsync(accountId, AuditAction.Update, "Job", exsitingJob.Id, oldChanges, newChanges);
				return result;
			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}", ex);

			}
		}

		public async Task<Job> CloseJobPostAsync(int accountId, int jobId)
		{
			try
			{
				var exsitingJob = await _unitOfWork.Recruiters.GetPostedJobByIdAsync(jobId);
				
				if (exsitingJob == null)
				{
					throw new NotFoundException($"Job with Id {jobId} not found");
				}
                var oldData = new
                {
                    exsitingJob.Status,
                };

                // Lấy account kèm theo navigation Recruiter
                var account = await _unitOfWork.Accounts.GetByIdRecruiter(accountId)
					?? throw new NotFoundException("Không tìm thấy tài khoản.");

				var query = _unitOfWork.Recruiters.GetJobsByRecruiterId(accountId);
				var isJobOfRecruiter = query.Any(j => j.Id == jobId);

				if (!isJobOfRecruiter)
				{
					throw new ForbiddenException("Đơn ứng tuyển này không hợp lệ");
				}
				// Chỉ cho phép tài khoản role Recruiter
				var primaryRole = account.AccountRoles.FirstOrDefault()?.Role.Name;
				if (primaryRole != RoleName.Recruiter)
				{
					throw new BadRequestException("Chỉ tài khoản Recruiter mới có thể cập nhật Job.");
				}
				// Validate bắt buộc
				// Tạo mới Job
				exsitingJob.Status = JobStatus.Closed;
				exsitingJob.UpdatedAt = DateTime.UtcNow;
				var result = await _unitOfWork.Recruiters.UpdateJobPostAsync(exsitingJob);


				await _unitOfWork.SaveChangesAsync();
				await _auditLogService.CreateAuditLogAsync(accountId, AuditAction.Update, "Job", exsitingJob.Id,
				new { oldData },
				new
				{
					exsitingJob.Status,
				});
				return result;
			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}", ex);

			}


		}

		public async Task<PagedList<GetAppliedJobApplicationCandidateResponse>> GetAppliedCandidateByJobIdAsync(int jobId, AppliedApplicationCandidateFilterRequest filterRequest)
		{
			try
			{
				var query = _unitOfWork.Recruiters.GetJobApplicationsListByJobId(jobId);
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.SearchTerm))
				{
					query = query.Where(j => j.Candidate.FullName.ToLower().Contains(filterRequest.SearchTerm.ToLower()) || j.Candidate.Email.ToLower().Contains(filterRequest.SearchTerm.ToLower()));
				}
				if (filterRequest != null && filterRequest.Status.HasValue)
				{
					query = query.Where(j => j.Status.Equals(filterRequest.Status));
				}
				var applications = query
					.Select(application => new GetAppliedJobApplicationCandidateResponse
					{
						ApplicationId = application.Id,
						AppliedDate = application.AppliedDate,
						CandidateId = application.CandidateId,
						RecruiterFeedback = application.RecruiterFeedback,
						CandidateEmail = application.Candidate.Email,
						CandidateFullName = application.Candidate.FullName,
						CandidateFileName = application.Cv.FileName,
						CandidateFileUrl = application.Cv.FileUrl,
						CandidateScannedData = application.Cv.ScannedData,
						Status = application.Status,

					});
				return await PagedList<GetAppliedJobApplicationCandidateResponse>.CreateAsync(applications, filterRequest.PageNumber, filterRequest.PageSize);
			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}", ex);
			}


		}

		public async Task<PagedList<GetAllOpenedJobResponse>> GetAllOpenedJobs(JobPostingCandidateFilter filterRequest)
		{
			try
			{
				var query = _unitOfWork.Recruiters.GetAllOpenJobs();
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.SearchTerm))
				{
					query = query.Where(j => j.Title.ToLower().Contains(filterRequest.SearchTerm.ToLower()));
				}

				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.EmploymentType))
				{
					query = query.Where(j => j.EmploymentType.ToLower().Contains(filterRequest.EmploymentType.ToLower()));
				}

				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.Location))
				{
					query = query.Where(j => j.Location.ToLower().Contains(filterRequest.Location.ToLower()));
				}

				if (filterRequest.SkillIds?.Any() == true && filterRequest.SkillIds != null)
				{
					query = query.Where(j =>
						j.JobSkills.Any(p => filterRequest.SkillIds.Contains(p.SkillId)));
				}

				if (filterRequest.PositionIds?.Any() == true || filterRequest.PositionIds != null)
				{
					query = query.Where(j =>
						j.JobPositions.Any(p => filterRequest.PositionIds.Contains(p.PositionId)));
				}
				query = query.Where(j => j.Status.Equals(JobStatus.Open));
				var jobs = query.Select(jobs => new GetAllOpenedJobResponse
				{
					Id = jobs.Id,
					Title = jobs.Title,
					JobDescription = jobs.JobDescription,
					EmploymentType = jobs.EmploymentType,
					Location = jobs.Location,
					MinSalary = jobs.MinSalary,
					MaxSalary = jobs.MaxSalary,
					ApplicationDeadline = jobs.ApplicationDeadline,
					JobSkills = jobs.JobSkills.Select(s => new JobSkillResponse
					{
						Id = s.SkillId,
						SkillName = s.Skill.Name
					}).ToList(),

					JobPositions = jobs.JobPositions.Select(p => new JobPositionResponse
					{
						Id = p.PositionId,
						PositionName = p.Position.Name
					}).ToList(),
					CompanyRecruiter = new ComapnyRecruitment
					{
						Email = jobs.Recruiter.Email,
						CompanyName = jobs.Recruiter.Recruiter.CompanyName,
						CompanyLogo = jobs.Recruiter.Recruiter.CompanyLogo,
						Website = jobs.Recruiter.Recruiter.Website,
						Industry = jobs.Recruiter.Recruiter.Industry,
						CompanySize = jobs.Recruiter.Recruiter.CompanySize,
						Address = jobs.Recruiter.Recruiter.Address,
						Phone = jobs.Recruiter.Recruiter.Phone
					},
				});
				return await PagedList<GetAllOpenedJobResponse>.CreateAsync(jobs, filterRequest.PageNumber, filterRequest.PageSize);

			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}", ex);
			}
		}

		public async Task<GetAllOpenedJobResponse> GetJobDetail(int jobId)
		{
			try
			{
				var query = _unitOfWork.Recruiters.GetAllOpenJobs();
				var jobs = query.Where(j => j.Id == jobId).Select(jobs => new GetAllOpenedJobResponse
				{
					Id = jobs.Id,
					Title = jobs.Title,
					JobDescription = jobs.JobDescription,
					EmploymentType = jobs.EmploymentType,
					Location = jobs.Location,
					MinSalary = jobs.MinSalary,
					MaxSalary = jobs.MaxSalary,
					Status = jobs.Status,
					ApplicationDeadline = jobs.ApplicationDeadline,
					JobSkills = jobs.JobSkills.Select(s => new JobSkillResponse
					{
						Id = s.SkillId,
						SkillName = s.Skill.Name
					}).ToList(),

					JobPositions = jobs.JobPositions.Select(p => new JobPositionResponse
					{
						Id = p.PositionId,
						PositionName = p.Position.Name
					}).ToList(),
					CompanyRecruiter = new ComapnyRecruitment
					{
						Email = jobs.Recruiter.Email,
						CompanyName = jobs.Recruiter.Recruiter.CompanyName,
						CompanyLogo = jobs.Recruiter.Recruiter.CompanyLogo,
						Website = jobs.Recruiter.Recruiter.Website,
						Industry = jobs.Recruiter.Recruiter.Industry,
						CompanySize = jobs.Recruiter.Recruiter.CompanySize,
						Address = jobs.Recruiter.Recruiter.Address,
						Phone = jobs.Recruiter.Recruiter.Phone
					},
				}).FirstOrDefaultAsync();
				return await jobs;
			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}", ex);

			}

		}

		public async Task<JobApplication> UpdateJobApplication(int accountId, UpdateJobApplicationRequest request)
		{
			try
			{
				var jobApplication = await _unitOfWork.Recruiters.GetJobApplicationByIdAsync(request.Id);
				
				if (jobApplication == null)
				{
					throw new NotFoundException($"Job with Id {request.Id} not found");
				}
                var oldData = new
                {
                    jobApplication.Status,
                    jobApplication.RecruiterFeedback
                };
                // Lấy account kèm theo navigation Recruiter
                var account = await _unitOfWork.Accounts.GetByIdRecruiter(accountId)
					?? throw new NotFoundException("Không tìm thấy tài khoản.");

				// Chỉ cho phép tài khoản role Recruiter
				var primaryRole = account.AccountRoles.FirstOrDefault()?.Role.Name;
				if (primaryRole != RoleName.Recruiter)
				{
					throw new BadRequestException("Chỉ tài khoản Recruiter mới có thể cập nhật Job.");
				}
				jobApplication.Status = request.Status;
				var result = await _unitOfWork.Recruiters.UpdateJobApplicationStatusAsync(jobApplication);
				var newData = new
				{
					jobApplication.Status,
					jobApplication.RecruiterFeedback
				};

				await _unitOfWork.SaveChangesAsync();
				var emailSubject = "Kết quả ứng tuyển của bạn";
				var emailBody = $@"
									<p>Xin chào {jobApplication.Candidate?.FullName ?? "Bạn"},</p>

									<p>Cảm ơn bạn đã ứng tuyển thông qua nền tảng của chúng tôi.</p>

									<p>Chúng tôi xin thông báo rằng nhà tuyển dụng đã cập nhật trạng thái hồ sơ ứng tuyển cho vị trí:</p>

									<p><strong>{jobApplication.Job?.Title ?? "Vị trí không xác định"}</strong></p>

									<p>
									🔹 <strong>Trạng thái:</strong> {jobApplication.Status}<br>
									🔹 <strong>Nhận xét từ nhà tuyển dụng:</strong><br>
									{jobApplication.RecruiterFeedback ?? "Không có nhận xét thêm."}
									</p>
									";

				if (jobApplication.Status == JobApplicationStatus.Approved)
				{
					emailBody += @"
							<p>🎉 Chúc mừng! Hồ sơ của bạn đã được nhà tuyển dụng đánh giá phù hợp. Bạn có thể sớm nhận được liên hệ cho các bước tiếp theo.</p>
							";
				}
				else if (jobApplication.Status == JobApplicationStatus.Rejected)
				{
					emailBody += @"
							<p>Rất tiếc, hồ sơ của bạn hiện chưa phù hợp với vị trí này theo đánh giá từ nhà tuyển dụng.</p>

							<p>Tuy nhiên, bạn vẫn có thể tiếp tục ứng tuyển các cơ hội khác trên nền tảng.</p>
							";
				}
				else
				{
					emailBody += @"
							<p>Hồ sơ của bạn vẫn đang trong quá trình được nhà tuyển dụng xem xét.</p>
							";
				}
				await _emailService.SendEmailAsync("startingimate@gmail.com", emailSubject, emailBody);
				var (oldChanges, newChanges) = AuditHelper.GetChanges(oldData, newData);
				await _auditLogService.CreateAuditLogAsync(accountId, AuditAction.Update, "JobApplication", jobApplication.Id, oldChanges, newChanges);
				return jobApplication;
			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}", ex);

			}

		}

		public async Task<PagedList<GetCandidateAppliedJobResponse>> GetCandidateAppliedJob(int accountId, AppliedApplicationCandidateFilterRequest filterRequest)
		{
			try
			{
				var query = _unitOfWork.Recruiters.GetCandidateAppliedJob(accountId);
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.SearchTerm))
				{
					query = query.Where(j => j.Job.Title.ToLower().Contains(filterRequest.SearchTerm.ToLower()));
				}
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.Status.ToString()))
				{
					query = query.Where(j => j.Status.Equals(filterRequest.Status));
				}
				var result = query.Select(job => new GetCandidateAppliedJobResponse
				{
					Id = job.Id,
					Title = job.Job.Title,
					CompanyName = job.Job.Recruiter.Recruiter.CompanyName,
					CompanyLogo = job.Job.Recruiter.Recruiter.CompanyLogo,
					EmploymentType = job.Job.EmploymentType,
					Location = job.Job.Location,
					MinSalary = job.Job.MinSalary,
					MaxSalary = job.Job.MaxSalary,
					Status = job.Status,
					AppliedDate = job.AppliedDate,
					Feedback = job.RecruiterFeedback

				});
				return await PagedList<GetCandidateAppliedJobResponse>.CreateAsync(result, filterRequest.PageNumber, filterRequest.PageSize);

			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}", ex);

			}

		}

		public async Task<JobApplication> CreateJobApplication(int accountId, CreateJobApplicationRequest request)
		{
			try
			{
				if (request == null)
					throw new BadRequestException("Dữ liệu hồ sơ Đăng tuyển không hợp lệ.");

				var account = await _unitOfWork.Accounts.GetByIdAsync(accountId)
					?? throw new NotFoundException("Không tìm thấy tài khoản.");

				var job = await _unitOfWork.Recruiters.GetPostedJobByIdAsync(request.JobId)
					?? throw new NotFoundException("Không tìm thấy công việc.");

				var existingApplication = _unitOfWork.Recruiters.GetAllJobApplication()
				.Where(x => x.CandidateId == accountId && x.JobId == request.JobId)
				.OrderByDescending(x => x.AppliedDate) // lấy bản mới nhất
				.FirstOrDefault();

				if (job.ApplicationDeadline < DateTime.UtcNow)
					throw new ApplicationException("Công việc đã hết hạn ứng tuyển");

				if (existingApplication != null && existingApplication.Status != JobApplicationStatus.Rejected)
				{
					throw new ApplicationException("Bạn đã ứng tuyển công việc này rồi");
				}
				var jobApplication = new JobApplication
				{
					CandidateId = accountId,
					JobId = request.JobId,
					CvId = request.CVId,
					AppliedDate = DateTime.UtcNow,
					Status = JobApplicationStatus.Waiting
				};
				var result = await _unitOfWork.Recruiters.CreateJobApplicationAsync(jobApplication);

				var (oldChanges, newChanges) = AuditHelper.GetChanges(new { }, jobApplication);

				await _auditLogService.CreateAuditLogAsync(accountId, AuditAction.Create, "JobApplication", result.Id, oldChanges, newChanges);
				await _unitOfWork.SaveChangesAsync();
				return result;
			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}", ex);

			}
		}

		public async Task<string> UploadCompanyLogoAsync(IFormFile file)
		{
			if (file == null || file.Length == 0)
				throw new BadRequestException("Vui lòng chọn file logo.");

			// Validate file type (Images only)
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(extension))
				throw new BadRequestException("Vui lòng tải lên file ảnh (jpg, jpeg, png, gif, webp).");

			// Max size 2MB
			if (file.Length > 2 * 1024 * 1024)
				throw new BadRequestException("Kích thước tệp logo không được vượt quá 2MB.");

			return await _s3StorageService.UploadFileAsync(file, "company-logos");
		}
	}
}