using System.Text.Json.Serialization;
using FootballBoardGame.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers & JSON 設定 (Enumを文字列としてシリアライズ)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI 登録
builder.Services.AddSingleton<IDiceService, DiceService>();
builder.Services.AddSingleton<IOffsideRuleService, OffsideRuleService>();
builder.Services.AddSingleton<IGameEngineService, GameEngineService>();
builder.Services.AddSingleton<ICpuAiService, CpuAiService>();

// CORS設定 (開発時のVite用)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// 静的ファイル配信 (Cloud Run / SPA 本番ホスト用)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// SPA用フォールバック (ルーティング対応)
app.MapFallbackToFile("index.html");

app.Run();
