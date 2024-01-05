using Aspros.Base.Framework.Infrastructure;
using Aspros.Base.Framework.Infrastructure.Event;
using Aspros.Base.Framework.Infrastructure.Interface;
using Aspros.Base.Framework.Infrastructure.Ioc;
using Aspros.Project.User.Infrastructure.Repository;
using Aspros.SaaS.System.Application.Command;
using Aspros.SaaS.System.Application.Query;
using Aspros.SaaS.System.Domain.DomainEvent;
using Aspros.SaaS.System.Domain.DomainEvent.EventHandler;
using Aspros.SaaS.System.Domain.Repository;
using Aspros.SaaS.System.Infrastructure;
using Aspros.SaaS.System.Infrastructure.Repostory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nacos.AspNetCore.V2;
using Newtonsoft.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services
    .AddMvc()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
    });
//�ֶ�����url ������apisixת��
builder.WebHost.UseUrls($"http://*:5033");
//��ȡnacos�����ļ�
builder.Host.UseNacosConfig("Nacos");

builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Description = "���¿�����������ͷ����Ҫ���Jwt��ȨToken��Bearer Token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

});

//ע��jwt������
builder.Services.AddSingleton<JwtHandler>();
//Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true, //�Ƿ���֤Issuer
            ValidIssuer = builder.Configuration.GetSection("jwt")["Issuer"], //������Issuer
            ValidateAudience = true, //�Ƿ���֤Audience
            ValidAudience = builder.Configuration.GetSection("jwt")["Audience"], //������Audience
            ValidateIssuerSigningKey = true, //�Ƿ���֤SecurityKey
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("jwt")["SecretKey"])), //SecurityKey
            ValidateLifetime = true, //�Ƿ���֤ʧЧʱ��
            ClockSkew = TimeSpan.FromSeconds(30), //����ʱ���ݴ�ֵ�������������ʱ�䲻ͬ�����⣨�룩
            RequireExpirationTime = true,
        };
    });

//����db����
builder.Services.AddDbContext<SystemDbContext>(op =>
        op.UseMySql(builder.Configuration.GetSection("data")["ConnectionString"], new MySqlServerVersion(new Version(8, 2, 0))));
//CAP
builder.Services.AddCap(x =>
{
    x.UseMySql(builder.Configuration.GetSection("data")["ConnectionString"]);
    x.UseRedis(builder.Configuration.GetSection("data")["RedisServer"]);
    x.UseRabbitMQ(builder.Configuration.GetSection("data")["RabbitMqServer"]);
});
//����redis����
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.InstanceName = "";
    options.Configuration = builder.Configuration.GetSection("data")["RedisServer"];
});
//ע�����nacos
builder.Services.AddNacosAspNet(builder.Configuration, "Nacos");
//������Ԫ��
builder.Services.AddTransient<IUnitOfWork, Aspros.SaaS.System.Infrastructure.UnitOfWork>();
//��ȡtoken�е�ǰ������,�⻧����Ϣ
builder.Services.AddTransient<IWorkContext, WorkContext>();
//dbContext
builder.Services.AddTransient<IDbContext, SystemDbContext>();
//http context ������
builder.Services.AddHttpContextAccessor();
//�ִ�
builder.Services.AddTransient<ITenantPackageRepository, TenantPackageRepository>();
builder.Services.AddTransient<IUserReporistory, UserReporistory>();
builder.Services.AddTransient<IRoleReporistory, RoleReporistory>();
builder.Services.AddTransient<IMenuReporistory, MenuReporistory>();
//�¼�����
builder.Services.AddTransient<IEventBus, EventBus>();
builder.Services.AddTransient<IEventHandler<TenentUserAddEvent>, TenentUserAddEventHandler>();
//cqrs cmd query
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantPackageCreateCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantPackageModifyCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantPackageDelCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantPackageListQuery).Assembly));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantCreateCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(UserRoleConferCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(UserLoginCommand).Assembly));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//��д����
//app.UseRewriteQueryString();

//��ȡ����IOC
ServiceLocator.Instance = app.Services;

app.UseHttpsRedirection();

app.UseAuthorization();

//Ȩ��У��
//app.UsePermissionValid();

app.MapControllers();

app.Run();
