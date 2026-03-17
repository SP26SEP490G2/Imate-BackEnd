using System.Linq;
using Amazon.Runtime.Internal;
using Azure;
using Azure.Core;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
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

namespace Imate.API.Business.Services.Recruiters
{
	public class RecruiterService : IRecruiterService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAuditLogService _auditLogService;

		public RecruiterService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
		{
			_unitOfWork = unitOfWork;
			_auditLogService = auditLogService;
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
				var recruiter = new Recruiter
				{
					AccountId = account.Id,
					CompanyName = request.CompanyName.Trim(),
					Industry = request.Industry?.Trim() ?? "General",
					CompanySize = request.CompanySize?.Trim(),
					Website = request.CompanyWebsite?.Trim(),
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
			var recruiter = await _unitOfWork.Recruiters.GetRecruiterByIdAsync(accountId)
				?? throw new NotFoundException("Không tìm thấy hồ sơ Recruiter.");

			recruiter.CompanyName = request.CompanyName;
			recruiter.CompanyLogo = request.CompanyLogo;
			recruiter.Website = request.Website;
			recruiter.Industry = request.Industry;
			recruiter.CompanySize = request.CompanySize;
			recruiter.Address = request.Address;
			recruiter.Phone = request.Phone;

			await _unitOfWork.Recruiters.UpdateRecruiterAsync(recruiter);
			await _unitOfWork.SaveChangesAsync();
		}

		public async Task<Job> CreateJobPostAsync(int accountId, CreateUpdateJobRequest request)
		{
			if (request == null || request.ApplicationDeadline < DateTime.UtcNow.Date || request.MinSalary > request.MinSalary)
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

		public async Task<Job> UpdateJobPostAsync(int accountId, CreateUpdateJobRequest request)
		{
			var exsitingJob = await _unitOfWork.Recruiters.GetPostedJobByIdAsync(request.Id);
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
			if (exsitingJob == null)
			{
				throw new NotFoundException($"Job with Id {request.Id} not found");
			}


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
				}).ToList(),
				JobPosition = exsitingJob.JobPositions.Select(id => new JobPositionResponse
				{
					Id = id.PositionId,
					PositionName = id.Position.Name,

				}).ToList(),
			};
			var (oldChanges, newChanges) = AuditHelper.GetChanges(oldData, newData);
			await _auditLogService.CreateAuditLogAsync(accountId, AuditAction.Update, "Job", exsitingJob.Id, oldChanges, newChanges);
			return result;
		}

		public async Task<Job> CloseJobPostAsync(int accountId, int jobId)
		{
			var exsitingJob = await _unitOfWork.Recruiters.GetPostedJobByIdAsync(jobId);
			var oldData = new
			{
				exsitingJob.Status,
			};
			if (exsitingJob == null)
			{
				throw new NotFoundException($"Job with Id {jobId} not found");
			}


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

		public async Task<PagedList<GetAppliedJobApplicationCandidateResponse>> GetAppliedCandidateByJobIdAsync(int jobId, AppliedApplicationCandidateFilterRequest filterRequest)
		{
			try
			{
				var query = _unitOfWork.Recruiters.GetJobApplicationsListByJobId(jobId);
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.SearchTerm))
				{
					query = query.Where(j => j.Candidate.FullName.ToLower().Contains(filterRequest.SearchTerm.ToLower()) || j.Candidate.Email.ToLower().Contains(filterRequest.SearchTerm.ToLower()));
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
				throw new ApplicationException("An error occurred while retrieving Candidates.", ex);
			}


		}

		public async Task<PagedList<GetAllOpenedJobResponse>> GetAllOpenedJobs(JobPostingCandidateFilter filterRequest)
		{
			try
			{
				var query = _unitOfWork.Recruiters.GetAllOpenJobs();
				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.SearchTerm))
				{
					query = query.Where(j=>j.Title.ToLower().Contains(filterRequest.SearchTerm.ToLower()));
				}

				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.EmploymentType))
				{
					query = query.Where(j=>j.EmploymentType.ToLower().Contains(filterRequest.EmploymentType.ToLower()));
				}

				if (filterRequest != null && !string.IsNullOrEmpty(filterRequest.Location))
				{
					query = query.Where(j=>j.Location.ToLower().Contains(filterRequest.Location.ToLower()));
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
				throw new ApplicationException("An error occurred while retrieving Jobs.", ex);
			}

			
		}
	}
}
