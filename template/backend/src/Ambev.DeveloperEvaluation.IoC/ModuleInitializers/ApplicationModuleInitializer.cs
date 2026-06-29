using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class ApplicationModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // DIP: validators registrados como abstrações
        builder.Services.AddScoped<IValidator<CreateSaleCommand>, CreateSaleCommandValidator>();
        builder.Services.AddScoped<IValidator<UpdateSaleCommand>, UpdateSaleCommandValidator>();

        // OCP/SRP: estratégias e serviços de domínio
        builder.Services.AddSingleton<IDiscountStrategy, QuantityDiscountStrategy>();
        builder.Services.AddSingleton<ISaleNumberGenerator, SaleNumberGenerator>();
    }
}
