using Imate.API.Business.Interfaces;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Imate.API.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public IActionResult GetAllAccounts()
        {
            var accounts = _accountService.GetAllAccounts();
            var response = accounts.Select(a => new AccountResponse
            {
                Id = a.Id.ToString(),
                Email = a.Email
            });
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetAccountById(int id)
        {
            var account = _accountService.GetAccountById(id);
            if (account == null) return NotFound();

            return Ok(new AccountResponse { Id = account.Id.ToString(), Email = account.Email });
        }
    }
}
