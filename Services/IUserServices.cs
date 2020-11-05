using ApiResidencial.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Netflix.Data.ModelView;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace Netflix.Services
{
    public interface IUserService
    {
        Task<UserManagerResponse> RegisterUSerAsync(RegisterViewModel model);
        Task<UserManagerResponse> LoginUserAsync(LoginViewModel model);
        Task<UserManagerResponse> ConfirmEmail(string userId, string token);
        Task<UserManagerResponse> ForgetPasswordAsync(string email);
        Task<UserManagerResponse> ResetPasswordAsync(ResetPasswordViewModel model);
        Task<UserManagerResponse> DeleteUser(string UserId);
        Task<UserManagerResponse> UpdateRoleAdmin(string UserId);
        Task<UserManagerResponse> RemoveRoleAdmin(string UserId);
    }

    public class UserService : IUserService
    {
        private UserManager<IdentityUser> _userManager;
        private IConfiguration _configuration;
        private IMailService _mailService;
        public UserService(UserManager<IdentityUser> userManager,IConfiguration configuration,IMailService mailService, SignInManager<IdentityUser> signInManager) 
        {
            _userManager = userManager;
            _configuration = configuration;
            _mailService = mailService;            
        }
        public async Task<UserManagerResponse> RegisterUSerAsync(RegisterViewModel model)
        {
            if (model == null)
                throw new NullReferenceException("Register Model is Null");

            if(model.Password!= model.ConfirmPassword)
            {
                return new UserManagerResponse
                {
                    Message = "Confirm password doesn't match the password",
                    IsSuccess = false,
                };
            }

            var identityUser = new IdentityUser
            {
                Email = model.Email,
                UserName = model.UserName,
            };

            var result = await _userManager.CreateAsync(identityUser, model.Password);

            if (result.Succeeded)
            {

                var confirmEmailToken = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                var encodedEmailToken = Encoding.UTF8.GetBytes(confirmEmailToken);
                var validemailtoken = WebEncoders.Base64UrlEncode(encodedEmailToken);

                string url = $"{_configuration["appUrl"]}/api/auth/ConfirmEmail?userid={identityUser.Id}&token={validemailtoken}";
                

                await _mailService.SendEmail(identityUser.Email, "Confirm your email", $"<h1>Welcome to Residencial</h1>" +
                    $"<p>Please confirm your email by <a href='{url}'>Clicking here</a></p>");

                return new UserManagerResponse
                {
                    Message = "User created successfully!",
                    IsSuccess = true,                    
                };
            }

            return new UserManagerResponse
            {
                Message="User did not create",
                IsSuccess=false,
                Errors=result.Errors.Select(e=> e.Description)
            };
        }
        public async Task<UserManagerResponse> LoginUserAsync(LoginViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new UserManagerResponse
                {
                    Message="there is no user with that Email address",
                    IsSuccess=false
                };
            }

            var result = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!result)
                return new UserManagerResponse
                {
                    Message = "Invalid password",
                    IsSuccess = false
                };

            if (!user.EmailConfirmed)
                return new UserManagerResponse
                {
                    Message = "Email not Confirmed",
                    IsSuccess = false
                };
            //string tokenAsString = await _userManager.GenerateUserTokenAsync(user, "MyApp", "RefreshToken");

            var claims = new[]
            {
                new Claim("Email",model.Email),
                new Claim(ClaimTypes.Name,user.Id),
            };

            if (model.RememberMe)
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["AuthSettings:Key"]));
                var token = new JwtSecurityToken(
                    issuer: _configuration["AuthSettings:Issuer"],
                    audience: _configuration["AuthSettings:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddYears(3),
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

                string tokenAsString = new JwtSecurityTokenHandler().WriteToken(token);

                return new UserManagerResponse
                {
                    Message = tokenAsString,
                    IsSuccess = true,
                    ExpireDate = token.ValidTo
                };
            }
            else
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["AuthSettings:Key"]));
                var token = new JwtSecurityToken(
                    issuer: _configuration["AuthSettings:Issuer"],
                    audience: _configuration["AuthSettings:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

                string tokenAsString = new JwtSecurityTokenHandler().WriteToken(token);

                return new UserManagerResponse
                {
                    Message = tokenAsString,
                    IsSuccess = true,
                    ExpireDate = token.ValidTo
                };
            }

           

        }
        public async Task<UserManagerResponse> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if(user==null)
                return new UserManagerResponse
                {
                    Message = "User not found",
                    IsSuccess = false,
                };

            var decodertoken = WebEncoders.Base64UrlDecode(token);
            var nomaltoken = Encoding.UTF8.GetString(decodertoken);

            var result = await _userManager.ConfirmEmailAsync(user, nomaltoken);
            if(result.Succeeded)
                return new UserManagerResponse
                {
                    Message = "Email confirmed successfully!",
                    IsSuccess = true,
                };

            return new UserManagerResponse
            {
                Message = "Email did not confirm",
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description)
            };
        }
        public async Task<UserManagerResponse> ForgetPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new UserManagerResponse
                {
                    IsSuccess = false,
                    Message = "No user associated with email",
                };

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Encoding.UTF8.GetBytes(token);
            var validToken = WebEncoders.Base64UrlEncode(encodedToken);

            string url = $"{_configuration["appUrl"]}/ResetPassword?email={email}&token={validToken}";

            await _mailService.SendEmail(email, "Reset Password", "<h1>Follow the instructions to reset your password</h1>" +
                $"<p>To reset your password <a href='{url}'>Click here</a></p>");

            return new UserManagerResponse
            {
                IsSuccess = true,
                Message = "Reset password URL has been sent to the email successfully!"
            };
        }
        public async Task<UserManagerResponse> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return new UserManagerResponse
                {
                    IsSuccess = false,
                    Message = "No user associated with email",
                };

            if (model.NewPassword != model.ConfirmPassword)
                return new UserManagerResponse
                {
                    IsSuccess = false,
                    Message = "Password doesn't match its confirmation",
                };

            var decodedToken = WebEncoders.Base64UrlDecode(model.Token);
            string normalToken = Encoding.UTF8.GetString(decodedToken);

            var result = await _userManager.ResetPasswordAsync(user, normalToken, model.NewPassword);

            if (result.Succeeded)
                return new UserManagerResponse
                {
                    Message = "Password has been reset successfully!",
                    IsSuccess = true,
                };

            return new UserManagerResponse
            {
                Message = "Something went wrong",
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description),
            };
        }        
        public async Task<UserManagerResponse> DeleteUser(string UserID)
        {
            var user = await _userManager.FindByIdAsync(UserID);
            if (user == null)
                return new UserManagerResponse
                {
                    Message = "User not found",
                    IsSuccess = false,
                };

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return new UserManagerResponse
                {
                    Message = "User Deleted successfully!",
                    IsSuccess = true,
                };

            return new UserManagerResponse
            {
                Message = "User did not delete",
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description)
            };

        }
        public async Task<UserManagerResponse> UpdateRoleAdmin(string UserId)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
                return new UserManagerResponse
                {
                    Message = "User not found",
                    IsSuccess = false,
                };
            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (result.Succeeded)
                return new UserManagerResponse
                {
                    Message = "User update role successfully!",
                    IsSuccess = true,
                };

            return new UserManagerResponse
            {
                Message = "User did not update",
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description)
            };
        }
        public async Task<UserManagerResponse> RemoveRoleAdmin(string UserId)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
                return new UserManagerResponse
                {
                    Message = "User not found",
                    IsSuccess = false,
                };
            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");            
            if (result.Succeeded)
                return new UserManagerResponse
                {
                    Message = "User update role successfully!",
                    IsSuccess = true,
                };

            return new UserManagerResponse
            {
                Message = "User did not update",
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description)
            };
        }

    }
}