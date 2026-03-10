using Amazon.Runtime.Internal;
using Azure;
using Azure.Core;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Recruiters;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Recruiters;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.Recruiter;
using Microsoft.EntityFrameworkCore;

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

                var jobs =  query
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

            await _auditLogService.CreateAuditLogAsync(accountId, AuditAction.Create, "Job", result.Id,
                new { },
                new
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
                }
             );
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
                }),
                JobPosition = exsitingJob.JobPositions.Select(id => new JobPositionResponse
                {
                    Id = id.PositionId,
                    PositionName = id.Position.Name,

                }),
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
            await _auditLogService.CreateAuditLogAsync(accountId, AuditAction.Update, "Job", exsitingJob.Id,
            new { oldData },
            new
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
            });
            return result;
        }

        public async Task<Job> CloseJobPostAsync(int accountId, CreateUpdateJobRequest request)
        {
            var exsitingJob = await _unitOfWork.Recruiters.GetPostedJobByIdAsync(request.Id);
            var oldData = new
            {
                exsitingJob.Status,
            };
            if (exsitingJob == null)
            {
                throw new NotFoundException($"Job with Id {request.Id} not found");
            }


            if (request == null)
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
            exsitingJob.Status = request.Status;
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
    }
}
