using Assesment.Infrastructure.Postgres;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Assesment.Tests;

public class AssessmentWebApplicationFactory : WebApplicationFactory<Program> { }
