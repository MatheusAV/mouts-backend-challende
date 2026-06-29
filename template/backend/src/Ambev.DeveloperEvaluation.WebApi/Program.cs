using Ambev.DeveloperEvaluation.Application;
using Ambev.DeveloperEvaluation.Common.HealthChecks;
using Ambev.DeveloperEvaluation.Common.Logging;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

namespace Ambev.DeveloperEvaluation.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Log.Information("Iniciando aplicação web");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.AddDefaultLogging();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.AddBasicHealthChecks();

            // Swagger com suporte a XML docs e annotations
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "DeveloperStore – Sales API",
                    Version = "v1",
                    Description = "API de gerenciamento de vendas seguindo DDD, CQRS e External Identities pattern.",
                    Contact = new OpenApiContact
                    {
                        Name = "DeveloperStore",
                        Email = "dev@developerstore.com"
                    }
                });

                // Habilitar comentários XML do controller
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath);

                // Suporte a [SwaggerOperation] annotations
                options.EnableAnnotations();

                // Autenticação JWT no Swagger UI
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Informe o token JWT no formato: Bearer {seu_token}"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddDbContext<DefaultContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM")
                )
            );

            builder.Services.AddJwtAuthentication(builder.Configuration);

            builder.RegisterDependencies();

            builder.Services.AddAutoMapper(typeof(Program).Assembly, typeof(ApplicationLayer).Assembly);

            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(
                    typeof(ApplicationLayer).Assembly,
                    typeof(Program).Assembly
                );
            });

            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            var app = builder.Build();

            // Aplica migrations e garante schema correto ao iniciar
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();

                // Bloco 1: tenta aplicar migrations EF (pode falhar se historico inconsistente)
                try
                {
                    db.Database.Migrate();
                    Log.Information("Migrations EF aplicadas com sucesso");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Falha ao aplicar migrations EF — continuando com SQL direto");
                }

                // Bloco 2: garante schema via SQL idempotente (sempre executa, independente das migrations)
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now();
                        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""UpdatedAt"" timestamp with time zone;

                        CREATE TABLE IF NOT EXISTS ""Sales"" (
                            ""Id""           uuid NOT NULL DEFAULT gen_random_uuid(),
                            ""SaleNumber""   character varying(50) NOT NULL,
                            ""SaleDate""     timestamp with time zone NOT NULL,
                            ""CustomerId""   uuid NOT NULL,
                            ""CustomerName"" character varying(200) NOT NULL,
                            ""BranchId""     uuid NOT NULL,
                            ""BranchName""   character varying(200) NOT NULL,
                            ""TotalAmount""  numeric(18,2) NOT NULL,
                            ""IsCancelled""  boolean NOT NULL DEFAULT false,
                            ""CreatedAt""    timestamp with time zone NOT NULL DEFAULT now(),
                            ""UpdatedAt""    timestamp with time zone,
                            CONSTRAINT ""PK_Sales"" PRIMARY KEY (""Id"")
                        );

                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Sales_SaleNumber"" ON ""Sales"" (""SaleNumber"");

                        CREATE TABLE IF NOT EXISTS ""SaleItems"" (
                            ""Id""          uuid NOT NULL DEFAULT gen_random_uuid(),
                            ""SaleId""      uuid NOT NULL,
                            ""ProductId""   uuid NOT NULL,
                            ""ProductName"" character varying(200) NOT NULL,
                            ""Quantity""    integer NOT NULL,
                            ""UnitPrice""   numeric(18,2) NOT NULL,
                            ""Discount""    numeric(5,4) NOT NULL,
                            ""TotalAmount"" numeric(18,2) NOT NULL,
                            ""IsCancelled"" boolean NOT NULL DEFAULT false,
                            CONSTRAINT ""PK_SaleItems"" PRIMARY KEY (""Id""),
                            CONSTRAINT ""FK_SaleItems_Sales_SaleId"" FOREIGN KEY (""SaleId"")
                                REFERENCES ""Sales"" (""Id"") ON DELETE CASCADE
                        );

                        CREATE INDEX IF NOT EXISTS ""IX_SaleItems_SaleId"" ON ""SaleItems"" (""SaleId"");
                    ");
                    Log.Information("Schema de Users/Sales/SaleItems verificado/corrigido");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Falha critica ao garantir schema — API pode nao funcionar corretamente");
                }
            }

            // Middleware global de exceções (antes de qualquer outro middleware)
            app.UseMiddleware<ValidationExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sales API v1");
                    c.RoutePrefix = "swagger";
                    c.DisplayRequestDuration();
                });
            }

            // Sem HTTPS no Docker (desabilitado via ASPNETCORE_HTTPS_PORTS= no override)
            if (!app.Environment.IsEnvironment("Docker"))
                app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseBasicHealthChecks();

            app.MapControllers();

            Log.Information("Aplicação iniciada com sucesso");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Aplicação encerrada inesperadamente");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
