using System.ComponentModel.DataAnnotations;

namespace Orchestrun.Core.Hosting.Options;

internal sealed class RabbitMqOptions
{
    internal static readonly string SectionName = "BusConfiguration:RabbitMq";
    [Required]
    internal string Host { get; set; } = string.Empty;
    [Required]
    internal string VirtualHost { get; set; } = string.Empty;
    [Required]
    internal string Username { get; set; } = string.Empty;
    [Required]
    internal string Password { get; set; } = string.Empty;
}