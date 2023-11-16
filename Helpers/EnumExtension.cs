using System.Windows.Markup;
//#nullable enable

namespace Mapper_v1.Helpers;

public class EnumExtension : MarkupExtension
{
    public EnumExtension() {}
    public EnumExtension(Type enumType) => EnumType = enumType;
    
    public Type? EnumType { get; private set; }

    public override object ProvideValue(IServiceProvider serviceProvider) => Enum.GetValues(EnumType);
}
