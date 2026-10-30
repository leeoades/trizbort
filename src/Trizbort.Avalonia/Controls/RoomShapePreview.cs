using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreColor = System.Drawing.Color;
using CoreColors = Trizbort.Domain.Misc.Colors;
using CoreRect = Trizbort.Domain.Misc.Rect;

using DrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   Small sample room drawn with the dialog's current shape, corner radii, border style and hand-drawn setting.
///   Redraws whenever the bound <see cref="RoomPropertiesViewModel" /> changes.
/// </summary>
public class RoomShapePreview : Control
{
    private RoomPropertiesViewModel? _viewModel;

    public RoomShapePreview()
    {
        Width = 160;
        Height = 90;
        DataContextChanged += (_, _) => Attach(DataContext as RoomPropertiesViewModel);
    }

    private void Attach(RoomPropertiesViewModel? viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        InvalidateVisual();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_viewModel == null)
        {
            return;
        }

        var vm = _viewModel;
        var bounds = new CoreRect(4, 4, (float)Math.Max(1, Bounds.Width - 8), (float)Math.Max(1, Bounds.Height - 8));
        var handDrawn = vm.HandDrawnStyle switch
        {
            HandDrawnStyle.HandDrawn => true,
            HandDrawnStyle.Straight => false,
            _ => Settings.HandDrawn
        };

        var corners = new CornerRadii
        {
            TopLeft = vm.CornerTopLeft,
            TopRight = vm.AllCornersEqual ? vm.CornerTopLeft : vm.CornerTopRight,
            BottomRight = vm.AllCornersEqual ? vm.CornerTopLeft : vm.CornerBottomRight,
            BottomLeft = vm.AllCornersEqual ? vm.CornerTopLeft : vm.CornerBottomLeft
        };

        var geometry = RoomDrawingAdapter.CreateShapeGeometry(bounds, vm.Shape, corners, handDrawn, 1);

        var fill = vm.RoomFillColor != CoreColor.Transparent ? vm.RoomFillColor : RegionColor(vm.SelectedRegion);
        var border = vm.RoomBorderColor != CoreColor.Transparent ? vm.RoomBorderColor : Settings.Color[CoreColors.Border];

        var pen = vm.BorderStyle == BorderDashStyle.None
            ? null
            : border.ToPen(2, vm.BorderStyle.ToDashStyle());
        context.DrawGeometry(fill.ToBrush(), pen, geometry);
    }

    private static CoreColor RegionColor(string? regionName)
    {
        var region = Settings.Regions.FirstOrDefault(r => r.RegionName == regionName);
        return region?.RColor ?? CoreColor.White;
    }
}
