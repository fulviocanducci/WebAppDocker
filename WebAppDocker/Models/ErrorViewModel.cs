namespace WebAppDocker.Models;

public class ErrorViewModel
{
    public string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

public record Source(string Id, string Name)
{
    public static Source Create(string Name) => new(Guid.NewGuid().ToString(), Name);
    public static Source Create(string id, string Name) => new(id, Name);
}
public record Logins(string Name);