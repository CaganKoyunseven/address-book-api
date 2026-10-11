using System.Reflection;
using Microsoft.OpenApi;

namespace AddressBook.Api.Extensions;

/// <summary>
/// Keeps Program.cs short by moving one-time service wiring (Swagger, etc.) here.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAddressBookSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Address Book API",
                Version = "v1",
                Description = "A REST API for managing a personal address book: contacts with search, filter, sort, pagination and favorites. " +
                              "Log in via /api/v1/auth/login with the seeded demo credentials, then use the Authorize button with the returned token.",
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Infers the "Bearer" scheme from the JWT bearer authentication handler registered
            // in Program.cs, so the Swagger UI "Authorize" button appears automatically.
            options.InferSecuritySchemes();
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            });
        });

        return services;
    }
}
