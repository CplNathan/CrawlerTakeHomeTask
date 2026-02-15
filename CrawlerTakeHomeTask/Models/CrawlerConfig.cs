namespace CrawlerTakeHomeTask.Models;

using System;
using System.ComponentModel.DataAnnotations;

public class CrawlerConfig
{
    public required Uri BaseUrl { get; set; }

    [Required]
    public required int MaxDepth { get; init; }

    [Required]

    public required int MaxParallelism { get; init; }

    [Required]

    public required TimeSpan RequestTimeout { get; init; }

    [Required]
    public required string UserAgent { get; init; }

    public required bool FollowRedirects { get; init; } = false;
}
