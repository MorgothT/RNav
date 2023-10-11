using System.Windows.Controls;

namespace Mapper_v1.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);

    Page GetPage(string key);
}
