using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using dotnet_rpg.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace dotnet_rpg.Data
{
  public class AuthRepository : IAuthRepository
  {
    private readonly DataContext _context;
    private readonly IConfiguration _configuration;

    public AuthRepository(DataContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<ServiceResponse<string>> Login(string userName, string password)
    {
        var serviceResponse = new ServiceResponse<string>();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == userName.ToLower());

        if (user == null)
        {
            serviceResponse.Success = false;
            serviceResponse.Message = "User not found.";
        }
        else if(!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        {
           serviceResponse.Success = false;
           serviceResponse.Message = "Incorrect Password.";
        }
        else
        {
            serviceResponse.Data = CreateToken(user);
        }
        return serviceResponse;
    }

    public async Task<ServiceResponse<int>> Register(User user, string password)
    {
      var serviceResponse = new ServiceResponse<int>();
      var userExist = await _context.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower());
      if(userExist)
      {
          serviceResponse.Success = false;
          serviceResponse.Message = "User already exists.";
          return serviceResponse;
      }
      
      CreatePasswordHash(password, out var passwordSalt, out var passwordHash);

      user.PasswordSalt = passwordSalt;
      user.PasswordHash = passwordHash;
      
      await _context.Users.AddAsync(user);
      await _context.SaveChangesAsync();


      serviceResponse.Data = user.Id;
      return serviceResponse;
    }
    private void CreatePasswordHash(string password, out byte[] passwordSalt, out byte[] passwordHash)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512())
      {
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
      }
    }

    private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
      {
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Enumerable.SequenceEqual(computedHash, passwordHash);
      }
    }

    private string CreateToken(User user)
    {
      var claims = new List<Claim>
      {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
      };

      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
      var tokenDescriptor = new SecurityTokenDescriptor{
        Subject = new ClaimsIdentity(claims),
        Expires = System.DateTime.Now.AddDays(1),
        SigningCredentials = creds
      };

      var tokenHandler = new JwtSecurityTokenHandler();
      var token = tokenHandler.CreateToken(tokenDescriptor);

      return tokenHandler.WriteToken(token);
    }
  }
}