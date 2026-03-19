window.fatumMapPicker = {
    map: null,
    marker: null,

    init: function (containerId, lat, lon, dotNetRef) {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.marker = null;
        }
        var container = document.getElementById(containerId);
        if (!container) return;
        this.map = L.map(containerId).setView([lat, lon], 13);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap'
        }).addTo(this.map);
        this.marker = L.marker([lat, lon]).addTo(this.map);
        var self = this;
        this.map.on('click', function (e) {
            if (self.marker) self.marker.setLatLng(e.latlng);
            else self.marker = L.marker(e.latlng).addTo(self.map);
            dotNetRef.invokeMethodAsync('OnMapClicked', e.latlng.lat, e.latlng.lng);
        });
    }
};
