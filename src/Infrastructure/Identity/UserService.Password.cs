using Boilerate.Application.Common.Exceptions;
using Boilerate.Application.Common.Mailing;
using Boilerate.Application.Identity.Users.Password;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerate.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, string origin)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            // Don't reveal that the user does not exist or is not confirmed
            return "If your email is registered, you will receive a password reset link shortly.";
        }

        string code = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Link for frontend: {origin}/account/reset-password?Token=...
        const string route = "account/reset-password";
        var endpointUri = new Uri(string.Concat($"{origin}/", route));
        string passwordResetUrl = QueryHelpers.AddQueryString(endpointUri.ToString(), "Token", code);

        var mailRequest = new MailRequest(
            new List<string> { request.Email },
            "Reset Password",
            $"Your Password Reset Token is '{code}'. You can reset your password using the following link: {passwordResetUrl}");

        _jobService.Enqueue(() => _mailService.SendAsync(mailRequest, CancellationToken.None));

        return "Password Reset Mail has been sent to your authorized Email.";
    }

    public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email!);

        _ = user ?? throw new InternalServerException("An Error has occurred!");

        var result = await _userManager.ResetPasswordAsync(user, request.Token!, request.Password!);

        return result.Succeeded
            ? "Password Reset Successful!"
            : throw new InternalServerException("An Error has occurred!");
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest model, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("User Not Found.");

        var result = await _userManager.ChangePasswordAsync(user, model.Password, model.NewPassword);

        if (!result.Succeeded)
        {
            throw new InternalServerException("Change password failed", result.GetErrors());
        }
    }
}
