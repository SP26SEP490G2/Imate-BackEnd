using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.Infrastructure.Configurations;
using Imate.API.Models.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Imate.API.Business.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        // Inject JwtSettings qua IOptions
        public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public string GenerateToken(int accountId, IEnumerable<RoleName> roles)
        {
            // 1. Định nghĩa Security Key (từ Secret)
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 2. Định nghĩa Claims (Thông tin người dùng/ủy quyền)
            var claims = new List<Claim>
            {
                // Claim quan trọng nhất: ID của Account trong DB (PostgreSQL PK)
                new Claim(ClaimTypes.NameIdentifier, accountId.ToString()),
                // Thường dùng cho Subject
                new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()), 
                // Email (tùy chọn)
                // new Claim(JwtRegisteredClaimNames.Email, email), 
            };

            // 3. Thêm Claims cho Role
            foreach (var role in roles)
            {
                // Thêm vai trò dưới dạng string vào Claims
                claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }

            // 4. Tạo Token
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: credentials);

            // 5. Viết Token ra chuỗi
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            // Tạo một random string dài và an toàn cho refresh token
            var randomNumber = new byte[64];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }
    }
}
