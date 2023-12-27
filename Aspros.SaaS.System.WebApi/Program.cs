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
using Aspros.SaaS.System.Infrastructure.Repostory;
using Microsoft.EntityFrameworkCore;
using Nacos.AspNetCore.V2;
using Newtonsoft.Json;

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
//�¼�����
builder.Services.AddTransient<IEventBus, EventBus>();
builder.Services.AddTransient<IEventHandler<TenentUserAddEvent>, TenentUserAddEventHandler>();
//cqrs cmd query
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantPackageCreateCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantPackageModifyCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantPackageDelCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantPackageListQuery).Assembly));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(TenantCreateCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(UserRoleAddCommand).Assembly));

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

app.MapControllers();

app.Run();
