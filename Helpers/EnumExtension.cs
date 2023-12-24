using System.Windows.Markup;
//#nullable enable

namespace Mapper_v1.Helpers;

public class EnumExtension : MarkupExtension
{
    public EnumExtension() { }
    public EnumExtension(Type enumType) => EnumType = enumType;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public Type? EnumType { get; private set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

    public override object ProvideValue(IServiceProvider serviceProvider) => Enum.GetValues(EnumType);
}
