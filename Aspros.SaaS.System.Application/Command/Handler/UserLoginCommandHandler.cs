﻿using Aspros.Base.Framework.Infrastructure;
using Aspros.SaaS.System.Application.ViewModel;
using Aspros.SaaS.System.Domain.Repository;
using Aspros.SaaS.System.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aspros.SaaS.System.Application.Command.Handler
{
    public class UserLoginCommandHandler(IUserReporistory userReporistory, JwtHandler jwtHandler) : IRequestHandler<UserLoginCommand, ResultModel>
    {
        private readonly IUserReporistory _userReporistory = userReporistory;
        private readonly JwtHandler _jwtHandler = jwtHandler;
        public async Task<ResultModel> Handle(UserLoginCommand cmd, CancellationToken cancellationToken)
        {
            var user = await _userReporistory.QueryUser(cmd.UserName, cmd.UserPassword, cmd.TenantId).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (user == null) return ResultModel.Fail("账号或密码不正确");
            var token = _jwtHandler.GenerateAccessToken(user);
            var refreshToken = _jwtHandler.GenerateRefreshToken();
            var result = new TokenViewModel { AccessToken = token, RefreshToken = refreshToken, UserId = user.Id, UserName = user.UserName, TenantId = user.TenantId };
            return ResultModel.Success(result);
        }
    }
}
