using CommunityToolkit.Mvvm.ComponentModel;

namespace Mapper_v1.ViewModels
{
    public partial class ProjectionDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string projectionWkt;
        public ProjectionDialogViewModel()
        {
            ProjectionWkt = string.Empty; //@"to add projection, please paste WKT (Well Known Text format) here.";
        }
    }
}