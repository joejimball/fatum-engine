using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace FatumApp.Maui.Pages;

public partial class MapPickerPage : ContentPage
{
    private double _selectedLatitude;
    private double _selectedLongitude;
    private readonly Action<double, double> _onConfirm;
    private readonly Action _onCancel;
    private Pin? _pin;

    public MapPickerPage(double initialLatitude, double initialLongitude, double radiusKm,
        Action<double, double> onConfirm, Action onCancel)
    {
        InitializeComponent();
        _selectedLatitude = initialLatitude;
        _selectedLongitude = initialLongitude;
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        var position = new Location(initialLatitude, initialLongitude);
        map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromKilometers(Math.Max(0.5, radiusKm * 1.5))));
        AddPinAt(position);
        UpdateCoordLabel();
    }

    private void AddPinAt(Location location)
    {
        map.Pins.Clear();
        _pin = new Pin
        {
            Label = "Ubicación",
            Location = location,
            Type = PinType.Place
        };
        map.Pins.Add(_pin);
    }

    private void UpdateCoordLabel()
    {
        coordLabel.Text = $"Lat: {_selectedLatitude:F5}  Lon: {_selectedLongitude:F5}";
    }

    private void OnMapClicked(object? sender, MapClickedEventArgs e)
    {
        _selectedLatitude = e.Location.Latitude;
        _selectedLongitude = e.Location.Longitude;
        AddPinAt(e.Location);
        UpdateCoordLabel();
    }

    private void OnConfirmClicked(object? sender, EventArgs e)
    {
        _onConfirm(_selectedLatitude, _selectedLongitude);
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        _onCancel();
    }
}
