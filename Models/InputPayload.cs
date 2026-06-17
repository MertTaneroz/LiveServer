namespace Testbackend.Models;

public class InputPayload
{
    public Keys Keys { get; set; } = new();
    public Actions Actions { get; set; } = new();
}