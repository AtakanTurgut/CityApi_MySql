﻿using CityApi.Core;
using CityApi.Core.Entities;
using CityApi.Data.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CityApi.Data.Concrete;

public class AuthRepository : IAuthRepository
{
	private readonly DataContext _context;
    private readonly IConfiguration _configuration;

    public AuthRepository(DataContext context, IConfiguration configuration)
	{
		_context = context;
        _configuration = configuration;
    }

	public async Task<ServiceResponse<string>> Login(string username, string password)
	{
		var response = new ServiceResponse<string>();
		var user = await _context.Users.FirstOrDefaultAsync(x => x.Username.ToLower().Equals(username.ToLower()));

		if (user == null)
		{
			response.Success = false;
			response.Message = "Kullanıcı Bulunamadı!";
		}
		else if(!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
		{
			response.Success = false;
			response.Message = "Hatalı Şifre!";
		}
		else
		{
			response.Data = CreateToken(user);
		}

		return response;
	}

	public async Task<ServiceResponse<int>> Register(User user, string password)
	{
		ServiceResponse<int> response = new ServiceResponse<int>();

		if (await UserExists(user.Username))
		{
			response.Success = true;
			response.Message = "Kullanıcı Mevcut!";

			return response;
		}

		CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);
		user.PasswordHash = passwordHash;
		user.PasswordSalt = passwordSalt;

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		response.Data = user.Id;

		return response;
	}

	public async Task<bool> UserExists(string username)
	{
		if (await _context.Users.AnyAsync(x => x.Username.ToLower() == username.ToLower()))
		{
			return true;
		}

		return false;
	}

	private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
	{
		using (var hmac = new System.Security.Cryptography.HMACSHA512())
		{
			passwordSalt = hmac.Key;
			passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
		};
	}

	private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
	{
		using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
		{
			var computeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

			return computeHash.SequenceEqual(passwordHash);
		}
	}

	private string CreateToken(User user)
	{
        List<Claim> claims = new List<Claim>()
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.Username)
		};

		SymmetricSecurityKey key = new SymmetricSecurityKey(System.Text.Encoding.UTF8
				.GetBytes(_configuration.GetSection("AppSettings:Token").Value));

		SigningCredentials credentials = new SigningCredentials(key,SecurityAlgorithms.HmacSha512Signature);

		SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor()
		{
			Subject = new ClaimsIdentity(claims),
			Expires = DateTime.Now.AddDays(1),
			SigningCredentials = credentials
		};

		JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
		SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

		return tokenHandler.WriteToken(token);	// Token
	}
}
