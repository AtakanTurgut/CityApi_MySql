﻿using CityApi.Core;
using CityApi.Core.Entities;

namespace CityApi.Data.Abstract
{
	public interface IAuthRepository
	{
		Task<ServiceResponse<int>> Register(User user, string password);
		Task<ServiceResponse<string>> Login(string username, string password);
		Task<bool> UserExists(string username);
	}
}
