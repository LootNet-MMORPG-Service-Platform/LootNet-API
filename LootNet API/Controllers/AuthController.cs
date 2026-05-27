namespace LootNet_API.Controllers;

using DTO;
using LootNet_API.Extensions;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[Route("api/auth")]
[ApiController]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        try
        {
            await _authService.RegisterAsync(dto);
            return Ok("Registration successful. Check your email to verify account.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] VerifyEmailDTO dto)
    {
        try
        {
            await _authService.VerifyEmailAsync(dto.Token);
            return Ok("Email verified");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendEmailVerificationDTO dto)
    {
        await _authService.ResendEmailVerificationAsync(dto.Email);
        return Ok("If the email exists and is not verified, a new verification email has been sent.");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
    {
        await _authService.RequestPasswordResetAsync(dto.Email);
        return Ok("If the email exists, a password reset link has been sent.");
    }

    [HttpPost("reset-password-email")]
    public async Task<IActionResult> ResetPasswordByEmail([FromBody] ResetPasswordByEmailDTO dto)
    {
        try
        {
            await _authService.ResetPasswordByEmailAsync(dto);
            return Ok("Password changed");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (AuthForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        try
        {
            var result = await _authService.RefreshAsync(refreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (AuthForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        await _authService.LogoutAsync(refreshToken);
        return Ok();
    }

    [HttpPost("reset-password")]
    [Authorize]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
    {
        try
        {
            await _authService.ResetPasswordAsync(User.GetUserId(), dto);
            return Ok("Password changed");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
