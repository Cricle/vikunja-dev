using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Interfaces;

public interface ITemplateEngine
{
    string Render(string template, TemplateContext context);
    IReadOnlyList<string> GetAvailablePlaceholders(string eventType);
}
