using System.ComponentModel.DataAnnotations;

namespace Orchestrun.Core.Hosting.Options;

internal sealed class RabbitMqOptions
{
    public static readonly string SectionName = "BusConfiguration:RabbitMq";
    [Required]
    public string Host { get; set; } = string.Empty;
    [Required]
    public string VirtualHost { get; set; } = string.Empty;
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}