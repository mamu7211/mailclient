using CronExpressionDescriptor;
using Quartz;

namespace Feirb.Web.Services;

internal static class CronValidator
{
    internal static (bool IsValid, string? Description) Validate(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return (false, null);

        if (!CronExpression.IsValidExpression(expression))
            return (false, null);

        try
        {
            var description = ExpressionDescriptor.GetDescription(expression, new Options
            {
                Use24HourTimeFormat = true,
                ThrowExceptionOnParseError = true,
            });
            return (true, description);
        }
        catch
        {
            return (true, null);
        }
    }
}
