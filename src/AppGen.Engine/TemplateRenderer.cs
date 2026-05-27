using Scriban;
using Scriban.Runtime;

namespace AppGen.Engine;

public sealed class TemplateRenderer
{
    public string Render(string templateContent, object model)
    {
        var template = Template.Parse(templateContent);
        if (template.HasErrors)
            throw new InvalidOperationException(string.Join("; ", template.Messages));

        var context = new TemplateContext { MemberRenamer = member => member.Name };
        context.PushGlobal(ScriptObject.From(model));
        return template.Render(context).TrimEnd() + Environment.NewLine;
    }
}
