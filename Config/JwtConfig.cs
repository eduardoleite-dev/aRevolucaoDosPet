using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ArquiteturaBaseApi.Config;

public static class JwtConfig
{
    public static IServiceCollection AddJwtConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var secretKey = configuration["JwtSettings:SecretKey"] 
                        ?? configuration["JwtSettings__SecretKey"]
                        ?? throw new InvalidOperationException("A chave secreta JWT não foi configurada em JwtSettings:SecretKey ou JwtSettings__SecretKey.");

        var key = Encoding.ASCII.GetBytes(secretKey);

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        return services;
    }
}